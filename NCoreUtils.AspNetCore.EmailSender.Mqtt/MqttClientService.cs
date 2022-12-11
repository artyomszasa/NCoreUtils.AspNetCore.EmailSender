using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public class MqttClientService : IMqttClientService
    {
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);

        private readonly ILogger<MqttClientService> _logger;

        private readonly MqttClientOptions _clientOptions;

        private readonly IMqttClientServiceOptions _serviceOptions;

        private IMqttClient? _client;

        private volatile bool _connected;

        public MqttClientService(
            ILogger<MqttClientService> logger,
            IMqttClientServiceOptions serviceOptions,
            MqttClientOptions clientOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _logger.LogDebug(
                "MQTT client created and connected successfully (result code = {ResultCode}, response = {ResponseInformation}).",
                eventArgs.ConnectResult.ResultCode,
                eventArgs.ConnectResult.ResponseInformation
            );
            _connected = true;
            return Task.CompletedTask;
        }

        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _connected = false;
            Interlocked.MemoryBarrierProcessWide();
            if (_client is not null && eventArgs.Reason != MqttClientDisconnectReason.AdministrativeAction && !_connected)
            {
                _logger.LogWarning(eventArgs.Exception, "MQTT client has disconnected, reason: {Reason}, trying to reconnect.", eventArgs.Reason);
                await _client.ConnectAsync(_clientOptions, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _sync.WaitAsync(cancellationToken);
            try
            {
                if (_client is null)
                {
                    var client = new MqttFactory().CreateMqttClient();
                    client.ConnectedAsync += HandleConnectedAsync;
                    client.DisconnectedAsync += HandleDisconnectedAsync;
                    await client.ConnectAsync(_clientOptions, cancellationToken).ConfigureAwait(false);
                    _client = client;
                    _logger.LogDebug("MQTT service started successfully.");
                }
                else
                {
                    _logger.LogWarning("MQTT service is already running.");
                }
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _sync.WaitAsync(cancellationToken);
            try
            {
                if (_client is null)
                {
                    _logger.LogWarning("MQTT service is not running.");
                }
                else
                {
                    await _client.DisconnectAsync(new MqttClientDisconnectOptions
                    {
                        Reason = MqttClientDisconnectReason.AdministrativeAction,
                        ReasonString = "shutdown"
                    }, cancellationToken).ConfigureAwait(false);
                    _client = default;
                    _logger.LogDebug("MQTT stopped successfully.");
                }
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<int?> PublishAsync<T>(T payload, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
        {
            if (_client is null)
            {
                throw new InvalidOperationException("MQTT service has not been started properly.");
            }
            if (!_connected)
            {
                throw new InvalidOperationException("MQTT client is not connected.");
            }
            var buffer = ArrayPool<byte>.Shared.Rent(_serviceOptions.BufferSize);
            try
            {
                int size;
                using (var stream = new MemoryStream(buffer, 0, buffer.Length, true, true))
                {
                    await JsonSerializer
                        .SerializeAsync(stream, payload, typeInfo, cancellationToken)
                        .ConfigureAwait(false);
                    stream.Flush();
                    size = unchecked((int)stream.Position);
                }
                using var payLoadStream = new MemoryStream(buffer, 0, size, false, true);
                var message = new MqttApplicationMessageBuilder()
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithTopic(_serviceOptions.Topic)
                    .WithPayload(payLoadStream)
                    .Build();
                var res = await _client.PublishAsync(message, cancellationToken).ConfigureAwait(false);
                if (res.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new InvalidOperationException($"Message publishing failed with reason: {res.ReasonCode} {res.ReasonString}");
                }
                return res.PacketIdentifier;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}