using System.IO;
using Credfeto.ChangeLog.Mcp.Exceptions;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

public sealed class RepositoryRootOptionTests : ToolTestBase
{
    public RepositoryRootOptionTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public static void EnvironmentVariableNameIsTheDocumentedName()
    {
        Assert.Equal(expected: "CHANGELOG_MCP_REPO_ROOT", actual: RepositoryRootOption.EnvironmentVariable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public static void UnsetEmptyOrWhitespaceMeansNoRoot(string? value)
    {
        Assert.Null(RepositoryRootOption.Parse(value));
    }

    [Fact]
    public void AbsoluteExistingDirectoryIsTheRoot()
    {
        Assert.Equal(expected: this.RepositoryFolder, actual: RepositoryRootOption.Parse(this.RepositoryFolder));
    }

    [Theory]
    [InlineData("repo", false)]
    // A leading space fails the absolute-path check on every OS; Windows would strip a trailing one
    [InlineData(" ", true)]
    public void NonAbsolutePathIsRejectedWithoutTrimming(string prefix, bool appendRepositoryFolder)
    {
        string path = appendRepositoryFolder ? prefix + this.RepositoryFolder : prefix;

        RepositoryRootException exception = Assert.Throws<RepositoryRootException>(() =>
            RepositoryRootOption.Parse(path)
        );

        Assert.Equal(
            expected: $"CHANGELOG_MCP_REPO_ROOT must be an absolute path: \"{path}\"",
            actual: exception.Message
        );
    }

    [Theory]
    [InlineData("missing", false)]
    [InlineData(CHANGELOG, true)]
    public void PathThatIsNotAnExistingDirectoryIsRejected(string name, bool existsAsFile)
    {
        string path = Path.Combine(path1: this.RepositoryFolder, path2: name);

        if (existsAsFile)
        {
            File.WriteAllText(path: path, contents: "# Changelog");
        }

        RepositoryRootException exception = Assert.Throws<RepositoryRootException>(() =>
            RepositoryRootOption.Parse(path)
        );

        Assert.Equal(
            expected: $"CHANGELOG_MCP_REPO_ROOT must name an existing directory: \"{path}\"",
            actual: exception.Message
        );
    }
}
