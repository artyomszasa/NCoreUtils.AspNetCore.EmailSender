namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public interface IMqttClientServiceOptions
{
    string Topic { get; }
}