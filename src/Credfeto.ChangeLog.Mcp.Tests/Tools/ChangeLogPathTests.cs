using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Tools;

public sealed class ChangeLogPathTests : ToolTestBase
{
    private const string REJECTED = "The changelog must be a file named CHANGELOG.md inside the repository";

    public ChangeLogPathTests(ITestOutputHelper output)
        : base(output) { }

    [Theory]
    [InlineData("../CHANGELOG.md")]
    [InlineData("README.md")]
    [InlineData("changelog.md")]
    [InlineData("/etc/CHANGELOG.md")]
    public async Task PathOutsideTheRepositoryOrNotAChangeLogIsRejectedAsync(string changeLog)
    {
        string text = await this.CallFailingAsync(
            name: "changelog_add",
            arguments: [Text("changelog", changeLog), Text("type", "Added"), Text("message", "Something")]
        );

        Assert.Equal(expected: REJECTED, actual: text);
    }

    [Fact]
    public async Task ChangeLogInASubFolderIsAcceptedAsync()
    {
        Directory.CreateDirectory(Path.Combine(path1: this.RepositoryFolder, path2: "docs"));

        string text = await this.CallSuccessfullyAsync(
            name: "changelog_add",
            arguments: [Text("changelog", "docs/CHANGELOG.md"), Text("type", "Added"), Text("message", "Something")]
        );

        Assert.Equal(
            expected: Path.Combine(path1: "docs", path2: CHANGELOG),
            actual: Property(json: text, name: "changeLog").GetString()
        );
    }

    [Fact]
    public async Task FolderLinkIsRejectedAsync()
    {
        string outside = Path.Combine(path1: this.TempFolder, path2: "outside");
        Directory.CreateDirectory(outside);
        Directory.CreateSymbolicLink(
            path: Path.Combine(path1: this.RepositoryFolder, path2: "linked"),
            pathToTarget: outside
        );

        string text = await this.CallFailingAsync(
            name: "changelog_add",
            arguments: [Text("changelog", "linked/CHANGELOG.md"), Text("type", "Added"), Text("message", "Something")]
        );

        Assert.Equal(expected: REJECTED, actual: text);
        Assert.False(
            condition: File.Exists(Path.Combine(path1: outside, path2: CHANGELOG)),
            userMessage: "Nothing should be written through the link"
        );
    }

    [Fact]
    public async Task FileLinkIsRejectedAsync()
    {
        string outside = Path.Combine(path1: this.TempFolder, path2: "target.md");
        await File.WriteAllTextAsync(
            path: outside,
            contents: "# Changelog",
            cancellationToken: this.CancellationToken()
        );
        File.CreateSymbolicLink(path: this.ChangeLogPath, pathToTarget: outside);

        string text = await this.CallFailingAsync(name: "changelog_lint", arguments: []);

        Assert.Equal(expected: REJECTED, actual: text);
    }

    [Fact]
    public async Task UndetectedChangeLogIsAnErrorAsync()
    {
        (string text, bool isError) = await this.CallAsync(
            name: "changelog_unreleased",
            arguments: [],
            detector: new FixedChangeLogDetector(null)
        );

        Assert.True(condition: isError, userMessage: text);
        Assert.Equal(expected: "Could not find changelog", actual: text);
    }

    [Fact]
    public async Task DetectedChangeLogOutsideTheRepositoryIsRejectedAsync()
    {
        (string text, bool isError) = await this.CallAsync(
            name: "changelog_unreleased",
            arguments: [],
            detector: new FixedChangeLogDetector(Path.Combine(path1: this.TempFolder, path2: CHANGELOG))
        );

        Assert.True(condition: isError, userMessage: text);
        Assert.Equal(expected: REJECTED, actual: text);
    }
}
