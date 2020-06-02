namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class PubSubMessage
    {
        public string Data { get; set; } = string.Empty;

        public string MessageId { get; set; } = string.Empty;
    }
}