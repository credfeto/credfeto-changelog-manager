using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Exceptions;
using LibGit2Sharp;

namespace Credfeto.ChangeLog.Helpers;

public static class Throws
{
    [DoesNotReturn]
    public static Branch CouldNotFindBranch(string originBranchName)
    {
        throw new BranchMissingException($"Could not find branch {originBranchName}");
    }

    [DoesNotReturn]
    public static string CouldNotFindChangeLog(string changeLogFileName)
    {
        throw new InvalidChangeLogException($"Could not find {changeLogFileName}");
    }

    [DoesNotReturn]
    public static string ChangeLogNotInRepository(string changeLogFileName, string workingDirectory)
    {
        throw new InvalidChangeLogException(
            $"Changelog \"{changeLogFileName}\" is not inside the repository working folder \"{workingDirectory}\""
        );
    }

    [DoesNotReturn]
    public static Commit CouldNotFindMergeBase(string headSha, string originBranchName)
    {
        throw new MergeBaseNotFoundException(
            $"Could not find a common ancestor between HEAD ({headSha}) and {originBranchName}"
        );
    }

    [DoesNotReturn]
    public static (string Version, string Date, bool IsYanked) MalformedVersionHeader(string line)
    {
        throw new InvalidChangeLogException($"Malformed version header (missing closing bracket): {line}");
    }
}
