namespace NCoreUtils;

public interface IEmailSender
{
    Task<string> ScheduleAsync(EmailMessage message, CancellationToken cancellationToken = default);
}