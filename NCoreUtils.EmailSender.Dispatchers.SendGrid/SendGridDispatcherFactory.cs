using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.EmailSender.Dispatchers.SendGrid;

/// <summary>
/// Creates <see cref="SendGridDispatcher" /> for <c>sendgrid://apikey@host[:port][/path]</c> URIs.
/// </summary>
public sealed class SendGridDispatcherFactory : IEmailDispatcherFactory
{
    public bool TryCreate(Uri uri, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IEmailSender sender)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(serviceProvider);
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
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SendGridDispatcher>();
            sender = new SendGridDispatcher(configuration, logger, serviceProvider.GetService<IHttpClientFactory>());
            return true;
        }
        sender = default;
        return false;
    }
}
