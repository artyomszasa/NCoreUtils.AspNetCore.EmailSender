using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils;

public interface IEmailSender
{
    Task<string> ScheduleAsync(EmailMessage message, CancellationToken cancellationToken = default);
}