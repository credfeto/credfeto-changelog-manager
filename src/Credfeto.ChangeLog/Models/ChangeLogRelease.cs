using System.Collections.Immutable;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("[{Version}] - {Date}{IsYanked ? \" [YANKED]\" : string.Empty,nq} (line {LineNumber})")]
public sealed record ChangeLogRelease(
    string Version,
    string Date,
    int LineNumber,
    ImmutableArray<ChangeLogSection> Sections,
    bool IsYanked = false,
    int BlankLinesBeforeHeading = 1
)
{
    // The sentinel Date value for a pending (not-yet-dated) release. Lives on the model both the
    // writer (ChangeLogUpdater) and reader (ChangeLogLinter) already depend on, rather than on
    // either of those services, so neither one is coupled to the other for it.
    public const string PendingDate = "TBD";
}
