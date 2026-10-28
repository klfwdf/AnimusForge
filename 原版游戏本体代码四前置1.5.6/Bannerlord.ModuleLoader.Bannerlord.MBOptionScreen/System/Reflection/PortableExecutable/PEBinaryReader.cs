using System.IO;
using System.Text;

namespace System.Reflection.PortableExecutable;

internal readonly struct PEBinaryReader(Stream stream, int size)
{
	private readonly long _startOffset = stream.Position;

	private readonly long _maxOffset = _startOffset + size;

	private readonly BinaryReader _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

	public int CurrentOffset => (int)(_reader.BaseStream.Position - _startOffset);

	public void Seek(int offset)
	{
		CheckBounds(_startOffset, offset);
		_reader.BaseStream.Seek(offset, SeekOrigin.Begin);
	}

	public byte[] ReadBytes(int count)
	{
		CheckBounds(_reader.BaseStream.Position, count);
		return _reader.ReadBytes(count);
	}

	public byte ReadByte()
	{
		CheckBounds(1u);
		return _reader.ReadByte();
	}

	public short ReadInt16()
	{
		CheckBounds(2u);
		return _reader.ReadInt16();
	}

	public ushort ReadUInt16()
	{
		CheckBounds(2u);
		return _reader.ReadUInt16();
	}

	public int ReadInt32()
	{
		CheckBounds(4u);
		return _reader.ReadInt32();
	}

	public uint ReadUInt32()
	{
		CheckBounds(4u);
		return _reader.ReadUInt32();
	}

	public ulong ReadUInt64()
	{
		CheckBounds(8u);
		return _reader.ReadUInt64();
	}

	public string ReadNullPaddedUTF8(int byteCount)
	{
		byte[] array = ReadBytes(byteCount);
		int count = 0;
		for (int num = array.Length; num > 0; num--)
		{
			if (array[num - 1] != 0)
			{
				count = num;
				break;
			}
		}
		return Encoding.UTF8.GetString(array, 0, count);
	}

	private void CheckBounds(uint count)
	{
		if ((ulong)(_reader.BaseStream.Position + count) > (ulong)_maxOffset)
		{
			System.Reflection.Throw.ImageTooSmall();
		}
	}

	private void CheckBounds(long startPosition, int count)
	{
		if ((ulong)(startPosition + (uint)count) > (ulong)_maxOffset)
		{
			System.Reflection.Throw.ImageTooSmallOrContainsInvalidOffsetOrCount();
		}
	}
}
