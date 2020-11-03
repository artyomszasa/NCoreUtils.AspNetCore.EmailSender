namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public struct SmtpCredentials
    {
        public string Username { get; }

        public string Password { get; }

        public SmtpCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}