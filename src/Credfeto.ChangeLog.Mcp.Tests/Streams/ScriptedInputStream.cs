using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Mcp.Tests.Streams;

internal sealed class ScriptedInputStream : Stream
{
    private readonly byte[] _script;
    private readonly bool _waitAtEnd;
    private int _position;

    public ScriptedInputStream(string script, bool waitAtEnd)
    {
        this._script = Encoding.UTF8.GetBytes(script);
        this._waitAtEnd = waitAtEnd;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int remaining = this._script.Length - this._position;

        if (remaining > 0)
        {
            int count = Math.Min(val1: remaining, val2: buffer.Length);
            this._script.AsMemory(start: this._position, length: count).CopyTo(buffer);
            this._position += count;

            return count;
        }

        if (this._waitAtEnd)
        {
            // Behaves like an idle client that keeps stdin open until the server shuts down
            await Task.Delay(millisecondsDelay: Timeout.Infinite, cancellationToken: cancellationToken);
        }

        return 0;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return this.ReadAsync(buffer.AsMemory(start: offset, length: count), cancellationToken).AsTask();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
