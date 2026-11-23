using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.EmailSender.Dispatchers;
using NCoreUtils.EmailSender.Dispatchers.Stalwart;

namespace NCoreUtils;

public static class StalwartDispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Enables <c>stalwart://</c> dispatcher URIs. Requires <see cref="IHttpClientFactory" /> to be registered.
    /// </summary>
    public static IServiceCollection AddStalwartDispatcher(this IServiceCollection services)
        => services.AddEmailDispatcherFactory<StalwartDispatcherFactory>();
}
