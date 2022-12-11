namespace NCoreUtils.Proto;

// NOTE: json parameter wrapping required for compatibility with earlier versions!
[ProtoInfo(typeof(IEmailSender), Path = "i_email_sender", Naming = Naming.SnakeCase, SingleJsonParameterWrapping = SingleJsonParameterWrapping.Wrap)]
public partial class EmailSenderInfo { }