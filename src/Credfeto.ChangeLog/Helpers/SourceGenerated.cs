using System.Text.RegularExpressions;

namespace Credfeto.ChangeLog.Helpers;

internal static partial class SourceGenerated
{
    [GeneratedRegex(
        pattern: RegexSettings.GitHunkPositionRegex,
        options: RegexSettings.GitHunkPositionOptions,
        matchTimeoutMilliseconds: RegexSettings.TimeoutMilliseconds
    )]
    public static partial Regex GitHunkPositionRegex();
}
