using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp;

public static class ServerHost
{
    public static async Task<int> RunAsync(
        string workingDirectory,
        Stream input,
        Stream output,
        CancellationToken stopRequested
    )
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddChangeLogMcpServer(workingDirectory)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        McpServerOptions options = services.GetRequiredService<McpServerOptions>();

        await using StreamServerTransport transport = new(
            inputStream: input,
            outputStream: output,
            serverName: options.ServerInfo?.Name,
            loggerFactory: NullLoggerFactory.Instance
        );
        await using McpServer server = McpServer.Create(
            transport: transport,
            serverOptions: options,
            loggerFactory: NullLoggerFactory.Instance,
            serviceProvider: services
        );

        // Stopping is a normal shutdown, so the cancellation it causes is not an error
        await server.RunAsync(stopRequested).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return 0;
    }
}
