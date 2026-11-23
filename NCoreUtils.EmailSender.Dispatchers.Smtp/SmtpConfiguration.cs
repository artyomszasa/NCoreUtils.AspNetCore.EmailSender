using System;

namespace NCoreUtils.EmailSender.Dispatchers.Smtp
{
    public class SmtpConfiguration
    {
        public string Host { get; }

        public int Port { get; } = 25;

        public bool UseSsl { get; }

        public UriCredentials? User { get; }

        public SmtpConfiguration(string host, int port, bool useSsl, UriCredentials? user)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            UseSsl = useSsl;
            User = user;
        }
    }
}