using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Google.Cloud.PubSub;
using NCoreUtils.Internal;
using NCoreUtils.Logging;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public class Program
{
    private static async Task Main(string[] args)
    {
        var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("secrets/appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("EMAIL_")
            .Build();

        var serviceCollection = new ServiceCollection();
        var googleCredentials = await Google.ServiceAccountCredentialData.ReadDefaultAsync();
        // CONFIGURE ***************************************************************************************************
        using var services = new ServiceCollection()
            .AddLogging(b => b
                .ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddGoogleFluentd(projectId: configuration["Google:ProjectId"])
            )
            // HTTP CLIENT
            .ConfigureHttpClientDefaults(b =>
            {
                b.ConfigurePrimaryHttpMessageHandler((handler, _) =>
                {
                    if (handler is SocketsHttpHandler socketsHandler)
                    {
                        socketsHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                        socketsHandler.SslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
                    }
                });
            })
            // GOOGLE
            .AddGoogleCloudPubSubClient(googleCredentials)
            // .AddGoogleCloudMonitoringClient(googleCredentials)
            // Processor implementation
            .AddSingleton<EmailProcessor>()
            .AddSingleton(serviceProvider => DispatcherConfig.FromConfiguration(serviceProvider, configuration.GetSection("Dispatchers")))
            .BuildServiceProvider(validateScopes: true);

        var processor = services.GetRequiredService<EmailProcessor>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var projectId = configuration.GetRequiredValue("Google:ProjectId");
        var subscriptionId = configuration.GetRequiredValue("Google:SubscriptionId");
        var pubSubClient = services.GetRequiredService<IPubSubV1Api>();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellation.Token,
            MaxDegreeOfParallelism = 24
        };

        logger.LogSubscriberClientMessageProcessStarted();

        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                var messages = await pubSubClient.PullAsync(projectId, subscriptionId, 12, cancellation.Token);
                if (messages.ReceivedMessages is { Count: > 0 } receivedMessages)
                {
                    logger.LogSubscriberClientReceivedMessages(receivedMessages.Count);
                    await Parallel.ForEachAsync(receivedMessages, parallelOptions, async (receivedMessage, cancellationToken) =>
                    {
                        var message = receivedMessage.Message;
                        if (message is not null && message.Data is not null)
                        {
                            var messageId = message.MessageId;
                            logger.LogSubscriberClientProcessMessage(messageId);
                            try
                            {
                                EmailMessageTask entry;
                                try
                                {
                                    entry = JsonSerializer.Deserialize(Convert.FromBase64String(message.Data), EmailMessageTaskSerializerContext.Default.EmailMessageTask)
                                        ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request entry.");
                                }
                                catch (Exception exn) when (exn is not OperationCanceledException)
                                {
                                    // Message should not be retried...
                                    logger.LogSubscriberClientDeserializeFailed(exn, messageId);
                                    if (receivedMessage.AckId is string ackId)
                                    {
                                        await pubSubClient
                                            .AcknowledgeAsync(projectId, subscriptionId, [ackId], CancellationToken.None)
                                            .ConfigureAwait(false);
                                    }
                                    return;
                                }
                                var status = await processor.ProcessAsync(entry, messageId ?? "<null>", cancellationToken).ConfigureAwait(false);
                                if (status < 400)
                                {
                                    // logger.LogSendGridMessageScheduled(messageId, sendGridMessageId);
                                    logger.LogSubscriberClientReplyMessage(messageId, true);
                                    if (receivedMessage.AckId is string ackId)
                                    {
                                        await pubSubClient
                                            .AcknowledgeAsync(projectId, subscriptionId, [ackId], CancellationToken.None)
                                            .ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    logger.LogSubscriberClientNoReplyMessageRetry(messageId);
                                }
                            }
                            catch (Exception exn) when (exn is not OperationCanceledException)
                            {
                                logger.LogSubscriberClientReplyMessageFailed(exn, messageId);
                            }
                        }
                    });
                }
            }
            catch (OperationCanceledException) { /* noop */ }
            catch (Exception exn)
            {
                logger.LogSubscriberClientPullFailed(exn);
            }
        }
    }
}