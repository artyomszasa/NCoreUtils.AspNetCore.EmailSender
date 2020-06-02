namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class PubSubRequest
    {
        public PubSubMessage Message { get; set; } = default!;

        public string Subscription { get; set; } = string.Empty;
    }
}