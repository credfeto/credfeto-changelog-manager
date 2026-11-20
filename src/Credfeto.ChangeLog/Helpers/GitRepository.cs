using LibGit2Sharp;

namespace Credfeto.ChangeLog.Helpers;

internal static class GitRepository
{
    public static Repository OpenRepository(string workDir)
    {
        string found = Repository.Discover(workDir);

        return new(found);
    }
}
