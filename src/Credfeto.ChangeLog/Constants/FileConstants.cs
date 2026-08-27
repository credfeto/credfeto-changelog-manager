namespace Credfeto.ChangeLog.Constants;

internal static class FileConstants
{
    public const string Unreleased = "Unreleased";

    public const string UnreleasedHeader = "## [" + Unreleased + "]";

    public const string ChangeLogFileName = "CHANGELOG.md";

    // The sentinel written for a pending (not-yet-dated) release, and recognised as such by
    // ChangeLogLinter - shared here so ChangeLogUpdater and ChangeLogLinter can't drift apart
    // on what "pending" means.
    public const string PendingReleaseDate = "TBD";
}
