using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.EmailSender.Dispatchers;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

/// <summary>
/// Maps owners to dispatcher URIs and creates dispatchers using the registered
/// <see cref="IEmailDispatcherFactory" /> implementations (e.g. <c>AddSmtpDispatcher()</c>).
/// </summary>
public class DispatcherConfig
{
    private static string GetRequiredValue(IConfiguration configuration, string path)
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

    private static IReadOnlyDictionary<string, Uri> GetConfigEntries(IConfiguration configuration)
    {
        var map = new Dictionary<string, Uri>();
        foreach (var section in configuration.GetChildren())
        {
            map[GetRequiredValue(section, "Owner")] = new Uri(GetRequiredValue(section, "Uri"), UriKind.Absolute);
        }
        return map;
    }

    public static DispatcherConfig FromConfiguration(IServiceProvider serviceProvider, IConfiguration configuration)
        => new(serviceProvider, GetConfigEntries(configuration));

    private readonly IServiceProvider _serviceProvider;

    private readonly IReadOnlyDictionary<string, Uri> _configuration;

    private readonly IEmailDispatcherFactory[] _factories;

    public DispatcherConfig(IServiceProvider serviceProvider, IReadOnlyDictionary<string, Uri> configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _factories = [.. serviceProvider.GetServices<IEmailDispatcherFactory>()];
    }

    public IEmailSender CreateSender(string owner)
    {
        if (_configuration.TryGetValue(owner, out var uri))
        {
            foreach (var factory in _factories)
            {
                if (factory.TryCreate(uri, _serviceProvider, out var sender))
                {
                    return sender;
                }
            }
            throw new InvalidOperationException($"Unsupported dispatcher URI: {uri.AbsoluteUri} (is the dispatcher for scheme \"{uri.Scheme}\" registered?).");
        }
        throw new InvalidOperationException($"No configuration for owner = {owner}.");
    }
}
