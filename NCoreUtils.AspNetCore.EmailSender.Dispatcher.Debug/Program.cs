using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils;
using NCoreUtils.AspNetCore.EmailSender.Dispatcher;

await using var serviceCollection = new ServiceCollection()
    .AddLogging(b => b.ClearProviders().SetMinimumLevel(LogLevel.Debug).AddSimpleConsole(o => o.SingleLine = true))
    .AddHttpClient()
    // .AddHttpBodyTraceFor("TokenServiceEndpointsClient")
    .AddSingleton<EmailProcessor>()
    .AddSingleton(serviceProvider => new DispatcherConfig(serviceProvider, new Dictionary<string, Uri>
    {
        { "test", new Uri(GetRequiredEnvironmentVariable("DISPATHER_URI")) }
    }))
    .BuildServiceProvider(validateScopes: true);

var processor = serviceCollection.GetRequiredService<EmailProcessor>();

var msgId = await processor.ProcessAsync(new(
    message: new(
        from: "artyom@stalwart.skape.io",
        to: ["artyom@skape.io"],
        subject: "ping",
        cc: [],
        bcc: [],
        contents: [ EmailContent.Text.Html("<p>ping</p>")],
        attachments: []
    ),
    owner: "test"
), "0", CancellationToken.None);

Console.WriteLine(msgId);


static string GetRequiredEnvironmentVariable(string key) => Environment.GetEnvironmentVariable(key) switch
{
    null or "" => throw new InvalidOperationException($"No {key} environment variable set"),
    var value => value
};