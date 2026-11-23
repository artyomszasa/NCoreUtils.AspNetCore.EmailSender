using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.EmailSender.Dispatchers;
using NCoreUtils.EmailSender.Dispatchers.SendGrid;

namespace NCoreUtils;

public static class SendGridDispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Enables <c>sendgrid://</c> dispatcher URIs.
    /// </summary>
    public static IServiceCollection AddSendGridDispatcher(this IServiceCollection services)
        => services.AddEmailDispatcherFactory<SendGridDispatcherFactory>();
}
