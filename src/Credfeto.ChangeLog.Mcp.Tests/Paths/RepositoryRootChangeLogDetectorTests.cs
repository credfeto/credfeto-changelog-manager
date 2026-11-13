using System.IO;
using Credfeto.ChangeLog.Mcp.Paths;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Paths;

public sealed class RepositoryRootChangeLogDetectorTests : ToolTestBase
{
    public RepositoryRootChangeLogDetectorTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void ChangeLogAtTheRootIsFound()
    {
        File.WriteAllText(path: this.ChangeLogPath, contents: "# Changelog");

        RepositoryRootChangeLogDetector detector = new(this.RepositoryFolder);

        Assert.True(detector.TryFindChangeLog(out string? found), userMessage: "Root changelog should be found");
        Assert.Equal(expected: this.ChangeLogPath, actual: found);
    }

    [Fact]
    public void NestedChangeLogIsNotFound()
    {
        string docs = Path.Combine(path1: this.RepositoryFolder, path2: "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(path: Path.Combine(path1: docs, path2: CHANGELOG), contents: "# Changelog");

        RepositoryRootChangeLogDetector detector = new(this.RepositoryFolder);

        Assert.False(detector.TryFindChangeLog(out string? found), userMessage: "Only the root is searched");
        Assert.Null(found);
    }
}
