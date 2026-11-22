using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore.EmailSender.Dispatcher.Stalwart;
using NCoreUtils.OAuth2.Internal;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public class DispatcherConfig
{
    private static string GetRequiredValue(IConfiguration configuration, string path)
    {
        var value = configuration[path];
        if (value is null)
        {
            var fullPath = configuration is IConfigurationSection section
                ? $"{section.Path}:{path}"
                : path;
            throw new InvalidOperationException($"No configuration value found at \"{fullPath}\".");
        }
        return value;
    }

    private static IReadOnlyDictionary<string, Uri> GetConfigEntries(IConfiguration configuration)
    {
        var map = new Dictionary<string, Uri>();
        foreach (var section in configuration.GetChildren())
        {
            map[GetRequiredValue(section, "Owner")] = new Uri(GetRequiredValue(section, "Uri"), UriKind.Absolute);
        }
        return map;
    }

    public static DispatcherConfig FromConfiguration(IServiceProvider serviceProvider, IConfiguration configuration)
        => new(serviceProvider, GetConfigEntries(configuration));

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
        return new SmtpCredentials(Uri.UnescapeDataString(uri.UserInfo.AsSpan(0, i)), Uri.UnescapeDataString(uri.UserInfo.AsSpan(i + 1)));
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
                var hostBuilder = new UriBuilder
                {
                    Scheme = "https",
                    Host = uri.Host,
                    Port = uri.Port,
                    Path = uri.AbsolutePath is null or "" or "/"
                        ? "/v3/mail/send"
                        : uri.AbsolutePath
                };
                var configuration = new SendGridConfiguration(
                    uri.UserInfo,
                    hostBuilder.Uri.AbsoluteUri
                );
                var logger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SendGridDispatcher>();
                return new SendGridDispatcher(configuration, logger, _serviceProvider.GetService<IHttpClientFactory>());
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
            if (uri.Scheme == "stalwart")
            {
                var cred = GetSmtpCredentials(uri) ?? throw new InvalidOperationException("AccountId is required");
                var configuration = new StalwartConfiguration(
                    domain: uri.Host,
                    accountId: cred.Username
                );
                var identity = new StalwartIdentityConfiguration(
                    email: cred.Username,
                    password: cred.Password
                );
                var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
                return new StalwartDispatcher(
                    logger: _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<StalwartDispatcher>(),
                    identityManager: new StalwartIdentityManager(
                        tokenService: new TokenServiceEndpointsWrapper(
                            new TokenServiceEndpointsClient(
                                configuration: new() { Endpoint = $"https://{uri.Host}", Path = "auth" },
                                httpClientFactory: httpClientFactory
                            )
                        ),
                        identity,
                        _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<StalwartIdentityManager>()
                    ),
                    configuration: configuration,
                    httpClientFactory: httpClientFactory
                );
            }
            throw new InvalidOperationException($"Unsupported dispatcher URI: {uri.AbsoluteUri}.");
        }
        throw new InvalidOperationException($"No configuration for owner = {owner}.");
    }
}