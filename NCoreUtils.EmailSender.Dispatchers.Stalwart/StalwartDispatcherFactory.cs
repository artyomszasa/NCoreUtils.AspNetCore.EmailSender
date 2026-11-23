using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.OAuth2.Internal;

namespace NCoreUtils.EmailSender.Dispatchers.Stalwart;

/// <summary>
/// Creates <see cref="StalwartDispatcher" /> for <c>stalwart://account[:password]@host</c> URIs.
/// </summary>
public sealed class StalwartDispatcherFactory : IEmailDispatcherFactory
{
    public bool TryCreate(Uri uri, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IEmailSender sender)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        if (uri.Scheme == "stalwart")
        {
            var cred = UriCredentials.FromUri(uri) ?? throw new InvalidOperationException("AccountId is required");
            var configuration = new StalwartConfiguration(
                domain: uri.Host,
                accountId: cred.Username
            );
            var identity = new StalwartIdentityConfiguration(
                email: cred.Username,
                password: cred.Password
            );
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            sender = new StalwartDispatcher(
                logger: loggerFactory.CreateLogger<StalwartDispatcher>(),
                identityManager: new StalwartIdentityManager(
                    tokenService: new TokenServiceEndpointsWrapper(
                        new TokenServiceEndpointsClient(
                            configuration: new() { Endpoint = $"https://{uri.Host}", Path = "auth" },
                            httpClientFactory: httpClientFactory
                        )
                    ),
                    identity,
                    loggerFactory.CreateLogger<StalwartIdentityManager>()
                ),
                configuration: configuration,
                httpClientFactory: httpClientFactory
            );
            return true;
        }
        sender = default;
        return false;
    }
}
