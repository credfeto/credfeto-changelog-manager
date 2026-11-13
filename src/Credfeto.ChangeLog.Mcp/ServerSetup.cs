using Credfeto.ChangeLog.Mcp.Paths;
using Credfeto.ChangeLog.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp;

public static class ServerSetup
{
    public static IServiceCollection AddChangeLogMcpServer(this IServiceCollection services, string workingDirectory)
    {
        return services
            .AddChangeLog()
            .AddSingleton(provider => new ChangeLogPathResolver(
                workingDirectory: workingDirectory,
                detector: provider.GetRequiredService<IChangeLogDetector>()
            ))
            .AddSingleton<ToolContext>()
            .AddSingleton<McpServerTool, ChangeLogAddTool>()
            .AddSingleton<McpServerTool, ChangeLogRemoveTool>()
            .AddSingleton<McpServerTool, ChangeLogUnreleasedTool>()
            .AddSingleton<McpServerTool, ChangeLogExtractTool>()
            .AddSingleton<McpServerTool, ChangeLogCreateReleaseTool>()
            .AddSingleton<McpServerTool, ChangeLogCheckInsertTool>()
            .AddSingleton<McpServerTool, ChangeLogLintTool>()
            .AddSingleton(provider => ServerOptionsFactory.Create(provider.GetServices<McpServerTool>()));
    }
}
