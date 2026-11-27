using System;
using System.IO;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Mcp.Constants;
using Credfeto.ChangeLog.Mcp.Exceptions;

namespace Credfeto.ChangeLog.Mcp.Paths;

public sealed class ChangeLogPathResolver
{
    private const string GIT_FOLDER_NAME = ".git";

    private readonly IChangeLogDetector _detector;

    // Null when the folder the server was started in no longer exists; there is then no repository root either
    private readonly string? _workingDirectory;

    private ChangeLogPathResolver(string? workingDirectory, string? repositoryRoot, IChangeLogDetector detector)
    {
        this._workingDirectory = workingDirectory;
        this.RepositoryRoot = repositoryRoot;
        this._detector = detector;
    }

    // Null when the server was started outside any repository; every path is then refused, so the tools never fall
    // back to working on whatever folder the server happened to be started in.
    public string? RepositoryRoot { get; }

    public static ChangeLogPathResolver FromWorkingDirectory(string? workingDirectory, IChangeLogDetector detector)
    {
        if (workingDirectory is null)
        {
            return new(workingDirectory: null, repositoryRoot: null, detector: detector);
        }

        string folder = Normalise(workingDirectory);

        return new(workingDirectory: folder, repositoryRoot: FindRepositoryRoot(folder), detector: detector);
    }

    // The detector is built from the same normalised root the resolver checks against, so the two cannot disagree.
    // The root is kept in its physical form, and a changelog named through the same path, including a linked root, is
    // resolved physically too, so the two still compare equal.
    public static ChangeLogPathResolver FromRepositoryRoot(string repositoryRoot)
    {
        string root = Normalise(repositoryRoot);

        return new(workingDirectory: root, repositoryRoot: root, detector: new RepositoryRootChangeLogDetector(root));
    }

    public string Resolve(string? changeLog)
    {
        string repositoryRoot = this.RequireRepositoryRoot();
        string candidate = changeLog ?? this.Detect();
        string fullPath = PhysicalPath.Resolve(
            Path.Combine(path1: this._workingDirectory ?? repositoryRoot, path2: candidate)
        );

        // The tools write to the file they are given, so a caller must not be able to reach
        // anything other than a changelog inside the repository the server works on.
        if (
            !IsInsideRepository(repositoryRoot: repositoryRoot, fullPath: fullPath)
            || !IsChangeLogFileName(fullPath)
            || PassesThroughLink(repositoryRoot: repositoryRoot, fullPath: fullPath)
        )
        {
            throw new ChangeLogPathException(
                "The changelog must be a file named " + FileConstants.ChangeLogFileName + " inside the repository"
            );
        }

        return fullPath;
    }

    public string RelativeToRepository(string fullPath)
    {
        return Path.GetRelativePath(relativeTo: this.RequireRepositoryRoot(), path: fullPath);
    }

    private string RequireRepositoryRoot()
    {
        return this.RepositoryRoot
            ?? throw new ChangeLogPathException(
                "No repository found: "
                    + this.DescribeWorkingDirectory()
                    + " and "
                    + RepositoryRootOption.EnvironmentVariable
                    + " is not set"
            );
    }

    private string DescribeWorkingDirectory()
    {
        return this._workingDirectory is null
            ? "the folder the server was started in no longer exists"
            : "\"" + this._workingDirectory + "\" is not inside a git repository";
    }

    private string Detect()
    {
        if (this._detector.TryFindChangeLog(out string? found))
        {
            return found;
        }

        throw new ChangeLogPathException("Could not find changelog");
    }

    private static bool IsInsideRepository(string repositoryRoot, string fullPath)
    {
        string prefix = Path.EndsInDirectorySeparator(repositoryRoot)
            ? repositoryRoot
            : repositoryRoot + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(value: prefix, comparisonType: StringComparison.Ordinal);
    }

    private static bool IsChangeLogFileName(string fullPath)
    {
        return StringComparer.Ordinal.Equals(x: Path.GetFileName(fullPath), y: FileConstants.ChangeLogFileName);
    }

    // A link inside the repository could point anywhere, so the file and every folder between it
    // and the repository root must be real.
    private static bool PassesThroughLink(string repositoryRoot, string fullPath)
    {
        return PathLinks.HasLinkBelow(root: repositoryRoot, path: fullPath);
    }

    private static string? FindRepositoryRoot(string workingDirectory)
    {
        for (string? folder = workingDirectory; folder is not null; folder = Path.GetDirectoryName(folder))
        {
            string git = Path.Combine(path1: folder, path2: GIT_FOLDER_NAME);

            if (Directory.Exists(git) || File.Exists(git))
            {
                return folder;
            }
        }

        return null;
    }

    // Without a trailing separator the root compares equal to the folder names Path.GetDirectoryName returns. The root
    // is physical, like every path Resolve checks against it, so a ".." after a link names the folder the OS would.
    private static string Normalise(string folder)
    {
        return Path.TrimEndingDirectorySeparator(PhysicalPath.Resolve(folder));
    }
}
