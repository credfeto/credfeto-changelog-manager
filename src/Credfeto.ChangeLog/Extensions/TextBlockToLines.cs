using System;
using System.Collections.Generic;

namespace Credfeto.ChangeLog.Extensions;

public static class TextBlockToLines
{
    private const string WindowsLineEnding = "\r\n";
    private const string UnixLineEnding = "\n";

    private static readonly string[] LineSeparators = [WindowsLineEnding, "\n\r", UnixLineEnding, "\r"];

    public static IReadOnlyList<string> SplitToLines(this string value)
    {
        return value.Split(separator: LineSeparators, options: StringSplitOptions.None);
    }

    public static string LinesToText(this IEnumerable<string> lines)
    {
        return string.Join(separator: Environment.NewLine, values: lines).Trim();
    }

    public static string DetectLineEnding(this string value)
    {
        ReadOnlySpan<char> text = value.AsSpan();
        int index = text.IndexOfAny('\r', '\n');

        if (index < 0)
        {
            return Environment.NewLine;
        }

        return text[index..].StartsWith(WindowsLineEnding, StringComparison.Ordinal)
            ? WindowsLineEnding
            : UnixLineEnding;
    }

    public static string WithLineEnding(this string value, string lineEnding)
    {
        return string.Join(separator: lineEnding, values: value.SplitToLines());
    }
}
