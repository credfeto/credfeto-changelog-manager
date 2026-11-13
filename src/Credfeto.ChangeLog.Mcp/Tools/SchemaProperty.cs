using System.Diagnostics;

namespace Credfeto.ChangeLog.Mcp.Tools;

[DebuggerDisplay("Name={Name}, Required={Required}, IsFlag={IsFlag}")]
public sealed record SchemaProperty(string Name, string Description, bool Required = false, bool IsFlag = false);
