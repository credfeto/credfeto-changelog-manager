using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp;

public static class ServerOptionsFactory
{
    public const string SERVER_NAME = "credfeto-changelog";

    private const string INSTRUCTIONS =
        "Manages the CHANGELOG.md of the repository the server was started in, using the Keep a Changelog format. "
        + "Add or remove entries in the [Unreleased] section, read unreleased changes, extract release notes, "
        + "create releases, check that only the [Unreleased] section was changed since a branch, and lint the file. "
        + "Every tool takes an optional changelog path, which must be a CHANGELOG.md inside the repository.";

    public static McpServerOptions Create(IEnumerable<McpServerTool> tools)
    {
        McpServerPrimitiveCollection<McpServerTool> toolCollection = [];

        foreach (McpServerTool tool in tools)
        {
            toolCollection.Add(tool);
        }

        return new()
        {
            ServerInfo = new() { Name = SERVER_NAME, Version = VersionInformation.Version },
            ServerInstructions = INSTRUCTIONS,
            Capabilities = new() { Tools = new() { ListChanged = false } },
            ToolCollection = toolCollection,
        };
    }
}
