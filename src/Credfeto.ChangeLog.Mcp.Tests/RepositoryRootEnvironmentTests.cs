using System;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Tests.Streams;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

[Collection(SerialEnvironment.Name)]
public sealed class RepositoryRootEnvironmentTests : TestBase
{
    private const string CALL_RESPONSE_MARKER = "\"id\":2";

    [Fact]
    public void ReadReturnsTheEnvironmentVariable()
    {
        string? original = Environment.GetEnvironmentVariable("CHANGELOG_MCP_REPO_ROOT");

        try
        {
            Environment.SetEnvironmentVariable(variable: "CHANGELOG_MCP_REPO_ROOT", value: "/configured/root");

            Assert.Equal(expected: "/configured/root", actual: RepositoryRootOption.Read());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable: "CHANGELOG_MCP_REPO_ROOT", value: original);
        }
    }

    [Fact]
    public async Task ServerOwningStreamsEndsWhenInputClosesAsync()
    {
        string? original = Environment.GetEnvironmentVariable("CHANGELOG_MCP_REPO_ROOT");

        try
        {
            Environment.SetEnvironmentVariable(variable: "CHANGELOG_MCP_REPO_ROOT", value: null);

            await using ScriptedInputStream input = new(script: string.Empty, waitAtEnd: false);
            await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

            int exitCode = await ServerHost.RunAsync(input: input, output: output);

            Assert.Equal(expected: 0, actual: exitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable: "CHANGELOG_MCP_REPO_ROOT", value: original);
        }
    }

    [Fact]
    public async Task ServerOwningStreamsRejectsAnInvalidRootFromTheEnvironmentAsync()
    {
        string? original = Environment.GetEnvironmentVariable("CHANGELOG_MCP_REPO_ROOT");

        try
        {
            Environment.SetEnvironmentVariable(variable: "CHANGELOG_MCP_REPO_ROOT", value: "relative/repo");

            await using ScriptedInputStream input = new(script: string.Empty, waitAtEnd: false);
            await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

            int exitCode = await ServerHost.RunAsync(input: input, output: output);

            Assert.Equal(expected: 1, actual: exitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable: "CHANGELOG_MCP_REPO_ROOT", value: original);
        }
    }
}
