using System;
using System.IO;
using Credfeto.ChangeLog.Mcp.Exceptions;

namespace Credfeto.ChangeLog.Mcp;

public static class RepositoryRootOption
{
    public const string ENVIRONMENT_VARIABLE = "CHANGELOG_MCP_REPO_ROOT";

    public static string? Read()
    {
        return Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE);
    }

    public static string? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!Path.IsPathFullyQualified(value))
        {
            throw new RepositoryRootException(ENVIRONMENT_VARIABLE + " must be an absolute path: " + value);
        }

        if (!Directory.Exists(value))
        {
            throw new RepositoryRootException(ENVIRONMENT_VARIABLE + " must name an existing directory: " + value);
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
    }
}
