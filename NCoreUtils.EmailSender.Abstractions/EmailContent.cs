namespace NCoreUtils;

public class EmailContent(string mediaType, string content)
{
    public static class Text
    {
        public static EmailContent Html(string content)
            => new("text/html", content);

        public static EmailContent Plain(string content)
            => new("text/plain", content);
    }

    public string MediaType { get; } = mediaType ?? throw new ArgumentNullException(nameof(mediaType));

    public string Content { get; } = content ?? string.Empty;
}