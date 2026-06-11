using FluentValidation;
using Hashicorp.Vault.Extensions;
using Hashicorp.Vault.Options;
using Hashicorp.Vault.SecretManagers;
using Hashicorp.Vault.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hashicorp.Vault;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            using var host = CreateHostBuilder(args).Build();
            var secretManager = host.Services.GetRequiredService<ISecretManager>();
            var options = host.Services.GetRequiredService<IOptions<HashiCorpVaultOptions>>();
            var secrets = await secretManager.GetSecretsAsync();
            MapOptions(secrets, options.Value);
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static void MapOptions(IReadOnlyDictionary<string, string> valuePairs, HashiCorpVaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(valuePairs);
        ArgumentNullException.ThrowIfNull(options);

        var type = options.GetType();

        foreach (var pair in valuePairs)
        {
            var prop = type.GetProperty(pair.Key)
                ?? throw new InvalidOperationException($"Property '{pair.Key}' not found on type '{type.FullName}'.");
            prop.SetValue(options, pair.Value);
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                AddSharedConfiguration(
                    config,
                    context.HostingEnvironment.EnvironmentName,
                    includeCommandLine: args.Length > 0,
                    args);
            })
            .ConfigureServices((context, services) =>
            {
                services
                    .AddScoped<IValidator<HashiCorpVaultOptions>, HashiCorpVaultOptionsValidator>()
                    .AddSecretManager(context.Configuration)
                    .AddHostedService<VaultBackgroundService>();
            });

    private static void AddSharedConfiguration(
        IConfigurationBuilder config,
        string environmentName,
        bool includeCommandLine,
        string[]? args)
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
        config.AddUserSecrets<Program>(optional: true);
        config.AddEnvironmentVariables();

        if (includeCommandLine && args is { Length: > 0 })
        {
            config.AddCommandLine(args);
        }
    }
}
