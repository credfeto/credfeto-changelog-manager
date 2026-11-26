using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Tests.Streams;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

public sealed class ServerHostTests : ToolTestBase
{
    private const string CALL_RESPONSE_MARKER = "\"id\":2";

    private const string SESSION = """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
        {"jsonrpc":"2.0","method":"notifications/initialized"}
        {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"changelog_unreleased","arguments":{"changelog":"CHANGELOG.md"}}}

        """;

    private const string DETECTING_SESSION = """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
        {"jsonrpc":"2.0","method":"notifications/initialized"}
        {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"changelog_unreleased","arguments":{}}}

        """;

    private const string ADDING_SESSION = """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
        {"jsonrpc":"2.0","method":"notifications/initialized"}
        {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"changelog_add","arguments":{"changelog":"CHANGELOG.md","type":"Added","message":"Written outside"}}}

        """;

    private const string LISTING_SESSION = """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
        {"jsonrpc":"2.0","method":"notifications/initialized"}
        {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}

        """;

    public ServerHostTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task ToolsAreListedInNameOrderAsync()
    {
        await using ScriptedInputStream input = new(script: LISTING_SESSION, waitAtEnd: true);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        await using StringWriter error = new(CultureInfo.InvariantCulture);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: this.RepositoryFolder,
            repositoryRoot: null,
            error: error,
            input: input,
            output: output,
            stopRequested: output.MarkerWritten
        );

        Assert.Equal(expected: 0, actual: exitCode);
        Assert.Equal(
            expected: ToolNamesInOrder,
            actual: ListedToolNames(output.Text),
            comparer: StringComparer.Ordinal
        );
    }

    private static IReadOnlyList<string> ListedToolNames(string responses)
    {
        string response = responses
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Single(line => line.Contains(value: CALL_RESPONSE_MARKER, comparisonType: StringComparison.Ordinal));

        using JsonDocument document = JsonDocument.Parse(response);

        return
        [
            .. document
                .RootElement.GetProperty("result")
                .GetProperty("tools")
                .EnumerateArray()
                .Select(tool => tool.GetProperty("name").GetString() ?? string.Empty),
        ];
    }

    [Fact]
    public async Task ServerEndsWhenInputClosesAsync()
    {
        await using ScriptedInputStream input = new(script: string.Empty, waitAtEnd: false);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        await using StringWriter error = new(CultureInfo.InvariantCulture);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: this.RepositoryFolder,
            repositoryRoot: null,
            error: error,
            input: input,
            output: output,
            stopRequested: this.CancellationToken()
        );

        Assert.Equal(expected: 0, actual: exitCode);
    }

    [Fact]
    public async Task ToolCallIsAnsweredBeforeStopAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "Served over the protocol");

        await using ScriptedInputStream input = new(script: SESSION, waitAtEnd: true);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        await using StringWriter error = new(CultureInfo.InvariantCulture);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: this.RepositoryFolder,
            repositoryRoot: null,
            error: error,
            input: input,
            output: output,
            stopRequested: output.MarkerWritten
        );

        Assert.Equal(expected: 0, actual: exitCode);
        Assert.Contains(
            expectedSubstring: "Served over the protocol",
            actualString: output.Text,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RepositoryRootIsServedWhenStartedElsewhereAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "Served from the configured root");

        string elsewhere = Path.Combine(path1: this.TempFolder, path2: "elsewhere");
        Directory.CreateDirectory(elsewhere);

        await using StringWriter error = new(CultureInfo.InvariantCulture);
        await using ScriptedInputStream input = new(script: DETECTING_SESSION, waitAtEnd: true);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: elsewhere,
            repositoryRoot: this.RepositoryFolder,
            error: error,
            input: input,
            output: output,
            stopRequested: output.MarkerWritten
        );

        Assert.Equal(expected: 0, actual: exitCode);
        Assert.Contains(
            expectedSubstring: "Served from the configured root",
            actualString: output.Text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task ToolCallStartedOutsideARepositoryIsAnErrorAndCreatesNothingAsync()
    {
        string elsewhere = Path.Combine(path1: this.TempFolder, path2: "elsewhere");
        Directory.CreateDirectory(elsewhere);

        await using StringWriter error = new(CultureInfo.InvariantCulture);
        await using ScriptedInputStream input = new(script: ADDING_SESSION, waitAtEnd: true);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: elsewhere,
            repositoryRoot: null,
            error: error,
            input: input,
            output: output,
            stopRequested: output.MarkerWritten
        );

        Assert.Equal(expected: 0, actual: exitCode);
        Assert.Contains(
            expectedSubstring: "\"isError\":true",
            actualString: output.Text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "No repository found",
            actualString: output.Text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: elsewhere,
            actualString: output.Text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Empty(Directory.EnumerateFileSystemEntries(elsewhere));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidRepositoryRootStopsBeforeServingWhetherOrNotAlreadyStoppedAsync(bool alreadyStopped)
    {
        await using StringWriter error = new(CultureInfo.InvariantCulture);
        await using ScriptedInputStream input = new(script: SESSION, waitAtEnd: false);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: this.RepositoryFolder,
            repositoryRoot: "relative/repo",
            error: error,
            input: input,
            output: output,
            stopRequested: alreadyStopped ? new CancellationToken(canceled: true) : this.CancellationToken()
        );

        Assert.NotEqual(expected: 0, actual: exitCode);
        Assert.Equal(
            expected: RepositoryRootOption.EnvironmentVariable
                + " must be an absolute path: \"relative/repo\""
                + Environment.NewLine,
            actual: error.ToString()
        );
        Assert.Empty(output.Text);
    }
}
