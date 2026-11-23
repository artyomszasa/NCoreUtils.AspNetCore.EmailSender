namespace NCoreUtils.EmailSender.Dispatchers.Stalwart;

internal sealed class StalwartIdentityConfiguration(string email, string password)
{
    public string Email { get; } = email;

    public string Password { get; } = password;
}
