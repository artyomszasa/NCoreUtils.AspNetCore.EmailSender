using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCoreUtils;

internal sealed class DebugTraceBody(ILogger<DebugTraceBody> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var reqBuffer = new MemoryStream();
        await request.Content!.CopyToAsync(reqBuffer);
        reqBuffer.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(reqBuffer, leaveOpen: true))
        {
            logger.LogInformation(reader.ReadToEnd());
        }
        reqBuffer.Seek(0, SeekOrigin.Begin);
        request.Content = new StreamContent(reqBuffer);
        return await base.SendAsync(request, cancellationToken);
    }
}

internal static class DebugTraceBodyExtensions
{
    public static IServiceCollection AddHttpBodyTraceFor(this IServiceCollection services, string configurationName)
    {
        services.AddTransient<DebugTraceBody>();
        services.AddHttpClient(configurationName)
            .AddHttpMessageHandler<DebugTraceBody>();
        return services;
    }
}