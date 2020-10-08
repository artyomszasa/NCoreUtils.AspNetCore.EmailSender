using System.Text.Json;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public interface IMqttClientServiceOptions
    {
        JsonSerializerOptions JsonSerializerOptions { get; }

        string Topic { get; }

        int BufferSize { get; }
    }
}