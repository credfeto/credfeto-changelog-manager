using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog.Extensions;

public static class ChangeLogHeadingExtensions
{
    private const string CHANGE_TYPE_HEADING_PREFIX = "### ";
    private const string VERSION_HEADER_PREFIX = "## [";
    private const string REFERENCE_LINK_SEPARATOR = "]: ";

    public static bool IsComparisonLink(this string line)
    {
        return line.Length > 3
            && line[0] == '['
            && line.Contains(value: REFERENCE_LINK_SEPARATOR, comparisonType: StringComparison.Ordinal);
    }

    public static bool IsChangeTypeHeading(this string line)
    {
        return line.StartsWith(value: CHANGE_TYPE_HEADING_PREFIX, comparisonType: StringComparison.Ordinal);
    }

    public static bool IsVersionHeader(this string line)
    {
        return line.StartsWith(value: VERSION_HEADER_PREFIX, comparisonType: StringComparison.Ordinal);
    }

    public static string GetChangeTypeName(this string line)
    {
        return line[CHANGE_TYPE_HEADING_PREFIX.Length..];
    }

    public static string AsChangeTypeHeading(this string name)
    {
        return CHANGE_TYPE_HEADING_PREFIX + name;
    }

    public static int FindUnreleasedStart(this IReadOnlyList<string> lines, ChangeLogLanguage language)
    {
        string unreleasedHeader = language.UnreleasedHeader;

        for (int i = 0; i < lines.Count; i++)
        {
            if (Unreleased.IsUnreleasedHeader(line: lines[i], unreleasedHeader: unreleasedHeader))
            {
                return i;
            }
        }

        return -1;
    }

    public static int FindUnreleasedEnd(
        this IReadOnlyList<string> lines,
        int unreleasedStart,
        ChangeLogLanguage language
    )
    {
        string unreleasedHeader = language.UnreleasedHeader;

        for (int i = unreleasedStart + 1; i < lines.Count; i++)
        {
            if (
                lines[i].IsVersionHeader()
                && !Unreleased.IsUnreleasedHeader(line: lines[i], unreleasedHeader: unreleasedHeader)
            )
            {
                return i;
            }
        }

        return lines.Count;
    }

    public static bool StartsWithHtmlComment(this string line)
    {
        return line.StartsWith(value: "<!--", comparisonType: StringComparison.Ordinal);
    }

    public static bool EqualsOrdinal(this string str, string other)
    {
        return StringComparer.Ordinal.Equals(x: str, y: other);
    }
}
