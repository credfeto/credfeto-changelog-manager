using System;
using System.IO;
using Credfeto.ChangeLog.Mcp.Exceptions;

namespace Credfeto.ChangeLog.Mcp.Paths;

public sealed class ChangeLogPathResolver
{
    private const string CHANGELOG_FILE_NAME = "CHANGELOG.md";
    private const string GIT_FOLDER_NAME = ".git";

    private readonly IChangeLogDetector _detector;
    private readonly string _workingDirectory;

    private ChangeLogPathResolver(string workingDirectory, string repositoryRoot, IChangeLogDetector detector)
    {
        this._workingDirectory = workingDirectory;
        this.RepositoryRoot = repositoryRoot;
        this._detector = detector;
    }

    public string RepositoryRoot { get; }

    public static ChangeLogPathResolver FromWorkingDirectory(string workingDirectory, IChangeLogDetector detector)
    {
        string folder = Normalise(workingDirectory);

        return new(workingDirectory: folder, repositoryRoot: FindRepositoryRoot(folder), detector: detector);
    }

    public static ChangeLogPathResolver FromRepositoryRoot(string repositoryRoot, IChangeLogDetector detector)
    {
        string root = Normalise(repositoryRoot);

        return new(workingDirectory: root, repositoryRoot: root, detector: detector);
    }

    public string Resolve(string? changeLog)
    {
        string candidate = changeLog ?? this.Detect();
        string fullPath = Path.GetFullPath(Path.Combine(path1: this._workingDirectory, path2: candidate));

        // The tools write to the file they are given, so a caller must not be able to reach
        // anything other than a changelog inside the repository the server works on.
        if (!this.IsInsideRepository(fullPath) || !IsChangeLogFileName(fullPath) || this.PassesThroughLink(fullPath))
        {
            throw new ChangeLogPathException(
                "The changelog must be a file named " + CHANGELOG_FILE_NAME + " inside the repository"
            );
        }

        return fullPath;
    }

    public string RelativeToRepository(string fullPath)
    {
        return Path.GetRelativePath(relativeTo: this.RepositoryRoot, path: fullPath);
    }

    private string Detect()
    {
        if (this._detector.TryFindChangeLog(out string? found))
        {
            return found;
        }

        throw new ChangeLogPathException("Could not find changelog");
    }

    private bool IsInsideRepository(string fullPath)
    {
        string prefix = Path.EndsInDirectorySeparator(this.RepositoryRoot)
            ? this.RepositoryRoot
            : this.RepositoryRoot + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(value: prefix, comparisonType: StringComparison.Ordinal);
    }

    private static bool IsChangeLogFileName(string fullPath)
    {
        return StringComparer.Ordinal.Equals(x: Path.GetFileName(fullPath), y: CHANGELOG_FILE_NAME);
    }

    // A link inside the repository could point anywhere, so the file and every folder between it
    // and the repository root must be real.
    private bool PassesThroughLink(string fullPath)
    {
        if (new FileInfo(fullPath).LinkTarget is not null)
        {
            return true;
        }

        for (
            string? folder = Path.GetDirectoryName(fullPath);
            folder is not null && !StringComparer.Ordinal.Equals(x: folder, y: this.RepositoryRoot);
            folder = Path.GetDirectoryName(folder)
        )
        {
            if (new DirectoryInfo(folder).LinkTarget is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static string FindRepositoryRoot(string workingDirectory)
    {
        for (string? folder = workingDirectory; folder is not null; folder = Path.GetDirectoryName(folder))
        {
            string git = Path.Combine(path1: folder, path2: GIT_FOLDER_NAME);

            if (Directory.Exists(git) || File.Exists(git))
            {
                return folder;
            }
        }

        return workingDirectory;
    }

    // Without a trailing separator the root compares equal to the folder names Path.GetDirectoryName returns
    private static string Normalise(string folder)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
    }
}
