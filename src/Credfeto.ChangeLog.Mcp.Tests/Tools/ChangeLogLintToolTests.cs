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
}
