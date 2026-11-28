using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests;

public sealed class ProgramEntryLengthTests : TestBase
{
    private const string UNRELEASED_ENTRY = "{unreleased}";
    private const string RELEASED_ENTRY = "{released}";

    private const string CHANGE_LOG_TEMPLATE = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        {unreleased}
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [0.0.1] - 2024-01-01
        ### Added
        {released}
        """;

    private static string ChangeLog(string unreleased, string released)
    {
        return CHANGE_LOG_TEMPLATE
            .Replace(oldValue: UNRELEASED_ENTRY, newValue: unreleased, comparisonType: StringComparison.Ordinal)
            .Replace(oldValue: RELEASED_ENTRY, newValue: released, comparisonType: StringComparison.Ordinal);
    }

    private static string Text(int length)
    {
        return new('a', count: length);
    }

    private static async Task<string> CreateTempChangeLogAsync(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        await File.WriteAllTextAsync(
            path: path,
            contents: content,
            encoding: Encoding.UTF8,
            cancellationToken: Xunit.TestContext.Current.CancellationToken
        );

        return path;
    }

    private static Task<string> ReadAsync(string path)
    {
        return File.ReadAllTextAsync(path: path, cancellationToken: Xunit.TestContext.Current.CancellationToken);
    }

    private static Task<int> AddAsync(string path, string message)
    {
        return Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", path, "--add", "Added", "--message", message]);
    }

    [Fact]
    public static async Task Main_AddEntryOfExactlyTheLimit_ReturnsSuccessAndAddsTheEntryAsync()
    {
        string tempFile = await CreateTempChangeLogAsync(
            ChangeLog(unreleased: "- Feature", released: "- Initial release")
        );

        try
        {
            int result = await AddAsync(path: tempFile, message: Text(100));

            Assert.Equal(expected: 0, actual: result);
            Assert.Contains(
                expectedSubstring: "- " + Text(100),
                actualString: await ReadAsync(tempFile),
                comparisonType: StringComparison.Ordinal
            );
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public static async Task Main_AddEntryOverTheLimit_ReturnsErrorAndLeavesTheFileUnchangedAsync()
    {
        string tempFile = await CreateTempChangeLogAsync(
            ChangeLog(unreleased: "- Feature", released: "- Initial release")
        );

        try
        {
            string before = await ReadAsync(tempFile);

            int result = await AddAsync(path: tempFile, message: Text(101));

            Assert.Equal(expected: 1, actual: result);
            Assert.Equal(expected: before, actual: await ReadAsync(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(101, 100, 1)]
    [InlineData(100, 101, 0)]
    [InlineData(100, 500, 0)]
    [InlineData(101, 150, 1)]
    public static async Task Main_Lint_FailsOnlyWhenAnUnreleasedEntryIsOverTheLimitAsync(
        int unreleasedLength,
        int releasedLength,
        int expectedResult
    )
    {
        string tempFile = await CreateTempChangeLogAsync(
            ChangeLog(unreleased: "- " + Text(unreleasedLength), released: "- " + Text(releasedLength))
        );

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--lint"]);

            Assert.Equal(expected: expectedResult, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public static async Task Main_LintWithFixAndAnEntryOverTheLimit_ReturnsErrorAndKeepsTheEntryAsync()
    {
        string tempFile = await CreateTempChangeLogAsync(
            ChangeLog(unreleased: "- " + Text(101), released: "- Initial release")
        );

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--lint", "--fix"]);

            Assert.Equal(expected: 1, actual: result);
            Assert.Contains(
                expectedSubstring: "- " + Text(101),
                actualString: await ReadAsync(tempFile),
                comparisonType: StringComparison.Ordinal
            );
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
