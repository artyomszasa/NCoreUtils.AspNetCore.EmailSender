using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.EmailSender.Dispatchers.Smtp;

/// <summary>
/// Creates <see cref="SmtpDispatcher" /> for <c>smtp://[user[:password]@]host[:port]</c> and
/// <c>smtps://[user[:password]@]host[:port]</c> URIs.
/// </summary>
public sealed class SmtpDispatcherFactory : IEmailDispatcherFactory
{
    public bool TryCreate(Uri uri, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IEmailSender sender)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (uri.Scheme == "smtp" || uri.Scheme == "smtps")
        {
            var configuration = new SmtpConfiguration(
                host: uri.Host,
                port: 0 > uri.Port ? 25 : uri.Port,
                useSsl: uri.Scheme == "smtps",
                user: UriCredentials.FromUri(uri)
            );
            sender = new SmtpDispatcher(configuration);
            return true;
        }
        sender = default;
        return false;
    }
}
