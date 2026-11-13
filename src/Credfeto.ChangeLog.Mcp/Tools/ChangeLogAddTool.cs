using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ChangeLogAddTool : ChangeLogEntryTool
{
    private readonly IChangeLogUpdater _updater;

    public ChangeLogAddTool(ToolContext context, IChangeLogUpdater updater)
        : base(
            context: context,
            name: "changelog_add",
            title: "Add changelog entry",
            description: "Adds an entry to a section of the [Unreleased] section of the changelog. Adding an entry that is already present changes nothing."
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
        return this._updater.AddEntryAsync(
            changeLogFileName: target.FullPath,
            language: target.Language,
            type: type,
            message: message,
            cancellationToken: cancellationToken
        );
    }
}
