namespace NCoreUtils.AspNetCore.EmailSender;

public interface IMqttClientServiceOptions
{
    string Topic { get; }

    int BufferSize { get; }
}