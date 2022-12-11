namespace NCoreUtils;

public class EmailAttachment
{
    public string Filename { get; }

    public string MediaType { get; }

    public string ContentId { get; }

    public EmailAttachmentDisposition Disposition { get; }

    public byte[] Data { get; }

    public EmailAttachment(string filename, string mediaType, string contentId, EmailAttachmentDisposition disposition, byte[] data)
    {
        Filename = filename;
        MediaType = mediaType;
        ContentId = contentId;
        Disposition = disposition;
        Data = data;
    }
}