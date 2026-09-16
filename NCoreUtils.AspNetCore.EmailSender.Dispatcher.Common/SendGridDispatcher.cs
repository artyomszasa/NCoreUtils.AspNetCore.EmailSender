using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public class SendGridDispatcher(
    ISendGridConfiguration configuration,
    ILogger<SendGridDispatcher> logger,
    IHttpClientFactory? httpClientFactory)
    : IEmailSender
{
    protected ISendGridConfiguration Configuration { get; } = configuration;

    protected ILogger Logger { get; } = logger;

    protected IHttpClientFactory? HttpClientFactory { get; } = httpClientFactory;

    protected HttpClient CreateClient()
        => HttpClientFactory?.CreateClient(Configuration.HttpClientConfigurationName ?? nameof(SendGridDispatcher))
            ?? new HttpClient();

    public async Task<string> ScheduleAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var httpClient = CreateClient();
        var m = new SendGridMessage(
            from: message.From.ToSendGridAddress(),
            to: message.To.ToSendGridAddresses(),
            subject: message.Subject,
            content: message.Contents.ToSendGridContents(),
            cc: message.Cc.ToOptionalSendGridAddresses(),
            bcc: message.Bcc.ToOptionalSendGridAddresses(),
            attachments: message.Attachments.ToOptionalSendGridAttachments()
        );
        using var request = new HttpRequestMessage(HttpMethod.Post, Configuration.SendEndpoint)
        {
            Content = JsonContent.Create(m, SendGridSerializerContext.Default.SendGridMessage)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Configuration.ApiKey);
        using var response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.Created or HttpStatusCode.OK)
        {
            return (response.Headers.TryGetValues("X-Message-Id", out var ss) ? ss.FirstOrDefault() : default) ?? string.Empty;
        }
        SendGridErrorResponse errorResponse;
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                errorResponse = JsonSerializer
                    .Deserialize(raw, SendGridSerializerContext.Default.SendGridErrorResponse)
                    ?? throw new InvalidOperationException("Deserializing error response returned null.");
            }
            catch (Exception exn)
            {
                Logger.LogError(exn, "Sending mail via Sendrid failed with response: {RawResponse}.", raw);
                return string.Empty; // no retry
            }
        }
        else
        {
            errorResponse = await response.Content
                .ReadFromJsonAsync(SendGridSerializerContext.Default.SendGridErrorResponse, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Deserializing error response returned null.");
        }
        if (errorResponse is null || errorResponse.Errors.Count == 0)
        {
            Logger.LogError("Sending mail has failed without an error.");
            return string.Empty; // no retry
        }
        foreach (var error in errorResponse.Errors)
        {
            Logger.LogError(
                "SendGrid delivery error: [{Field}] {Message}",
                error.Field,
                error.Message
            );
        }
        return string.Empty;
    }
}