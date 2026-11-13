using System;
using System.Threading.Tasks;
using LibGit2Sharp;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests.Tools;

public sealed class ChangeLogCheckInsertToolTests : ToolTestBase
{
    private const string ORIGIN_BRANCH = "origin/main";

    private const string RELEASED = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Added
        - Some unreleased item

        ## [1.0.0] - 2024-01-01
        ### Added
        - First release item

        ## [0.0.0] - Project created
        """;

    private static readonly Signature Author = new(
        name: "Test User",
        email: "test@example.com",
        when: new DateTimeOffset(year: 2024, month: 1, day: 1, hour: 0, minute: 0, second: 0, offset: TimeSpan.Zero)
    );

    public ChangeLogCheckInsertToolTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task ChangeOnlyInUnreleasedIsValidAsync()
    {
        await this.CreateOriginBranchAsync();
        await this.AddEntryAsync(type: "Added", message: "A feature");
        this.CommitChangeLog("Add entry");

        string text = await this.CallSuccessfullyAsync(name: "changelog_check_insert", arguments: []);

        Assert.Equal(expected: ORIGIN_BRANCH, actual: Property(json: text, name: "branch").GetString());
        Assert.True(condition: Property(json: text, name: "valid").GetBoolean(), userMessage: text);
    }

    [Fact]
    public async Task MissingBranchIsAnErrorAsync()
    {
        await this.CreateOriginBranchAsync();
        await this.AddEntryAsync(type: "Added", message: "A feature");
        this.CommitChangeLog("Add entry");

        await this.CallFailingAsync(name: "changelog_check_insert", arguments: [Text("branch", "origin/missing")]);
    }

    private async Task CreateOriginBranchAsync()
    {
        await this.WriteChangeLogAsync(RELEASED);
        Commit initial = this.CommitChangeLog("Initial commit");

        using Repository repository = new(this.RepositoryFolder);
        repository.Branches.Add(name: ORIGIN_BRANCH, commit: initial);
    }

    private Commit CommitChangeLog(string message)
    {
        using Repository repository = new(this.RepositoryFolder);
        Commands.Stage(repository: repository, path: this.ChangeLogPath);

        return repository.Commit(message: message, author: Author, committer: Author);
    }
}
