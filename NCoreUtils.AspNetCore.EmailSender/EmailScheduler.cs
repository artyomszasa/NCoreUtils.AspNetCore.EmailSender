using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using NCoreUtils.AspNetCore.Proto;
using NCoreUtils.Internal;
using NCoreUtils.OAuth2;
using NCoreUtils.Proto;

namespace NCoreUtils.AspNetCore.EmailSender;

[ProtoService(typeof(EmailSenderInfo), typeof(EmailSenderSerializerContext))]
public partial class EmailScheduler : IEmailSender
{
    private readonly PublisherClient _client;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmailScheduler(
        PublisherClient client,
        IHttpContextAccessor httpContextAccessor)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<string> ScheduleAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            throw new InvalidOperationException($"Email scheduler is being invoked outside of request context.");
        }
        if (true != context.User.Identity?.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }
        var owner = context.User.FindFirstValue(OAuth2ClaimTypes.Ownership);
        if (string.IsNullOrEmpty(owner))
        {
            throw new ForbiddenException($"Current user does not specify ownership.");
        }
        var jsonMessage = JsonSerializer.SerializeToUtf8Bytes(
            new EmailMessageTask(message, owner),
            EmailMessageTaskSerializerContext.Default.EmailMessageTask
        );
        var messageId = await _client.PublishAsync(new PubsubMessage
        {
            Data = Google.Protobuf.ByteString.CopyFrom(jsonMessage)
        });
        return messageId;
    }
}