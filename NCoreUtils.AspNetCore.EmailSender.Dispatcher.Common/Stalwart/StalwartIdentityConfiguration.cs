namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.Stalwart;

internal sealed class StalwartIdentityConfiguration(string email, string password)
{
    public string Email { get; } = email;

    public string Password { get; } = password;
}
