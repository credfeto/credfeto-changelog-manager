using System;
using System.Collections.Generic;

namespace Credfeto.ChangeLog.Extensions;

public static class TextBlockToLines
{
    private static readonly string[] LineSeparators = ["\r\n", "\n\r", "\n", "\r"];

    public static IReadOnlyList<string> SplitToLines(this string value)
    {
        return value.Split(separator: LineSeparators, options: StringSplitOptions.None);
    }

    public static string LinesToText(this IEnumerable<string> lines)
    {
        return string.Join(separator: Environment.NewLine, values: lines).Trim();
    }
}
