using System.Collections.Generic;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Cmd;

[DebuggerDisplay(
    "ChangeLog={ChangeLog}, Add={Add}, Remove={Remove}, Version={Version}, Extract={Extract}, CheckInsert={CheckInsert}, CreateRelease={CreateRelease}, Lint={Lint}, SelfTest={SelfTest}, ShowHelp={ShowHelp}"
)]
public sealed record Options
{
    public string? ChangeLog { get; init; }

    public string? Version { get; init; }

    public string? Extract { get; init; }

    public string? Remove { get; init; }

    public string? Add { get; init; }

    public string? Message { get; init; }

    public string? CheckInsert { get; init; }

    public string? CreateRelease { get; init; }

    public bool Pending { get; init; }

    public bool DisplayUnreleased { get; init; }

    public bool Lint { get; init; }

    public IEnumerable<string> AdditionalSections { get; init; } = [];

    public bool Fix { get; init; }

    public bool SelfTest { get; init; }

    public bool ShowHelp { get; init; }
}
