using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Mcp.Tests.Streams;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

[Collection(SerialEnvironment.Name)]
public sealed class DeletedStartFolderTests : ToolTestBase
{
    private const string CALL_RESPONSE_MARKER = "\"id\":2";

    private const string DETECTING_SESSION = """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
        {"jsonrpc":"2.0","method":"notifications/initialized"}
        {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"changelog_unreleased","arguments":{}}}

        """;

    public DeletedStartFolderTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task ToolCallAfterTheStartFolderIsDeletedIsAnErrorAsync()
    {
        using DeletedCurrentDirectoryScope deleted = new();

        await using StringWriter error = new(CultureInfo.InvariantCulture);
        await using ScriptedInputStream input = new(script: DETECTING_SESSION, waitAtEnd: true);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: StartFolder.TryRead(),
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
            expectedSubstring: "No repository found: the folder the server was started in no longer exists and "
                + RepositoryRootOption.EnvironmentVariable
                + " is not set",
            actualString: output.Text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task RepositoryRootIsServedAfterTheStartFolderIsDeletedAsync()
    {
        await this.AddEntryAsync(type: "Added", message: "Served without a start folder");

        using DeletedCurrentDirectoryScope deleted = new();

        await using StringWriter error = new(CultureInfo.InvariantCulture);
        await using ScriptedInputStream input = new(script: DETECTING_SESSION, waitAtEnd: true);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        int exitCode = await ServerHost.RunAsync(
            workingDirectory: StartFolder.TryRead(),
            repositoryRoot: this.RepositoryFolder,
            error: error,
            input: input,
            output: output,
            stopRequested: output.MarkerWritten
        );

        Assert.Equal(expected: 0, actual: exitCode);
        Assert.Contains(
            expectedSubstring: "Served without a start folder",
            actualString: output.Text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Empty(error.ToString());
    }
}
