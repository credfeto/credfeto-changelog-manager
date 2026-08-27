using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "NSubstitute .Returns() for ValueTask is idiomatic test pattern"
)]
public sealed class ChangeLogUpdaterAsyncFileTests : LoggingFolderCleanupTestBase, IDisposable
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private readonly ServiceProvider _serviceProvider;

    public ChangeLogUpdaterAsyncFileTests(ITestOutputHelper output)
        : base(output)
    {
        ServiceCollection services = new();
        services.AddChangeLog();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    private static void SetupLoad(IChangeLogStorage storage, ChangeLogDocument document)
    {
        storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(document));
    }

    private static void SetupSave(IChangeLogStorage storage)
    {
        storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
    }

    [Fact]
    public async Task CreateReleaseWithPendingFalseUsesCurrentDate()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - Some item
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        SetupLoad(storage: storage, document: document);

        ChangeLogDocument? saved = null;
        storage
            .SaveAsync(Arg.Any<string>(), Arg.Do<ChangeLogDocument>(d => saved = d), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage, new ChangeLogParser(), MockDateTimeSources.Past);

        await updater.CreateReleaseAsync(
            changeLogFileName: "test-release.md",
            language: Language,
            version: "1.0.0",
            pending: false,
            cancellationToken: cancellationTokenSource.Token
        );

        // MockDateTimeSources.Past is the fixed instant 1975-03-16T00:00:00Z; asserting the
        // literal keeps this independent of ChangeLogUpdater.CurrentDate's own expression, so a
        // bug there can't compute the same (wrong) value on both sides of the assertion.
        Assert.NotNull(saved);
        Assert.False(saved.Releases.IsEmpty, userMessage: "Expected at least one release to be created");
        Assert.Equal(expected: "1975-03-16", actual: saved.Releases[0].Date, comparer: StringComparer.Ordinal);
    }

    [Fact]
    public async Task UpdaterRemoveEntryAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string simpleChangeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - Item to remove
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(simpleChangeLog);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        SetupLoad(storage: storage, document: document);
        SetupSave(storage);

        ChangeLogUpdater updater = new(storage, new ChangeLogParser(), MockDateTimeSources.Past);

        string tempFile = Path.Combine(this.TempFolder, "test.md");
        await File.WriteAllTextAsync(tempFile, string.Empty, cancellationTokenSource.Token);

        await updater.RemoveEntryAsync(
            changeLogFileName: tempFile,
            language: Language,
            type: "Added",
            message: "Item to remove",
            cancellationToken: cancellationTokenSource.Token
        );

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<CancellationToken>());
        await storage.Received(1).SaveAsync(tempFile, Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdaterCreateReleaseAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string simpleChangeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - An item
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(simpleChangeLog);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        SetupLoad(storage: storage, document: document);
        SetupSave(storage);

        ChangeLogUpdater updater = new(storage, new ChangeLogParser(), MockDateTimeSources.Past);

        string tempFile = Path.Combine(this.TempFolder, "test-release.md");

        await updater.CreateReleaseAsync(
            changeLogFileName: tempFile,
            language: Language,
            version: "1.0.0",
            pending: true,
            cancellationToken: cancellationTokenSource.Token
        );

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<CancellationToken>());
        await storage.Received(1).SaveAsync(tempFile, Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdaterCreateEmptyAsyncWritesEmptyChangeLog()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.CreateEmptyAsync(
            changeLogFileName: fileName,
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        Assert.True(File.Exists(fileName), userMessage: "Expected changelog file to be created");
        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        Assert.True(
            content.Contains("[Unreleased]", StringComparison.Ordinal),
            userMessage: $"Expected empty changelog to contain [Unreleased] section, got:{Environment.NewLine}{content}"
        );
    }

    [Fact]
    public async Task CreateEmptyAsyncCreatesFileMatchingTemplate()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.CreateEmptyAsync(
            changeLogFileName: fileName,
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        Assert.Equal(expected: TemplateFile.Build(Language), actual: content.Trim());
    }

    // Regression guard for the "brand-new file" skeleton (TemplateFile.Build) staying
    // in sync with the round-trip add/remove path: asserted against a hardcoded
    // literal rather than TemplateFile.Build itself, so a reintroduced mismatch
    // between the two (e.g. the blank line before the trailer comment moving out of
    // step) fails this test even though CreateEmptyAsyncCreatesFileMatchingTemplate
    // above would not (that test compares a fresh file to TemplateFile.Build, which
    // moves in lockstep with any such bug). Add/remove never touch TrailingLines here,
    // so this does not exercise ChangeLogFixer.EnsureBlankLineBeforeTrailerComment; see
    // ChangeLogLinterTests/ChangeLogFixerTests instead for the rule that actively
    // enforces this blank line on pre-existing files via lint/--fix.
    [Fact]
    public async Task AddThenRemoveEntryOnNewFileMatchesPristineSkeleton()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.AddEntryAsync(
            changeLogFileName: fileName,
            language: Language,
            type: "Added",
            message: "__probe__",
            cancellationToken: cancellationTokenSource.Token
        );
        await updater.RemoveEntryAsync(
            changeLogFileName: fileName,
            language: Language,
            type: "Added",
            message: "__probe__",
            cancellationToken: cancellationTokenSource.Token
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
### Fixed
### Changed
### Deprecated
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created";

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        Assert.Equal(expected: expected.ToLocalEndLine(), actual: content.Trim());
    }

    [Fact]
    public async Task CreateEmptyAsyncParsedDocumentHasSeedRelease()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.CreateEmptyAsync(
            changeLogFileName: fileName,
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(content);

        ChangeLogRelease release = Assert.Single(document.Releases);
        Assert.Equal(expected: "0.0.0", actual: release.Version);
        Assert.Equal(expected: "Project created", actual: release.Date);
    }

    [Fact]
    public async Task CreateEmptyAsyncParsedDocumentHasTitle()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.CreateEmptyAsync(
            changeLogFileName: fileName,
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(content);

        Assert.Contains("# Changelog", document.HeaderLines, comparer: StringComparer.Ordinal);
        Assert.Contains(TemplateFile.PreambleLine1, document.HeaderLines, comparer: StringComparer.Ordinal);
    }

    [Fact]
    public async Task AddEntryAsyncCreatesAdditionalSectionWhenChangeLogIsMissing()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        ChangeLogLanguage languageWithAdditionalSection = Language with
        {
            SectionOrder = [.. Language.SectionOrder, "CustomSection"],
        };

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.AddEntryAsync(
            changeLogFileName: fileName,
            language: languageWithAdditionalSection,
            type: "CustomSection",
            message: "A custom entry",
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        Assert.True(
            content.Contains("### CustomSection", StringComparison.Ordinal),
            userMessage: $"Expected CustomSection heading to be created, got:{Environment.NewLine}{content}"
        );
        Assert.True(
            content.Contains("- A custom entry", StringComparison.Ordinal),
            userMessage: $"Expected entry to be added to CustomSection, got:{Environment.NewLine}{content}"
        );
    }

    [Fact]
    public async Task RemoveEntryAsyncRemovesEntryFromChangeLog()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - Item to remove
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

        await File.WriteAllTextAsync(fileName, changeLog, Encoding.UTF8, cancellationTokenSource.Token);

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.RemoveEntryAsync(
            changeLogFileName: fileName,
            language: Language,
            type: "Added",
            message: "Item to remove",
            cancellationToken: cancellationTokenSource.Token
        );

        string result = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        Assert.False(
            result.Contains("Item to remove", StringComparison.Ordinal),
            userMessage: $"Expected entry to be removed, but content was:{Environment.NewLine}{result}"
        );
    }

    [Fact]
    public async Task AddEntryAsyncNormalisesMissingBlankLineBeforeNextRelease()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        // No blank line between "- Existing item" and the following release heading (see #370).
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - Existing item
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

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(fileName, changeLog, Encoding.UTF8, cancellationTokenSource.Token);

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.AddEntryAsync(
            changeLogFileName: fileName,
            language: Language,
            type: "Fixed",
            message: "A new fix",
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(content);

        Assert.Equal(expected: 1, actual: reparsed.Releases[0].BlankLinesBeforeHeading);
    }

    [Fact]
    public async Task CreateReleaseAsyncNormalisesMissingBlankLineBeforeNextRelease()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        // No blank line between the last [Unreleased] entry and the following release heading.
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - A new item
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

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(fileName, changeLog, Encoding.UTF8, cancellationTokenSource.Token);

        IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

        await updater.CreateReleaseAsync(
            changeLogFileName: fileName,
            language: Language,
            version: "2.0.0",
            pending: true,
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(content);

        Assert.Equal(expected: 1, actual: reparsed.Releases[0].BlankLinesBeforeHeading);
    }
}
