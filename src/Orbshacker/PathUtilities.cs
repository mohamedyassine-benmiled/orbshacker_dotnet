using System.Text;
using System.Text.RegularExpressions;

namespace Orbshacker;

public static partial class PathUtilities
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

    public static string SanitizeFileName(string? name, string replacement = "")
    {
        if (string.IsNullOrEmpty(name)) return "unnamed";
        var builder = new StringBuilder();
        foreach (var character in name.Trim())
            if (character >= 32 && "<>:\"/\\|?*™®©℠℗".IndexOf(character) < 0) builder.Append(character);
            else builder.Append(replacement);
        var cleaned = TrailingWhitespaceOrDots().Replace(builder.ToString(), "");
        if (cleaned.Length == 0) return "unnamed";
        if (Reserved.Contains(cleaned.Split('.')[0])) cleaned = "_" + cleaned;
        return cleaned;
    }

    public static string SanitizePathSegment(string? segment, string replacement = "") => SanitizeFileName(segment, replacement);

    public static string SanitizeRelativePath(string? path, string replacement = "")
    {
        if (string.IsNullOrEmpty(path)) return "unnamed";
        var normalized = DrivePrefix().Replace(path.Replace('\\', '/'), "");
        var segments = normalized.Split('/').Select(x => x.Trim()).Where(x => x is not "" and not "." and not "..")
            .Select(x => SanitizeFileName(x, replacement)).Where(x => x.Length > 0).ToArray();
        return segments.Length == 0 ? "unnamed" : string.Join('/', segments);
    }

    [GeneratedRegex(@"[\s.]+$")]
    private static partial Regex TrailingWhitespaceOrDots();
    [GeneratedRegex(@"^[a-zA-Z]:[/\\]*")]
    private static partial Regex DrivePrefix();
}
