using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Credfeto.ChangeLog.Mcp;

public sealed class ShutdownSignal : IDisposable
{
    private readonly PosixSignalRegistration _interrupt;
    private readonly CancellationTokenSource _signalled = new();
    private readonly PosixSignalRegistration _terminate;

    public ShutdownSignal()
    {
        this._terminate = PosixSignalRegistration.Create(signal: PosixSignal.SIGTERM, handler: this.OnSignal);
        this._interrupt = PosixSignalRegistration.Create(signal: PosixSignal.SIGINT, handler: this.OnSignal);
    }

    public CancellationToken Requested => this._signalled.Token;

    public void Dispose()
    {
        this._terminate.Dispose();
        this._interrupt.Dispose();
        this._signalled.Dispose();
    }

    public void OnSignal(PosixSignalContext context)
    {
        // The server is stopped cleanly instead of the runtime terminating the process mid-write
        context.Cancel = true;
        this._signalled.Cancel();
    }
}
