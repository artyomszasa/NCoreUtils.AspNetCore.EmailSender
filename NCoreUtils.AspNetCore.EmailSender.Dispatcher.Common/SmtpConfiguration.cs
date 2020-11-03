using System;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class SmtpConfiguration
    {
        public string Host { get; }

        public int Port { get; } = 25;

        public bool UseSsl { get; }

        public SmtpCredentials? User { get; }

        public SmtpConfiguration(string host, int port, bool useSsl, SmtpCredentials? user)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            UseSsl = useSsl;
            User = user;
        }
    }
}