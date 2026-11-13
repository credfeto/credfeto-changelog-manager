using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ChangeLogRemoveTool : ChangeLogEntryTool
{
    private readonly IChangeLogUpdater _updater;

    public ChangeLogRemoveTool(ToolContext context, IChangeLogUpdater updater)
        : base(
            context: context,
            name: "changelog_remove",
            title: "Remove changelog entries",
            description: "Removes every entry starting with the message from a section of the [Unreleased] section of the changelog."
        )
    {
        this._updater = updater;
    }

    protected override Task ChangeEntryAsync(
        ChangeLogTarget target,
        string type,
        string message,
        CancellationToken cancellationToken
    )
    {
        return this._updater.RemoveEntryAsync(
            changeLogFileName: target.FullPath,
            language: target.Language,
            type: type,
            message: message,
            cancellationToken: cancellationToken
        );
    }
}
