using System.Collections.Immutable;
using System.Diagnostics;
using Credfeto.ChangeLog.Constants;

namespace Credfeto.ChangeLog;

[DebuggerDisplay("{UnreleasedSectionName}: {SectionOrder.Length} sections")]
public sealed record ChangeLogLanguage(
    string DocumentTitle,
    string UnreleasedSectionName,
    ImmutableArray<string> SectionOrder,
    string DateFormat
)
{
    public string UnreleasedHeader => ChangeLogHeadingConstants.VersionHeaderPrefix + this.UnreleasedSectionName + "]";
}
