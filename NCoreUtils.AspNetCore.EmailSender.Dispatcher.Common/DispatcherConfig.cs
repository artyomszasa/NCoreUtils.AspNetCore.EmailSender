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

        private static SmtpCredentials? GetSmtpCredentials(Uri uri)
        {
            if (string.IsNullOrEmpty(uri.UserInfo))
            {
                return default;
            }
            var i = uri.UserInfo.IndexOf(':');
            if (-1 == i)
            {
                return new SmtpCredentials(Uri.UnescapeDataString(uri.UserInfo), string.Empty);
            }
            return new SmtpCredentials(Uri.UnescapeDataString(uri.UserInfo.Substring(0, i)), Uri.UnescapeDataString(uri.UserInfo[(i + 1)..]));
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
                    options.HttpErrorAsException = true;
                    var logger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SendGridDispatcher>();
                    return new SendGridDispatcher(options, logger, _serviceProvider.GetService<IHttpClientFactory>());
                }
                if (uri.Scheme == "smtp" || uri.Scheme == "smtps")
                {
                    var configuration = new SmtpConfiguration(
                        host: uri.Host,
                        port: 0 > uri.Port ? 25 : uri.Port,
                        useSsl: uri.Scheme == "smtps",
                        user: GetSmtpCredentials(uri)
                    );
                    return new SmtpDispatcher(configuration);
                }
                throw new InvalidOperationException($"Unsupported dispatcher URI: {uri.AbsoluteUri}.");
            }
            throw new InvalidOperationException($"No configuration for owner = {owner}.");
        }
    }
}