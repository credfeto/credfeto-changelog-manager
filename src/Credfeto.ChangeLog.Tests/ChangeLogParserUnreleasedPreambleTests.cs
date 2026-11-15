using System.Collections.Immutable;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogParserUnreleasedPreambleTests : TestBase
{
    private const string CHANGE_LOG_WITH_CONTENT_BEFORE_FIRST_HEADING = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        - TBD
        ### Security
        ### Added
        - Banana
        ### Fixed

        ## [0.0.0] - Project created
        """;

    [Fact]
    public async Task ParseKeepsContentBeforeFirstHeadingWithLineNumberAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(CHANGE_LOG_WITH_CONTENT_BEFORE_FIRST_HEADING);

        ChangeLogPreambleLine preambleLine = Assert.Single(document.Unreleased?.PreambleLines ?? []);
        Assert.Equal(expected: 5, actual: preambleLine.LineNumber);
        Assert.Equal(expected: "- TBD", actual: preambleLine.Text);
    }

    [Fact]
    public async Task ParseDoesNotAddContentBeforeFirstHeadingToAnySectionAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(CHANGE_LOG_WITH_CONTENT_BEFORE_FIRST_HEADING);

        ImmutableArray<ChangeLogSection> sections = document.Unreleased?.Sections ?? [];

        Assert.Collection(
            sections,
            security => Assert.Empty(security.Entries),
            added => Assert.Equal(expected: ["- Banana"], actual: added.Entries),
            fixedSection => Assert.Empty(fixedSection.Entries)
        );
    }

    [Fact]
    public async Task ParseIgnoresBlankLinesBeforeFirstHeadingAsync()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]

            ### Added
            - Banana

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);

        Assert.Empty(document.Unreleased?.PreambleLines ?? []);
    }

    [Fact]
    public async Task ParseKeepsContentInUnreleasedWithNoHeadingsAsync()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            - TBD

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);

        ChangeLogPreambleLine preambleLine = Assert.Single(document.Unreleased?.PreambleLines ?? []);
        Assert.Equal(expected: 4, actual: preambleLine.LineNumber);
        Assert.Equal(expected: "- TBD", actual: preambleLine.Text);
        Assert.Equal(expected: [""], actual: document.Unreleased?.TrailingLines ?? []);
    }

    [Fact]
    public async Task RoundTripPreservesContentBeforeFirstHeadingAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(CHANGE_LOG_WITH_CONTENT_BEFORE_FIRST_HEADING);

        string serialised = await ChangeLogTestHelper.SerialiseAsync(document);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(serialised);

        ChangeLogPreambleLine preambleLine = Assert.Single(reparsed.Unreleased?.PreambleLines ?? []);
        Assert.Equal(expected: 5, actual: preambleLine.LineNumber);
        Assert.Equal(expected: "- TBD", actual: preambleLine.Text);
        Assert.Equal(expected: 6, actual: reparsed.Unreleased?.Sections[0].LineNumber);
    }
}
