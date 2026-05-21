using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Hashicorp.Vault;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = CreateBootstrapLogger();

        try
        {
            Log.Information("Starting console host");

            using IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Console host terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
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
                   .AddOptions<VaultOptions>()
                   .BindConfiguration(VaultOptions.SectionName)
                   .ValidateDataAnnotations()
                   .ValidateOnStart();
            });
    private static Serilog.Core.Logger CreateBootstrapLogger()
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environments.Production;

        var configuration = new ConfigurationBuilder();
        AddSharedConfiguration(configuration, environmentName, includeCommandLine: false, args: null);

        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration.Build())
            .CreateLogger();
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
