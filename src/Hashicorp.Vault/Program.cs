using FluentValidation;
using Hashicorp.Vault.Extensions;
using Hashicorp.Vault.Options;
using Hashicorp.Vault.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hashicorp.Vault;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            using var host = CreateHostBuilder(args);
            await SecretManagerService.MapOptions(host.Services);
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        Console.ReadKey();
    }

    private static IHost CreateHostBuilder(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
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

                services.AddOptions<HashiCorpVaultOptions>()
                    .Bind(context.Configuration.GetSection("HashiCorpVaultOptions"))
                    .Validate(options =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        var validator = serviceProvider.GetRequiredService<IValidator<HashiCorpVaultOptions>>();
                        var result = validator.Validate(options);
                        if (!result.IsValid)
                        {
                            var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                            throw new InvalidOperationException($"Invalid HashiCorpVaultOptions: {errors}");
                        }
                        return true;
                    });
            })
            .Build();

        return host;
    }

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
