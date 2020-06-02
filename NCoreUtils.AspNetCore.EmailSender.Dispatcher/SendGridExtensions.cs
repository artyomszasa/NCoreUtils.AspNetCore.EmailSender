using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    internal static class SendGridExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SendGrid.Helpers.Mail.EmailAddress ToSendGridAddress(this EmailAddress address)
            => new SendGrid.Helpers.Mail.EmailAddress(address.Email, address.Name);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToSendGridDisposition(this EmailAttachmentDisposition disposition)
            => disposition == EmailAttachmentDisposition.Inline ? "inline" : "attachment";
    }
}