using FluentValidation;
using Hashicorp.Vault.Package.Extensions;
using Hashicorp.Vault.Package.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System.Reflection;
using static Hashicorp.Vault.Package.Extensions.SecretManagerExtensions;

namespace Hashicorp.Vault;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            using var host = CreateHostBuilder(args);

            // Test direct HTTP call to Vault API to verify connectivity and certificate handling
            //var vaultHttpClient = host.Services.GetRequiredService<VaultHttpClient>();
            //var vaultResponse = await vaultHttpClient.GetSecretAsync("");

            await MapOptions<HashiCorpVaultOptions>(host.Services);
            var options = host.Services.GetRequiredService<IOptions<HashiCorpVaultOptions>>().Value;
            DisplayOptions(options);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        Console.ReadKey();
    }

    private static void DisplayOptions(HashiCorpVaultOptions options)
    {
        AnsiConsole.Write(new FigletText("Vault").Centered().Color(Color.Gold1));

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Gold1);

        table.AddColumn("[bold]Key[/]");
        table.AddColumn("[bold]Value[/]");

        foreach (var prop in typeof(HashiCorpVaultOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(options)?.ToString() ?? "null";
            table.AddRow(new Markup($"[bold]{prop.Name}[/]"), new Markup($"[gold1]{value}[/]"));
        }

        AnsiConsole.Write(table);
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
                    .AddSecretManager(context.Configuration);

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

                if (context.HostingEnvironment.IsDevelopment())
                {
                    services.AddHttpClient<VaultHttpClient>(client =>
                    {
                        client.BaseAddress = new Uri("https://vault.localhost");
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                }
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
