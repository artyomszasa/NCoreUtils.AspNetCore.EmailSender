using System.Runtime.CompilerServices;
using SG = NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

internal static class SendGridExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SG.EmailAddress ToSendGridAddress(this EmailAddress address)
        => new(email: address.Email, name: address.Name);

    public static SG.EmailAddress[] ToSendGridAddresses(this IEnumerable<EmailAddress> addresses)
        => [.. addresses.Select(ToSendGridAddress)];

    public static SG.EmailAddress[]? ToOptionalSendGridAddresses(this IReadOnlyList<EmailAddress> addresses)
        => addresses is { Count: > 0 }
            ? ToSendGridAddresses(addresses)
            : default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToSendGridDisposition(this EmailAttachmentDisposition disposition)
        => disposition == EmailAttachmentDisposition.Inline ? "inline" : "attachment";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SG.Content ToSendGridContent(this EmailContent content)
        => new(type: content.MediaType, value: content.Content);

    public static SG.Content[] ToSendGridContents(this IEnumerable<EmailContent> contents)
        => [.. contents.Select(ToSendGridContent)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SG.Attachment ToSendGridAttachment(this EmailAttachment attachment) => new(
        content: attachment.Data,
        filename: attachment.Filename,
        type: attachment.MediaType,
        disposition: ToSendGridDisposition(attachment.Disposition),
        contentId: attachment.ContentId
    );

    public static SG.Attachment[] ToSendGridAttachments(this IEnumerable<EmailAttachment> attachments)
        => [.. attachments.Select(ToSendGridAttachment)];

    public static SG.Attachment[]? ToOptionalSendGridAttachments(this IReadOnlyList<EmailAttachment> attachments)
        => attachments is { Count: > 0 }
            ? ToSendGridAttachments(attachments)
            : default;

}