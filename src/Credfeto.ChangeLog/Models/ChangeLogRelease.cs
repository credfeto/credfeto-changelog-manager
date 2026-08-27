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
    // static readonly, not const: a const is inlined into every referencing assembly at compile
    // time, so a consumer assembly built against an older package version would keep embedding a
    // stale literal even after upgrading its package reference.
    public static readonly string PendingDate = "TBD";
}
