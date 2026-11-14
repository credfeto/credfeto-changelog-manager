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
        int relativeStart = PathLinks.RelativeStart(workingDirectory);
        string relativePath =
            IsSpeltFrom(workingDirectory: workingDirectory, fullPath: fullPath, relativeStart: relativeStart)
            && !PathLinks.HasLinkBelow(path: fullPath, relativeStart: relativeStart)
                ? fullPath[relativeStart..]
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

    private static bool IsSpeltFrom(string workingDirectory, string fullPath, int relativeStart)
    {
        return fullPath.Length > relativeStart
            && fullPath.StartsWith(value: workingDirectory, comparisonType: StringComparison.Ordinal)
            && (
                relativeStart == workingDirectory.Length
                || PathSeparators.IsSeparator(fullPath[workingDirectory.Length])
            );
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
