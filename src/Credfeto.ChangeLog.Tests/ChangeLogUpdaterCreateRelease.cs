using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Testing the bit that changes the file rather than reading/writing"
)]
[SuppressMessage(
    category: "Microsoft.VisualStudio.Threading.Analyzers",
    checkId: "VSTHRD002",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
public sealed class ChangeLogUpdaterCreateRelease : TestBase
{
    private readonly ITestOutputHelper _output;

    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    public ChangeLogUpdaterCreateRelease(ITestOutputHelper output)
    {
        this._output = output ?? throw new ArgumentNullException(nameof(output));
    }

    private static ChangeLogDocument Parse(string content)
    {
        ChangeLogParser parser = new();
        return parser.ParseAsync(content, default).GetAwaiter().GetResult();
    }

    private static string Serialise(ChangeLogDocument document)
    {
        ChangeLogSerialiser serialiser = new();
        return serialiser.SerialiseAsync(document, default).GetAwaiter().GetResult();
    }

    [Fact]
    public void EmptyUnreleasedDoesNotCreateARelease()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        Assert.Throws<EmptyChangeLogException>(() =>
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public void UnreleasedWithOnlyWhitespaceEntriesDoesNotCreateARelease()
    {
        string changeLog =
            $@"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
{"   "}
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        Assert.Throws<EmptyChangeLogException>(() =>
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public void CannotCreateAReleaseThatAlreadyExists()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
- Something.
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.0.0] - 2020-11-23
## Added
- An Item

## [0.0.0] - Project created";

        Assert.Throws<ReleaseAlreadyExistsException>(() =>
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public void CannotCreateAReleaseOlderThanLatest()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
- Something.
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [2.0.0] - 2020-11-23
## Added
- An Item

## [0.0.0] - Project created";

        Assert.Throws<ReleaseTooOldException>(() =>
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public void ChangeLogWithOnlyAddedInUnreleasedProducesReleaseWithJustAdded()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Some Content
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = Serialise(
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [1.0.0] - TBD
### Added
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void ChangeLogWithWhitespaceOnlyEntryExcludesItFromRelease()
    {
        string changeLog =
            $@"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Some Content
{"   "}
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = Serialise(
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [1.0.0] - TBD
### Added
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void ChangeLogWithOnlyFixedInUnreleasedProducesReleaseWithJustAdded()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
- Some Content
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = Serialise(
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [1.0.0] - TBD
### Fixed
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void ChangeLogWithOnlyChangedInUnreleasedProducesReleaseWithJustAdded()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
- Some Content
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = Serialise(
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [1.0.0] - TBD
### Changed
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void ChangeLogWithOnlyRemovedInUnreleasedProducesReleaseWithJustAdded()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
- Some Content
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = Serialise(
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [1.0.0] - TBD
### Removed
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void NoPreviousReleaseAddsReleaseAtEndOfFile()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Some Content
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->";

        string updated = Serialise(
            ChangeLogUpdater.CreateRelease(Parse(changeLog), "1.0.0", ChangeLogRelease.PendingDate)
        );

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [1.0.0] - TBD
### Added
- Some Content";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    // CreateRelease prepends a new release ahead of whatever was previously Releases[0]; that
    // displaced release's BlankLinesBeforeHeading described its old boundary against [Unreleased]
    // and must be corrected to reflect that it is now preceded by the new release instead,
    // otherwise ChangeLogLinter.Lint reports a false blank-line violation for a document that has
    // not even been serialised yet.
    //
    // No blank line before ## [1.0.0] here deliberately: the source parses it to
    // BlankLinesBeforeHeading == 0, so the assertion below only passes if PrependRelease actually
    // corrects it to 1 rather than the fix being a no-op that happens to match an already-correct
    // input (see PR #371 review).
    [Fact]
    public void DisplacedReleaseBlankLinesBeforeHeadingIsCorrected()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- New content
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.0.0] - 2024-01-01
### Added
- Initial release

## [0.0.0] - Project created";

        ChangeLogDocument original = Parse(changeLog);
        Assert.Equal(expected: 0, actual: original.Releases[0].BlankLinesBeforeHeading);

        ChangeLogDocument updated = ChangeLogUpdater.CreateRelease(original, "2.0.0", ChangeLogRelease.PendingDate);

        ChangeLogRelease displaced = updated.Releases[1];
        Assert.Equal(expected: "1.0.0", actual: displaced.Version);
        Assert.Equal(expected: 1, actual: displaced.BlankLinesBeforeHeading);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(updated, Language);
        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains(value: "## [1.0.0]", comparisonType: StringComparison.Ordinal)
        );
    }
}
