using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using NSubstitute;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogServiceMockTests : TestBase
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private const string SIMPLE_CHANGE_LOG = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        - Added item
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [0.0.0] - Project created
        """;

    private static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        return ChangeLogTestHelper.ParseAsync(content, Language);
    }

    // ─── ChangeLogReader ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReaderExtractReleaseNotesCallsLoadAsync()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        ChangeLogDocument document = await ParseAsync(SIMPLE_CHANGE_LOG);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        ChangeLogStorageMock.MockLoad(storage: storage, document: document);

        ChangeLogReader reader = new(storage);

        string result = await reader.ExtractReleaseNotesFromFileAsync(
            changeLogFileName: "CHANGELOG.md",
            version: string.Empty,
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        Assert.Contains("### Added", result, System.StringComparison.Ordinal);
        Assert.Contains("- Added item", result, System.StringComparison.Ordinal);

        await storage.Received(1).LoadAsync("CHANGELOG.md", Arg.Any<ChangeLogLanguage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReaderFindFirstReleaseVersionPositionCallsLoadAsync()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added

            ## [1.0.0] - 2024-01-01
            ### Added
            - First release
            """;

        ChangeLogDocument document = await ParseAsync(changeLog);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        ChangeLogStorageMock.MockLoad(storage: storage, document: document);

        ChangeLogReader reader = new(storage);

        int? result = await reader.FindFirstReleaseVersionPositionAsync(
            changeLogFileName: "CHANGELOG.md",
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        Assert.NotNull(result);
        Assert.Equal(expected: document.Releases[0].LineNumber, actual: result.Value);

        await storage.Received(1).LoadAsync("CHANGELOG.md", Arg.Any<ChangeLogLanguage>(), Arg.Any<CancellationToken>());
    }

    // ─── ChangeLogFixer ───────────────────────────────────────────────────────────

    [Fact]
    public async Task FixerFixAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string contentWithBlankLines = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added

            - Added item
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes
            """;

        ChangeLogDocument document = await ParseAsync(contentWithBlankLines);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        ChangeLogStorageMock.MockLoad(storage: storage, document: document);

        ChangeLogFixer fixer = new(storage);

        await fixer.FixAsync(
            changeLogFileName: "CHANGELOG.md",
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        await storage.Received(1).LoadAsync("CHANGELOG.md", Arg.Any<ChangeLogLanguage>(), Arg.Any<CancellationToken>());
        await storage
            .Received(1)
            .SaveAsync(
                "CHANGELOG.md",
                Arg.Any<ChangeLogDocument>(),
                Arg.Any<ChangeLogLanguage>(),
                Arg.Any<CancellationToken>()
            );
    }

    // ─── ChangeLogLinter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LinterLintAsyncWithValidChangeLogReturnsNoErrors()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        ChangeLogDocument document = await ParseAsync(SIMPLE_CHANGE_LOG);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        ChangeLogStorageMock.MockLoad(storage: storage, document: document);

        ChangeLogLinter linter = new(storage);

        IReadOnlyList<LintError> errors = await linter.LintAsync(
            changeLogFileName: "CHANGELOG.md",
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        Assert.Empty(errors);
        await storage.Received(1).LoadAsync("CHANGELOG.md", Arg.Any<ChangeLogLanguage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinterLintAsyncWithMissingUnreleasedSectionReturnsError()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string invalidContent = """
            # Changelog
            All notable changes to this project will be documented in this file.
            """;

        ChangeLogDocument document = await ParseAsync(invalidContent);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        ChangeLogStorageMock.MockLoad(storage: storage, document: document);

        ChangeLogLinter linter = new(storage);

        IReadOnlyList<LintError> errors = await linter.LintAsync(
            changeLogFileName: "CHANGELOG.md",
            language: Language,
            cancellationToken: cancellationTokenSource.Token
        );

        LintError error = Assert.Single(errors);
        Assert.Equal(expected: "Missing [Unreleased] section", actual: error.Message);
        await storage.Received(1).LoadAsync("CHANGELOG.md", Arg.Any<ChangeLogLanguage>(), Arg.Any<CancellationToken>());
    }

    // ─── ChangeLogUpdater ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdaterAddEntryAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        ChangeLogDocument document = await ParseAsync(SIMPLE_CHANGE_LOG);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        ChangeLogStorageMock.MockLoad(storage: storage, document: document);

        ChangeLogUpdater updater = new(storage, new ChangeLogParser(), MockDateTimeSources.Past);

        string tempFile = System.IO.Path.GetTempFileName();
        try
        {
            await updater.AddEntryAsync(
                changeLogFileName: tempFile,
                language: Language,
                type: "Added",
                message: "New feature added",
                cancellationToken: cancellationTokenSource.Token
            );
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<ChangeLogLanguage>(), Arg.Any<CancellationToken>());
        await storage
            .Received(1)
            .SaveAsync(
                tempFile,
                Arg.Is<ChangeLogDocument>(d => d is object && d.Unreleased is object),
                Arg.Any<ChangeLogLanguage>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task UpdaterEnsureUnreleasedSectionsAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        ChangeLogDocument document = await ParseAsync(SIMPLE_CHANGE_LOG);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        ChangeLogStorageMock.MockLoad(storage: storage, document: document);

        ChangeLogUpdater updater = new(storage, new ChangeLogParser(), MockDateTimeSources.Past);

        string tempFile = System.IO.Path.GetTempFileName();
        try
        {
            await updater.EnsureUnreleasedSectionsAsync(
                changeLogFileName: tempFile,
                language: Language,
                cancellationToken: cancellationTokenSource.Token
            );
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<ChangeLogLanguage>(), Arg.Any<CancellationToken>());
        await storage
            .Received(1)
            .SaveAsync(
                tempFile,
                Arg.Any<ChangeLogDocument>(),
                Arg.Any<ChangeLogLanguage>(),
                Arg.Any<CancellationToken>()
            );
    }
}
