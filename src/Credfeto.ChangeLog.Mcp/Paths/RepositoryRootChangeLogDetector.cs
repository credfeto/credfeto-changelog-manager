using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Credfeto.ChangeLog.Mcp.Paths;

public sealed class RepositoryRootChangeLogDetector : IChangeLogDetector
{
    private const string CHANGELOG_FILE_NAME = "CHANGELOG.md";

    private readonly string _changeLogFileName;

    public RepositoryRootChangeLogDetector(string repositoryRoot)
    {
        this._changeLogFileName = Path.Combine(path1: repositoryRoot, path2: CHANGELOG_FILE_NAME);
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
