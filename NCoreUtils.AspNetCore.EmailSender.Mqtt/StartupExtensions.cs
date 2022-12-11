using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace NCoreUtils.AspNetCore.EmailSender;

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

    internal static MqttClientConfiguration GetMqttClientConfiguration(this IConfigurationSection configuration)
        => new(
            host: configuration[nameof(MqttClientConfiguration.Host)],
            port: configuration.GetOptionalInt32(nameof(MqttClientConfiguration.Port)),
            cleanSession: configuration.GetOptionalBoolean(nameof(MqttClientConfiguration.CleanSession))
        );

    internal static MqttClientServiceOptions GetMqttClientServiceOptions(this IConfigurationSection configuration)
        => new(
            topic: configuration.GetRequiredValue(nameof(MqttClientServiceOptions.Topic)),
            bufferSize: configuration.GetOptionalInt32(nameof(MqttClientServiceOptions.BufferSize))
        );
}