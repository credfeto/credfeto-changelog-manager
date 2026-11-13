using System.Diagnostics.CodeAnalysis;
using System.IO;
using Credfeto.ChangeLog.Mcp.Constants;

namespace Credfeto.ChangeLog.Mcp.Paths;

public sealed class RepositoryRootChangeLogDetector : IChangeLogDetector
{
    private readonly string _changeLogFileName;

    public RepositoryRootChangeLogDetector(string repositoryRoot)
    {
        this._changeLogFileName = Path.Combine(path1: repositoryRoot, path2: FileConstants.ChangeLogFileName);
    }

    public bool TryFindChangeLog([NotNullWhen(true)] out string? changeLogFileName)
    {
        if (File.Exists(this._changeLogFileName))
        {
            changeLogFileName = this._changeLogFileName;

            return true;
        }

        changeLogFileName = null;

        return false;
    }
}
