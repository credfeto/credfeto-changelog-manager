using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogLinterTests : TestBase
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

    [Fact]
    public async Task ValidChangelog_ReturnsNoErrors()
    {
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(VALID_CHANGE_LOG), Language);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task MissingUnreleasedSection_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "[Unreleased]", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task MissingRequiredSection_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "### Added", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "Missing", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task DuplicateSection_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - First added
            ### Added
            - Duplicate added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "### Added", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "duplicated", comparisonType: StringComparison.Ordinal)
            && e.LineNumber == 7
        );
    }

    [Fact]
    public async Task UnknownSection_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes
            ### Custom

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "### Custom", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "Unknown", comparisonType: StringComparison.Ordinal)
            && e.LineNumber == 10
        );
    }

    [Fact]
    public async Task UnknownSection_AllowedViaAdditionalSections_ReturnsNoError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes
            ### Custom

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(
            await ParseAsync(changeLog),
            Language with
            {
                SectionOrder = [.. Language.SectionOrder, "Custom"],
            }
        );

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "### Custom", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "Unknown", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task BlankLineAfterHeading_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added

            - item
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "Blank line after heading '### Added'", comparisonType: StringComparison.Ordinal)
            && e.LineNumber == 5
        );
    }

    [Fact]
    public async Task NoBlankLineAfterHeading_ReturnsNoError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - item
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "Blank line after heading", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task InvalidVersionHeader_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [not-a-version] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "not-a-version", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "Invalid version", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task ValidVersionsInOrder_ReturnsNoErrors()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [2.0.0] - 2024-03-01
            ### Added
            - Release 2

            ## [1.0.0] - 2024-02-01
            ### Added
            - Release 1

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "descending order", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task WrongDateFormat_ReturnsError()
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

            ## [1.0.0] - 01/01/2024
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "01/01/2024", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "not in the expected format", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task TextLabelInsteadOfDate_ReturnsNoError()
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

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "not in the expected format", comparisonType: StringComparison.Ordinal)
        );
    }

    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        return ChangeLogTestHelper.ParseAsync(content, Language);
    }

    [Fact]
    public async Task VersionsOutOfOrder_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [1.0.0] - 2024-02-01
            ### Added
            - Release 1

            ## [2.0.0] - 2024-03-01
            ### Added
            - Release 2

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "2.0.0", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "descending order", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task SectionOutOfOrder_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            ### Security
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "out of order", comparisonType: StringComparison.Ordinal) && e.LineNumber == 4
        );
    }

    [Fact]
    public async Task TbdDateInRelease_ReturnsNoDateFormatError()
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

            ## [1.0.0] - TBD
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "not in the expected format", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task CheckReleaseDateWithBlankDateReturnsNoError()
    {
        // A release with an empty date (just version, no date) should not produce a date format error
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

            ## [1.0.0]
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "not in the expected format", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task UnparseableGarbageDateInRelease_ReturnsDateFormatError()
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

            ## [1.0.0] - banana
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "banana", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "not in the expected format", comparisonType: StringComparison.Ordinal)
        );
    }

    [Theory]
    [InlineData(0, "Missing")]
    [InlineData(2, "Extra")]
    public static async Task BlankLinesBeforeReleaseHeading_Mismatched_ReturnsErrorAsync(
        int blankLines,
        string expectedToken
    )
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

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "## [1.0.0]", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: expectedToken, comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task ExactlyOneBlankLineBeforeReleaseHeading_ReturnsNoError()
    {
        // Also confirms ### sub-headings (which VALID_CHANGE_LOG has none before) are out of
        // scope for this rule; only ## release headings are checked.
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(VALID_CHANGE_LOG), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "blank line(s) before release heading", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task MissingBlankLineBetweenTwoReleases_ReturnsError()
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

            ## [2.0.0] - 2024-03-01
            ### Added
            - Release 2
            ## [1.0.0] - 2024-02-01
            ### Added
            - Release 1

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Contains(errors, e =>
            e.Message.Contains(value: "## [1.0.0]", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "Missing", comparisonType: StringComparison.Ordinal)
        );
    }

    [Theory]
    [InlineData(0, "Missing", 12)]
    [InlineData(2, "Extra", 14)]
    public static async Task BlankLinesBeforeDeploymentTrailerComment_Mismatched_ReturnsErrorAsync(
        int blankLines,
        string expectedToken,
        int expectedLineNumber
    )
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

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        // Line number must point at the "<!--" comment itself (line 12/14 in the fixture above),
        // not merely somewhere inside [Unreleased].
        Assert.Contains(errors, e =>
            e.Message.Contains(value: "deployment trailer comment", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: expectedToken, comparisonType: StringComparison.Ordinal)
            && e.LineNumber == expectedLineNumber
        );
    }

    [Fact]
    public static async Task BlankLineBeforeFirstSection_DoesNotSkewTrailerCommentLineNumberAsync()
    {
        const string changeLog = """
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
            <!--
            Deployment comment
            -->

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        // A blank line between "## [Unreleased]" and its first "### " section heading must not
        // throw off the reported line number: it is derived from the trailer's own position in
        // the source, not reconstructed by counting sections/entries after the heading.
        Assert.Contains(errors, e =>
            e.Message.Contains(value: "deployment trailer comment", comparisonType: StringComparison.Ordinal)
            && e.Message.Contains(value: "Missing", comparisonType: StringComparison.Ordinal)
            && e.LineNumber == 13
        );
    }

    [Fact]
    public async Task ExactlyOneBlankLineBeforeDeploymentTrailerComment_ReturnsNoError()
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

            <!--
            Deployment comment
            -->

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(
                value: "blank line(s) before deployment trailer comment",
                comparisonType: StringComparison.Ordinal
            )
        );
    }

    [Fact]
    public async Task NonBlankContentBeforeDeploymentTrailerComment_DoesNotTriggerBlankLineCheck()
    {
        // A non-blank line directly under "### Deployment Changes" stays part of that section's
        // own entries (ChangeLogParser only ever moves trailing *blank* lines into TrailingLines,
        // never arbitrary content), so only the one blank line actually adjacent to the comment
        // reaches this rule; it correctly sees that as already-correct rather than as a large
        // blank-line count skewed by the note above it.
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
            Some deployment note.

            <!--
            Deployment comment
            -->

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(
                value: "blank line(s) before deployment trailer comment",
                comparisonType: StringComparison.Ordinal
            )
        );
    }

    [Fact]
    public async Task NoDeploymentTrailerComment_DoesNotTriggerBlankLineCheck()
    {
        // VALID_CHANGE_LOG's trailer has no HTML comment at all (goes straight from
        // "### Deployment Changes" to the next release heading): the rule must stay
        // silent rather than treating that gap as a missing comment-blank-line.
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(VALID_CHANGE_LOG), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(
                value: "blank line(s) before deployment trailer comment",
                comparisonType: StringComparison.Ordinal
            )
        );
    }

    private const string CHANGE_LOG_WITH_CONTENT_BEFORE_FIRST_HEADING = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        - TBD
        ### Security
        ### Added
        - Banana
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        <!--
        Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
        -->

        ## [0.0.0] - Project created
        """;

    [Fact]
    public async Task ContentBeforeFirstUnreleasedHeading_ReturnsErrorNamingLine()
    {
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(
            await ParseAsync(CHANGE_LOG_WITH_CONTENT_BEFORE_FIRST_HEADING),
            Language
        );

        LintError error = Assert.Single(errors);
        Assert.Equal(expected: 5, actual: error.LineNumber);
        Assert.Equal(
            expected: "Unexpected content '- TBD' between [Unreleased] and its first '### ' heading",
            actual: error.Message
        );
    }

    [Fact]
    public async Task MultipleLinesBeforeFirstUnreleasedHeading_ReturnsErrorForEachLine()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            - First

            - Second
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.Collection(
            errors,
            first =>
            {
                Assert.Equal(expected: 4, actual: first.LineNumber);
                Assert.Contains(
                    expectedSubstring: "'- First'",
                    actualString: first.Message,
                    comparisonType: StringComparison.Ordinal
                );
            },
            second =>
            {
                Assert.Equal(expected: 6, actual: second.LineNumber);
                Assert.Contains(
                    expectedSubstring: "'- Second'",
                    actualString: second.Message,
                    comparisonType: StringComparison.Ordinal
                );
            }
        );
    }

    [Fact]
    public async Task BlankLinesBeforeFirstUnreleasedHeading_DoNotTriggerUnexpectedContentError()
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

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(await ParseAsync(changeLog), Language);

        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "Unexpected content", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task ContentBeforeFirstUnreleasedHeading_IsNotRemovedByFix()
    {
        ChangeLogDocument fixedDocument = ChangeLogFixer.Fix(
            await ParseAsync(CHANGE_LOG_WITH_CONTENT_BEFORE_FIRST_HEADING),
            Language
        );

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(fixedDocument, Language);

        Assert.Contains(errors, e => e.Message.Contains(value: "'- TBD'", comparisonType: StringComparison.Ordinal));
    }
}
