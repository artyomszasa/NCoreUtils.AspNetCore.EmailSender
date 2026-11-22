using Microsoft.Extensions.Logging;
using NCoreUtils.OAuth2;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.Stalwart;

internal class StalwartIdentityManager(
    ITokenService tokenService,
    StalwartIdentityConfiguration configuration,
    ILogger<StalwartIdentityManager> logger)
    : IDisposable
{
    private string? _accessToken;

    private string? _refreshToken;

    private DateTimeOffset _expiry = DateTimeOffset.MinValue;

    private AsyncLock Sync { get; } = new();

    private ITokenService TokenService { get; } = tokenService ?? throw new ArgumentNullException(nameof(tokenService));

    public StalwartIdentityConfiguration Configuration { get; } = configuration ?? throw new ArgumentNullException(nameof(configuration));

    private ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    private async Task<string> DoGetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var _ = await Sync.LockAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // check token has already been refreshed/reacquired while actual flow was blocked by the semaphore
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.Now <= _expiry)
            {
                return _accessToken;
            }
            // if there is a refresh token, try refreshing first.
            if (!string.IsNullOrEmpty(_refreshToken))
            {
                var response = await TokenService.RefreshTokenAsync(_refreshToken, default, cancellationToken).ConfigureAwait(false);
                _accessToken = response.AccessToken;
                _refreshToken = response.RefreshToken;
                _expiry = DateTimeOffset.Now + response.ExpiresIn - TimeSpan.FromSeconds(2); // to be sure
                Logger.LogTokenRefreshSuccess((int)response.ExpiresIn.TotalSeconds);
                return response.AccessToken;
            }
        }
        catch (Exception exn)
        {
            // clear tokens (otherwise refresh may be retried before reauthentication occures).
            _accessToken = default;
            _refreshToken = default;
            Logger.LogTokenRefreshFailure(exn);
        }
        return await DoPasswordGrantAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> DoPasswordGrantAsync(CancellationToken cancellationToken)
    {
        var response = await TokenService
            .PasswordGrantAsync(
                username: Configuration.Email,
                password: Configuration.Password,
                clientId: "webadmin", //"newsletter_test",
                clientSecret: default,
                scopes: default,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        _accessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        _expiry = DateTimeOffset.Now + response.ExpiresIn - TimeSpan.FromSeconds(2); // to be sure
        Logger.LogPasswordGrantSuccess((int)response.ExpiresIn.TotalSeconds);
        return response.AccessToken;
    }

    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.Now <= _expiry)
        {
            return new ValueTask<string>(_accessToken);
        }
        return new ValueTask<string>(DoGetAccessTokenAsync(cancellationToken));
    }

    public bool Invalidate(string accessToken)
    {
        var current = _accessToken;
        return ReferenceEquals(current, Interlocked.CompareExchange(ref _accessToken, null, accessToken));
    }

    #region disposable

    private InterlockedBoolean IsDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && IsDisposed.TrySet())
        {
            Sync.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}