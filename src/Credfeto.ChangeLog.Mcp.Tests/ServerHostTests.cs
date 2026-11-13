using System;
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

    public ServerHostTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task ServerEndsWhenInputClosesAsync()
    {
        await using ScriptedInputStream input = new(script: string.Empty, waitAtEnd: false);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: this.RepositoryFolder,
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

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: this.RepositoryFolder,
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
}
