using System;
using System.IO;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogUpdaterEntryLengthTests : LoggingFolderCleanupTestBase
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    public ChangeLogUpdaterEntryLengthTests(ITestOutputHelper output)
        : base(output) { }

    private string ChangeLogPath => Path.Combine(path1: this.TempFolder, path2: "CHANGELOG.md");

    private static async Task<string> AddEntryAndSerialiseAsync(string message)
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(TemplateFile.Build(Language), Language);
        ChangeLogDocument updated = ChangeLogUpdater.AddEntry(document: document, type: "Added", message: message);

        return await ChangeLogTestHelper.SerialiseAsync(updated, Language);
    }

    [Fact]
    public static async Task AddEntryOfExactlyTheLimitIsAddedAsync()
    {
        string message = new('a', count: 100);

        string result = await AddEntryAndSerialiseAsync(message);

        Assert.Contains(
            expectedSubstring: "- " + message,
            actualString: result,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public static async Task SurroundingWhitespaceIsNotCountedAsync()
    {
        string message = "  " + new string('a', count: 100) + "  ";

        string result = await AddEntryAndSerialiseAsync(message);

        Assert.Contains(
            expectedSubstring: new('a', count: 100),
            actualString: result,
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public static async Task AddEntryOverTheLimitIsRejectedAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(TemplateFile.Build(Language), Language);

        ChangeLogEntryTooLongException exception = Assert.Throws<ChangeLogEntryTooLongException>(() =>
            ChangeLogUpdater.AddEntry(document: document, type: "Added", message: new('a', count: 101))
        );

        AssertNamesLengthAndLimit(exception);
    }

    [Fact]
    public async Task AddEntryAsyncOverTheLimitLeavesTheFileUnchangedAsync()
    {
        await using ServiceProvider services = new ServiceCollection().AddChangeLog().BuildServiceProvider();
        IChangeLogUpdater updater = services.GetRequiredService<IChangeLogUpdater>();
        await updater.CreateEmptyAsync(
            changeLogFileName: this.ChangeLogPath,
            language: Language,
            cancellationToken: this.CancellationToken()
        );
        string before = await File.ReadAllTextAsync(
            path: this.ChangeLogPath,
            cancellationToken: this.CancellationToken()
        );

        ChangeLogEntryTooLongException exception = await Assert.ThrowsAsync<ChangeLogEntryTooLongException>(() =>
            this.AddEntryOverTheLimitAsync(updater)
        );

        AssertNamesLengthAndLimit(exception);
        string after = await File.ReadAllTextAsync(
            path: this.ChangeLogPath,
            cancellationToken: this.CancellationToken()
        );
        Assert.Equal(expected: before, actual: after);
    }

    [Fact]
    public async Task AddEntryAsyncOverTheLimitDoesNotCreateAMissingFileAsync()
    {
        await using ServiceProvider services = new ServiceCollection().AddChangeLog().BuildServiceProvider();
        IChangeLogUpdater updater = services.GetRequiredService<IChangeLogUpdater>();

        await Assert.ThrowsAsync<ChangeLogEntryTooLongException>(() => this.AddEntryOverTheLimitAsync(updater));

        Assert.False(
            condition: File.Exists(this.ChangeLogPath),
            userMessage: "A rejected entry must not create the file"
        );
    }

    private Task AddEntryOverTheLimitAsync(IChangeLogUpdater updater)
    {
        return updater.AddEntryAsync(
            changeLogFileName: this.ChangeLogPath,
            language: Language,
            type: "Added",
            message: new('a', count: 101),
            cancellationToken: this.CancellationToken()
        );
    }

    private static void AssertNamesLengthAndLimit(ChangeLogEntryTooLongException exception)
    {
        Assert.Contains(
            expectedSubstring: "101 characters",
            actualString: exception.Message,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "limit is 100 characters",
            actualString: exception.Message,
            comparisonType: StringComparison.Ordinal
        );
    }
}
