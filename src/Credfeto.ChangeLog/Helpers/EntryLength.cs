using System;
using Credfeto.ChangeLog.Constants;

namespace Credfeto.ChangeLog.Helpers;

internal static class EntryLength
{
    private const string BULLET = "- ";

    // Surrounding whitespace is not part of the entry, so it is not counted.
    public static int OfMessage(string message)
    {
        return message.Trim().Length;
    }

    // An entry line as written in the file, without its leading "- ", measured the same way as
    // OfMessage, so a message that --add accepts always passes lint.
    public static int OfLine(string line)
    {
        string trimmed = line.Trim();

        return trimmed.StartsWith(value: BULLET, comparisonType: StringComparison.Ordinal)
            ? OfMessage(trimmed[BULLET.Length..])
            : trimmed.Length;
    }

    public static bool IsTooLong(int length)
    {
        return length > ChangeLogLimits.MaxEntryLength;
    }
}
