
using Spectre.Console;

internal static class SpectreConsole
{
    public static readonly Style SystemStyle = new(Color.SpringGreen3);
    public static readonly Style BannerStyle = new(Color.Grey);
    public static readonly Style InfoStyle = new(Color.Yellow);

    public static bool IsExitCommand(string input) =>
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("q", StringComparison.OrdinalIgnoreCase);

    public static void WriteBanner()
    {
        var rule = new Rule("[bold white]Key/Value[/]");
        rule.Justification = Justify.Left;
        AnsiConsole.Write(rule);

        AnsiConsole.Write(new Text("Type your secret key and press Enter.", SpectreConsole.BannerStyle));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Text("Type 'exit' or 'quit' to close the application.", SpectreConsole.BannerStyle));
        AnsiConsole.WriteLine();
    }

    public static void DisplayTitle(string title = "Vault")
    {
        AnsiConsole.Write(new FigletText(title).Centered().Color(Color.Purple));
    }
}
