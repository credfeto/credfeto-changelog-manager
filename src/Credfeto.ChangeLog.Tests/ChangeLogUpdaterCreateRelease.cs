using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

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

    private static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        return ChangeLogTestHelper.ParseAsync(content, Language);
    }

    private static async ValueTask<string> CreatePendingReleaseAndSerialiseAsync(string changeLog)
    {
        ChangeLogDocument document = await ParseAsync(changeLog);

        return await ChangeLogTestHelper.SerialiseAsync(
            ChangeLogUpdater.CreateRelease(document, "1.0.0", ChangeLogRelease.PendingDate),
            Language
        );
    }

    [Fact]
    public async Task EmptyUnreleasedDoesNotCreateARelease()
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

        ChangeLogDocument document = await ParseAsync(changeLog);

        Assert.Throws<EmptyChangeLogException>(() =>
            ChangeLogUpdater.CreateRelease(document, "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public async Task UnreleasedWithOnlyWhitespaceEntriesDoesNotCreateARelease()
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

        ChangeLogDocument document = await ParseAsync(changeLog);

        Assert.Throws<EmptyChangeLogException>(() =>
            ChangeLogUpdater.CreateRelease(document, "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public async Task CannotCreateAReleaseThatAlreadyExists()
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

        ChangeLogDocument document = await ParseAsync(changeLog);

        Assert.Throws<ReleaseAlreadyExistsException>(() =>
            ChangeLogUpdater.CreateRelease(document, "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public async Task CannotCreateAReleaseOlderThanLatest()
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

        ChangeLogDocument document = await ParseAsync(changeLog);

        Assert.Throws<ReleaseTooOldException>(() =>
            ChangeLogUpdater.CreateRelease(document, "1.0.0", ChangeLogRelease.PendingDate)
        );
    }

    [Fact]
    public async Task ChangeLogWithOnlyAddedInUnreleasedProducesReleaseWithJustAdded()
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

        string updated = await CreatePendingReleaseAndSerialiseAsync(changeLog);

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
    public async Task ChangeLogWithWhitespaceOnlyEntryExcludesItFromRelease()
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

        string updated = await CreatePendingReleaseAndSerialiseAsync(changeLog);

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
    public async Task ChangeLogWithOnlyFixedInUnreleasedProducesReleaseWithJustAdded()
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

        string updated = await CreatePendingReleaseAndSerialiseAsync(changeLog);

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
    public async Task ChangeLogWithOnlyChangedInUnreleasedProducesReleaseWithJustAdded()
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

        string updated = await CreatePendingReleaseAndSerialiseAsync(changeLog);

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
    public async Task ChangeLogWithOnlyRemovedInUnreleasedProducesReleaseWithJustAdded()
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

        string updated = await CreatePendingReleaseAndSerialiseAsync(changeLog);

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
    public async Task NoPreviousReleaseAddsReleaseAtEndOfFile()
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

        string updated = await CreatePendingReleaseAndSerialiseAsync(changeLog);

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
    public async Task DisplacedReleaseBlankLinesBeforeHeadingIsCorrected()
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

        ChangeLogDocument original = await ParseAsync(changeLog);
        Assert.Equal(expected: 0, actual: original.Releases[0].BlankLinesBeforeHeading);

        ChangeLogDocument updated = ChangeLogUpdater.CreateRelease(original, "2.0.0", ChangeLogRelease.PendingDate);

        ChangeLogRelease displaced = updated.Releases[1];
        Assert.Equal(expected: "1.0.0", actual: displaced.Version);
        Assert.Equal(expected: 1, actual: displaced.BlankLinesBeforeHeading);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(updated, Language);
        Assert.DoesNotContain(errors, e =>
            e.Message.Contains(value: "## [1.0.0]", comparisonType: StringComparison.Ordinal)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public static async Task NullOrWhitespaceDateThrowsAsync(string? date)
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
## [0.0.0] - Project created";

        ChangeLogDocument document = await ParseAsync(changeLog);

        // ! Intentionally passing null/empty/whitespace to exercise CreateRelease's guard.
        Assert.ThrowsAny<ArgumentException>(() => ChangeLogUpdater.CreateRelease(document, "1.0.0", date!));
    }
}
