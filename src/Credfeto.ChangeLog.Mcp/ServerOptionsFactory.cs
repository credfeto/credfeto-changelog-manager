using System;
using System.Collections.Generic;
using System.Linq;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp;

public static class ServerOptionsFactory
{
    public const string ServerName = "credfeto-changelog";

    private const string INSTRUCTIONS =
        "Manages the CHANGELOG.md of the repository named by "
        + RepositoryRootOption.EnvironmentVariable
        + ", or of the repository the server was started in when that is not set, using the Keep a Changelog format. "
        + "Add or remove entries in the [Unreleased] section, read unreleased changes, extract release notes, "
        + "create releases, check that only the [Unreleased] section was changed since a branch, and lint the file. "
        + "Every tool takes an optional changelog path, which must be a CHANGELOG.md inside the repository; "
        + "a relative path is resolved against the configured repository root, or the start folder when none is set. "
        + "The tools never create a changelog, and refuse to work when there is no repository to work on.";

    public static McpServerOptions Create(IEnumerable<McpServerTool> tools)
    {
        McpServerPrimitiveCollection<McpServerTool> toolCollection = [];

        foreach (
            McpServerTool tool in tools.OrderBy(
                keySelector: tool => tool.ProtocolTool.Name,
                comparer: StringComparer.OrdinalIgnoreCase
            )
        )
        {
            toolCollection.Add(tool);
        }

        return new()
        {
            ServerInfo = new() { Name = ServerName, Version = VersionInformation.Version },
            ServerInstructions = INSTRUCTIONS,
            Capabilities = new() { Tools = new() { ListChanged = false } },
            ToolCollection = toolCollection,
        };
    }
}
