using System;
using System.Text;

namespace TaleWorlds.Library;

public class BinaryReader : IReader
{
	private int _cursor;

	private byte[] _buffer;

	public byte[] Data { get; private set; }

	public int UnreadByteCount => Data.Length - _cursor;

	public BinaryReader(byte[] data)
	{
		Data = data;
		_cursor = 0;
		_buffer = new byte[4];
	}

	public ISerializableObject ReadSerializableObject()
	{
		throw new NotImplementedException();
	}

	public int Read3ByteInt()
	{
		_buffer[0] = ReadByte();
		_buffer[1] = ReadByte();
		_buffer[2] = ReadByte();
		if (_buffer[0] == byte.MaxValue && _buffer[1] == byte.MaxValue && _buffer[2] == byte.MaxValue)
		{
			_buffer[3] = byte.MaxValue;
		}
		else
		{
			_buffer[3] = 0;
		}
		return BitConverter.ToInt32(_buffer, 0);
	}

	public int ReadInt()
	{
		int result = BitConverter.ToInt32(Data, _cursor);
		_cursor += 4;
		return result;
	}

	public short ReadShort()
	{
		short result = BitConverter.ToInt16(Data, _cursor);
		_cursor += 2;
		return result;
	}

	public void ReadFloats(float[] output, int count)
	{
		int num = count * 4;
		Buffer.BlockCopy(Data, _cursor, output, 0, num);
		_cursor += num;
	}

	public void ReadShorts(short[] output, int count)
	{
		int num = count * 2;
		Buffer.BlockCopy(Data, _cursor, output, 0, num);
		_cursor += num;
	}

	public string ReadString()
	{
		int num = ReadInt();
		string result = null;
		if (num >= 0)
		{
			result = Encoding.UTF8.GetString(Data, _cursor, num);
			_cursor += num;
		}
		return result;
	}

	public Color ReadColor()
	{
		float red = ReadFloat();
		float green = ReadFloat();
		float blue = ReadFloat();
		float alpha = ReadFloat();
		return new Color(red, green, blue, alpha);
	}

	public bool ReadBool()
	{
		byte num = Data[_cursor];
		_cursor++;
		return num == 1;
	}

	public float ReadFloat()
	{
		float result = BitConverter.ToSingle(Data, _cursor);
		_cursor += 4;
		return result;
	}

	public uint ReadUInt()
	{
		uint result = BitConverter.ToUInt32(Data, _cursor);
		_cursor += 4;
		return result;
	}

	public ulong ReadULong()
	{
		ulong result = BitConverter.ToUInt64(Data, _cursor);
		_cursor += 8;
		return result;
	}

	public long ReadLong()
	{
		long result = BitConverter.ToInt64(Data, _cursor);
		_cursor += 8;
		return result;
	}

	public byte ReadByte()
	{
		byte result = Data[_cursor];
		_cursor++;
		return result;
	}

	public byte[] ReadBytes(int length)
	{
		byte[] array = new byte[length];
		Array.Copy(Data, _cursor, array, 0, length);
		_cursor += length;
		return array;
	}

	public Vec2 ReadVec2()
	{
		float a = ReadFloat();
		float b = ReadFloat();
		return new Vec2(a, b);
	}

	public Vec3 ReadVec3()
	{
		float x = ReadFloat();
		float y = ReadFloat();
		float z = ReadFloat();
		float w = ReadFloat();
		return new Vec3(x, y, z, w);
	}

	public Vec3i ReadVec3Int()
	{
		int x = ReadInt();
		int y = ReadInt();
		int z = ReadInt();
		return new Vec3i(x, y, z);
	}

	public sbyte ReadSByte()
	{
		sbyte result = (sbyte)Data[_cursor];
		_cursor++;
		return result;
	}

	public ushort ReadUShort()
	{
		ushort result = BitConverter.ToUInt16(Data, _cursor);
		_cursor += 2;
		return result;
	}

	public double ReadDouble()
	{
		double result = BitConverter.ToDouble(Data, _cursor);
		_cursor += 8;
		return result;
	}
}
