using System.IO;
using Credfeto.ChangeLog.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class RepositoryRelativePathTests : LoggingFolderCleanupTestBase
{
    public RepositoryRelativePathTests(ITestOutputHelper output)
        : base(output)
    {
        this.Root = PhysicalPath.Resolve(this.TempFolder);
    }

    private string Root { get; }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PathSpeltFromTheWorkingFolderGivesTheSameAnswerAsResolvingIt(bool trailingSeparator)
    {
        string repository = this.CreateFolder("repository");
        this.CreateFolder(Path.Combine(path1: "repository", path2: "docs"));
        string workingDirectory = trailingSeparator ? repository + Path.DirectorySeparatorChar : repository;
        string changeLog = Path.Combine(path1: repository, path2: "docs", path3: "CHANGELOG.md");
        string resolved = Path.GetRelativePath(
                relativeTo: PhysicalPath.Resolve(workingDirectory),
                path: PhysicalPath.Resolve(changeLog)
            )
            .Replace(oldChar: '\\', newChar: '/');

        Assert.Equal(expected: "docs/CHANGELOG.md", actual: resolved);
        Assert.Equal(
            expected: resolved,
            actual: RepositoryRelativePath.Find(workingDirectory: workingDirectory, fullPath: changeLog)
        );
    }

    [Fact]
    public void PathReachedThroughALinkIsResolved()
    {
        string repository = this.CreateFolder("repository");
        string link = Path.Combine(path1: this.Root, path2: "linked");
        Directory.CreateSymbolicLink(path: link, pathToTarget: repository);

        Assert.Equal(
            expected: "CHANGELOG.md",
            actual: RepositoryRelativePath.Find(
                workingDirectory: repository + Path.DirectorySeparatorChar,
                fullPath: Path.Combine(path1: link, path2: "CHANGELOG.md")
            )
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PathThroughALinkInsideTheRepositoryIsResolvedToWhatItReaches(bool linkedFolder)
    {
        string repository = this.CreateFolder("repository");
        string docs = this.CreateFolder(Path.Combine(path1: "repository", path2: "docs"));
        File.WriteAllText(path: Path.Combine(path1: docs, path2: "CHANGELOG.md"), contents: "# Changelog");
        string changeLog;

        if (linkedFolder)
        {
            Directory.CreateSymbolicLink(path: Path.Combine(path1: repository, path2: "link"), pathToTarget: "docs");
            changeLog = Path.Combine(path1: repository, path2: "link", path3: "CHANGELOG.md");
        }
        else
        {
            changeLog = Path.Combine(path1: repository, path2: "CHANGELOG.md");
            File.CreateSymbolicLink(path: changeLog, pathToTarget: Path.Combine(path1: "docs", path2: "CHANGELOG.md"));
        }

        Assert.Equal(
            expected: "docs/CHANGELOG.md",
            actual: RepositoryRelativePath.Find(
                workingDirectory: repository + Path.DirectorySeparatorChar,
                fullPath: changeLog
            )
        );
    }

    [Theory]
    [InlineData("repository-other")]
    [InlineData("elsewhere")]
    public void PathOutsideTheWorkingFolderIsNotFound(string folder)
    {
        string repository = this.CreateFolder("repository");
        string outside = this.CreateFolder(folder);

        Assert.Null(
            RepositoryRelativePath.Find(
                workingDirectory: repository,
                fullPath: Path.Combine(path1: outside, path2: "CHANGELOG.md")
            )
        );
    }

    private string CreateFolder(string name)
    {
        string folder = Path.Combine(path1: this.Root, path2: name);
        Directory.CreateDirectory(folder);

        return folder;
    }
}
