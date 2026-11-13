using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Tools;

public sealed class ToolArgumentsTests : ToolTestBase
{
    public ToolArgumentsTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task UnknownParameterIsAnErrorAsync()
    {
        string text = await this.CallFailingAsync(name: "changelog_lint", arguments: [Text("colour", "red")]);

        Assert.Equal(expected: "Unknown parameter; this tool takes: changelog, additionalSections, fix", actual: text);
    }

    [Fact]
    public async Task NonStringValueIsAnErrorAsync()
    {
        string text = await this.CallFailingAsync(
            name: "changelog_add",
            arguments: [Number("type", 1), Text("message", "Something")]
        );

        Assert.Equal(expected: "Invalid value for type; expected a string", actual: text);
    }

    [Fact]
    public async Task NonBooleanFlagIsAnErrorAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "Something");

        string text = await this.CallFailingAsync(name: "changelog_lint", arguments: [Text("fix", "yes")]);

        Assert.Equal(expected: "Invalid value for fix; expected true or false", actual: text);
    }

    [Fact]
    public async Task NullValueIsTreatedAsNotSuppliedAsync()
    {
        string text = await this.CallSuccessfullyAsync(
            name: "changelog_add",
            arguments:
            [
                Null("changelog"),
                Null("additionalSections"),
                Text("type", "Added"),
                Text("message", "Something"),
            ]
        );

        Assert.Equal(expected: CHANGELOG, actual: Property(json: text, name: "changeLog").GetString());
    }

    [Fact]
    public async Task NoArgumentsAtAllIsAMissingParameterErrorAsync()
    {
        await using ServiceProvider services = this.BuildServices();

        CallToolResult result = await GetTool(services: services, name: "changelog_add")
            .CallAsync(arguments: null, cancellationToken: this.CancellationToken());

        Assert.True(condition: result.IsError ?? false, userMessage: "Expected an error result");
        string text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.StartsWith(
            expectedStartString: "Missing required parameter",
            actualString: text,
            comparisonType: StringComparison.Ordinal
        );
    }
}
