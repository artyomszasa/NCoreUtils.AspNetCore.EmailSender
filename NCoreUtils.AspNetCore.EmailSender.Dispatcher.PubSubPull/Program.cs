using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Internal;
using NCoreUtils.Logging;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class Program
    {
        // NOTE: Required to use GOOGLE_APPLICATION_CREDENTIALS
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Google.Apis.Auth.OAuth2.JsonCredentialParameters))]
#pragma warning disable IDE0060
        private static async Task Main(string[] args)
#pragma warning restore IDE0060
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

            using var services = new ServiceCollection()
                .AddLogging(b => b
                    .ClearProviders()
                    .AddConfiguration(configuration.GetSection("Logging"))
                    .AddGoogleFluentd(projectId: configuration["Google:ProjectId"])
                )
                .AddHttpClient()
                .AddHttpClient()
                .AddSingleton<EmailProcessor>()
                .AddSingleton(serviceProvider => DispatcherConfig.FromConfiguration(serviceProvider, configuration.GetSection("Dispatchers")))
                .BuildServiceProvider(true);

            var subscriptionName = configuration.GetSubscriptionConfiguration();
            var subscriber = await new SubscriberClientBuilder
            {
                SubscriptionName = subscriptionName,
                Logger = services.GetRequiredService<ILogger<SubscriberClient>>(),
                Settings = new SubscriberClient.Settings
                {
                    AckDeadline = TimeSpan.FromMinutes(5)
                },
                GoogleCredential = await Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefaultAsync()
            }.BuildAsync(cancellation.Token).ConfigureAwait(false);
            using var __ = cancellation.Token.Register(() =>
            {
                _ = subscriber.StopAsync(TimeSpan.FromSeconds(5));
            });
            var processor = services.GetRequiredService<EmailProcessor>();
            processor.Logger.LogDebug("Start processing messages.");
            await subscriber.StartAsync(async (message, cancellationToken) =>
            {
                var messageId = message.MessageId;
                processor.Logger.LogDebug("Processing message {MessageId}.", messageId);
                try
                {
                    EmailMessageTask entry;
                    try
                    {
                        entry = JsonSerializer.Deserialize(message.Data.ToByteArray(), EmailMessageTaskSerializerContext.Default.EmailMessageTask)
                            ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request entry.");
                    }
                    catch (Exception exn)
                    {
                        processor.Logger.LogError(exn, "Failed to deserialize pub/sub message {MessageId}.", messageId);
                        return SubscriberClient.Reply.Ack; // Message should not be retried...
                    }
                    var status = await processor.ProcessAsync(entry, messageId, cancellationToken).ConfigureAwait(false);
                    var ack = status < 400 ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack;
                    processor.Logger.LogDebug("Processed message {MessageId} => {Ack}.", messageId, ack);
                    return ack;
                }
                catch (Exception exn)
                {
                    processor.Logger.LogError(exn, "Failed to process pub/sub message {MessageId}.", messageId);
                    return SubscriberClient.Reply.Nack;
                }
            }).ConfigureAwait(false);
            processor.Logger.LogDebug("Processing messages stopped succefully.");
        }
    }
}