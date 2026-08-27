using System.Collections.Immutable;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("Unreleased (line {LineNumber}): {Sections.Length} sections")]
public sealed record ChangeLogUnreleased(
    int LineNumber,
    ImmutableArray<ChangeLogSection> Sections,
    ImmutableArray<string> TrailingLines,
    // Valid only immediately after ChangeLogParser.ParseAsync: like every other LineNumber in
    // this model, it describes a position in the originally parsed source text and is never
    // recomputed by a `with` expression that changes Sections/TrailingLines. Callers that
    // transform a parsed document (ChangeLogFixer, ChangeLogUpdater) must reload/reparse from
    // storage before relying on it again, exactly as Credfeto.ChangeLog.Cmd's own lint/fix/relint
    // flow already does.
    int TrailingLinesStartLineNumber
);
