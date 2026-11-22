using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore.EmailSender.Dispatcher.Stalwart;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

internal class StalwartException(string message)
    : Exception(message)
{ }

internal class JmapError(string operation, string type, string? description, IReadOnlyList<string>? properties)
    : StalwartException(CreateMessage(operation, type, description, properties))
{
    public string Operation { get; } = operation;

    public string Type { get; } = type;

    public string? Description { get; } = description;

    public IReadOnlyList<string>? Properties { get; } = properties;

    private static string CreateMessage(string operation, string type, string? description, IReadOnlyList<string>? properties)
    {
        if (description is not string { Length: > 0 })
        {
            return $"{operation} failed ({type}).";
        }
        if (properties is { Count: > 0 })
        {
            return $"{description.TrimEnd('.')} [{type}] ({string.Join(',', properties)}).";
        }
        return $"{description.TrimEnd('.')} [{type}].";
    }
}

internal class EmailSetError(string type, string? description, IReadOnlyList<string>? properties)
    : JmapError("Email/set", type, description, properties)
{ }

internal class EmailSubmissionError(string type, string? description, IReadOnlyList<string>? properties)
    : JmapError("EmailSubmission/set", type, description, properties)
{ }

[JsonSerializable(typeof(string))]
internal sealed partial class StalwartDispatcherHelperContext : JsonSerializerContext { }

internal class StalwartDispatcher(
    ILogger<StalwartDispatcher> logger,
    StalwartIdentityManager identityManager,
    StalwartConfiguration configuration,
    IHttpClientFactory? httpClientFactory)
    : IEmailSender
{
    private sealed class Calls
    {
        public const string GetIdentity = "id0";

        public const string QueryMailboxes = "mb0";

        public const string EmailSet = "c0";

        public const string EmailSubmit = "c1";
    }

    private static readonly JsonElement ListUnsubscribePostValue = ToJsonElement("List-Unsubscribe=One-Click");

    private static JsonElement ToJsonElement(string value)
        => JsonSerializer.SerializeToElement(value, StalwartDispatcherHelperContext.Default.String);

    protected virtual HttpClient CreateClient() => httpClientFactory is null
        ? new HttpClient()
        : httpClientFactory.CreateClient(nameof(StalwartDispatcher));

    private ValueTask AddAuthorizationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // var accessToken = await identityManager.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        // request.Headers.Authorization = new("bearer", accessToken);
        request.Headers.Authorization = new(
            "Basic",
            Base64.EncodeToString(Encoding.ASCII.GetBytes($"{identityManager.Configuration.Email}:{identityManager.Configuration.Password}"))
        );
        return default;
    }

    private async Task<BlobUploadResponse> UploadBlobAsync(Stream stream, string mediaType, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        var requestUri = $"https://{configuration.Domain}/upload/{configuration.AccountId}";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StreamContent(stream)
            {
                Headers = { ContentType = MediaTypeHeaderValue.Parse(mediaType) }
            }
        };
        await AddAuthorizationAsync(request, cancellationToken).ConfigureAwait(false);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        var blobData = await response.EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync(StalwartSerializerContext.Default.BlobUploadResponse, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Deserializing blob upload response yielded null.");
        return blobData;
    }

    private async Task<JmapResponse> ExecuteJmapCallAsync(JmapCall jmapRequest, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        // FIXME: precompute
        var requestUri = $"https://{configuration.Domain}/jmap";

        var debug0 = JsonSerializer.Serialize(jmapRequest, StalwartSerializerContext.Default.JmapCall);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(jmapRequest, StalwartSerializerContext.Default.JmapCall)
        };
        await AddAuthorizationAsync(request, cancellationToken).ConfigureAwait(false);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        return await response.EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync(StalwartSerializerContext.Default.JmapResponse, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Deserializing JMAP response yielded null.");
    }

    private string ChooseAccountId(JmapResponse jmapResponse, string senderEmail)
    {
        var accounts = jmapResponse[Calls.GetIdentity].GetResponse(StalwartSerializerContext.Default.IdentityGetResponse).List;
        if (accounts is not { Count: > 0 })
        {
            throw new StalwartException("Could not get sender account (Identity/get returned no accounts).");
        }
        if (accounts.TryGetFirst(id => id.Email == senderEmail, out var account))
        {
            logger.LogDebug("Found stalwart account for \"{Email}\" =>  \"{AccountId}\".", senderEmail, account.Id);
            return account.Id;
        }
        var accountId = accounts[0].Id;
        logger.LogWarning("Could not find stalwart account for \"{Email}\", using \"{AccountId}\" (first account).", senderEmail, accountId);
        return accountId;
    }

    private string ChooseMailboxId(JmapResponse jmapResponse)
    {
        var mbres = jmapResponse[Calls.QueryMailboxes].GetResponse(StalwartSerializerContext.Default.MailboxQueryResult);
        if (mbres.Ids is not { Count: > 0 } mailboxIds)
        {
            throw new StalwartException("Could not get drafts mailbox id (Mailbox/query returned no mailboxes).");
        }
        if (mailboxIds.Count == 1)
        {
            return mailboxIds[0];
        }
        var mailboxId = mailboxIds[0];
        logger.LogWarning("Multiple draft mailboxes found, using \"{AccountId}\" (first suitable mailbox).", mailboxId);
        return mailboxId;
    }

    public async Task<string> ScheduleAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // attachments
        var toAttach = new List<(EmailAttachment Attachment, BlobUploadResponse BlobData)>(message.Attachments.Count);
        foreach (var attachment in message.Attachments)
        {
            var blobData = await UploadBlobAsync(
                stream: new MemoryStream(attachment.Data, 0, attachment.Data.Length, writable: false, publiclyVisible: true),
                mediaType: attachment.MediaType,
                cancellationToken: cancellationToken
            );
            toAttach.Add((attachment, blobData));
        }

        var bodyValues = new Dictionary<string, EmailSetMessageBodyValue>();
        string? htmlBody = default;
        foreach (var content in message.Contents)
        {
            if (content.MediaType == "text/html")
            {
                bodyValues.Add("htmlContent", new(content.Content));
                htmlBody = "htmlContent";
            }
        }

        var result0 = await ExecuteJmapCallAsync(new JmapCall(
            @using: ["urn:ietf:params:jmap:core", "urn:ietf:params:jmap:mail", "urn:ietf:params:jmap:submission"],
            methodCalls: [
                MethodCall.Create(
                    "Identity/get",
                    Calls.GetIdentity
                ),
                MethodCall.Create(
                    "Mailbox/query",
                    new MailboxQueryArguments(filter: MailboxQueryFilterCondition.ForRole("drafts")),
                    Calls.QueryMailboxes
                )
            ]
        ), cancellationToken).ConfigureAwait(false);

        // Choose account id
        var accountId = ChooseAccountId(result0, message.From.Email);
        // Choose mailbox
        var mailboxId = ChooseMailboxId(result0);
        // Draft and submit email
        var jmapMessage = new EmailSetMessage(
            mailboxIds: new SingletonDictionary<string, bool>(mailboxId, true),
            from: [new(message.From.Email, message.From.Name)],
            to: message.To.Select(item => new EmailSetMessageFrom(item.Email, item.Name)).ToArray(),
            subject: message.Subject,
            bodyValues: bodyValues,
            htmlBody: htmlBody is string { Length: > 0 }
                ? [new EmailSetMessageBodyPart("text/html", htmlBody)]
                : [],
            attachments: toAttach.Select(tup => new EmailSetMessageAttachment(
                blobId: tup.BlobData.BlobId,
                type: tup.BlobData.Type ?? tup.Attachment.MediaType,
                name: tup.Attachment.Filename,
                cid: tup.Attachment.ContentId,
                disposition: tup.Attachment.Disposition == EmailAttachmentDisposition.Inline
                    ? "inline"
                    : "attachment",
                isInline: tup.Attachment.Disposition == EmailAttachmentDisposition.Inline
            )).ToArray(),
            replyTo: message.ReplyTo is string { Length: > 0 } replyTo
                ? [new EmailSetMessageFrom(replyTo)]
                : null,
            cc: message.Cc is { Count: > 0 } cc
                ? cc.Select(item => new EmailSetMessageFrom(item.Email, item.Name)).ToArray()
                : null,
            bcc: message.Cc is { Count: > 0 } bcc
                ? bcc.Select(item => new EmailSetMessageFrom(item.Email, item.Name)).ToArray()
                : null
        );
        if (message.OneClickUnsubscribe is { Count: > 0 } oneClickUnsubscribe)
        {
            jmapMessage.Additional.Add("header:List-Unsubscribe:asRaw", ToJsonElement(oneClickUnsubscribe.ToHeaderValue()));
            jmapMessage.Additional.Add("header:List-Unsubscribe-Post:asRaw", ListUnsubscribePostValue);
        }
        const string messageId = "msg1";
        const string submissionId = "sub1";
        var result = await ExecuteJmapCallAsync(new JmapCall(
            @using: ["urn:ietf:params:jmap:core", "urn:ietf:params:jmap:mail", "urn:ietf:params:jmap:submission"],
            methodCalls: [
                MethodCall.Create(
                    "Email/set",
                    new EmailSetArguments(messageId, jmapMessage),
                    Calls.EmailSet
                ),
                MethodCall.Create(
                    "EmailSubmission/set",
                    new EmailSubmissionSetArguments(submissionId, new("#msg1", accountId)),
                    Calls.EmailSubmit
                )
            ]
        ), cancellationToken).ConfigureAwait(false);

        var draft = result[Calls.EmailSet].GetResponse(StalwartSerializerContext.Default.EmailSetResult);
        if (draft.NotCreated is { Count: > 0 } notCreated && notCreated.TryGetValue(messageId, out var error))
        {
            throw new EmailSetError(error.Type, error.Description, error.Properties);
        }
        if (draft.Created is not { Count: > 0 } created || !created.TryGetValue(messageId, out var createdItem))
        {
            throw new StalwartException($"Email/set result does not contain the created message.");
        }
        var submission = result[Calls.EmailSubmit].GetResponse(StalwartSerializerContext.Default.EmailSubmissionResult);
        if (submission.NotCreated is { Count: > 0 } sNotCreated && sNotCreated.TryGetValue(submissionId, out var sError))
        {
            throw new EmailSubmissionError(sError.Type, sError.Description, sError.Properties);
        }
        if (submission.Created is not { Count: > 0 } sCreated || !sCreated.TryGetValue(submissionId, out var sCreatedItem))
        {
            throw new StalwartException($"EmailSubmission/set result does not contain the created message.");
        }
        logger.LogInformation("JMAP call succeeeded (message id = {MessageId}, submssion id = {SubmissionId})", createdItem.Id, sCreatedItem.Id);
        return createdItem.Id;
    }
}