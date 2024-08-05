namespace NCoreUtils.Internal;

public class EmailMessageTask
{
    public EmailMessage Message { get; }

    public string Owner { get; }

    public EmailMessageTask(EmailMessage message, string owner)
    {
        if (string.IsNullOrEmpty(owner))
        {
            throw new ArgumentException($"'{nameof(owner)}' cannot be null or empty.", nameof(owner));
        }
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Owner = owner;
    }
}