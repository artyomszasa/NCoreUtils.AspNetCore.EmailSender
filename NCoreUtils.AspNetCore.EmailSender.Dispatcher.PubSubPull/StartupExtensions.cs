using System;
using System.Globalization;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Configuration;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

internal static class StartupExtensions
{
    internal static string GetRequiredValue(this IConfiguration configuration, string path)
    {
        var value = configuration[path];
        if (value is null)
        {
            var fullPath = configuration is IConfigurationSection section
                ? $"{section.Path}:{path}"
                : path;
            throw new InvalidOperationException($"No configuration value found at \"{fullPath}\".");
        }
        return value;
    }

    internal static int? GetOptionalInt32(this IConfiguration configuration, string path)
    {
        var value = configuration[path];
        if (value is null)
        {
            return default;
        }
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            var fullPath = configuration is IConfigurationSection section
                ? $"{section.Path}:{path}"
                : path;
            throw new FormatException($"Invalid integer value at \"{fullPath}\".");
        }
        return intValue;
    }

    internal static bool? GetOptionalBoolean(this IConfiguration configuration, string path)
    {
        var value = configuration[path];
        if (value is null)
        {
            return default;
        }
        if (!bool.TryParse(value, out var boolValue))
        {
            var fullPath = configuration is IConfigurationSection section
                ? $"{section.Path}:{path}"
                : path;
            throw new FormatException($"Invalid boolean value at \"{fullPath}\".");
        }
        return boolValue;
    }

    internal static SubscriptionName GetSubscriptionConfiguration(this IConfiguration configuration)
        => SubscriptionName.FromProjectSubscription(
                projectId: configuration.GetRequiredValue("Google:ProjectId"),
                subscriptionId: configuration.GetRequiredValue("Google:SubscriptionId")
            );


}