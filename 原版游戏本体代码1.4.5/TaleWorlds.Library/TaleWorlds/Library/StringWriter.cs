using System;
using System.Text;

namespace TaleWorlds.Library;

public class StringWriter : IWriter
{
	private StringBuilder _stringBuilder;

	public string Data => _stringBuilder.ToString();

	public StringWriter()
	{
		_stringBuilder = new StringBuilder();
	}

	private void AddToken(string token)
	{
		_stringBuilder.Append(token);
		_stringBuilder.Append(" ");
	}

	public void WriteSerializableObject(ISerializableObject serializableObject)
	{
		throw new NotImplementedException();
	}

	public void WriteByte(byte value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteBytes(byte[] bytes)
	{
		throw new NotImplementedException();
	}

	public void WriteInt(int value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteShort(short value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteString(string value)
	{
		WriteInt(value.Length);
		AddToken(value);
	}

	public void WriteColor(Color value)
	{
		WriteFloat(value.Red);
		WriteFloat(value.Green);
		WriteFloat(value.Blue);
		WriteFloat(value.Alpha);
	}

	public void WriteBool(bool value)
	{
		AddToken(value ? "1" : "0");
	}

	public void WriteFloat(float value)
	{
		AddToken((value == 0f) ? "0" : Convert.ToString(value));
	}

	public void WriteUInt(uint value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteULong(ulong value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteLong(long value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteVec2(Vec2 vec2)
	{
		WriteFloat(vec2.x);
		WriteFloat(vec2.y);
	}

	public void WriteVec3(Vec3 vec3)
	{
		WriteFloat(vec3.x);
		WriteFloat(vec3.y);
		WriteFloat(vec3.z);
		WriteFloat(vec3.w);
	}

	public void WriteVec3Int(Vec3i vec3)
	{
		WriteInt(vec3.X);
		WriteInt(vec3.Y);
		WriteInt(vec3.Z);
	}

	public void WriteSByte(sbyte value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteUShort(ushort value)
	{
		AddToken(Convert.ToString(value));
	}

	public void WriteDouble(double value)
	{
		AddToken((value == 0.0) ? "0" : Convert.ToString(value));
	}
}
