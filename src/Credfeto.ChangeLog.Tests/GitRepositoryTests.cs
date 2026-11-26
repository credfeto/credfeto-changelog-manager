using System.IO;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Helpers;
using FunFair.Test.Common;
using LibGit2Sharp;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class GitRepositoryTests : LoggingFolderCleanupTestBase
{
    public GitRepositoryTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void OpenRepositoryThrowsWhenNoRepositoryExists()
    {
        string folder = this.CreateFolder("not-a-repository");

        GitRepositoryNotFoundException exception = Assert.Throws<GitRepositoryNotFoundException>(() =>
            GitRepository.OpenRepository(folder)
        );

        Assert.Equal(expected: $"No git repository found at or above \"{folder}\"", actual: exception.Message);
    }

    [Fact]
    public void OpenRepositoryThrowsWhenRepositoryIsBare()
    {
        string folder = this.CreateFolder("bare");
        string repositoryPath = Repository.Init(path: folder, isBare: true);

        GitRepositoryNotFoundException exception = Assert.Throws<GitRepositoryNotFoundException>(() =>
            GitRepository.OpenRepository(folder)
        );

        Assert.Equal(expected: $"Bare repositories are not supported: \"{repositoryPath}\"", actual: exception.Message);
    }

    [Fact]
    public void OpenRepositoryOpensNormalRepository()
    {
        string folder = this.CreateFolder("normal");
        Repository.Init(folder);

        using (Repository repository = GitRepository.OpenRepository(folder))
        {
            Assert.False(condition: repository.Info.IsBare, userMessage: "Expected a non-bare repository");
            Assert.Equal(
                expected: Path.TrimEndingDirectorySeparator(PhysicalPath.Resolve(folder)),
                actual: Path.TrimEndingDirectorySeparator(PhysicalPath.Resolve(repository.Info.WorkingDirectory))
            );
        }
    }

    private string CreateFolder(string name)
    {
        string folder = Path.Combine(path1: this.TempFolder, path2: name);
        Directory.CreateDirectory(folder);

        return folder;
    }
}
