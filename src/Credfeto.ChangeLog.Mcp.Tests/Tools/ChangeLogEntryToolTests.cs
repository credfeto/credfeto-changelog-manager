using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Tools;

public sealed class ChangeLogEntryToolTests : ToolTestBase
{
    public ChangeLogEntryToolTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task AddAddsTheEntryAsync()
    {
        string text = await this.AddEntryAsync(type: "Added", message: "A new feature");

        Assert.Equal(expected: CHANGELOG, actual: Property(json: text, name: "changeLog").GetString());
        Assert.Equal(expected: "Added", actual: Property(json: text, name: "type").GetString());
        Assert.Equal(expected: "A new feature", actual: Property(json: text, name: "message").GetString());

        string content = await this.ReadChangeLogAsync();
        Assert.Contains(
            expectedSubstring: "- A new feature",
            actualString: content,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RemoveRemovesTheEntryAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "A new feature");

        await this.CallSuccessfullyAsync(
            name: "changelog_remove",
            arguments: [Text("changelog", CHANGELOG), Text("type", "Added"), Text("message", "A new")]
        );

        string content = await this.ReadChangeLogAsync();
        Assert.DoesNotContain(
            expectedSubstring: "A new feature",
            actualString: content,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task AddToAnUnknownSectionIsAnErrorAsync()
    {
        await this.CreateChangeLogAsync(this.ChangeLogPath);

        string text = await this.CallFailingAsync(
            name: "changelog_add",
            arguments: [Text("type", "Bananas"), Text("message", "Patched")]
        );

        Assert.Contains(expectedSubstring: "Bananas", actualString: text, comparisonType: StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddToAnAdditionalSectionSucceedsAsync()
    {
        await this.CreateChangeLogAsync(fullPath: this.ChangeLogPath, additionalSections: ["Bananas", "Other"]);

        await this.CallSuccessfullyAsync(
            name: "changelog_add",
            arguments:
            [
                Text("additionalSections", " Bananas , ,Other"),
                Text("type", "Bananas"),
                Text("message", "Patched"),
            ]
        );

        string content = await this.ReadChangeLogAsync();
        Assert.Contains(
            expectedSubstring: "- Patched",
            actualString: content,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task MissingMessageIsAnErrorAsync()
    {
        await this.CreateChangeLogAsync(this.ChangeLogPath);
        string before = await this.ReadChangeLogAsync();

        string text = await this.CallFailingAsync(name: "changelog_add", arguments: [Text("type", "Added")]);

        Assert.Equal(expected: "Missing required parameter message", actual: text);
        Assert.Equal(expected: before, actual: await this.ReadChangeLogAsync());
    }

    [Fact]
    public async Task AddAnEntryOfExactlyTheLimitSucceedsAsync()
    {
        string message = new('a', count: 100);

        await this.AddEntryAsync(type: "Added", message: message);

        string content = await this.ReadChangeLogAsync();
        Assert.Contains(
            expectedSubstring: "- " + message,
            actualString: content,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task AddAnEntryOverTheLimitIsAnErrorAndLeavesTheFileUnchangedAsync()
    {
        await this.CreateChangeLogAsync(this.ChangeLogPath);
        string before = await this.ReadChangeLogAsync();

        string text = await this.CallFailingAsync(
            name: "changelog_add",
            arguments: [Text("type", "Added"), Text("message", new('a', count: 101))]
        );

        Assert.Contains(
            expectedSubstring: "101 characters",
            actualString: text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "limit is 100 characters",
            actualString: text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Equal(expected: before, actual: await this.ReadChangeLogAsync());
    }

    [Fact]
    public void AddDescriptionsStateTheLimit()
    {
        using ServiceProvider services = this.BuildServices();

        Tool add = GetTool(services: services, name: "changelog_add").ProtocolTool;
        string? messageDescription = add
            .InputSchema.GetProperty("properties")
            .GetProperty("message")
            .GetProperty("description")
            .GetString();

        Assert.Contains(
            expectedSubstring: "one line, at most 100 characters",
            actualString: add.Description,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "one line, at most 100 characters",
            actualString: messageDescription,
            comparisonType: StringComparison.Ordinal
        );
    }
}
