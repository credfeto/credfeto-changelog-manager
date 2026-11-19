using System;
using System.IO;
using Credfeto.ChangeLog.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class PathLinksTests : LoggingFolderCleanupTestBase
{
    public PathLinksTests(ITestOutputHelper output)
        : base(output)
    {
        this.Root = PhysicalPath.Resolve(this.TempFolder);
    }

    private string Root { get; }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PathWithNoLinkBelowTheRootHasNoLink(bool trailingSeparator)
    {
        string docs = this.CreateFolder("docs");
        string changeLog = Path.Combine(path1: docs, path2: "CHANGELOG.md");
        File.WriteAllText(path: changeLog, contents: "# Changelog");

        Assert.False(
            PathLinks.HasLinkBelow(root: this.RootSpelt(trailingSeparator), path: changeLog),
            userMessage: "A path with no link below the root should not report a link"
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PathThroughALinkedFolderBelowTheRootHasALink(bool trailingSeparator)
    {
        this.CreateFolder("docs");
        Directory.CreateSymbolicLink(path: Path.Combine(path1: this.Root, path2: "link"), pathToTarget: "docs");

        Assert.True(
            PathLinks.HasLinkBelow(
                root: this.RootSpelt(trailingSeparator),
                path: Path.Combine(path1: this.Root, path2: "link", path3: "CHANGELOG.md")
            ),
            userMessage: "A path through a linked folder below the root should report a link"
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PathThatIsItselfALinkHasALink(bool trailingSeparator)
    {
        string docs = this.CreateFolder("docs");
        File.WriteAllText(path: Path.Combine(path1: docs, path2: "CHANGELOG.md"), contents: "# Changelog");
        string changeLog = Path.Combine(path1: this.Root, path2: "CHANGELOG.md");
        File.CreateSymbolicLink(path: changeLog, pathToTarget: Path.Combine(path1: "docs", path2: "CHANGELOG.md"));

        Assert.True(
            PathLinks.HasLinkBelow(root: this.RootSpelt(trailingSeparator), path: changeLog),
            userMessage: "A path that is itself a link should report a link"
        );
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PathThatIsTheRootHasNoLinkBelowIt(bool rootTrailingSeparator, bool pathTrailingSeparator)
    {
        string link = Path.Combine(path1: this.Root, path2: "link");
        Directory.CreateSymbolicLink(path: link, pathToTarget: this.CreateFolder("docs"));
        string root = rootTrailingSeparator ? link + Path.DirectorySeparatorChar : link;
        string path = pathTrailingSeparator ? link + Path.DirectorySeparatorChar : link;

        Assert.False(
            PathLinks.HasLinkBelow(root: root, path: path),
            userMessage: "The root itself is never checked, so a path that is the root has no link below it"
        );
    }

    [Fact]
    public void NegativeRelativeStartIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathLinks.HasLinkBelow(path: this.Root, relativeStart: -1));
    }

    private string RootSpelt(bool trailingSeparator)
    {
        return trailingSeparator ? this.Root + Path.DirectorySeparatorChar : this.Root;
    }

    private string CreateFolder(string name)
    {
        string folder = Path.Combine(path1: this.Root, path2: name);
        Directory.CreateDirectory(folder);

        return folder;
    }
}
