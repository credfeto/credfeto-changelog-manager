using System.IO;
using Credfeto.ChangeLog.Mcp.Paths;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Paths;

public sealed class ChangeLogPathResolverTests : ToolTestBase
{
    public ChangeLogPathResolverTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void FolderInsideRepositoryUsesTheRepositoryRoot()
    {
        string folder = Path.Combine(path1: this.RepositoryFolder, path2: "src");
        Directory.CreateDirectory(folder);

        ChangeLogPathResolver resolver = new(workingDirectory: folder, detector: new FixedChangeLogDetector(null));

        Assert.Equal(expected: this.RepositoryFolder, actual: resolver.RepositoryRoot);
    }

    [Fact]
    public void WorktreeGitFileMarksTheRepositoryRoot()
    {
        string worktree = Path.Combine(path1: this.TempFolder, path2: "worktree");
        Directory.CreateDirectory(worktree);
        File.WriteAllText(path: Path.Combine(path1: worktree, path2: ".git"), contents: "gitdir: elsewhere");

        ChangeLogPathResolver resolver = new(workingDirectory: worktree, detector: new FixedChangeLogDetector(null));

        Assert.Equal(expected: worktree, actual: resolver.RepositoryRoot);
    }

    [Fact]
    public void FolderOutsideAnyRepositoryIsItsOwnRoot()
    {
        // The file system root is never inside a repository, so the search runs out of parents
        string root = Path.GetPathRoot(Path.GetFullPath(this.TempFolder)) ?? Path.DirectorySeparatorChar.ToString();

        ChangeLogPathResolver resolver = new(workingDirectory: root, detector: new FixedChangeLogDetector(null));

        Assert.Equal(expected: root, actual: resolver.RepositoryRoot);
    }
}
