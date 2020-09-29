using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet.Client.Options;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public class Startup
    {
        private sealed class ConfigureJson : IConfigureOptions<JsonSerializerOptions>
        {
            public void Configure(JsonSerializerOptions options)
            {
                EmailSenderJsonOptions.ApplyDefaults(options);
                options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailMessageTask>());
            }
        }

        private static ForwardedHeadersOptions ConfigureForwardedHeaders()
        {
            var opts = new ForwardedHeadersOptions();
            opts.KnownNetworks.Clear();
            opts.KnownProxies.Clear();
            opts.ForwardedHeaders = ForwardedHeaders.All;
            return opts;
        }

        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // MQTT client options
            var mqttConfig = _configuration.GetSection("Mqtt:Client").Get<MqttClientConfiguration>() ?? new MqttClientConfiguration();
            var mqttHost = mqttConfig.Host ?? throw new InvalidOperationException("No MQTT host supplied.");
            var addresses = Dns.GetHostAddresses(mqttHost);
            foreach (var address in addresses)
            {
                Console.WriteLine($"{mqttHost} => {address}");
            }
            if (addresses.Length == 0)
            {
                Console.Error.WriteLine($"Unable to resolve MQTT host address [{mqttHost}]");
                Environment.Exit(-1);
            }
            var mqttClientOptions =
                new MqttClientOptionsBuilder()
                    .WithTcpServer(addresses[0].ToString(), mqttConfig.Port)
                    .WithCleanSession(mqttConfig.CleanSession ?? true)
                    .Build();

            services
                // HTTP Context access
                .AddHttpContextAccessor()
                // MQTT client
                .AddSingleton<IMqttClientServiceOptions>(serviceProvider =>
                {
                    var options = ActivatorUtilities.CreateInstance<MqttClientServiceOptions>(serviceProvider);
                    _configuration.GetSection("Mqtt").Bind(options);
                    return options;
                })
                .AddSingleton(mqttClientOptions)
                .AddSingleton<IMqttClientService, MqttClientService>()
                .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<IMqttClientService>())
                .AddSingleton<IEmailSender, MqttEmailScheduler>()
                // JSON serialization
                .AddOptions<JsonSerializerOptions>().Services
                .ConfigureOptions<ConfigureJson>()
                // CORS
                .AddCors(b => b.AddDefaultPolicy(opts => opts
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    // must be at least 2 domains for CORS middleware to send Vary: Origin
                    .WithOrigins("https://example.com", "http://127.0.0.1")
                    .SetIsOriginAllowed(_ => true)
                ))
                // Autentication
                .AddRemoteOAuth2Authentication(_configuration.GetSection("Authentication"), TokenHandlers.Bearer)
                .AddAuthorization()
                .AddRouting();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            #if DEBUG
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            #endif

            app
                .UseForwardedHeaders(ConfigureForwardedHeaders())
                #if !DEBUG
                .UsePrePopulateLoggingContext()
                #endif
                .UseCors()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapProto<IEmailSender>(b => b.ApplyEmailSenderDefaults(null));
                });
        }
    }
}
