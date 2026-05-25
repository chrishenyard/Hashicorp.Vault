using Hashicorp.Vault.SecretManagers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Hashicorp.Vault.Console;

internal class VaultConsole
{
    public static async Task ExecuteAsync(
        ISecretManager secretManager,
        IHostApplicationLifetime lifetime,
        ILogger logger,
        CancellationToken cancellationToken)
    {

        SpectreConsole.DisplayTitle();
        SpectreConsole.WriteBanner();
        AnsiConsole.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Markup("[bold cyan]Secret Key > [/]"));

                var userInput = await System.Console.In.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                userInput = userInput.Trim();

                if (SpectreConsole.IsExitCommand(userInput))
                {
                    logger.LogInformation("User requested application shutdown.");
                    AnsiConsole.MarkupLine("[grey]Goodbye.[/]");
                    lifetime.StopApplication();
                    break;
                }

                var secretValue = await secretManager.GetSecretAsync(userInput, cancellationToken);

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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Console chat loop cancelled.");
                break;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Markup("[bold springgreen3]System > [/]"));
                AnsiConsole.Write(new Text("Sorry, something went wrong.", SpectreConsole.SystemStyle));
                AnsiConsole.WriteLine();

                logger.LogError(ex, "Unhandled exception in chat loop.");
            }
        }
    }
}

