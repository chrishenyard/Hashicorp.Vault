using Hashicorp.Vault.SecretManagers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;

namespace Hashicorp.Vault.Services;

public class VaultBackgroundService(
    ISecretManager secretManager,
    IHostApplicationLifetime lifeTime,
    ILogger<VaultBackgroundService> logger)
{
    private readonly ISecretManager _secretManager = secretManager;
    private readonly IHostApplicationLifetime _lifeTime = lifeTime;
    private readonly ILogger<VaultBackgroundService> _logger = logger;

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _logger.LogInformation("Vault Background Service - Stopping (thread {ThreadId})...", Thread.CurrentThread.ManagedThreadId));
        _logger.LogInformation("Vault Background Service - Starting (thread {ThreadId})...", Thread.CurrentThread.ManagedThreadId);

        SpectreConsole.DisplayTitle();
        SpectreConsole.WriteBanner();
        AnsiConsole.WriteLine();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Markup("[bold cyan]Secret Key > [/]"));

                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                userInput = userInput.Trim();

                if (SpectreConsole.IsExitCommand(userInput))
                {
                    _logger.LogInformation("User requested application shutdown.");
                    AnsiConsole.MarkupLine("[grey]Goodbye.[/]");
                    _lifeTime.StopApplication();
                    break;
                }

                var secretValue = await _secretManager.GetSecretAsync(userInput, stoppingToken);

                if (secretValue != null)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Markup("[bold springgreen3]System > [/]"));
                    AnsiConsole.Write(new Text(secretValue, SpectreConsole.SystemStyle));
                    AnsiConsole.WriteLine();
                }
                else
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Markup("[bold springgreen3]System > [/]"));
                    AnsiConsole.Write(new Text("Secret not found.", SpectreConsole.SystemStyle));
                    AnsiConsole.WriteLine();
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Vault Background Service - Task.Delay was cancelled (thread {ThreadId}).", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Markup("[bold springgreen3]System > [/]"));
                AnsiConsole.Write(new Text(GetAllExceptionMessages(ex), SpectreConsole.InfoStyle));
                AnsiConsole.WriteLine();
            }
        }
    }

    private static string GetAllExceptionMessages(Exception exception)
    {
        var builder = new StringBuilder();
        var current = exception;
        var index = 0;

        while (current is not null)
        {
            if (index > 0)
            {
                builder.Append(" --> ");
            }

            builder.Append(current.Message);
            current = current.InnerException;
            index++;
        }

        return builder.ToString();
    }
}
