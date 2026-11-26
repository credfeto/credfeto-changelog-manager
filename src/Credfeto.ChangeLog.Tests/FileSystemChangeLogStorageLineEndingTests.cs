using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class FileSystemChangeLogStorageLineEndingTests : LoggingFolderCleanupTestBase
{
    private const string WindowsLineEnding = "\r\n";
    private const string UnixLineEnding = "\n";

    private static readonly string[] ChangeLogLines =
    [
        "# Changelog",
        "",
        "## [Unreleased]",
        "### Security",
        "### Added",
        "- Existing item",
        "### Fixed",
        "### Changed",
        "### Deprecated",
        "### Removed",
        "### Deployment Changes",
        "",
        "## [1.0.0] - 2024-01-01",
        "### Added",
        "- Initial release",
        "",
        "## [0.0.0] - Project created",
    ];

    private static readonly string[] ReleasedLines =
    [
        "## [1.0.0] - 2024-01-01",
        "### Added",
        "- Initial release",
        "",
        "## [0.0.0] - Project created",
    ];

    private readonly FileSystemChangeLogStorage _storage;

    public FileSystemChangeLogStorageLineEndingTests(ITestOutputHelper output)
        : base(output)
    {
        this._storage = new(parser: new ChangeLogParser(), serialiser: new ChangeLogSerialiser());
    }

    [Theory]
    [InlineData(WindowsLineEnding)]
    [InlineData(UnixLineEnding)]
    public async Task SaveKeepsTheLineEndingOfTheExistingFileAsync(string lineEnding)
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = await this.WriteChangeLogAsync(
            lineEnding: lineEnding,
            cancellationToken: cancellationTokenSource.Token
        );

        ChangeLogDocument document = await this._storage.LoadAsync(
            changeLogFileName: fileName,
            language: ChangeLogTestHelper.EnglishLanguage,
            cancellationToken: cancellationTokenSource.Token
        );

        await this._storage.SaveAsync(
            changeLogFileName: fileName,
            document: document,
            language: ChangeLogTestHelper.EnglishLanguage,
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        Assert.Equal(
            expected: string.Join(separator: lineEnding, value: ChangeLogLines),
            actual: content,
            comparer: StringComparer.Ordinal
        );
    }

    [Fact]
    public async Task SaveOfANewFileUsesTheEnvironmentNewLineAsync()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(
            string.Join(separator: WindowsLineEnding, value: ChangeLogLines)
        );

        await this._storage.SaveAsync(
            changeLogFileName: fileName,
            document: document,
            language: ChangeLogTestHelper.EnglishLanguage,
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        Assert.Equal(
            expected: string.Join(separator: Environment.NewLine, value: ChangeLogLines),
            actual: content,
            comparer: StringComparer.Ordinal
        );
    }

    [Theory]
    [InlineData(WindowsLineEnding)]
    [InlineData(UnixLineEnding)]
    public async Task AddEntryKeepsTheReleasedSectionsOfTheExistingFileUnchangedAsync(string lineEnding)
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = await this.WriteChangeLogAsync(
            lineEnding: lineEnding,
            cancellationToken: cancellationTokenSource.Token
        );

        ChangeLogUpdater updater = new(
            storage: this._storage,
            parser: new ChangeLogParser(),
            timeProvider: MockDateTimeSources.Past
        );

        await updater.AddEntryAsync(
            changeLogFileName: fileName,
            language: ChangeLogTestHelper.EnglishLanguage,
            type: "Fixed",
            message: "A new fix",
            cancellationToken: cancellationTokenSource.Token
        );

        string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
        string released = string.Join(separator: lineEnding, value: ReleasedLines);
        Assert.True(
            content.EndsWith(lineEnding + released, StringComparison.Ordinal),
            userMessage: $"Expected the released sections to be unchanged, got:{Environment.NewLine}{content}"
        );
        Assert.True(
            content.Contains("- A new fix" + lineEnding, StringComparison.Ordinal),
            userMessage: $"Expected the new entry to use the existing line ending, got:{Environment.NewLine}{content}"
        );
    }

    private async Task<string> WriteChangeLogAsync(string lineEnding, CancellationToken cancellationToken)
    {
        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(
            path: fileName,
            contents: string.Join(separator: lineEnding, value: ChangeLogLines),
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );

        return fileName;
    }
}
