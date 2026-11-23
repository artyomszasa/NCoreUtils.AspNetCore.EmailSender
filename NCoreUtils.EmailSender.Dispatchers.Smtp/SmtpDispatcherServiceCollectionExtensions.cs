using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.EmailSender.Dispatchers;
using NCoreUtils.EmailSender.Dispatchers.Smtp;

namespace NCoreUtils;

public static class SmtpDispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Enables <c>smtp://</c> and <c>smtps://</c> dispatcher URIs.
    /// </summary>
    public static IServiceCollection AddSmtpDispatcher(this IServiceCollection services)
        => services.AddEmailDispatcherFactory<SmtpDispatcherFactory>();
}
