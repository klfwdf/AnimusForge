using System;
using System.Text;

namespace TaleWorlds.Library;

public class BinaryWriter : IWriter
{
	private byte[] _data;

	private int _availableIndex;

	public byte[] Data => _data;

	public int Length => _availableIndex;

	public BinaryWriter()
	{
		_data = new byte[4096];
		_availableIndex = 0;
	}

	public BinaryWriter(int capacity)
	{
		_data = new byte[capacity];
		_availableIndex = 0;
	}

	public void Clear()
	{
		Array.Clear(_data, 0, _data.Length);
		_availableIndex = 0;
	}

	public void EnsureLength(int added)
	{
		int num = _availableIndex + added;
		if (num > _data.Length)
		{
			int num2 = _data.Length * 2;
			if (num > num2)
			{
				num2 = num;
			}
			byte[] array = new byte[num2];
			Buffer.BlockCopy(_data, 0, array, 0, _availableIndex);
			_data = array;
		}
	}

	public void WriteSerializableObject(ISerializableObject serializableObject)
	{
		throw new NotImplementedException();
	}

	public void WriteByte(byte value)
	{
		EnsureLength(1);
		_data[_availableIndex] = value;
		_availableIndex++;
	}

	public void WriteBytes(byte[] bytes)
	{
		EnsureLength(bytes.Length);
		Buffer.BlockCopy(bytes, 0, _data, _availableIndex, bytes.Length);
		_availableIndex += bytes.Length;
	}

	public void Write3ByteInt(int value)
	{
		EnsureLength(3);
		_data[_availableIndex++] = (byte)value;
		_data[_availableIndex++] = (byte)(value >> 8);
		_data[_availableIndex++] = (byte)(value >> 16);
	}

	public void WriteInt(int value)
	{
		EnsureLength(4);
		_data[_availableIndex++] = (byte)value;
		_data[_availableIndex++] = (byte)(value >> 8);
		_data[_availableIndex++] = (byte)(value >> 16);
		_data[_availableIndex++] = (byte)(value >> 24);
	}

	public void WriteShort(short value)
	{
		EnsureLength(2);
		_data[_availableIndex++] = (byte)value;
		_data[_availableIndex++] = (byte)(value >> 8);
	}

	public void WriteString(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			WriteInt(bytes.Length);
			WriteBytes(bytes);
		}
		else
		{
			WriteInt(0);
		}
	}

	public void WriteFloats(float[] value, int count)
	{
		int num = count * 4;
		EnsureLength(num);
		Buffer.BlockCopy(value, 0, _data, _availableIndex, num);
		_availableIndex += num;
	}

	public void WriteShorts(short[] value, int count)
	{
		int num = count * 2;
		EnsureLength(num);
		Buffer.BlockCopy(value, 0, _data, _availableIndex, num);
		_availableIndex += num;
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
		EnsureLength(1);
		_data[_availableIndex] = (byte)(value ? 1u : 0u);
		_availableIndex++;
	}

	public void WriteFloat(float value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		EnsureLength(bytes.Length);
		Buffer.BlockCopy(bytes, 0, _data, _availableIndex, bytes.Length);
		_availableIndex += bytes.Length;
	}

	public void WriteUInt(uint value)
	{
		EnsureLength(4);
		_data[_availableIndex++] = (byte)value;
		_data[_availableIndex++] = (byte)(value >> 8);
		_data[_availableIndex++] = (byte)(value >> 16);
		_data[_availableIndex++] = (byte)(value >> 24);
	}

	public void WriteULong(ulong value)
	{
		EnsureLength(8);
		_data[_availableIndex++] = (byte)value;
		_data[_availableIndex++] = (byte)(value >> 8);
		_data[_availableIndex++] = (byte)(value >> 16);
		_data[_availableIndex++] = (byte)(value >> 24);
		_data[_availableIndex++] = (byte)(value >> 32);
		_data[_availableIndex++] = (byte)(value >> 40);
		_data[_availableIndex++] = (byte)(value >> 48);
		_data[_availableIndex++] = (byte)(value >> 56);
	}

	public void WriteLong(long value)
	{
		EnsureLength(8);
		_data[_availableIndex++] = (byte)value;
		_data[_availableIndex++] = (byte)(value >> 8);
		_data[_availableIndex++] = (byte)(value >> 16);
		_data[_availableIndex++] = (byte)(value >> 24);
		_data[_availableIndex++] = (byte)(value >> 32);
		_data[_availableIndex++] = (byte)(value >> 40);
		_data[_availableIndex++] = (byte)(value >> 48);
		_data[_availableIndex++] = (byte)(value >> 56);
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
		EnsureLength(1);
		_data[_availableIndex] = (byte)value;
		_availableIndex++;
	}

	public void WriteUShort(ushort value)
	{
		EnsureLength(2);
		_data[_availableIndex++] = (byte)value;
		_data[_availableIndex++] = (byte)(value >> 8);
	}

	public void WriteDouble(double value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		EnsureLength(bytes.Length);
		Buffer.BlockCopy(bytes, 0, _data, _availableIndex, bytes.Length);
		_availableIndex += bytes.Length;
	}

	public void AppendData(BinaryWriter writer)
	{
		EnsureLength(writer._availableIndex);
		Buffer.BlockCopy(writer._data, 0, _data, _availableIndex, writer._availableIndex);
		_availableIndex += writer._availableIndex;
	}

	public byte[] GetFinalData()
	{
		byte[] array = new byte[_availableIndex];
		Buffer.BlockCopy(_data, 0, array, 0, _availableIndex);
		return array;
	}
}
