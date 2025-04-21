using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Extensions;

public static class TBuilderExtensions
{
    public static TBuilder AddDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var assembly = Assembly.GetCallingAssembly();

        var configurationRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets(assembly)
            .Build();

        builder.Services.AddSingleton(configurationRoot);
        builder.Configuration.AddConfiguration(configurationRoot);

        return builder;
    }
}
