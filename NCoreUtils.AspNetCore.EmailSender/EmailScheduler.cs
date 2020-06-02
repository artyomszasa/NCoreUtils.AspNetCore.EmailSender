using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NCoreUtils.OAuth2;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public class EmailScheduler : IEmailSender
    {
        private readonly IOptionsMonitor<JsonSerializerOptions> _jsonOptions;

        private readonly PublisherClient _client;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailScheduler(
            IOptionsMonitor<JsonSerializerOptions> jsonOptions,
            PublisherClient client,
            IHttpContextAccessor httpContextAccessor)
        {
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
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
            if (!context.User.Identity.IsAuthenticated)
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
                _jsonOptions.CurrentValue
            );
            var messageId = await _client.PublishAsync(new PubsubMessage
            {
                Data = Google.Protobuf.ByteString.CopyFrom(jsonMessage)
            });
            return messageId;
        }
    }
}