using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogYankedReleaseTests : TestBase
{
    private const string YANKED_CHANGE_LOG = """
        # Changelog

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [1.1.0] - 2024-02-01 [YANKED]
        ### Added
        - Yanked feature

        ## [1.0.0] - 2024-01-01
        ### Added
        - Initial release

        ## [0.0.0] - Project created
        """;

    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        return ChangeLogTestHelper.ParseAsync(content, Language);
    }

    private static ValueTask<string> SerialiseAsync(ChangeLogDocument document)
    {
        return ChangeLogTestHelper.SerialiseAsync(document, Language);
    }

    [Fact]
    public async Task ParseYankedRelease_SetsIsYankedTrue()
    {
        ChangeLogDocument document = await ParseAsync(YANKED_CHANGE_LOG);

        Assert.True(document.Releases[0].IsYanked, userMessage: "Expected release 1.1.0 to have IsYanked = true");
    }

    [Fact]
    public async Task ParseYankedRelease_StripsDatesYankedSuffix()
    {
        ChangeLogDocument document = await ParseAsync(YANKED_CHANGE_LOG);

        Assert.Equal(expected: "2024-02-01", actual: document.Releases[0].Date);
    }

    [Fact]
    public async Task ParseNonYankedRelease_SetsIsYankedFalse()
    {
        ChangeLogDocument document = await ParseAsync(YANKED_CHANGE_LOG);

        Assert.False(document.Releases[1].IsYanked, userMessage: "Expected release 1.0.0 to have IsYanked = false");
    }

    [Theory]
    [InlineData("## [1.0.0] - 2024-01-01 [YANKED]")]
    [InlineData("## [1.0.0] - 2024-01-01 [yanked]")]
    [InlineData("## [1.0.0] - 2024-01-01 [Yanked]")]
    public static async Task ParseYankedRelease_CaseInsensitiveAsync(string header)
    {
        string content = $"""
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            {header}
            ### Added
            - Something

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = await ParseAsync(content);

        Assert.True(
            document.Releases[0].IsYanked,
            userMessage: $"Expected release parsed from '{header}' to have IsYanked = true"
        );
        Assert.Equal(expected: "2024-01-01", actual: document.Releases[0].Date);
    }

    [Fact]
    public async Task SerialiseYankedRelease_IncludesYankedMarker()
    {
        ChangeLogDocument document = await ParseAsync(YANKED_CHANGE_LOG);
        string serialised = await SerialiseAsync(document);

        Assert.True(
            serialised.Contains("## [1.1.0] - 2024-02-01 [YANKED]", StringComparison.Ordinal),
            userMessage: $"Expected serialised output to contain yanked header, but got:{Environment.NewLine}{serialised}"
        );
    }

    [Fact]
    public async Task SerialiseNonYankedRelease_DoesNotIncludeYankedMarker()
    {
        ChangeLogDocument document = await ParseAsync(YANKED_CHANGE_LOG);
        string serialised = await SerialiseAsync(document);

        Assert.True(
            serialised.Contains("## [1.0.0] - 2024-01-01", StringComparison.Ordinal),
            userMessage: $"Expected serialised output to contain non-yanked header, but got:{Environment.NewLine}{serialised}"
        );
        Assert.False(
            serialised.Contains("## [1.0.0] - 2024-01-01 [YANKED]", StringComparison.Ordinal),
            userMessage: $"Expected serialised output NOT to contain [YANKED] for non-yanked release, but got:{Environment.NewLine}{serialised}"
        );
    }

    [Fact]
    public async Task RoundTrip_YankedRelease_PreservesMarker()
    {
        ChangeLogDocument document = await ParseAsync(YANKED_CHANGE_LOG);
        string serialised = await SerialiseAsync(document);
        ChangeLogDocument reparsed = await ParseAsync(serialised);

        Assert.True(
            reparsed.Releases[0].IsYanked,
            userMessage: "Expected round-tripped release to still have IsYanked = true"
        );
        Assert.Equal(expected: "2024-02-01", actual: reparsed.Releases[0].Date);
    }

    [Fact]
    public async Task LintYankedRelease_ReturnsNoVersionErrors()
    {
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(YANKED_CHANGE_LOG), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "1.1.0", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "Invalid version", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task LintYankedRelease_ReturnsNoOrderErrors()
    {
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(YANKED_CHANGE_LOG), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "descending order", comparisonType: StringComparison.Ordinal)
        );
    }
}
