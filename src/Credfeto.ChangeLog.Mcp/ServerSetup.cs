using Credfeto.ChangeLog.Mcp.Paths;
using Credfeto.ChangeLog.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp;

public static class ServerSetup
{
    public static IServiceCollection AddChangeLogMcpServer(
        this IServiceCollection services,
        string workingDirectory,
        string? repositoryRoot
    )
    {
        return services
            .AddChangeLog()
            .AddChangeLogPaths(workingDirectory: workingDirectory, repositoryRoot: repositoryRoot)
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

    private static IServiceCollection AddChangeLogPaths(
        this IServiceCollection services,
        string workingDirectory,
        string? repositoryRoot
    )
    {
        if (repositoryRoot is null)
        {
            return services.AddSingleton(provider =>
                ChangeLogPathResolver.FromWorkingDirectory(
                    workingDirectory: workingDirectory,
                    detector: provider.GetRequiredService<IChangeLogDetector>()
                )
            );
        }

        return services
            .Replace(
                ServiceDescriptor.Singleton<IChangeLogDetector>(new RepositoryRootChangeLogDetector(repositoryRoot))
            )
            .AddSingleton(provider =>
                ChangeLogPathResolver.FromRepositoryRoot(
                    repositoryRoot: repositoryRoot,
                    detector: provider.GetRequiredService<IChangeLogDetector>()
                )
            );
    }
}
