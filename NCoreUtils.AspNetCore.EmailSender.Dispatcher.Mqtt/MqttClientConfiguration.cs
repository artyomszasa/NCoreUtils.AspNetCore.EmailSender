namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class MqttClientConfiguration
    {
        public string? Host { get; set; }

        public int? Port { get; set; }

        public bool? CleanSession { get; set; }

        public string? ClientId { get; set; }
    }
}