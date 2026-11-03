using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.ReplyTool;

// Lazy response conversion keeps AF's original status/stale acceptance order.
internal sealed class ReplyHttpContent : HttpContent
{
    private readonly HttpContent _inner;
    private readonly bool _streaming;
    private readonly CancellationToken _token;
    private Stream _owned;
    internal ReplyHttpContent(HttpContent inner, bool streaming, CancellationToken token)
    {
        _inner = inner; _streaming = streaming; _token = token;
        foreach (var header in inner.Headers)
            if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (!streaming) Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
    }
    protected override bool TryComputeLength(out long length) { length = 0; return false; }
    protected override async Task<Stream> CreateContentReadStreamAsync()
    {
        _token.ThrowIfCancellationRequested();
        if (_streaming) _owned = new ReplySseStream(await _inner.ReadAsStreamAsync().ConfigureAwait(false), _token);
        else
        {
            string raw = await _inner.ReadAsStringAsync().ConfigureAwait(false);
            _token.ThrowIfCancellationRequested();
            _owned = new MemoryStream(Encoding.UTF8.GetBytes(ReplyProtocol.Restore(raw)), false);
        }
        _token.ThrowIfCancellationRequested();
        return _owned;
    }
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        using (Stream source = await CreateContentReadStreamAsync().ConfigureAwait(false))
            await source.CopyToAsync(stream, 8192, _token).ConfigureAwait(false);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _owned?.Dispose(); _inner.Dispose(); }
        base.Dispose(disposing);
    }
}

internal sealed class ReplySseStream : Stream
{
    private readonly StreamReader _reader;
    private readonly CancellationToken _token;
    private readonly ReplyStreamState _state = new ReplyStreamState();
    private byte[] _pending = Array.Empty<byte>();
    private int _offset;
    private bool _eof;
    internal ReplySseStream(Stream inner, CancellationToken token)
    {
        _reader = new StreamReader(inner, new UTF8Encoding(false, true), false, 4096); _token = token;
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
        if (count == 0) return 0;
        cancellationToken.ThrowIfCancellationRequested(); _token.ThrowIfCancellationRequested();
        while (_offset == _pending.Length && !_eof)
        {
            var data = new StringBuilder(); bool hasData = false;
            while (true)
            {
                _token.ThrowIfCancellationRequested();
                string line = await _reader.ReadLineAsync().ConfigureAwait(false);
                _token.ThrowIfCancellationRequested(); cancellationToken.ThrowIfCancellationRequested();
                if (line == null) { _eof = true; break; }
                if (line.Length == 0) { if (hasData) break; continue; }
                if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
                if (hasData) data.Append('\n');
                string value = line.Substring(5);
                data.Append(value.StartsWith(" ", StringComparison.Ordinal) ? value.Substring(1) : value); hasData = true;
            }
            if (hasData)
            {
                string value = data.ToString();
                _pending = value.Length > 0 ? Encoding.UTF8.GetBytes("data: " + _state.Transform(value) + "\n\n") : Array.Empty<byte>();
                _offset = 0;
            }
            if (_eof) _state.EndOfStream();
        }
        int copied = Math.Min(count, _pending.Length - _offset);
        Buffer.BlockCopy(_pending, _offset, buffer, offset, copied); _offset += copied; return copied;
    }
    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    protected override void Dispose(bool disposing) { if (disposing) _reader.Dispose(); base.Dispose(disposing); }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
