using System.Text.RegularExpressions;

namespace Credfeto.ChangeLog.Helpers;

internal static class RegexSettings
{
    public const short TimeoutMilliseconds = 1000;

    public const RegexOptions GitHunkPositionOptions =
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture;

    public const string GitHunkPositionRegex =
        @"^@@\s*\-(?<OriginalFileStart>\d*)(,(?<OriginalFileEnd>\d*))?\s*\+(?<CurrentFileStart>\d*)(,(?<CurrentFileChangeLength>\d*))?\s*@@";
}
