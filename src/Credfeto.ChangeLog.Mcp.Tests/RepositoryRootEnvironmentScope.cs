using System;

namespace Credfeto.ChangeLog.Mcp.Tests;

// Sets the repository root environment variable for the lifetime of the scope and restores the
// previous value on dispose; only use from tests in the SerialEnvironment collection
internal sealed class RepositoryRootEnvironmentScope : IDisposable
{
    private const string VARIABLE = "CHANGELOG_MCP_REPO_ROOT";

    private readonly string? _original;

    public RepositoryRootEnvironmentScope(string? value)
    {
        this._original = Environment.GetEnvironmentVariable(VARIABLE);
        Environment.SetEnvironmentVariable(variable: VARIABLE, value: value);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(variable: VARIABLE, value: this._original);
    }
}
