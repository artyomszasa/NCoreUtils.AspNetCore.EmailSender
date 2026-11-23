namespace NCoreUtils.EmailSender.Dispatchers;

/// <summary>
/// Username/password pair, typically extracted from the user info part of a dispatcher URI.
/// </summary>
public readonly struct UriCredentials(string username, string password)
{
    /// <summary>
    /// Extracts credentials from the user info part of the specified URI (<c>scheme://user:password@host</c>).
    /// Both username and password are unescaped.
    /// </summary>
    /// <returns>Extracted credentials or <c>null</c> if the URI has no user info.</returns>
    public static UriCredentials? FromUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var userInfo = uri.UserInfo;
        if (string.IsNullOrEmpty(userInfo))
        {
            return default;
        }
        var i = userInfo.IndexOf(':');
        if (-1 == i)
        {
            return new UriCredentials(Uri.UnescapeDataString(userInfo), string.Empty);
        }
        return new UriCredentials(Uri.UnescapeDataString(userInfo.AsSpan(0, i)), Uri.UnescapeDataString(userInfo.AsSpan(i + 1)));
    }

    public string Username { get; } = username;

    public string Password { get; } = password;
}
