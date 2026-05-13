namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public interface ISendGridConfiguration
{
    string ApiKey { get; }

    string SendEndpoint { get; }

    string? HttpClientConfigurationName { get; }
}

public sealed class SendGridConfiguration(
    string apiKey,
    string sendEndpoint = "https://api.sendgrid.com/v3/mail/send",
    string? httpClientConfigurationName = default)
    : ISendGridConfiguration
{
    public string ApiKey { get; } = apiKey;

    public string SendEndpoint { get; } = sendEndpoint;

    public string? HttpClientConfigurationName { get; } = httpClientConfigurationName;
}