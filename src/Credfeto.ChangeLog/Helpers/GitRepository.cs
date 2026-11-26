using LibGit2Sharp;

namespace Credfeto.ChangeLog.Helpers;

public static class GitRepository
{
    public static Repository OpenRepository(string workDir)
    {
        string? found = Repository.Discover(workDir);

        if (found is null)
        {
            return Throws.NoGitRepositoryFound(workDir);
        }

        Repository repository = new(found);

        if (repository.Info.IsBare)
        {
            repository.Dispose();

            return Throws.BareRepositoryNotSupported(found);
        }

        return repository;
    }
}
