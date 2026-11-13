using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ChangeLogCreateReleaseTool : ChangeLogTool
{
    private readonly IChangeLogUpdater _updater;

    public ChangeLogCreateReleaseTool(ToolContext context, IChangeLogUpdater updater)
        : base(
            context: context,
            name: "changelog_create_release",
            title: "Create release",
            description: "Moves the content of the [Unreleased] section into a new release with the given version.",
            specificSchema: [ToolProperties.Version, ToolProperties.Pending],
            readOnly: false
        )
    {
        this._updater = updater;
    }

    protected override async ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    )
    {
        string version = arguments.GetRequiredString(ToolProperties.Version);
        bool pending = arguments.GetFlag(ToolProperties.Pending);

        await this._updater.CreateReleaseAsync(
            changeLogFileName: target.FullPath,
            language: target.Language,
            version: version,
            pending: pending,
            cancellationToken: cancellationToken
        );

        return JsonResult.Create(
            changeLog: target.RelativePath,
            writeProperties: writer =>
            {
                writer.WriteString(propertyName: ToolProperties.Version.Name, value: version);
                writer.WriteBoolean(propertyName: ToolProperties.Pending.Name, value: pending);
            }
        );
    }
}
