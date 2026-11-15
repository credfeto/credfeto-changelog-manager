using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogUpdaterAddEntryTests : TestBase
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private static ValueTask<ChangeLogDocument> ParseOrCreateAsync(string content)
    {
        return ChangeLogTestHelper.ParseAsync(
            string.IsNullOrEmpty(content) ? TemplateFile.Build(Language) : content,
            Language
        );
    }

    private static async ValueTask<string> AddEntryAndSerialiseAsync(string content, string type, string message)
    {
        ChangeLogDocument document = await ParseOrCreateAsync(content);

        return await ChangeLogTestHelper.SerialiseAsync(ChangeLogUpdater.AddEntry(document, type, message), Language);
    }

    [Fact]
    public async Task AddToEmptyChangelog()
    {
        string result = await AddEntryAndSerialiseAsync(
            content: string.Empty,
            type: "Added",
            message: "Added a new entry"
        );

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Security
### Added
- Added a new entry
### Fixed
### Changed
### Deprecated
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public async Task AddToExistingChangelog()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = await AddEntryAndSerialiseAsync(content: existing, type: "Added", message: "Another entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
- Another entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public async Task AddingDuplicateToExistingChangelogDoesNotAdd()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = await AddEntryAndSerialiseAsync(content: existing, type: "Added", message: "Added a new entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public async Task AddToExistingChangelogWithTrailingBlanks()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry

### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = await AddEntryAndSerialiseAsync(content: existing, type: "Added", message: "Another entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
- Another entry

### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("added")]
    [InlineData("ADDED")]
    public static async Task AddUsingLowerOrUpperCaseTypeResolvesToCanonicalHeadingAsync(string type)
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = await AddEntryAndSerialiseAsync(content: existing, type: type, message: "Another entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
- Another entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public async Task AddToExistingChangelogForSectionThatDoesNotExistFails()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.1] - 2020-12-29
### Added
- Added a new entry

## [0.0.0] - Project created";

        ChangeLogDocument document = await ParseOrCreateAsync(existing);

        Assert.Throws<InvalidChangeLogException>(() => ChangeLogUpdater.AddEntry(document, "Added", "Another entry"));
    }
}
