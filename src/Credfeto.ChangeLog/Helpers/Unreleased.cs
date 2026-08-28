using System;

namespace Credfeto.ChangeLog.Helpers;

internal static class Unreleased
{
    public static bool IsUnreleasedHeader(string line, ChangeLogLanguage language)
    {
        string expectedHeader = "## [" + language.UnreleasedSectionName + "]";
        return StringComparer.Ordinal.Equals(x: line, y: expectedHeader);
    }
}
