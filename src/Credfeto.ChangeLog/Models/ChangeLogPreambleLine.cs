using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("Line {LineNumber}: {Text}")]
public sealed record ChangeLogPreambleLine(int LineNumber, string Text);
