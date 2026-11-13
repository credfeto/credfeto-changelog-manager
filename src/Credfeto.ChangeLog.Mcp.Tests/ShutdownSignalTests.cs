using System.Runtime.InteropServices;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Mcp.Tests;

public sealed class ShutdownSignalTests : TestBase
{
    [Fact]
    public void NotRequestedUntilSignalled()
    {
        using ShutdownSignal shutdown = new();

        Assert.False(condition: shutdown.Requested.IsCancellationRequested, userMessage: "No signal has been received");
    }

    [Fact]
    public void SignalRequestsShutdownAndStopsTheRuntimeTerminating()
    {
        using ShutdownSignal shutdown = new();
        PosixSignalContext context = new(PosixSignal.SIGTERM);

        shutdown.OnSignal(context);

        Assert.True(condition: context.Cancel, userMessage: "The runtime should not terminate the process");
        Assert.True(condition: shutdown.Requested.IsCancellationRequested, userMessage: "Shutdown should be requested");
    }
}
