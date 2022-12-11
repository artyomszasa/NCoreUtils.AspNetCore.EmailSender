using NCoreUtils.Proto;

namespace NCoreUtils;

[ProtoClient(typeof(EmailSenderInfo), typeof(EmailSenderSerializerContext))]
public partial class EmailSenderClient { }