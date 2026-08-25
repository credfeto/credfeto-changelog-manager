using System.Collections.Immutable;

namespace Credfeto.ChangeLog.Extensions;

public static class ImmutableArrayExtensions
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
}
