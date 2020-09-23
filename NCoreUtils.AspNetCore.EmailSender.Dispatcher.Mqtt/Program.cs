using System;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client.Options;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    class Program
    {
        private sealed class ConfigureJson : IConfigureOptions<JsonSerializerOptions>
        {
            public void Configure(JsonSerializerOptions options)
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailAddress>());
                options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailAttachment>());
                options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailContent>());
                options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailMessage>());
                options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailMessageTask>());
            }
        }

        private static IConfiguration CreateConfiguration()
            => new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("secrets/appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = CreateConfiguration();
            // MQTT client options
            var mqttConfig = configuration.GetSection("Mqtt:Client").Get<MqttClientConfiguration>() ?? new MqttClientConfiguration();
            var mqttClientOptions =
                new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttConfig.Host ?? throw new InvalidOperationException("No MQTT host supplied."), mqttConfig.Port)
                    .WithCleanSession(mqttConfig.CleanSession ?? true)
                    .WithClientId(mqttConfig.ClientId ?? "ncoreutils-email-client")
                    .Build();
            return new HostBuilder()
                .UseConsoleLifetime()
                .UseContentRoot(Environment.CurrentDirectory)
                .UseEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development")
                .ConfigureLogging(b => b
                    .ClearProviders()
                    .AddConfiguration(configuration.GetSection("Logging"))
                    .AddConsole()
                )
                .ConfigureServices((context, services) =>
                {
                    services
                        // HTTP client
                        .AddHttpClient()
                        // JSON serialization
                        .AddOptions<JsonSerializerOptions>().Services
                        .ConfigureOptions<ConfigureJson>()
                        .AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().CurrentValue)
                        // MQTT client
                        .AddSingleton<IMqttClientServiceOptions>(serviceProvider =>
                        {
                            var options = ActivatorUtilities.CreateInstance<MqttClientServiceOptions>(serviceProvider);
                            configuration.GetSection("Mqtt").Bind(options);
                            return options;
                        })
                        .AddSingleton(mqttClientOptions)
                        .AddHostedService<MqttSubscriberService>()
                        .AddSingleton<EmailProcessor>();
                });
        }
    }
}
