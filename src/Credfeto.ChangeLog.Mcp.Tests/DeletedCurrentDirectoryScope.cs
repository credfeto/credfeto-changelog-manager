using System;
using System.IO;

namespace Credfeto.ChangeLog.Mcp.Tests;

// Moves into a new folder and deletes it, so the process is left in a current folder that no
// longer exists, then moves back on dispose; only use from tests in the SerialEnvironment collection
internal sealed class DeletedCurrentDirectoryScope : IDisposable
{
    private readonly string _original;

    public DeletedCurrentDirectoryScope()
    {
        this._original = Directory.GetCurrentDirectory();

        string folder = Path.Combine(path1: Path.GetTempPath(), path2: "deleted-start-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        Directory.SetCurrentDirectory(folder);
        Directory.Delete(folder);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(this._original);
    }
}
