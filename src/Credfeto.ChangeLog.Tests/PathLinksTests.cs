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
