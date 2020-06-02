namespace NCoreUtils
{
    public class EmailAddress
    {
        public static implicit operator EmailAddress(string email)
            => new EmailAddress(email, default);

        public string Email { get; }

        public string? Name { get; }

        public EmailAddress(string email, string? name = default)
        {
            Email = email;
            Name = name;
        }

        // FIXME: noalloc version.
        public override string ToString()
            => string.IsNullOrEmpty(Name)
                ? Email
                : $"{Name} <{Email}>";
    }
}