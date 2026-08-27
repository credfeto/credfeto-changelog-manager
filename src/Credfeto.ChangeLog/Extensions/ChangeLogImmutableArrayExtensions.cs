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

    // Number of blank lines before the first HTML comment line, provided only blank lines
    // precede it (which is also that comment's index, since it is counted from position 0).
    // Returns -1 when there is no such comment (none present, or non-blank/non-comment content
    // comes first) so callers never mistake unrelated trailer content for a comment boundary.
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

    // Whether a CountBlankLinesBeforeHtmlComment result already reflects exactly one blank line,
    // or no HTML comment at all - the only counts that need no correction. Shared by the linter
    // (to decide whether to report an error) and the fixer (to decide whether to rewrite
    // TrailingLines), so the two stay in agreement if the accepted count is ever revisited.
    public static bool IsAlreadyOneBlankLineOrNoComment(this int blankLineCount) => blankLineCount is < 0 or 1;
}
