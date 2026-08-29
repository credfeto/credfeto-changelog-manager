using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using LibGit2Sharp;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogChecker : IChangeLogChecker
{
    private readonly IChangeLogReader _reader;

    public ChangeLogChecker(IChangeLogReader reader)
    {
        this._reader = reader;
    }

    public async Task<bool> ChangeLogModifiedInReleaseSectionAsync(
        string changeLogFileName,
        string originBranchName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        changeLogFileName = GetFullChangeLogFilePath(changeLogFileName);
        int? position = await this._reader.FindFirstReleaseVersionPositionAsync(
            changeLogFileName: changeLogFileName,
            language: language,
            cancellationToken: cancellationToken
        );

        if (position is null)
        {
            return false;
        }

        string changelogDir = GetChangeLogDirectory(changeLogFileName);
        Console.WriteLine($"Changelog Folder: {changelogDir}");

        using (Repository repo = GitRepository.OpenRepository(changelogDir))
        {
            return EvaluateRepository(
                repo: repo,
                changeLogFileName: changeLogFileName,
                originBranchName: originBranchName,
                firstReleaseVersionIndex: position.Value
            );
        }
    }

    private static bool EvaluateRepository(
        Repository repo,
        string changeLogFileName,
        string originBranchName,
        int firstReleaseVersionIndex
    )
    {
        string sha = HeadSha(repo);

        Branch originBranch = FindOriginBranch(repo: repo, originBranchName: originBranchName);

        if (originBranch.Tip.Sha.EqualsOrdinal(sha))
        {
            return false;
        }

        string changeLogInRepoPath = FindChangeLogPositionInRepo(repo: repo, changeLogFileName: changeLogFileName);
        Console.WriteLine($"Relative to Repo Root: {changeLogInRepoPath}");

        // Diff against the merge base, not the origin branch's tip: if origin has advanced
        // (e.g. a release was cut) since this branch diverged, diffing against its tip would
        // pull in origin's own unrelated changes and could misreport them as this branch's.
        Commit mergeBase =
            repo.ObjectDatabase.FindMergeBase(originBranch.Tip, repo.Head.Tip)
            ?? Throws.CouldNotFindMergeBase(headSha: sha, originBranchName: originBranchName);

        Patch changes = repo.Diff.Compare<Patch>(
            mergeBase.Tree,
            HeadTree(repo),
            paths: [changeLogInRepoPath],
            compareOptions: CompareSettings.BuildCompareOptions
        );

        PatchEntryChanges? change = changes.FirstOrDefault();

        if (change is not null)
        {
            return CheckForChangesAfterFirstRelease(change: change, firstReleaseVersionIndex: firstReleaseVersionIndex);
        }

        Console.WriteLine("Could not find change in diff");

        return true;
    }

    private static Branch FindOriginBranch(Repository repo, string originBranchName)
    {
        return repo.Branches.FirstOrDefault(b => b.FriendlyName.EqualsOrdinal(originBranchName))
            ?? Throws.CouldNotFindBranch(originBranchName);
    }

    private static Tree BranchTree(Branch branch)
    {
        return branch.Tip.Tree;
    }

    private static Tree HeadTree(Repository repo)
    {
        return BranchTree(repo.Head);
    }

    private static string BranchSha(Branch branch)
    {
        return branch.Tip.Sha;
    }

    private static string HeadSha(Repository repo)
    {
        return BranchSha(repo.Head);
    }

    private static bool CheckForChangesAfterFirstRelease(PatchEntryChanges change, int firstReleaseVersionIndex)
    {
        Console.WriteLine("Change Details");
        string patchDetails = ExtractPatchDetails(change.Patch);
        Console.WriteLine(patchDetails);

        MatchCollection matches = CommonRegex.GitHunkPosition.Matches(patchDetails);

        foreach (Match? match in matches.OfType<Match?>())
        {
            if (match is null)
            {
                continue;
            }

            int changeStart = Convert.ToInt32(
                value: match.Groups["CurrentFileStart"].Value,
                provider: CultureInfo.InvariantCulture
            );

            if (
                !int.TryParse(
                    s: match.Groups["CurrentFileChangeLength"].Value,
                    style: NumberStyles.Integer,
                    provider: CultureInfo.InvariantCulture,
                    out int changeLength
                )
            )
            {
                changeLength = 1;
            }

            int changeEnd = changeLength == 0 ? changeStart : changeStart + changeLength - 1;

            if (changeEnd >= firstReleaseVersionIndex)
            {
                return false;
            }
        }

        return true;
    }

    private static string ExtractPatchDetails(string patch)
    {
        List<string> lines = [.. patch.SplitToLines()];

        RemoveLastLineIfBlank(lines);

        int lastHunk = lines.FindLastIndex(CommonRegex.GitHunkPosition.IsMatch);

        if (lastHunk != -1)
        {
            (List<string> before, List<string> after) = CompareHunk(lines: lines, lastHunk: lastHunk);

            if (before.SequenceEqual(second: after, comparer: StringComparer.Ordinal))
            {
                lines.RemoveRange(index: lastHunk, lines.Count - lastHunk);
            }
        }

        return string.Join(separator: Environment.NewLine, values: lines);
    }

    private static (List<string> before, List<string> after) CompareHunk(List<string> lines, int lastHunk)
    {
        List<string> before = [];
        List<string> after = [];

        foreach (string line in lines.Skip(lastHunk + 1))
        {
            if (line[0] == '+')
            {
                after.Add(line[1..]);
            }
            else if (line[0] == '-')
            {
                before.Add(line[1..]);
            }
            else if (line[0] == '\\')
            {
                // "\ No newline at end of file" — skip silently
            }
        }

        return (before, after);
    }

    private static void RemoveLastLineIfBlank(List<string> lines)
    {
        int lastLine = lines.Count - 1;

        if (string.IsNullOrEmpty(lines[lastLine]))
        {
            lines.RemoveAt(lastLine);
        }
    }

    private static string GetFullChangeLogFilePath(string changeLogFileName)
    {
        FileInfo changeLog = new(changeLogFileName);

        if (!changeLog.Exists)
        {
            return Throws.CouldNotFindChangeLog(changeLogFileName);
        }

        return changeLog.FullName;
    }

    private static string GetChangeLogDirectory(string changeLogFileName)
    {
        string? path = Path.GetDirectoryName(changeLogFileName);

        return path ?? Directory.GetCurrentDirectory();
    }

    private static string FindChangeLogPositionInRepo(Repository repo, string changeLogFileName)
    {
        return changeLogFileName[repo.Info.WorkingDirectory.Length..].Replace(oldChar: '\\', newChar: '/');
    }
}
