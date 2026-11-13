using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Models;

namespace Credfeto.ChangeLog.Mcp.Tools;

public abstract class ChangeLogEntryTool : ChangeLogTool
{
    protected ChangeLogEntryTool(ToolContext context, string name, string title, string description)
        : base(
            context: context,
            name: name,
            title: title,
            description: description,
            specificSchema: [ToolProperties.Type, ToolProperties.Message],
            readOnly: false
        ) { }

    protected sealed override async ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    )
    {
        string type = arguments.GetRequiredString(ToolProperties.Type);
        string message = arguments.GetRequiredString(ToolProperties.Message);

        await this.ChangeEntryAsync(target: target, type: type, message: message, cancellationToken: cancellationToken);

        return JsonResult.Create(
            changeLog: target.RelativePath,
            writeProperties: writer =>
            {
                writer.WriteString(propertyName: ToolProperties.Type.Name, value: type);
                writer.WriteString(propertyName: ToolProperties.Message.Name, value: message);
            }
        );
    }

    protected abstract Task ChangeEntryAsync(
        ChangeLogTarget target,
        string type,
        string message,
        CancellationToken cancellationToken
    );
}
