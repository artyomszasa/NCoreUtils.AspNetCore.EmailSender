using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.EmailSender.Dispatchers;

/// <summary>
/// Creates <see cref="IEmailSender" /> instances from dispatcher URIs (e.g. <c>smtp://user:password@host:port</c>).
/// </summary>
public interface IEmailDispatcherFactory
{
    /// <summary>
    /// Attempts to create dispatcher for the specified URI.
    /// </summary>
    /// <param name="uri">Dispatcher URI.</param>
    /// <param name="serviceProvider">Service provider to resolve dependencies from.</param>
    /// <param name="sender">Created dispatcher on success.</param>
    /// <returns>
    /// <c>true</c> if the URI is supported by the factory and the dispatcher has been created, <c>false</c> otherwise.
    /// </returns>
    bool TryCreate(Uri uri, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IEmailSender sender);
}
