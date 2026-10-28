using System;

namespace HarmonyLib;

internal class ByteBuffer
{
	internal byte[] buffer;

	internal int position;

	internal ByteBuffer(byte[] buffer)
	{
		this.buffer = buffer;
	}

	internal byte ReadByte()
	{
		CheckCanRead(1);
		return buffer[position++];
	}

	internal byte[] ReadBytes(int length)
	{
		CheckCanRead(length);
		byte[] array = new byte[length];
		Buffer.BlockCopy(buffer, position, array, 0, length);
		position += length;
		return array;
	}

	internal short ReadInt16()
	{
		CheckCanRead(2);
		short result = (short)(buffer[position] | (buffer[position + 1] << 8));
		position += 2;
		return result;
	}

	internal int ReadInt32()
	{
		CheckCanRead(4);
		int result = buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) | (buffer[position + 3] << 24);
		position += 4;
		return result;
	}

	internal long ReadInt64()
	{
		CheckCanRead(8);
		uint num = (uint)(buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) | (buffer[position + 3] << 24));
		uint num2 = (uint)(buffer[position + 4] | (buffer[position + 5] << 8) | (buffer[position + 6] << 16) | (buffer[position + 7] << 24));
		long result = (long)(((ulong)num2 << 32) | num);
		position += 8;
		return result;
	}

	internal float ReadSingle()
	{
		if (!BitConverter.IsLittleEndian)
		{
			byte[] array = ReadBytes(4);
			Array.Reverse(array);
			return BitConverter.ToSingle(array, 0);
		}
		CheckCanRead(4);
		float result = BitConverter.ToSingle(buffer, position);
		position += 4;
		return result;
	}

	internal double ReadDouble()
	{
		if (!BitConverter.IsLittleEndian)
		{
			byte[] array = ReadBytes(8);
			Array.Reverse(array);
			return BitConverter.ToDouble(array, 0);
		}
		CheckCanRead(8);
		double result = BitConverter.ToDouble(buffer, position);
		position += 8;
		return result;
	}

	private void CheckCanRead(int count)
	{
		if (position + count > buffer.Length)
		{
			throw new ArgumentOutOfRangeException("count", $"position({position}) + count({count}) > buffer.Length({buffer.Length})");
		}
	}
}
