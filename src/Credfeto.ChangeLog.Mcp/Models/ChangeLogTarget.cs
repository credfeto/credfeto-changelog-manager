using System.Diagnostics;

namespace Credfeto.ChangeLog.Mcp.Models;

[DebuggerDisplay("{RelativePath}")]
public sealed record ChangeLogTarget(string FullPath, string RelativePath, ChangeLogLanguage Language);
