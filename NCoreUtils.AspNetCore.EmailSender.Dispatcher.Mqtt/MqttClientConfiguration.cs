namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public partial record MqttClientConfiguration(
    string? Host,
    int? Port,
    bool? CleanSession,
    string? ClientId
);

public partial record MqttClientConfiguration
{
    public static MqttClientConfiguration Default { get; } = new(default, default, default, default);
}