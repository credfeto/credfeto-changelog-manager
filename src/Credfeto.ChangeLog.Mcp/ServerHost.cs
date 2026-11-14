using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Mcp.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;

namespace Credfeto.ChangeLog.Mcp;

public static class ServerHost
{
    private const int INVALID_OPTIONS_EXIT_CODE = 1;

    public static async Task<int> RunAsync(Stream input, Stream output)
    {
        // Stdout carries the protocol; anything the library writes to the console must not corrupt it
        Console.SetOut(Console.Error);

        using ShutdownSignal shutdown = new();
        await using Stream ownedInput = input;
        await using Stream ownedOutput = output;

        return await RunAsync(
            workingDirectory: Environment.CurrentDirectory,
            repositoryRoot: RepositoryRootOption.Read(),
            error: Console.Error,
            input: ownedInput,
            output: ownedOutput,
            stopRequested: shutdown.Requested
        );
    }

    public static async Task<int> RunAsync(
        string workingDirectory,
        string? repositoryRoot,
        TextWriter error,
        Stream input,
        Stream output,
        CancellationToken stopRequested
    )
    {
        string? root;

        try
        {
            root = RepositoryRootOption.Parse(repositoryRoot);
        }
        catch (RepositoryRootException exception)
        {
            // The reason for exiting must still be reported when shutdown has already been requested
            await error.WriteLineAsync(exception.Message.AsMemory(), cancellationToken: CancellationToken.None);

            return INVALID_OPTIONS_EXIT_CODE;
        }

        return await ServeAsync(
            workingDirectory: workingDirectory,
            repositoryRoot: root,
            input: input,
            output: output,
            stopRequested: stopRequested
        );
    }

    private static async Task<int> ServeAsync(
        string workingDirectory,
        string? repositoryRoot,
        Stream input,
        Stream output,
        CancellationToken stopRequested
    )
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddChangeLogMcpServer(workingDirectory: workingDirectory, repositoryRoot: repositoryRoot)
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
