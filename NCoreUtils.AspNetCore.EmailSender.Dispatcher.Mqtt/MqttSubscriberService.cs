using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class MqttSubscriberService
        : IHostedService
        , IMqttClientConnectedHandler
        , IMqttClientDisconnectedHandler
        , IMqttApplicationMessageReceivedHandler
    {
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);

        private readonly EmailProcessor _processor;

        private readonly ILogger<MqttSubscriberService> _logger;

        private readonly IMqttClientOptions _clientOptions;

        private readonly IMqttClientServiceOptions _serviceOptions;

        private IMqttClient? _client;

        private volatile bool _connected;

        public MqttSubscriberService(
            EmailProcessor processor,
            ILogger<MqttSubscriberService> logger,
            IMqttClientServiceOptions serviceOptions,
            IMqttClientOptions clientOptions)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Received payload: {{ {0} }}.", string.Join(", ", eventArgs.ApplicationMessage.Payload.Select(b => "0x" + b.ToString("X2"))));
                }
                var entry = JsonSerializer.Deserialize<EmailMessageTask>(eventArgs.ApplicationMessage.Payload, _serviceOptions.JsonSerializerOptions);
                var status = await _processor.ProcessAsync(entry, "<none>", CancellationToken.None);
                eventArgs.ProcessingFailed = status >= 400;
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, "Failed to process entry.");
                eventArgs.ProcessingFailed = true;
            }
        }

        private async Task DoConnectAsync(IMqttClient client, CancellationToken cancellationToken)
        {
            await client.ConnectAsync(_clientOptions, cancellationToken);
        }

        public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _logger.LogDebug(
                "MQTT client created and connected successfully (result code = {0}, response = {1}).",
                eventArgs.AuthenticateResult.ResultCode,
                eventArgs.AuthenticateResult.ResponseInformation
            );
            await _client!.SubscribeAsync(new MqttClientSubscribeOptions
            {
                TopicFilters = {
                    new MqttTopicFilterBuilder()
                        .WithAtLeastOnceQoS()
                        .WithTopic(_serviceOptions.Topic)
                        .Build()
                }
            });
            _logger.LogDebug(
                "MQTT client successfully subscribed to topic {0}.",
                _serviceOptions.Topic
            );
            _connected = true;
        }

        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _connected = false;
            Interlocked.MemoryBarrierProcessWide();
            if (!(_client is null) && eventArgs.ReasonCode != MqttClientDisconnectReason.AdministrativeAction && !_connected)
            {
                _logger.LogWarning(eventArgs.Exception, "MQTT client has disconnected, reason: {0}, trying to reconnect.", eventArgs.ReasonCode);
                await DoConnectAsync(_client, CancellationToken.None);
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
                    client.ConnectedHandler = this;
                    client.DisconnectedHandler = this;
                    client.ApplicationMessageReceivedHandler = this;
                    _client = client;
                    await DoConnectAsync(client, cancellationToken);
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
                        ReasonCode = MqttClientDisconnectReason.AdministrativeAction,
                        ReasonString = "shutdown"
                    });
                    _client = default;
                    _logger.LogDebug("MQTT stopped successfully.");
                }
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}