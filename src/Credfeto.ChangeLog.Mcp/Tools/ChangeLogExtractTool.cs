using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public sealed class ChangeLogExtractTool : ChangeLogTool
{
    private readonly IChangeLogReader _reader;

    public ChangeLogExtractTool(ToolContext context, IChangeLogReader reader)
        : base(
            context: context,
            name: "changelog_extract",
            title: "Extract release notes",
            description: "Returns the release notes for a version as plain text, ready to use as the body of a release.",
            specificSchema: [ToolProperties.Version],
            readOnly: true
        )
    {
        this._reader = reader;
    }

    protected override ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    )
    {
        return this._reader.ExtractReleaseNotesFromFileAsync(
            changeLogFileName: target.FullPath,
            version: arguments.GetRequiredString(ToolProperties.Version),
            language: target.Language,
            cancellationToken: cancellationToken
        );
    }
}
