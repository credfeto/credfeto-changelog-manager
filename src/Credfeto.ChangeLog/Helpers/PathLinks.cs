using System.IO;

namespace Credfeto.ChangeLog.Helpers;

public static class PathLinks
{
    // The path must be spelt textually from the root, and longer than it. The root itself is never checked, only each
    // component after it, up to and including the path.
    public static bool HasLinkBelow(string root, string path)
    {
        int start = Path.EndsInDirectorySeparator(root) ? root.Length : root.Length + 1;

        for (
            int index = path.IndexOfAny(anyOf: PathSeparators.All, startIndex: start);
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

    private static bool IsLink(string path)
    {
        return new FileInfo(path).LinkTarget is not null;
    }
}
