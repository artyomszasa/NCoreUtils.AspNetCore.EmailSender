namespace NCoreUtils;

public class EmailAttachment(string filename, string mediaType, string contentId, EmailAttachmentDisposition disposition, byte[] data)
{
    public string Filename { get; } = filename;

    public string MediaType { get; } = mediaType;

    public string ContentId { get; } = contentId;

    public EmailAttachmentDisposition Disposition { get; } = disposition;

    public byte[] Data { get; } = data;
}