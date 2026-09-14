using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Thread-confined framed SHA256 sink for complete memory-source fields.
/// Fixed buffer only; no reflection, JSON, boxing of scalar values or whole-payload bytes.
/// The caller owns schema/type ordering. Strings preserve every UTF-16 code unit.
/// This reduces atomic work cost, not its O(N) source traversal or a game-frame bound.
/// </summary>
internal sealed class MemorySourceFingerprintWriter : IDisposable
{
    private readonly SHA256 _hash = SHA256.Create();
    private readonly byte[] _buffer = new byte[4096];
    private int _count;
    private bool _complete;

    internal void Write(bool value) => WriteByte(value ? (byte)1 : (byte)0);
    internal void Write(bool? value)
    {
        Write(value.HasValue);
        if (value.HasValue) Write(value.Value);
    }
    internal void Write(int value)
    {
        for (int shift = 0; shift < 32; shift += 8) WriteByte((byte)((value >> shift) & 0xff));
    }
    internal void Write(long value)
    {
        Write(unchecked((int)value));
        Write(unchecked((int)(value >> 32)));
    }
    internal void Write(string value)
    {
        Write(value == null ? -1 : value.Length);
        if (value == null) return;
        foreach (char character in value)
        {
            WriteByte((byte)(character & 0xff));
            WriteByte((byte)(character >> 8));
        }
    }
    internal void WriteList<T>(List<T> values, Action<MemorySourceFingerprintWriter, T> write)
    {
        Write(values == null ? -1 : values.Count);
        if (values == null) return;
        // The real List enumerator retains structural mutation detection.
        foreach (T value in values) write(this, value);
    }
    private void WriteByte(byte value)
    {
        if (_complete) throw new InvalidOperationException("Fingerprint is already complete.");
        _buffer[_count++] = value;
        if (_count != _buffer.Length) return;
        _hash.TransformBlock(_buffer, 0, _count, _buffer, 0);
        _count = 0;
    }
    internal string Finish()
    {
        if (_complete) throw new InvalidOperationException("Fingerprint is already complete.");
        _hash.TransformFinalBlock(_buffer, 0, _count);
        _complete = true;
        return BitConverter.ToString(_hash.Hash).Replace("-", "");
    }
    public void Dispose()
    {
        _complete = true;
        _hash.Dispose();
    }
}
