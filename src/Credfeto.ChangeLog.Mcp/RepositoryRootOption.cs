using System;
using System.IO;
using Credfeto.ChangeLog.Mcp.Exceptions;

namespace Credfeto.ChangeLog.Mcp;

public static class RepositoryRootOption
{
    public const string EnvironmentVariable = "CHANGELOG_MCP_REPO_ROOT";

    public static string? Read()
    {
        return Environment.GetEnvironmentVariable(EnvironmentVariable);
    }

    public static string? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Path.IsPathFullyQualified(value))
        {
            throw new RepositoryRootException(EnvironmentVariable + " must be an absolute path: " + value);
        }

        if (!Directory.Exists(value))
        {
            throw new RepositoryRootException(EnvironmentVariable + " must name an existing directory: " + value);
        }

        return value;
    }
}
