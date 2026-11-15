using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogReaderTests : TestBase
{
    private const string MULTI_RELEASE_CHANGE_LOG =
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
## [1.1.1] - TBD
### Added
- Something was added here.

## [1.1.0] - TBD
### Added
- This is release 1.1.0.

## [1.0] - TBD
### Added
- This is release 1.0.0.

## [0.0.0] - Project created
### Added
- Initial version
";

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    [InlineData("1.0.0.1")]
    public static async Task ReadEmptyChangeLogReturnsEmptyAsync(string version)
    {
        string result = await ExtractReleaseNotesAsync(changeLog: string.Empty, version: version);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionWithNoContentReturnsEmptyAsync(string version)
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
## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionWithJustAddedReturnsAddedSectionOnlyAsync(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Something was added.
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        const string expected =
            @"### Added
- Something was added.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionWithJustFixedReturnsAddedSectionOnlyAsync(string version)
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
- Something was fixed.
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        const string expected =
            @"### Fixed
- Something was fixed.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionWithJustChangedReturnsChangedSectionOnlyAsync(string version)
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
- Something was changed.
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        const string expected =
            @"### Changed
- Something was changed.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionWithJustRemovedReturnsRemovedSectionOnlyAsync(string version)
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
- Something was removed.
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        const string expected =
            @"### Removed
- Something was removed.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionWithJustDeploymentChangesReturnsDeploymentChangesSectionOnlyAsync(
        string version
    )
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
- Need to do something special here on the next deployment.

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        const string expected =
            @"### Deployment Changes
- Need to do something special here on the next deployment.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("1.1.1")]
    [InlineData("1.1.1.1")]
    [InlineData("1.1.1.3000")]
    public static async Task ReadASpecificReleaseReturnsThatReleaseOnlyAsync(string version)
    {
        string result = await ExtractReleaseNotesAsync(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);
        const string expected =
            @"### Added
- Something was added here.";
        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0")]
    [InlineData("1.0.0.0")]
    [InlineData("1.0.0.3000")]
    public static async Task ReadASpecificReleaseReturnsThatReleaseOnlyIgnoringZeroVersionPartsAsync(string version)
    {
        string result = await ExtractReleaseNotesAsync(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);
        const string expected =
            @"### Added
- This is release 1.0.0.";
        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("0.0.0")]
    [InlineData("0.0.0.0")]
    [InlineData("0.0.0.3000")]
    public static async Task ReadASpecificReleaseAtEndOfFileAsync(string version)
    {
        string result = await ExtractReleaseNotesAsync(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);

        const string expected =
            @"### Added
- Initial version";
        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionWithWhitespaceOnlyEntryExcludesItFromReleaseNotesAsync(string version)
    {
        string changeLog =
            $@"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Something was added.
{"   "}
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        const string expected =
            @"### Added
- Something was added.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("7.0")]
    [InlineData("8.1.0")]
    [InlineData("9.2.3.0")]
    [InlineData("10.3.4.3000")]
    public static async Task ReadNonExistentVersionAsync(string version)
    {
        string result = await ExtractReleaseNotesAsync(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);

        Assert.Equal(expected: string.Empty, actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public static async Task ReadUnReleasedSectionIncludesContentBeforeFirstHeadingAsync(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

## [Unreleased]
- TBD
### Security
### Added
- Banana
### Fixed

## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: version);
        const string expected =
            @"- TBD
### Added
- Banana";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public static async Task ReadReleaseIgnoresUnreleasedContentBeforeFirstHeadingAsync()
    {
        const string changeLog =
            @"# Changelog

## [Unreleased]
- TBD
### Added

## [1.0.0] - 2024-01-01
### Added
- Released item

## [0.0.0] - Project created
";

        string result = await ExtractReleaseNotesAsync(changeLog: changeLog, version: "1.0.0");
        const string expected =
            @"### Added
- Released item";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    private static async ValueTask<string> ExtractReleaseNotesAsync(string changeLog, string version)
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);

        return ChangeLogReader.ExtractReleaseNotes(document: document, version: version);
    }
}
