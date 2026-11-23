using Microsoft.Extensions.Logging;

namespace NCoreUtils.EmailSender.Dispatchers.Stalwart;

internal static partial class LoggingExtensions
{
    public const int TokenRefreshSuccess = 5000;

    public const int TokenRefreshFailure = 5001;

    public const int PasswordGrantSuccess = 5002;

    [LoggerMessage(
        EventId = TokenRefreshSuccess,
        EventName = nameof(TokenRefreshSuccess),
        Level = LogLevel.Debug,
        Message = "RefreshToken success [expires in {expires}s]."
    )]
    public static partial void LogTokenRefreshSuccess(this ILogger logger, int expires);

    [LoggerMessage(
        EventId = TokenRefreshFailure,
        EventName = nameof(TokenRefreshFailure),
        Level = LogLevel.Error,
        Message = "Refreshing token failed, fallback to reauthentication."
    )]
    public static partial void LogTokenRefreshFailure(this ILogger logger, Exception exn);

    [LoggerMessage(
        EventId = PasswordGrantSuccess,
        EventName = nameof(PasswordGrantSuccess),
        Level = LogLevel.Debug,
        Message = "Password grant success [expires in {expires}s]."
    )]
    public static partial void LogPasswordGrantSuccess(this ILogger logger, int expires);
}