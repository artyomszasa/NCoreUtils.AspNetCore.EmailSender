using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NCoreUtils.OAuth2;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public class MqttEmailScheduler : IEmailSender
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IMqttClientService _mqttClientService;

        public MqttEmailScheduler(IHttpContextAccessor httpContextAccessor, IMqttClientService mqttClientService)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _mqttClientService = mqttClientService ?? throw new ArgumentNullException(nameof(mqttClientService));
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
            var messageId = await _mqttClientService.PublishAsync(new EmailMessageTask(message, owner), cancellationToken);
            return messageId.HasValue ? messageId.Value.ToString() : string.Empty;
        }
    }
}