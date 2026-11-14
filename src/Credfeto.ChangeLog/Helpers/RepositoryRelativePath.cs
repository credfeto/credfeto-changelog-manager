using System;
using System.IO;
using Credfeto.ChangeLog.Extensions;

namespace Credfeto.ChangeLog.Helpers;

// git reports the physical working folder, so a path that may reach the repository through links must be resolved
// the same way before the two are compared. A path already spelt from that folder, with no link below it, needs no
// resolving, which keeps the common case free of the full filesystem walk.
public static class RepositoryRelativePath
{
    public static string? Find(string workingDirectory, string fullPath)
    {
        // git records a link as its own entry, so a path through a link below the working folder would never match the
        // file it reaches
        string relativePath =
            IsSpeltFrom(workingDirectory: workingDirectory, fullPath: fullPath)
            && !PathLinks.HasLinkBelow(root: workingDirectory, path: fullPath)
                ? fullPath[RelativeStart(workingDirectory)..]
                : Path.GetRelativePath(
                    relativeTo: PhysicalPath.Resolve(workingDirectory),
                    path: PhysicalPath.Resolve(fullPath)
                );

        if (IsOutside(relativePath))
        {
            return null;
        }

        return relativePath.Replace(oldChar: '\\', newChar: '/');
    }

    private static bool IsSpeltFrom(string workingDirectory, string fullPath)
    {
        int start = RelativeStart(workingDirectory);

        return fullPath.Length > start
            && fullPath.StartsWith(value: workingDirectory, comparisonType: StringComparison.Ordinal)
            && (start == workingDirectory.Length || PathSeparators.IsSeparator(fullPath[workingDirectory.Length]));
    }

    private static int RelativeStart(string workingDirectory)
    {
        return Path.EndsInDirectorySeparator(workingDirectory) ? workingDirectory.Length : workingDirectory.Length + 1;
    }

    private static bool IsOutside(string relativePath)
    {
        return Path.IsPathRooted(relativePath)
            || relativePath.EqualsOrdinal("..")
            || relativePath.StartsWith(
                value: ".." + Path.DirectorySeparatorChar,
                comparisonType: StringComparison.Ordinal
            );
    }
}
