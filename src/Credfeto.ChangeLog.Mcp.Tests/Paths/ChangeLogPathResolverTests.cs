using System.IO;
using Credfeto.ChangeLog.Mcp.Exceptions;
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

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromWorkingDirectory(
            workingDirectory: folder,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Equal(expected: this.RepositoryFolder, actual: resolver.RepositoryRoot);
    }

    [Fact]
    public void WorktreeGitFileMarksTheRepositoryRoot()
    {
        string worktree = Path.Combine(path1: this.TempFolder, path2: "worktree");
        Directory.CreateDirectory(worktree);
        File.WriteAllText(path: Path.Combine(path1: worktree, path2: ".git"), contents: "gitdir: elsewhere");

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromWorkingDirectory(
            workingDirectory: worktree,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Equal(expected: worktree, actual: resolver.RepositoryRoot);
    }

    [Fact]
    public void FolderOutsideAnyRepositoryIsItsOwnRoot()
    {
        // The file system root is never inside a repository, so the search runs out of parents
        string root = Path.GetPathRoot(Path.GetFullPath(this.TempFolder)) ?? Path.DirectorySeparatorChar.ToString();

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromWorkingDirectory(
            workingDirectory: root,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Equal(expected: root, actual: resolver.RepositoryRoot);
    }

    [Fact]
    public void RepositoryRootIsUsedWithoutSearchingUpwards()
    {
        string root = this.CreateNestedRoot();

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Equal(expected: root, actual: resolver.RepositoryRoot);
    }

    [Fact]
    public void RepositoryRootTrailingSeparatorIsRemoved()
    {
        string root = this.CreateNestedRoot();

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root + Path.DirectorySeparatorChar,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Equal(expected: root, actual: resolver.RepositoryRoot);
    }

    [Theory]
    [InlineData("CHANGELOG.md")]
    [InlineData("docs/CHANGELOG.md")]
    public void RelativeChangeLogResolvesAgainstTheRepositoryRoot(string changeLog)
    {
        string root = this.CreateNestedRoot();

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Equal(
            expected: Path.GetFullPath(Path.Combine(path1: root, path2: changeLog)),
            actual: resolver.Resolve(changeLog)
        );
    }

    [Fact]
    public void AbsoluteChangeLogInsideTheRepositoryRootIsAccepted()
    {
        string root = this.CreateNestedRoot();
        string changeLog = Path.Combine(path1: root, path2: CHANGELOG);

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Equal(expected: changeLog, actual: resolver.Resolve(changeLog));
    }

    [Theory]
    [InlineData("../CHANGELOG.md")]
    [InlineData("README.md")]
    [InlineData("/etc/CHANGELOG.md")]
    public void ChangeLogOutsideTheRepositoryRootOrNotAChangeLogIsRejected(string changeLog)
    {
        string root = this.CreateNestedRoot();

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Throws<ChangeLogPathException>(() => resolver.Resolve(changeLog));
    }

    [Fact]
    public void FolderLinkInsideTheRepositoryRootIsRejected()
    {
        string root = this.CreateNestedRoot();
        string outside = Path.Combine(path1: this.TempFolder, path2: "outside");
        Directory.CreateDirectory(outside);
        Directory.CreateSymbolicLink(path: Path.Combine(path1: root, path2: "linked"), pathToTarget: outside);

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root,
            detector: new FixedChangeLogDetector(null)
        );

        Assert.Throws<ChangeLogPathException>(() => resolver.Resolve("linked/CHANGELOG.md"));
    }

    [Fact]
    public void ChangeLogIsDetectedAtTheRepositoryRoot()
    {
        string root = this.CreateNestedRoot();
        string changeLog = Path.Combine(path1: root, path2: CHANGELOG);
        File.WriteAllText(path: changeLog, contents: "# Changelog");

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root,
            detector: new RepositoryRootChangeLogDetector(root)
        );

        Assert.Equal(expected: changeLog, actual: resolver.Resolve(null));
    }

    [Fact]
    public void MissingChangeLogAtTheRepositoryRootIsNotDetected()
    {
        string root = this.CreateNestedRoot();

        ChangeLogPathResolver resolver = ChangeLogPathResolver.FromRepositoryRoot(
            repositoryRoot: root,
            detector: new RepositoryRootChangeLogDetector(root)
        );

        ChangeLogPathException exception = Assert.Throws<ChangeLogPathException>(() => resolver.Resolve(null));

        Assert.Equal(expected: "Could not find changelog", actual: exception.Message);
    }

    // A folder inside the test repository, so any walk up to the enclosing .git would be visible
    private string CreateNestedRoot()
    {
        string root = Path.Combine(path1: this.RepositoryFolder, path2: "nested");
        Directory.CreateDirectory(root);

        return root;
    }
}
