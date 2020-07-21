using NCoreUtils.AspNetCore.Proto;

namespace NCoreUtils
{
    public static class EmailSenderProtoConfiguration
    {
        public static ServiceDescriptorBuilder ApplyEmailSenderDefaults(this ServiceDescriptorBuilder builder)
            => builder
                .SetDefaultInputType(InputType.Json(EmailSenderJsonOptions.Default))
                .SetDefaultOutputType(OutputType.Json(EmailSenderJsonOptions.Default))
                .SetDefaultErrorType(ErrorType.Json(EmailSenderJsonOptions.Default))
                .SetNamingPolicy(NamingConvention.SnakeCase);

        public static ServiceDescriptorBuilder ApplyEmailSenderDefaults(this ServiceDescriptorBuilder builder, string? prefix)
            => builder
                .SetPath(prefix ?? "i_email_sender")
                .SetDefaultInputType(InputType.Json(EmailSenderJsonOptions.Default))
                .SetDefaultOutputType(OutputType.Json(EmailSenderJsonOptions.Default))
                .SetDefaultErrorType(ErrorType.Json(EmailSenderJsonOptions.Default))
                .SetNamingPolicy(NamingConvention.SnakeCase);
    }
}