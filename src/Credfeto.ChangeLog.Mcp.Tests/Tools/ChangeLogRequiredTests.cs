using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Tools;

public sealed class ChangeLogRequiredTests : ToolTestBase
{
    public ChangeLogRequiredTests(ITestOutputHelper output)
        : base(output)
    {
        this.Elsewhere = Path.Combine(path1: this.TempFolder, path2: "elsewhere");
        Directory.CreateDirectory(this.Elsewhere);
    }

    // A folder beside the test repository, so no .git is found at or above it
    private string Elsewhere { get; }

    [Theory]
    [InlineData("changelog_add", false)]
    [InlineData("changelog_add", true)]
    [InlineData("changelog_remove", false)]
    [InlineData("changelog_remove", true)]
    [InlineData("changelog_unreleased", false)]
    [InlineData("changelog_unreleased", true)]
    [InlineData("changelog_extract", false)]
    [InlineData("changelog_extract", true)]
    [InlineData("changelog_create_release", false)]
    [InlineData("changelog_create_release", true)]
    [InlineData("changelog_check_insert", false)]
    [InlineData("changelog_check_insert", true)]
    [InlineData("changelog_lint", false)]
    [InlineData("changelog_lint", true)]
    public async Task OutsideARepositoryEveryToolIsAnErrorAndCreatesNothingAsync(string name, bool changeLogArgument)
    {
        (string text, bool isError) = await this.CallAsync(
            name: name,
            arguments: Arguments(name: name, changeLogArgument: changeLogArgument),
            detector: new FixedChangeLogDetector(Path.Combine(path1: this.Elsewhere, path2: CHANGELOG)),
            workingDirectory: this.Elsewhere
        );

        Assert.True(condition: isError, userMessage: text);
        Assert.Equal(
            expected: "No repository found: \""
                + this.Elsewhere
                + "\" is not inside a git repository and "
                + RepositoryRootOption.EnvironmentVariable
                + " is not set",
            actual: text
        );
        Assert.Empty(Directory.EnumerateFileSystemEntries(this.Elsewhere));
    }

    [Theory]
    [InlineData("changelog_add", false)]
    [InlineData("changelog_add", true)]
    [InlineData("changelog_remove", false)]
    [InlineData("changelog_remove", true)]
    [InlineData("changelog_create_release", false)]
    [InlineData("changelog_create_release", true)]
    [InlineData("changelog_lint", false)]
    [InlineData("changelog_lint", true)]
    public async Task WriteToolInARepositoryWithoutAChangeLogIsAnErrorAndCreatesNothingAsync(
        string name,
        bool changeLogArgument
    )
    {
        string text = await this.CallFailingAsync(
            name: name,
            arguments: Arguments(name: name, changeLogArgument: changeLogArgument)
        );

        Assert.Equal(expected: "No CHANGELOG.md at \"" + this.RepositoryFolder + "\"", actual: text);
        Assert.False(condition: File.Exists(this.ChangeLogPath), userMessage: "No changelog should be created");
    }

    [Fact]
    public async Task RepositoryRootSetWhileStartedElsewhereWritesToThatRepositoryAsync()
    {
        await this.CreateChangeLogAsync(this.ChangeLogPath);

        await using ServiceProvider services = new ServiceCollection()
            .AddChangeLogMcpServer(workingDirectory: this.Elsewhere, repositoryRoot: this.RepositoryFolder)
            .BuildServiceProvider();

        (string text, bool isError) = await this.CallToolAsync(
            services: services,
            name: "changelog_add",
            arguments: Arguments(name: "changelog_add", changeLogArgument: false)
        );

        Assert.False(condition: isError, userMessage: text);
        Assert.Contains(
            expectedSubstring: "- Something",
            actualString: await this.ReadChangeLogAsync(),
            comparisonType: StringComparison.Ordinal
        );
        Assert.Empty(Directory.EnumerateFileSystemEntries(this.Elsewhere));
    }

    private static IReadOnlyList<KeyValuePair<string, JsonElement>> Arguments(string name, bool changeLogArgument)
    {
        List<KeyValuePair<string, JsonElement>> arguments = changeLogArgument ? [Text("changelog", CHANGELOG)] : [];

        switch (name)
        {
            case "changelog_add":
            case "changelog_remove":
                arguments.Add(Text("type", "Added"));
                arguments.Add(Text("message", "Something"));

                break;
            case "changelog_extract":
            case "changelog_create_release":
                arguments.Add(Text("version", "1.0.0"));

                break;
            case "changelog_lint":
                arguments.Add(Flag("fix", true));

                break;
            default:
                break;
        }

        return arguments;
    }
}
