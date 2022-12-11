namespace NCoreUtils.AspNetCore.EmailSender;

public class MqttClientServiceOptions : IMqttClientServiceOptions
{
    public const int DefaultBufferSize = 32 * 1024;

    public string Topic { get; }

    public int BufferSize { get; }

    public MqttClientServiceOptions(string topic, int? bufferSize)
    {
        Topic = topic;
        BufferSize = bufferSize ?? DefaultBufferSize;
    }
}