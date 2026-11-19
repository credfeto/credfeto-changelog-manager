using System;
using System.IO;

namespace Credfeto.ChangeLog.Helpers;

public static class PathLinks
{
    // The path must be spelt textually from the root. The root itself is never checked, only each component after it,
    // up to and including the path, so a path with nothing after the root has no link below it.
    public static bool HasLinkBelow(string root, string path)
    {
        return HasLinkBelow(path: path, relativeStart: RelativeStart(root));
    }

    // relativeStart is the index of the first character after the root and its separator, as RelativeStart gives.
    public static bool HasLinkBelow(string path, int relativeStart)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(relativeStart);

        if (relativeStart >= path.Length)
        {
            return false;
        }

        for (
            int index = path.IndexOfAny(anyOf: PathSeparators.All, startIndex: relativeStart);
            index != -1;
            index = path.IndexOfAny(anyOf: PathSeparators.All, startIndex: index + 1)
        )
        {
            if (IsLink(path[..index]))
            {
                return true;
            }
        }

        return IsLink(path);
    }

    public static int RelativeStart(string root)
    {
        return Path.EndsInDirectorySeparator(root) ? root.Length : root.Length + 1;
    }

    private static bool IsLink(string path)
    {
        return new FileInfo(path).LinkTarget is not null;
    }
}
