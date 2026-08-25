using System.Collections.Immutable;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogParserTrailingBlanksTests : TestBase
{
    private const string ChangeLogWithTrailingBlanksBeforeComment = """
        # Changelog

        ## [Unreleased]
        ### Added
        - Some unreleased item



        <!--
        Releases that have at least been deployed to staging.
        -->

        ## [0.0.0] - Project created
        """;

    [Fact]
    public async Task ParseMovesTrailingBlankLinesIntoUnreleasedTrailerInOrderAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(ChangeLogWithTrailingBlanksBeforeComment);

        ImmutableArray<string> trailingLines = document.Unreleased?.TrailingLines ?? [];

        Assert.Equal(
            expected: ["", "", "", "<!--", "Releases that have at least been deployed to staging.", "-->", ""],
            actual: trailingLines
        );
    }

    [Fact]
    public async Task ParseKeepsNonBlankEntriesOutOfTheTrailerAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(ChangeLogWithTrailingBlanksBeforeComment);

        ImmutableArray<ChangeLogSection> sections = document.Unreleased?.Sections ?? [];

        ChangeLogSection added = Assert.Single(sections);
        Assert.Equal(expected: "Added", actual: added.Name);
        Assert.Equal(expected: ["- Some unreleased item"], actual: added.Entries);
    }

    // A release with no ### sections at all never adds anything to ReleaseParseState.CurrentEntries,
    // so the blank-line count before the *next* release heading must be tracked independently of
    // whether there is a current section, not derived from trimming CurrentEntries.
    private const string ReleaseWithNoSectionsFollowedByBlankLine = """
        # Changelog

        ## [Unreleased]
        ### Added
        - Something

        ## [3.0.0] - 2024-03-01

        ## [2.0.0] - 2024-02-01
        ### Added
        - Release 2 content

        ## [0.0.0] - Project created
        """;

    [Fact]
    public async Task ParseCountsBlankLineBeforeHeadingWhenPrecedingReleaseHasNoSectionsAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(ReleaseWithNoSectionsFollowedByBlankLine);

        ChangeLogRelease release2 = document.Releases[1];

        Assert.Equal(expected: "2.0.0", actual: release2.Version);
        Assert.Equal(expected: 1, actual: release2.BlankLinesBeforeHeading);
    }

    // [Unreleased] with no ### sections at all never reaches ChangeLogSection/currentEntries
    // either, so the blank-line gap before the first release must be tracked independently of
    // whether any section heading was ever seen, the same way as the no-sections release case
    // above.
    private const string UnreleasedWithNoSectionsFollowedByBlankLine = """
        # Changelog

        ## [Unreleased]

        ## [1.0.0] - 2024-01-01
        ### Added
        - Initial release

        ## [0.0.0] - Project created
        """;

    [Fact]
    public async Task ParseCountsBlankLineBeforeFirstReleaseWhenUnreleasedHasNoSectionsAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(UnreleasedWithNoSectionsFollowedByBlankLine);

        ChangeLogRelease release1 = document.Releases[0];

        Assert.Equal(expected: "1.0.0", actual: release1.Version);
        Assert.Equal(expected: 1, actual: release1.BlankLinesBeforeHeading);
    }
}
