using System;

namespace NCoreUtils;

public class EmailContent
{
    public static class Text
    {
        public static EmailContent Html(string content)
            => new("text/html", content);

        public static EmailContent Plain(string content)
            => new("text/plain", content);
    }

    public string MediaType { get; }

    public string Content { get; }

    public EmailContent(string mediaType, string content)
    {
        MediaType = mediaType ?? throw new ArgumentNullException(nameof(mediaType));
        Content = content ?? string.Empty;
    }
}