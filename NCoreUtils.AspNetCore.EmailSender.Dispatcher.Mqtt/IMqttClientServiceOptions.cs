using System.Text.Json;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public interface IMqttClientServiceOptions
    {
        JsonSerializerOptions JsonSerializerOptions { get; }

        string Topic { get; }
    }
}