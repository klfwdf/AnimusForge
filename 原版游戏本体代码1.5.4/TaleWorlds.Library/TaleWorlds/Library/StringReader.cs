using System;
using System.Globalization;

namespace TaleWorlds.Library;

public class StringReader : IReader
{
	private string[] _tokens;

	private int _currentIndex;

	public string Data { get; private set; }

	private string GetNextToken()
	{
		string result = _tokens[_currentIndex];
		_currentIndex++;
		return result;
	}

	public StringReader(string data)
	{
		Data = data;
		_tokens = data.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
	}

	public ISerializableObject ReadSerializableObject()
	{
		throw new NotImplementedException();
	}

	public int ReadInt()
	{
		return Convert.ToInt32(GetNextToken());
	}

	public short ReadShort()
	{
		return Convert.ToInt16(GetNextToken());
	}

	public string ReadString()
	{
		int num = ReadInt();
		int num2 = 0;
		string text = "";
		while (num2 < num)
		{
			string nextToken = GetNextToken();
			text += nextToken;
			num2 = text.Length;
			if (num2 < num)
			{
				text += " ";
			}
		}
		if (text.Length != num)
		{
			throw new Exception("invalid string format, length does not match");
		}
		return text;
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
		string nextToken = GetNextToken();
		if (nextToken == "1")
		{
			return true;
		}
		if (nextToken == "0")
		{
			return false;
		}
		return Convert.ToBoolean(nextToken);
	}

	public float ReadFloat()
	{
		return Convert.ToSingle(GetNextToken(), CultureInfo.InvariantCulture);
	}

	public uint ReadUInt()
	{
		return Convert.ToUInt32(GetNextToken());
	}

	public ulong ReadULong()
	{
		return Convert.ToUInt64(GetNextToken());
	}

	public long ReadLong()
	{
		return Convert.ToInt64(GetNextToken());
	}

	public byte ReadByte()
	{
		return Convert.ToByte(GetNextToken());
	}

	public byte[] ReadBytes(int length)
	{
		throw new NotImplementedException();
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
		return Convert.ToSByte(GetNextToken());
	}

	public ushort ReadUShort()
	{
		return Convert.ToUInt16(GetNextToken());
	}

	public double ReadDouble()
	{
		return Convert.ToDouble(GetNextToken());
	}
}
