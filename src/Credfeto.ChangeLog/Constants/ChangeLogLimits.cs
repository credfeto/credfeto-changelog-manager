namespace Credfeto.ChangeLog.Constants;

public static class ChangeLogLimits
{
    // The one limit on an entry's length, shared by --add, the MCP changelog_add tool and --lint, so
    // what can be added and what passes lint cannot drift apart. The [Unreleased] section is packed
    // into the NuGet ReleaseNotes property, which nuget.org caps, so long entries break publishing.
    public const int MaxEntryLength = 100;
}
