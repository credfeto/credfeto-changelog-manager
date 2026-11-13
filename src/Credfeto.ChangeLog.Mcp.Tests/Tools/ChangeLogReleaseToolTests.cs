using System;
using System.Threading.Tasks;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Tools;

public sealed class ChangeLogReleaseToolTests : ToolTestBase
{
    public ChangeLogReleaseToolTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task UnreleasedReturnsTheUnreleasedContentAsync()
    {
        await this.AddEntryAsync(type: "Fixed", message: "A bug");

        string text = await this.CallSuccessfullyAsync(name: "changelog_unreleased", arguments: []);

        Assert.Equal(expected: CHANGELOG, actual: Property(json: text, name: "changeLog").GetString());
        Assert.Contains(
            expectedSubstring: "- A bug",
            actualString: Property(json: text, name: "content").GetString(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task CreateReleaseThenExtractReturnsTheReleaseNotesAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "A new feature");

        string release = await this.CallSuccessfullyAsync(
            name: "changelog_create_release",
            arguments: [Text("version", "1.2.3"), Flag("pending", value: true)]
        );

        Assert.Equal(expected: "1.2.3", actual: Property(json: release, name: "version").GetString());
        Assert.True(condition: Property(json: release, name: "pending").GetBoolean(), userMessage: release);

        string notes = await this.CallSuccessfullyAsync(
            name: "changelog_extract",
            arguments: [Text("version", "1.2.3")]
        );

        Assert.Contains(
            expectedSubstring: "- A new feature",
            actualString: notes,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task CreateReleaseWithNoChangesIsAnErrorAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "A new feature");
        await this.CallSuccessfullyAsync(
            name: "changelog_remove",
            arguments: [Text("type", "Added"), Text("message", "A new feature")]
        );

        string text = await this.CallFailingAsync(
            name: "changelog_create_release",
            arguments: [Text("version", "1.0.0")]
        );

        Assert.Equal(expected: "No changes for the release", actual: text);
    }

    [Fact]
    public async Task CreateReleaseThatAlreadyExistsIsAnErrorAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "First");
        await this.CallSuccessfullyAsync(name: "changelog_create_release", arguments: [Text("version", "1.0.0")]);
        await this.AddEntryAsync(type: "Added", message: "Second");

        await this.CallFailingAsync(name: "changelog_create_release", arguments: [Text("version", "1.0.0")]);
    }

    [Fact]
    public async Task CreateReleaseOlderThanTheLatestIsAnErrorAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "First");
        await this.CallSuccessfullyAsync(name: "changelog_create_release", arguments: [Text("version", "2.0.0")]);
        await this.AddEntryAsync(type: "Added", message: "Second");

        await this.CallFailingAsync(name: "changelog_create_release", arguments: [Text("version", "1.0.0")]);
    }

    [Fact]
    public Task ExtractFromAMissingChangeLogIsAnErrorAsync()
    {
        return this.CallFailingAsync(name: "changelog_extract", arguments: [Text("version", "1.0.0")]);
    }

    [Fact]
    public Task CreateReleaseWithoutAVersionIsAnErrorAsync()
    {
        return this.CallFailingAsync(name: "changelog_create_release", arguments: [Text("version", " ")]);
    }
}
