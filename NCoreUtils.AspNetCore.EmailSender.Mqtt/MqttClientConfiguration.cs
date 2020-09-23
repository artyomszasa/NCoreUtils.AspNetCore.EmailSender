namespace NCoreUtils.AspNetCore.EmailSender
{
    public class MqttClientConfiguration
    {
        public string? Host { get; set; }

        public int? Port { get; set; }

        public bool? CleanSession { get; set; }
    }
}