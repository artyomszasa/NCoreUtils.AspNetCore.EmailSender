using System.Text.Json;
using Microsoft.Extensions.Options;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public class MqttClientServiceOptions : IMqttClientServiceOptions
    {
        private readonly IOptionsMonitor<JsonSerializerOptions> _jsonSerializerOptionsMonitor;

        public JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptionsMonitor.CurrentValue;

        public string Topic { get; set; } = default!;

        public int BufferSize { get; set; } = 32 * 1024;

        public MqttClientServiceOptions(IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsMonitor)
        {
            _jsonSerializerOptionsMonitor = jsonSerializerOptionsMonitor;
        }
    }
}