using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore;
using NCoreUtils.AspNetCore.Proto;

namespace NCoreUtils
{
    public static class ServiceCollectionEmailClientProtoExtensions
    {
        public static IServiceCollection AddEmailSenderClient(this IServiceCollection services, IEndpointConfiguration configuration)
            => services
                .AddProtoClient<IEmailSender>(configuration, b => b.ApplyEmailSenderDefaults());

        public static IServiceCollection AddEmailSenderClient(this IServiceCollection services, IEndpointConfiguration configuration, string? prefix)
            => services
                .AddProtoClient<IEmailSender>(configuration, b => b.ApplyEmailSenderDefaults(prefix));

        public static IServiceCollection AddEmailSenderClient(this IServiceCollection services, IConfiguration configuration)
        {
            var config = new EndpointConfiguration();
            configuration.Bind(config);
            return services.AddEmailSenderClient(config);
        }

        public static IServiceCollection AddEmailSenderClient(this IServiceCollection services, IConfiguration configuration, string? prefix)
        {
            var config = new EndpointConfiguration();
            configuration.Bind(config);
            return services.AddEmailSenderClient(config, prefix);
        }

        public static IServiceCollection AddEmailSenderClient(this IServiceCollection services, string endpoint)
        {
            var config = new EndpointConfiguration { Endpoint = endpoint };
            return services.AddEmailSenderClient(config);
        }

        public static IServiceCollection AddEmailSenderClient(this IServiceCollection services, string endpoint, string? prefix)
        {
            var config = new EndpointConfiguration { Endpoint = endpoint };
            return services.AddEmailSenderClient(config, prefix);
        }
    }
}