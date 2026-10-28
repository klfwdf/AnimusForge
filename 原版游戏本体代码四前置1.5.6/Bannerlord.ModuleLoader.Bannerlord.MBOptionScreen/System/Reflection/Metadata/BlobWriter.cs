using System.Collections.Immutable;
using System.IO;
using System.Reflection.Internal;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata;

internal struct BlobWriter
{
	private readonly byte[] _buffer;

	private readonly int _start;

	private readonly int _end;

	private int _position;

	internal bool IsDefault => _buffer == null;

	public int Offset
	{
		get
		{
			return _position - _start;
		}
		set
		{
			if (value < 0 || _start > _end - value)
			{
				System.Reflection.Throw.ValueArgumentOutOfRange();
			}
			_position = _start + value;
		}
	}

	public int Length => _end - _start;

	public int RemainingBytes => _end - _position;

	public Blob Blob => new Blob(_buffer, _start, Length);

	public BlobWriter(int size)
		: this(new byte[size])
	{
	}

	public BlobWriter(byte[] buffer)
		: this(buffer, 0, buffer.Length)
	{
	}

	public BlobWriter(Blob blob)
		: this(blob.Buffer, blob.Start, blob.Length)
	{
	}

	public BlobWriter(byte[] buffer, int start, int count)
	{
		_buffer = buffer;
		_start = start;
		_position = start;
		_end = start + count;
	}

	public bool ContentEquals(BlobWriter other)
	{
		if (Length == other.Length)
		{
			return _buffer.AsSpan(_start, Length).SequenceEqual(other._buffer.AsSpan(other._start, other.Length));
		}
		return false;
	}

	public byte[] ToArray()
	{
		return ToArray(0, Offset);
	}

	public byte[] ToArray(int start, int byteCount)
	{
		System.Reflection.BlobUtilities.ValidateRange(Length, start, byteCount, "byteCount");
		return _buffer.AsSpan(_start + start, byteCount).ToArray();
	}

	public System.Collections.Immutable.ImmutableArray<byte> ToImmutableArray()
	{
		return ToImmutableArray(0, Offset);
	}

	public System.Collections.Immutable.ImmutableArray<byte> ToImmutableArray(int start, int byteCount)
	{
		System.Reflection.BlobUtilities.ValidateRange(Length, start, byteCount, "byteCount");
		return System.Collections.Immutable.ImmutableArray.Create(_buffer.AsSpan(_start + start, byteCount));
	}

	private int Advance(int value)
	{
		int position = _position;
		if (position > _end - value)
		{
			System.Reflection.Throw.OutOfBounds();
		}
		_position = position + value;
		return position;
	}

	public void WriteBytes(byte value, int byteCount)
	{
		if (byteCount < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("byteCount");
		}
		int start = Advance(byteCount);
		_buffer.AsSpan(start, byteCount).Fill(value);
	}

	public unsafe void WriteBytes(byte* buffer, int byteCount)
	{
		if (buffer == null)
		{
			System.Reflection.Throw.ArgumentNull("buffer");
		}
		if (byteCount < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("byteCount");
		}
		WriteBytes(new ReadOnlySpan<byte>(buffer, byteCount));
	}

	internal void WriteBytes(ReadOnlySpan<byte> buffer)
	{
		int start = Advance(buffer.Length);
		buffer.CopyTo(_buffer.AsSpan(start));
	}

	public void WriteBytes(BlobBuilder source)
	{
		if (source == null)
		{
			System.Reflection.Throw.ArgumentNull("source");
		}
		source.WriteContentTo(ref this);
	}

	public int WriteBytes(Stream source, int byteCount)
	{
		if (source == null)
		{
			System.Reflection.Throw.ArgumentNull("source");
		}
		if (byteCount < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("byteCount");
		}
		int num = Advance(byteCount);
		int num2 = source.TryReadAll(_buffer, num, byteCount);
		_position = num + num2;
		return num2;
	}

	public void WriteBytes(System.Collections.Immutable.ImmutableArray<byte> buffer)
	{
		if (buffer.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("buffer");
		}
		WriteBytes(buffer.AsSpan());
	}

	public void WriteBytes(System.Collections.Immutable.ImmutableArray<byte> buffer, int start, int byteCount)
	{
		if (buffer.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("buffer");
		}
		System.Reflection.BlobUtilities.ValidateRange(buffer.Length, start, byteCount, "byteCount");
		WriteBytes(buffer.AsSpan(start, byteCount));
	}

	public void WriteBytes(byte[] buffer)
	{
		if (buffer == null)
		{
			System.Reflection.Throw.ArgumentNull("buffer");
		}
		WriteBytes(buffer.AsSpan());
	}

	public void WriteBytes(byte[] buffer, int start, int byteCount)
	{
		if (buffer == null)
		{
			System.Reflection.Throw.ArgumentNull("buffer");
		}
		System.Reflection.BlobUtilities.ValidateRange(buffer.Length, start, byteCount, "byteCount");
		WriteBytes(buffer.AsSpan(start, byteCount));
	}

	public void PadTo(int offset)
	{
		WriteBytes(0, offset - Offset);
	}

	public void Align(int alignment)
	{
		int offset = Offset;
		WriteBytes(0, BitArithmetic.Align(offset, alignment) - offset);
	}

	public void WriteBoolean(bool value)
	{
		WriteByte(value ? ((byte)1) : ((byte)0));
	}

	public void WriteByte(byte value)
	{
		int num = Advance(1);
		_buffer[num] = value;
	}

	public void WriteSByte(sbyte value)
	{
		WriteByte((byte)value);
	}

	public void WriteDouble(double value)
	{
		int start = Advance(8);
		_buffer.WriteDouble(start, value);
	}

	public void WriteSingle(float value)
	{
		int start = Advance(4);
		_buffer.WriteSingle(start, value);
	}

	public void WriteInt16(short value)
	{
		WriteUInt16((ushort)value);
	}

	public void WriteUInt16(ushort value)
	{
		int start = Advance(2);
		_buffer.WriteUInt16(start, value);
	}

	public void WriteInt16BE(short value)
	{
		WriteUInt16BE((ushort)value);
	}

	public void WriteUInt16BE(ushort value)
	{
		int start = Advance(2);
		_buffer.WriteUInt16BE(start, value);
	}

	public void WriteInt32BE(int value)
	{
		WriteUInt32BE((uint)value);
	}

	public void WriteUInt32BE(uint value)
	{
		int start = Advance(4);
		_buffer.WriteUInt32BE(start, value);
	}

	public void WriteInt32(int value)
	{
		WriteUInt32((uint)value);
	}

	public void WriteUInt32(uint value)
	{
		int start = Advance(4);
		_buffer.WriteUInt32(start, value);
	}

	public void WriteInt64(long value)
	{
		WriteUInt64((ulong)value);
	}

	public void WriteUInt64(ulong value)
	{
		int start = Advance(8);
		_buffer.WriteUInt64(start, value);
	}

	public void WriteDecimal(decimal value)
	{
		int start = Advance(13);
		_buffer.WriteDecimal(start, value);
	}

	public void WriteGuid(Guid value)
	{
		int start = Advance(16);
		_buffer.WriteGuid(start, value);
	}

	public void WriteDateTime(DateTime value)
	{
		WriteInt64(value.Ticks);
	}

	public void WriteReference(int reference, bool isSmall)
	{
		if (isSmall)
		{
			WriteUInt16((ushort)reference);
		}
		else
		{
			WriteInt32(reference);
		}
	}

	public void WriteUTF16(char[] value)
	{
		if (value == null)
		{
			System.Reflection.Throw.ArgumentNull("value");
		}
		WriteUTF16(value.AsSpan());
	}

	public void WriteUTF16(string value)
	{
		if (value == null)
		{
			System.Reflection.Throw.ArgumentNull("value");
		}
		WriteUTF16(value.AsSpan());
	}

	private void WriteUTF16(ReadOnlySpan<char> value)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteBytes(MemoryMarshal.AsBytes(value));
			return;
		}
		ReadOnlySpan<char> readOnlySpan = value;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char value2 = readOnlySpan[i];
			WriteUInt16(value2);
		}
	}

	public void WriteSerializedString(string? str)
	{
		if (str == null)
		{
			WriteByte(byte.MaxValue);
		}
		else
		{
			WriteUTF8(str, 0, str.Length, allowUnpairedSurrogates: true, prependSize: true);
		}
	}

	public void WriteUserString(string value)
	{
		if (value == null)
		{
			System.Reflection.Throw.ArgumentNull("value");
		}
		WriteCompressedInteger(System.Reflection.BlobUtilities.GetUserStringByteLength(value.Length));
		WriteUTF16(value);
		WriteByte(System.Reflection.BlobUtilities.GetUserStringTrailingByte(value));
	}

	public void WriteUTF8(string value, bool allowUnpairedSurrogates)
	{
		if (value == null)
		{
			System.Reflection.Throw.ArgumentNull("value");
		}
		WriteUTF8(value, 0, value.Length, allowUnpairedSurrogates, prependSize: false);
	}

	private unsafe void WriteUTF8(string str, int start, int length, bool allowUnpairedSurrogates, bool prependSize)
	{
		fixed (char* ptr = str)
		{
			char* ptr2 = ptr + start;
			int uTF8ByteCount = System.Reflection.BlobUtilities.GetUTF8ByteCount(ptr2, length);
			if (prependSize)
			{
				WriteCompressedInteger(uTF8ByteCount);
			}
			int start2 = Advance(uTF8ByteCount);
			_buffer.WriteUTF8(start2, ptr2, length, uTF8ByteCount, allowUnpairedSurrogates);
		}
	}

	public void WriteCompressedSignedInteger(int value)
	{
		BlobWriterImpl.WriteCompressedSignedInteger(ref this, value);
	}

	public void WriteCompressedInteger(int value)
	{
		BlobWriterImpl.WriteCompressedInteger(ref this, (uint)value);
	}

	public void WriteConstant(object? value)
	{
		BlobWriterImpl.WriteConstant(ref this, value);
	}

	public void Clear()
	{
		_position = _start;
	}
}
