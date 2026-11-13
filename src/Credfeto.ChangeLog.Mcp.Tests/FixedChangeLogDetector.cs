using System.Diagnostics.CodeAnalysis;

namespace Credfeto.ChangeLog.Mcp.Tests;

// The real detector searches from the test process's current folder, not the temporary repository
internal sealed class FixedChangeLogDetector : IChangeLogDetector
{
    private readonly string? _changeLogFileName;

    public FixedChangeLogDetector(string? changeLogFileName)
    {
        this._changeLogFileName = changeLogFileName;
    }

    public bool TryFindChangeLog([NotNullWhen(true)] out string? changeLogFileName)
    {
        changeLogFileName = this._changeLogFileName;

        return changeLogFileName is not null;
    }
}
