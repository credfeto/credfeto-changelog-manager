using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp.Tests;

internal sealed class NamedTool : McpServerTool
{
    public NamedTool(string name)
    {
        this.ProtocolTool = new() { Name = name };
    }

    public override Tool ProtocolTool { get; }

    public override IReadOnlyList<object> Metadata => [];

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }
}
