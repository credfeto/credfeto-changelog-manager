using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogLinterEntryLengthTests : TestBase
{
    private const string UNRELEASED_ENTRY = "{unreleased}";
    private const string RELEASED_ENTRY = "{released}";

    // The [Unreleased] entry is on line 6 and the released entry on line 15.
    private const string CHANGE_LOG_TEMPLATE = """
        # Changelog

        ## [Unreleased]
        ### Security
        ### Added
        {unreleased}
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [1.0.0] - 2024-01-01
        ### Added
        {released}

        ## [0.0.0] - Project created
        """;

    private const int UNRELEASED_LINE = 6;
    private const int RELEASED_LINE = 15;

    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private static string ChangeLog(string unreleased, string released)
    {
        return CHANGE_LOG_TEMPLATE
            .Replace(oldValue: UNRELEASED_ENTRY, newValue: unreleased, comparisonType: StringComparison.Ordinal)
            .Replace(oldValue: RELEASED_ENTRY, newValue: released, comparisonType: StringComparison.Ordinal);
    }

    private static string Entry(int length)
    {
        return "- " + new string('a', count: length);
    }

    private static async Task<IReadOnlyList<LintError>> LintAsync(string changeLog)
    {
        return ChangeLogLinter.Lint(await ChangeLogTestHelper.ParseAsync(changeLog, Language), Language);
    }

    private static void AssertEntryTooLong(LintError error, int lineNumber, int length)
    {
        Assert.Equal(expected: lineNumber, actual: error.LineNumber);
        Assert.Contains(
            expectedSubstring: $"is {length} characters long",
            actualString: error.Message,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "limit is 100 characters",
            actualString: error.Message,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public static async Task UnreleasedEntryOfExactlyTheLimitPassesAsync()
    {
        IReadOnlyList<LintError> errors = await LintAsync(
            ChangeLog(unreleased: Entry(100), released: "- Initial release")
        );

        Assert.Empty(errors);
    }

    [Fact]
    public static async Task UnreleasedEntryOverTheLimitFailsAsync()
    {
        IReadOnlyList<LintError> errors = await LintAsync(
            ChangeLog(unreleased: Entry(101), released: "- Initial release")
        );

        AssertEntryTooLong(error: Assert.Single(errors), lineNumber: UNRELEASED_LINE, length: 101);
    }

    [Fact]
    public static async Task ReleasedEntryOfExactlyTheLimitPassesAsync()
    {
        IReadOnlyList<LintError> errors = await LintAsync(ChangeLog(unreleased: "- New feature", released: Entry(100)));

        Assert.Empty(errors);
    }

    [Fact]
    public static async Task ReleasedEntryOverTheLimitFailsAsync()
    {
        IReadOnlyList<LintError> errors = await LintAsync(ChangeLog(unreleased: "- New feature", released: Entry(101)));

        AssertEntryTooLong(error: Assert.Single(errors), lineNumber: RELEASED_LINE, length: 101);
    }

    [Fact]
    public static async Task SurroundingWhitespaceIsNotCountedAsync()
    {
        IReadOnlyList<LintError> errors = await LintAsync(
            ChangeLog(unreleased: "  " + Entry(100) + "   ", released: "- Initial release")
        );

        Assert.Empty(errors);
    }

    [Fact]
    public static async Task EveryOverLongEntryIsReportedAsync()
    {
        IReadOnlyList<LintError> errors = await LintAsync(ChangeLog(unreleased: Entry(101), released: Entry(150)));

        Assert.Collection(
            errors,
            first => AssertEntryTooLong(error: first, lineNumber: UNRELEASED_LINE, length: 101),
            second => AssertEntryTooLong(error: second, lineNumber: RELEASED_LINE, length: 150)
        );
    }

    [Fact]
    public static async Task FixLeavesAnOverLongEntryUnchangedAsync()
    {
        string changeLog = ChangeLog(unreleased: Entry(101), released: Entry(120));
        ChangeLogDocument fixedDocument = ChangeLogFixer.Fix(
            await ChangeLogTestHelper.ParseAsync(changeLog, Language),
            Language
        );

        string serialised = await ChangeLogTestHelper.SerialiseAsync(fixedDocument, Language);
        IReadOnlyList<LintError> errors = await LintAsync(serialised);

        Assert.Contains(
            expectedSubstring: Entry(101),
            actualString: serialised,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: Entry(120),
            actualString: serialised,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Equal(expected: 2, actual: errors.Count);
    }
}
