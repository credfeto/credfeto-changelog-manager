using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogSerialiserTests : TestBase
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    [Fact]
    public async Task OrderSectionsMergesDuplicateSectionsWithSameName()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - First added
            ### Added
            - Duplicate added
            ### Fixed
            ### Changed
            ### Removed

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);
        ImmutableArray<ChangeLogSection> sections = document.Unreleased?.Sections ?? [];

        // OrderSections will merge duplicate "Added" sections
        ImmutableArray<ChangeLogSection> ordered = ChangeLogSerialiser.OrderSections(
            sections: sections,
            sectionOrder: Language.SectionOrder
        );

        // After merging, there should be at most one "Added" section
        int addedCount = 0;

        foreach (ChangeLogSection s in ordered)
        {
            if (StringComparer.Ordinal.Equals(s.Name, "Added"))
            {
                addedCount++;
            }
        }

        Assert.Equal(expected: 1, actual: addedCount);
    }

    [Fact]
    public async Task MissingBlankLineBeforeFirstRelease_IsNormalisedOnSerialise()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry
            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument original = await ChangeLogTestHelper.ParseAsync(changeLog);
        Assert.Equal(expected: 0, actual: original.Releases[0].BlankLinesBeforeHeading);

        string serialised = await SerialiseAsync(original);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(serialised);

        Assert.Equal(expected: 1, actual: reparsed.Releases[0].BlankLinesBeforeHeading);
    }

    [Fact]
    public async Task ExtraBlankLinesBeforeFirstRelease_AreCollapsedToOneOnSerialise()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry


            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument original = await ChangeLogTestHelper.ParseAsync(changeLog);
        Assert.Equal(expected: 2, actual: original.Releases[0].BlankLinesBeforeHeading);

        string serialised = await SerialiseAsync(original);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(serialised);

        Assert.Equal(expected: 1, actual: reparsed.Releases[0].BlankLinesBeforeHeading);
    }

    [Fact]
    public async Task HtmlCommentTrailerBeforeFirstRelease_KeepsCommentAndGetsOneBlankLine()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry
            <!--
            Deployment comment
            -->
            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument original = await ChangeLogTestHelper.ParseAsync(changeLog);
        string serialised = await SerialiseAsync(original);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(serialised);

        Assert.Contains("Deployment comment", serialised, StringComparison.Ordinal);
        Assert.Equal(expected: 1, actual: reparsed.Releases[0].BlankLinesBeforeHeading);
    }

    [Fact]
    public async Task NoReleases_UnreleasedTrailerIsReproducedVerbatim()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry
            <!--
            Deployment comment
            -->
            """;

        ChangeLogDocument original = await ChangeLogTestHelper.ParseAsync(changeLog);
        string serialised = await SerialiseAsync(original);

        Assert.Contains("Deployment comment", serialised, StringComparison.Ordinal);
    }

    private static ValueTask<string> SerialiseAsync(ChangeLogDocument document)
    {
        return new ChangeLogSerialiser().SerialiseAsync(document, default);
    }
}
