using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed partial class ChangeLogFixerTests : TestBase
{
    private const string VALID_CHANGE_LOG = """
        # Changelog

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

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
    public async Task AlreadyValidChangelog_RemainsValid()
    {
        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(VALID_CHANGE_LOG), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BlankLineAfterHeading_IsRemoved()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added

            - an entry
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task MultipleBlankLinesAfterHeading_AreAllRemoved()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added


            - an entry
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task MissingRequiredSection_IsAdded()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task OutOfOrderSections_AreReordered()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry
            ### Security

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(result), Language);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public static async Task BlankLinesBeforeReleaseHeading_Mismatched_AreFixedAsync(int blankLines)
    {
        string gap = new(c: '\n', count: blankLines);
        string changeLog = $"""
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - an entry
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes
            {gap}## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(result), Language);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public static async Task BlankLinesBeforeDeploymentTrailerComment_Mismatched_AreFixedAsync(int blankLines)
    {
        string gap = new(c: '\n', count: blankLines);
        string changeLog = $"""
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - an entry
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes
            {gap}<!--
            Deployment comment
            -->

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        Assert.Contains("Deployment comment", result, StringComparison.Ordinal);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task MissingPreamble_IsAdded()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        Assert.Contains(
            "The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),",
            result,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).",
            result,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task ExistingPreamble_IsNotDuplicated()
    {
        const string changeLog = """
            # Changelog
            All notable changes to this project will be documented in this file.

            The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
            and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        int count = PreambleFormatLineRegex().Count(result);
        Assert.Equal(expected: 1, actual: count);
    }

    [Fact]
    public async Task MissingPreamble_IsInsertedBeforeHtmlComment()
    {
        const string changeLog = """
            # Changelog

            <!--
            Please ADD ALL Changes to the UNRELEASED SECTION
            -->

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = await SerialiseAsync(ChangeLogFixer.Fix(await ParseAsync(changeLog), Language));

        int preambleIndex = result.IndexOf("The format is based on", StringComparison.Ordinal);
        int commentIndex = result.IndexOf("<!--", StringComparison.Ordinal);
        Assert.True(preambleIndex < commentIndex, "Preamble should appear before HTML comment");
    }

    [GeneratedRegex(
        pattern: @"The format is based on \[Keep a Changelog\]\(https://keepachangelog\.com/en/1\.1\.0/\),",
        options: RegexOptions.None,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex PreambleFormatLineRegex();
}
