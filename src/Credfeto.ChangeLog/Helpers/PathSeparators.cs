using System.IO;

namespace Credfeto.ChangeLog.Helpers;

internal static class PathSeparators
{
    public static readonly char[] All = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static bool IsSeparator(char character)
    {
        return character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar;
    }
}
