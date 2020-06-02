using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SendGrid;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class DispatcherConfig
    {
        public static DispatcherConfig FromConfiguration(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var entries = configuration.Get<List<DispatcherConfigEntry>>();
            var config = new Dictionary<string, Uri>();
            foreach (var entry in entries)
            {
                config[entry.Owner] = new Uri(entry.Uri, UriKind.Absolute);
            }
            return new DispatcherConfig(serviceProvider, config);
        }

        private readonly IServiceProvider _serviceProvider;

        private readonly IReadOnlyDictionary<string, Uri> _configuration;

        public DispatcherConfig(IServiceProvider serviceProvider, IReadOnlyDictionary<string, Uri> configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public IEmailSender CreateSender(string owner)
        {
            if (_configuration.TryGetValue(owner, out var uri))
            {
                if (uri.Scheme == "sendgrid")
                {
                    var options = new SendGridClientOptions
                    {
                        ApiKey = uri.UserInfo
                    };
                    var hostBuilder = new UriBuilder
                    {
                        Scheme = "https",
                        Host = uri.Host,
                        Port = uri.Port,
                        Path = uri.AbsolutePath
                    };
                    options.Host = hostBuilder.Uri.AbsoluteUri;
                    var logger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SendGridDispatcher>();
                    return new SendGridDispatcher(options, logger, _serviceProvider.GetService<IHttpClientFactory>());
                }
                throw new InvalidOperationException($"Unsupported dispatcher URI: {uri.AbsoluteUri}.");
            }
            throw new InvalidOperationException($"No configuration for owner = {owner}.");
        }
    }
}