using System.IO;
using Credfeto.ChangeLog.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class PhysicalPathTests : LoggingFolderCleanupTestBase
{
    public PhysicalPathTests(ITestOutputHelper output)
        : base(output)
    {
        this.Root = PhysicalPath.Resolve(this.TempFolder);
    }

    private string Root { get; }

    [Fact]
    public void RealFolderResolvesToItself()
    {
        string folder = this.CreateFolder("real");

        Assert.Equal(expected: folder, actual: PhysicalPath.Resolve(folder + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void MissingPathResolvesToTheSamePath()
    {
        string missing = Path.Combine(path1: this.Root, path2: "missing", path3: "CHANGELOG.md");

        Assert.Equal(expected: missing, actual: PhysicalPath.Resolve(missing));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LinkedFolderResolvesToItsTarget(bool relativeTarget)
    {
        string target = this.CreateFolder("target");
        string link = Path.Combine(path1: this.Root, path2: "link");
        Directory.CreateSymbolicLink(path: link, pathToTarget: relativeTarget ? "target" : target);

        Assert.Equal(
            expected: Path.Combine(path1: target, path2: "CHANGELOG.md"),
            actual: PhysicalPath.Resolve(Path.Combine(path1: link, path2: "CHANGELOG.md"))
        );
    }

    [Fact]
    public void RelativeTargetIsFollowedFromTheFolderTheLinkPhysicallySitsIn()
    {
        // start/root -> through-folder/link -> ../target, where through-folder is a link to linked-folder:
        // ../target is the target beside linked-folder, not start/target beside the through-folder link.
        string target = this.CreateFolder("target");
        string linkedFolder = this.CreateFolder("linked-folder");
        string start = this.CreateFolder("start");
        this.CreateFolder(Path.Combine(path1: "start", path2: "target"));
        Directory.CreateSymbolicLink(
            path: Path.Combine(path1: start, path2: "through-folder"),
            pathToTarget: linkedFolder
        );
        Directory.CreateSymbolicLink(
            path: Path.Combine(path1: linkedFolder, path2: "link"),
            pathToTarget: Path.Combine(path1: "..", path2: "target")
        );
        string root = Path.Combine(path1: start, path2: "root");
        Directory.CreateSymbolicLink(path: root, pathToTarget: Path.Combine(path1: "through-folder", path2: "link"));

        Assert.Equal(expected: target, actual: PhysicalPath.Resolve(root));
    }

    [Fact]
    public void CurrentFolderSegmentsInALinkTargetAreIgnored()
    {
        string target = this.CreateFolder("target");
        string link = Path.Combine(path1: this.Root, path2: "link");
        Directory.CreateSymbolicLink(path: link, pathToTarget: Path.Combine(path1: ".", path2: "target", path3: "."));

        Assert.Equal(expected: target, actual: PhysicalPath.Resolve(link));
    }

    [Fact]
    public void LinkToItselfIsRejected()
    {
        string link = Path.Combine(path1: this.Root, path2: "loop");
        Directory.CreateSymbolicLink(path: link, pathToTarget: link);

        IOException exception = Assert.Throws<IOException>(() => PhysicalPath.Resolve(link));

        Assert.Equal(expected: "Too many levels of symbolic links: \"" + link + "\"", actual: exception.Message);
    }

    private string CreateFolder(string name)
    {
        string folder = Path.Combine(path1: this.Root, path2: name);
        Directory.CreateDirectory(folder);

        return folder;
    }
}
