using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Mcp.Exceptions;
using Credfeto.ChangeLog.Mcp.Models;
using LibGit2Sharp;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp.Tools;

public abstract class ChangeLogTool : McpServerTool
{
    private readonly ToolContext _context;
    private readonly IReadOnlyList<SchemaProperty> _schema;

    protected ChangeLogTool(
        ToolContext context,
        string name,
        string title,
        string description,
        IReadOnlyList<SchemaProperty> specificSchema,
        bool readOnly
    )
    {
        this._context = context;
        this._schema = [ToolProperties.ChangeLog, ToolProperties.AdditionalSections, .. specificSchema];
        this.ProtocolTool = new()
        {
            Name = name,
            Title = title,
            Description = description,
            InputSchema = ToolSchema.Create(this._schema),
            Annotations = new()
            {
                Title = title,
                ReadOnlyHint = readOnly,
                DestructiveHint = false,
                IdempotentHint = readOnly,
                OpenWorldHint = false,
            },
        };
    }

    public override Tool ProtocolTool { get; }

    public override IReadOnlyList<object> Metadata => [];

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default
    )
    {
        return this.CallAsync(arguments: request.Params?.Arguments, cancellationToken: cancellationToken);
    }

    public async ValueTask<CallToolResult> CallAsync(
        IEnumerable<KeyValuePair<string, JsonElement>>? arguments,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ToolArguments parsed = ToolArguments.Parse(schema: this._schema, arguments: arguments);
            ChangeLogTarget target = this._context.Resolve(parsed);
            string text = await this.ExecuteAsync(
                target: target,
                arguments: parsed,
                cancellationToken: cancellationToken
            );

            return Result(text: text, isError: false);
        }
        catch (Exception exception) when (IsToolFailure(exception))
        {
            // Returned as a tool error so the calling model can see what went wrong and correct its request
            return Result(text: exception.Message, isError: true);
        }
    }

    protected abstract ValueTask<string> ExecuteAsync(
        ChangeLogTarget target,
        ToolArguments arguments,
        CancellationToken cancellationToken
    );

    private static CallToolResult Result(string text, bool isError)
    {
        return new() { Content = [new TextContentBlock { Text = text }], IsError = isError };
    }

    private static bool IsToolFailure(Exception exception)
    {
        return exception
            is ToolArgumentException
                or ChangeLogPathException
                or BranchMissingException
                or EmptyChangeLogException
                or InvalidChangeLogException
                or MergeBaseNotFoundException
                or ReleaseAlreadyExistsException
                or ReleaseTooOldException
                or ArgumentException
                or IOException
                or UnauthorizedAccessException
                or LibGit2SharpException;
    }
}
