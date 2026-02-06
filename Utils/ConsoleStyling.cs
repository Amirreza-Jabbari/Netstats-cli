using Spectre.Console;
using Spectre.Console.Rendering;

namespace NetStats.Utils;

public static class ConsoleStyling
{
    private static bool _enabled = true;
    public static void DisableColors() => _enabled = false;

    public static void WriteError(string text)
    {
        if (_enabled)
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(text)}[/]");
        else
            Console.WriteLine(text);
    }

    public static void WriteInfo(string text)
    {
        if (_enabled)
            AnsiConsole.MarkupLine($"[green]{Markup.Escape(text)}[/]");
        else
            Console.WriteLine(text);
    }

    public static void WritePlain(string text)
    {
        if (_enabled)
            AnsiConsole.MarkupLine($"[white]{Markup.Escape(text)}[/]");
        else
            Console.WriteLine(text);
    }
}
