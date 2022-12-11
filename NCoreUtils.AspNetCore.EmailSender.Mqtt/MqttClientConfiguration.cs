namespace NCoreUtils.AspNetCore.EmailSender;

public class MqttClientConfiguration
{
    public static MqttClientConfiguration Default { get; } = new(default, default, default);

    public string? Host { get; }

    public int? Port { get; }

    public bool? CleanSession { get; }

    public MqttClientConfiguration(string? host, int? port, bool? cleanSession)
    {
        Host = host;
        Port = port;
        CleanSession = cleanSession;
    }
}