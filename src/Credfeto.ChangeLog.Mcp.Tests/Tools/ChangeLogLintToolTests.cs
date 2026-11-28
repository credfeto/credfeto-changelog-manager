using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Tools;

public sealed class ChangeLogLintToolTests : ToolTestBase
{
    private const string MISSING_SECTIONS = """
        # Changelog
        All notable changes to this project will be documented in this file.

        <!--
        Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
        -->

        ## [Unreleased]
        ### Added
        - Something

        <!--
        Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
        -->
        ## [0.0.0] - Project created
        """;

    public ChangeLogLintToolTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task ValidChangeLogHasNoErrorsAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "Something");

        string text = await this.CallSuccessfullyAsync(name: "changelog_lint", arguments: []);

        Assert.True(condition: Property(json: text, name: "valid").GetBoolean(), userMessage: text);
        Assert.False(condition: Property(json: text, name: "fixed").GetBoolean(), userMessage: text);
        Assert.Equal(expected: 0, actual: Property(json: text, name: "errors").GetArrayLength());
    }

    [Fact]
    public async Task InvalidChangeLogReportsErrorsAsync()
    {
        await this.WriteChangeLogAsync(MISSING_SECTIONS);

        string text = await this.CallSuccessfullyAsync(name: "changelog_lint", arguments: [Flag("fix", value: false)]);

        Assert.False(condition: Property(json: text, name: "valid").GetBoolean(), userMessage: text);
        JsonElement error = Property(json: text, name: "errors")[0];
        Assert.True(condition: error.GetProperty("lineNumber").GetInt32() > 0, userMessage: text);
        Assert.False(condition: string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()), userMessage: text);
    }

    [Fact]
    public async Task FixRepairsTheChangeLogAsync()
    {
        await this.WriteChangeLogAsync(MISSING_SECTIONS);

        string text = await this.CallSuccessfullyAsync(name: "changelog_lint", arguments: [Flag("fix", value: true)]);

        Assert.True(condition: Property(json: text, name: "fixed").GetBoolean(), userMessage: text);
        Assert.True(condition: Property(json: text, name: "valid").GetBoolean(), userMessage: text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnEntryOverTheLimitIsReportedAndNeverShortenedAsync(bool fix)
    {
        string entry = "- " + new string('a', count: 101);
        int lineNumber = await this.WriteChangeLogWithAddedEntryAsync(entry);

        string text = await this.CallSuccessfullyAsync(name: "changelog_lint", arguments: [Flag("fix", value: fix)]);

        Assert.False(condition: Property(json: text, name: "valid").GetBoolean(), userMessage: text);
        JsonElement error = Assert.Single(Property(json: text, name: "errors").EnumerateArray());
        Assert.Equal(expected: lineNumber, actual: error.GetProperty("lineNumber").GetInt32());
        Assert.Contains(
            expectedSubstring: "101 characters",
            actualString: error.GetProperty("message").GetString(),
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: entry,
            actualString: await this.ReadChangeLogAsync(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task AnEntryOverTheLimitInAReleasedSectionPassesAsync()
    {
        await this.WriteChangeLogWithReleasedEntryAsync("- " + new string('a', count: 150));

        string text = await this.CallSuccessfullyAsync(name: "changelog_lint", arguments: [Flag("fix", value: false)]);

        Assert.True(condition: Property(json: text, name: "valid").GetBoolean(), userMessage: text);
        Assert.Equal(expected: 0, actual: Property(json: text, name: "errors").GetArrayLength());
    }

    // Adds a released version holding the entry just above the template's first release heading
    private async Task WriteChangeLogWithReleasedEntryAsync(string entry)
    {
        await this.CreateChangeLogAsync(this.ChangeLogPath);
        string[] lines = (await this.ReadChangeLogAsync()).ReplaceLineEndings("\n").Split('\n');
        int heading = Array.FindIndex(array: lines, match: line =>
            line.StartsWith(value: "## [0.0.0]", comparisonType: StringComparison.Ordinal)
        );
        Assert.True(condition: heading > 0, userMessage: "No release heading in the template");
        await this.WriteChangeLogAsync(
            string.Join(
                separator: '\n',
                [.. lines[..heading], "## [1.0.0] - 2024-01-01", "### Added", entry, "", .. lines[heading..]]
            )
        );
    }

    // Puts the entry straight under the template's '### Added' heading and returns its line number
    private async Task<int> WriteChangeLogWithAddedEntryAsync(string entry)
    {
        await this.CreateChangeLogAsync(this.ChangeLogPath);
        string[] lines = (await this.ReadChangeLogAsync()).ReplaceLineEndings("\n").Split('\n');
        int heading = Array.FindIndex(array: lines, match: line =>
            StringComparer.Ordinal.Equals(x: line, y: "### Added")
        );
        await this.WriteChangeLogAsync(
            string.Join(separator: '\n', [.. lines[..(heading + 1)], entry, .. lines[(heading + 1)..]])
        );

        return heading + 2;
    }
}
