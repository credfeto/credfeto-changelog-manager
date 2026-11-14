using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FunFair.Test.Infrastructure.Mocks;
using LibGit2Sharp;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

internal static class GitRepositoryHelpers
{
    public static void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        ForceDeleteDirectory(path);
    }

    public static void ForceDeleteDirectory(string path)
    {
        DirectoryInfo di = new(path);

        foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        foreach (DirectoryInfo dir in di.GetDirectories("*", SearchOption.AllDirectories))
        {
            dir.Attributes = FileAttributes.Normal;
        }

        di.Delete(true);
    }

    public static async Task<(
        string ChangeLogPath,
        string OriginBranchName,
        Repository Repo
    )> CreateRepoWithOriginBranchAsync(string tempDir, byte[] changeLogBytes, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(tempDir);
        string changeLogPath = Path.Combine(tempDir, "CHANGELOG.md");
        await File.WriteAllBytesAsync(changeLogPath, changeLogBytes, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, changeLogPath);
        Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
        Commit initialCommit = repo.Commit("Initial commit", author, author);

        const string ORIGIN_BRANCH_NAME = "origin/main";
        repo.Branches.Add(ORIGIN_BRANCH_NAME, initialCommit);

        return (changeLogPath, ORIGIN_BRANCH_NAME, repo);
    }
}
