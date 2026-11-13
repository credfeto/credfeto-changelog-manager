using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ChangeLogUnreleasedTool : ChangeLogTool
{
    private const string UNRELEASED_VERSION = "0.0.0.0-unreleased";

    private readonly IChangeLogReader _reader;

    public ChangeLogUnreleasedTool(ToolContext context, IChangeLogReader reader)
        : base(
            context: context,
            name: "changelog_unreleased",
            title: "Show unreleased changes",
            description: "Returns the content of the [Unreleased] section of the changelog.",
            specificSchema: [],
            readOnly: true
        )
    {
        this._reader = reader;
    }

    protected override async ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    )
    {
        string content = await this._reader.ExtractReleaseNotesFromFileAsync(
            changeLogFileName: target.FullPath,
            version: UNRELEASED_VERSION,
            language: target.Language,
            cancellationToken: cancellationToken
        );

        return JsonResult.Create(
            changeLog: target.RelativePath,
            writeProperties: writer => writer.WriteString(propertyName: "content", value: content)
        );
    }
}
