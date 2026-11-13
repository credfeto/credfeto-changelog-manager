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

    [Fact]
    public void RelativePathIsRejected()
    {
        RepositoryRootException exception = Assert.Throws<RepositoryRootException>(() =>
            RepositoryRootOption.Parse("repo")
        );

        Assert.Equal(expected: "CHANGELOG_MCP_REPO_ROOT must be an absolute path: repo", actual: exception.Message);
    }

    [Fact]
    public void MissingDirectoryIsRejected()
    {
        string missing = Path.Combine(path1: this.TempFolder, path2: "missing");

        RepositoryRootException exception = Assert.Throws<RepositoryRootException>(() =>
            RepositoryRootOption.Parse(missing)
        );

        Assert.Equal(
            expected: "CHANGELOG_MCP_REPO_ROOT must name an existing directory: " + missing,
            actual: exception.Message
        );
    }

    [Fact]
    public void FileIsRejected()
    {
        File.WriteAllText(path: this.ChangeLogPath, contents: "# Changelog");

        RepositoryRootException exception = Assert.Throws<RepositoryRootException>(() =>
            RepositoryRootOption.Parse(this.ChangeLogPath)
        );

        Assert.Equal(
            expected: "CHANGELOG_MCP_REPO_ROOT must name an existing directory: " + this.ChangeLogPath,
            actual: exception.Message
        );
    }
}
