using System;
using System.IO;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Mcp;

public static class Program
{
    public static async Task<int> Main()
    {
        // Stdout carries the protocol; anything the library writes to the console must not corrupt it
        Console.SetOut(Console.Error);

        using ShutdownSignal shutdown = new();
        await using Stream input = Console.OpenStandardInput();
        await using Stream output = Console.OpenStandardOutput();

        return await ServerHost.RunAsync(
            workingDirectory: Environment.CurrentDirectory,
            input: input,
            output: output,
            stopRequested: shutdown.Requested
        );
    }
}
