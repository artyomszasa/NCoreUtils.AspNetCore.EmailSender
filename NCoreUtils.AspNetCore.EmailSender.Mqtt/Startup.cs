using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Client;
#if !DEBUG
using NCoreUtils.Logging;
#endif

namespace NCoreUtils.AspNetCore.EmailSender
{
    public class Startup
    {
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
            var mqttConfig = _configuration.GetSection("Mqtt:Client").GetMqttClientConfiguration() ?? MqttClientConfiguration.Default;
            var mqttClientOptions =
                new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttConfig.Host ?? throw new InvalidOperationException("No MQTT host supplied."), mqttConfig.Port)
                    .WithCleanSession(mqttConfig.CleanSession ?? true)
                    .Build();

            services
                // HTTP Context access
                .AddHttpContextAccessor()
                // MQTT client
                .AddSingleton<IMqttClientServiceOptions>(_configuration.GetSection("Mqtt").GetMqttClientServiceOptions())
                .AddSingleton(mqttClientOptions)
                .AddSingleton<IMqttClientService, MqttClientService>()
                .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<IMqttClientService>())
                .AddSingleton<IEmailSender, MqttEmailScheduler>()
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

        public void Configure(IApplicationBuilder app)
        {
#if DEBUG
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
#endif

            app
                .UseForwardedHeaders(_configuration.GetSection("ForwardedHeaders"))
#if !DEBUG
                .UsePrePopulateLoggingContext()
#endif
                .UseCors()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapMqttEmailScheduler();
                });
        }
    }
}
