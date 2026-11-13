using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Mcp.Tests.Streams;

internal sealed class CapturingOutputStream : Stream
{
    private readonly Lock _lock = new();
    private readonly string _marker;
    private readonly CancellationTokenSource _seen = new();
    private readonly StringBuilder _text = new();

    public CapturingOutputStream(string marker)
    {
        this._marker = marker;
    }

    public CancellationToken MarkerWritten => this._seen.Token;

    public string Text
    {
        get
        {
            lock (this._lock)
            {
                return this._text.ToString();
            }
        }
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return this.AppendAsync(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return this.AppendAsync(buffer.AsMemory(start: offset, length: count)).AsTask();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._seen.Dispose();
        }

        base.Dispose(disposing);
    }

    private async ValueTask AppendAsync(ReadOnlyMemory<byte> bytes)
    {
        bool seen;

        lock (this._lock)
        {
            this._text.Append(Encoding.UTF8.GetString(bytes.Span));
            seen = this._text.ToString().Contains(value: this._marker, comparisonType: StringComparison.Ordinal);
        }

        // Cancelled outside the lock because cancellation runs the server's shutdown callbacks on this thread
        if (seen && !this._seen.IsCancellationRequested)
        {
            await this._seen.CancelAsync();
        }
    }
}
