using System.Collections.Immutable;

namespace Credfeto.ChangeLog.Extensions;

public static class ChangeLogImmutableArrayExtensions
{
    public static int CountTrailingBlankLines(this in ImmutableArray<string> lines)
    {
        int count = 0;

        for (int i = lines.Length - 1; i >= 0 && string.IsNullOrWhiteSpace(lines[i]); i--)
        {
            count++;
        }

        return count;
    }

    public static ImmutableArray<string> TrimTrailingBlanks(this in ImmutableArray<string> lines)
    {
        int end = lines.Length - lines.CountTrailingBlankLines();
        return end == lines.Length ? lines : lines[..end];
    }

    // -1 covers both "no HTML comment present" and "non-comment content precedes it", so callers
    // never mistake unrelated trailer content for a comment boundary.
    public static int CountBlankLinesBeforeHtmlComment(this in ImmutableArray<string> lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWithHtmlComment())
            {
                return i;
            }

            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                return -1;
            }
        }

        return -1;
    }
}
