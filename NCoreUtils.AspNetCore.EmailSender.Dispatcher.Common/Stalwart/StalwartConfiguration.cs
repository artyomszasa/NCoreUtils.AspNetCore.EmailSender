namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.Stalwart;

internal sealed class StalwartConfiguration(string domain, string accountId)
{
    public string Domain { get; } = domain;

    public string AccountId { get; } = accountId;
}