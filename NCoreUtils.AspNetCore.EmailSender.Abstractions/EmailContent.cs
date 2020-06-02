namespace NCoreUtils
{
    public class EmailContent
    {
        public static class Text
        {
            public static EmailContent Html(string content)
                => new EmailContent("text/html", content);

            public static EmailContent Plain(string content)
                => new EmailContent("text/plain", content);
        }

        public string MediaType { get; }

        public string Content { get; }

        public EmailContent(string mediaType, string content)
        {
            MediaType = mediaType;
            Content = content;
        }
    }
}