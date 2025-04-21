using Microsoft.Extensions.Configuration;

namespace Agents;

/// <summary>
/// This is a class only used in demos - it MUST not be used in production code.
/// </summary>
public class Configuration
{
    public static IConfigurationRoot Config { get; private set; }

    public static string GetValue(string key, string? defaultValue = null)
    {
        _ = key ?? throw new ArgumentNullException(nameof(key));

        if (Config == null)
        {
            Initialize();
        }

        return Config![key] ?? (defaultValue ?? throw new Exception($"{key} not found"));
    }

    private static void Initialize()
    {
        Config = new ConfigurationBuilder()
            .AddUserSecrets<Configuration>()
            .AddEnvironmentVariables()
            .Build() ??
            throw new InvalidOperationException("Configuration is not setup correctly.");
    }
}
