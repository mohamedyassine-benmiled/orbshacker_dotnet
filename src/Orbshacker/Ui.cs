namespace Orbshacker;

public static class Ui
{
    public static void Success(string text) => Write($"[OK] {text}", ConsoleColor.Green);
    public static void Warning(string text) => Write($"[!] {text}", ConsoleColor.Yellow);
    public static void Error(string text) => Write($"[ERROR] {text}", ConsoleColor.Red);
    public static void Info(string text) => Write($"[*] {text}", ConsoleColor.Cyan);
    public static void Write(string text, ConsoleColor color = ConsoleColor.Gray) { var old = Console.ForegroundColor; Console.ForegroundColor = color; Console.WriteLine(text); Console.ForegroundColor = old; }
    public static bool Confirm(string prompt = "Create and launch?") { Console.Write($"\n{prompt} [Y/n]: "); return (Console.ReadLine() ?? "").Trim().ToLowerInvariant() is "" or "y" or "yes"; }
    public static void Banner() => Write($@"
 _____ _____ _____ _____    _____ _____ _____ _____ _____ _____
|     | __  | __  |   __|  |  |  |  _  |     |  |  |   __| __  |
|  |  |    -| __ -|__   |  |     |     |   --|    -|   __|    -|
|_____|__|__|_____|_____|  |__|__|__|__|_____|__|__|_____|__|__|

 Developer: {AppConfig.Developer}
 Version: {AppConfig.Version}
 Database: Discord Official API + GitHub Archive
", ConsoleColor.Cyan);
    public static void BoxedTitle(string title, int width = 50)
    {
        Write("+" + new string('-', width - 2) + "+", ConsoleColor.Cyan);
        Write("|" + title.PadLeft((width - 2 + title.Length) / 2).PadRight(width - 2) + "|", ConsoleColor.Cyan);
        Write("+" + new string('-', width - 2) + "+", ConsoleColor.Cyan);
    }
    public static void Loading(string text, int milliseconds = 800)
    {
        var frames = new[] { '|', '/', '-', '\\' }; var until = DateTime.UtcNow.AddMilliseconds(milliseconds); var index = 0;
        while (DateTime.UtcNow < until) { Console.Write($"\r{frames[index++ % frames.Length]} {text}"); Thread.Sleep(100); }
        Console.Write("\r" + new string(' ', text.Length + 4) + "\r");
    }
    public static void Menu()
    {
        BoxedTitle("MAIN MENU");
        Console.WriteLine("  1. Search Discord database\n  2. Manual mode\n  3. Steam Quest Mode\n  4. Credits & Info\n  5. Exit\n");
    }
}
