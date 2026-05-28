using Hashicorp.Vault.Extensions;
using Hashicorp.Vault.SecretManagers;
using Hashicorp.Vault.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hashicorp.Vault;

public class Program
{
    private static readonly AutoResetEvent ShutdownEvent = new(false);

    public static async Task Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        using var host = CreateHostBuilder(args).Build();

        var secretManager = host.Services.GetRequiredService<ISecretManager>();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        ILogger<VaultBackgroundService> logger = host.Services.GetRequiredService<ILogger<VaultBackgroundService>>();
        var vaultService = new VaultBackgroundService(secretManager, lifetime, logger);

        await vaultService.ExecuteAsync(cancellationToken);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            cancellationTokenSource.Cancel();
            eventArgs.Cancel = true;
            ShutdownEvent.Set();
        };

        ShutdownEvent.WaitOne();
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
                logging.SetMinimumLevel(LogLevel.Information);
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
                    .AddSecretManager(context.Configuration);
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