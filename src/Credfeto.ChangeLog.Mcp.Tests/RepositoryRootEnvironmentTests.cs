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
        using RepositoryRootEnvironmentScope environment = new("/configured/root");

        Assert.Equal(expected: "/configured/root", actual: RepositoryRootOption.Read());
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("relative/repo", 1)]
    public static async Task ServerOwningStreamsExitCodeReflectsTheRootFromTheEnvironmentAsync(
        string? configuredRoot,
        int expectedExitCode
    )
    {
        using RepositoryRootEnvironmentScope environment = new(configuredRoot);

        await using ScriptedInputStream input = new(script: string.Empty, waitAtEnd: false);
        await using CapturingOutputStream output = new(CALL_RESPONSE_MARKER);

        int exitCode = await ServerHost.RunAsync(input: input, output: output);

        Assert.Equal(expected: expectedExitCode, actual: exitCode);
    }
}
