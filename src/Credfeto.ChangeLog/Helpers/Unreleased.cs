using System;

namespace Credfeto.ChangeLog.Helpers;

internal static class Unreleased
{
    public static bool IsUnreleasedHeader(string line, string unreleasedHeader)
    {
        return StringComparer.Ordinal.Equals(x: line, y: unreleasedHeader);
    }
}
