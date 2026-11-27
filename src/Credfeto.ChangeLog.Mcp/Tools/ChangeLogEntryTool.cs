using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public abstract class ChangeLogEntryTool : ChangeLogTool
{
    private readonly SchemaProperty _message;

    protected ChangeLogEntryTool(
        ToolContext context,
        string name,
        string title,
        string description,
        SchemaProperty message
    )
        : base(
            context: context,
            name: name,
            title: title,
            description: description,
            specificSchema: [ToolProperties.Type, message],
            readOnly: false
        )
    {
        this._message = message;
    }

    protected sealed override async ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    )
    {
        string type = arguments.GetRequiredString(ToolProperties.Type);
        string message = arguments.GetRequiredString(this._message);

        await this.ChangeEntryAsync(target: target, type: type, message: message, cancellationToken: cancellationToken);

        return JsonResult.Create(changeLog: target.RelativePath, writeProperties: writer =>
        {
            writer.WriteString(propertyName: ToolProperties.Type.Name, value: type);
            writer.WriteString(propertyName: this._message.Name, value: message);
        });
    }

    protected abstract Task ChangeEntryAsync(
        ChangeLogTarget target,
        string type,
        string message,
        CancellationToken cancellationToken
    );
}
