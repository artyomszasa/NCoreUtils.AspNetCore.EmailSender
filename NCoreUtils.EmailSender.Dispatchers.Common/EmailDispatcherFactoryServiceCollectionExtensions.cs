using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NCoreUtils.EmailSender.Dispatchers;

public static class EmailDispatcherFactoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers the specified dispatcher factory (each factory type is registered at most once).
    /// </summary>
    public static IServiceCollection AddEmailDispatcherFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFactory>(this IServiceCollection services)
        where TFactory : class, IEmailDispatcherFactory
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEmailDispatcherFactory, TFactory>());
        return services;
    }
}
