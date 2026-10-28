using System.Buffers.Binary;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;

namespace System.Reflection;

internal static class BlobUtilities
{
	public const int SizeOfSerializedDecimal = 13;

	public const int SizeOfGuid = 16;

	public static void WriteBytes(this byte[] buffer, int start, byte value, int byteCount)
	{
		new Span<byte>(buffer, start, byteCount).Fill(value);
	}

	public unsafe static void WriteDouble(this byte[] buffer, int start, double value)
	{
		buffer.WriteUInt64(start, *(ulong*)(&value));
	}

	public unsafe static void WriteSingle(this byte[] buffer, int start, float value)
	{
		buffer.WriteUInt32(start, *(uint*)(&value));
	}

	public static void WriteByte(this byte[] buffer, int start, byte value)
	{
		buffer[start] = value;
	}

	public static void WriteUInt16(this byte[] buffer, int start, ushort value)
	{
		Unsafe.WriteUnaligned(ref buffer[start], (!BitConverter.IsLittleEndian) ? BinaryPrimitives.ReverseEndianness(value) : value);
	}

	public static void WriteUInt16BE(this byte[] buffer, int start, ushort value)
	{
		Unsafe.WriteUnaligned(ref buffer[start], BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
	}

	public static void WriteUInt32BE(this byte[] buffer, int start, uint value)
	{
		Unsafe.WriteUnaligned(ref buffer[start], BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
	}

	public static void WriteUInt32(this byte[] buffer, int start, uint value)
	{
		Unsafe.WriteUnaligned(ref buffer[start], (!BitConverter.IsLittleEndian) ? BinaryPrimitives.ReverseEndianness(value) : value);
	}

	public static void WriteUInt64(this byte[] buffer, int start, ulong value)
	{
		Unsafe.WriteUnaligned(ref buffer[start], (!BitConverter.IsLittleEndian) ? BinaryPrimitives.ReverseEndianness(value) : value);
	}

	public static void WriteDecimal(this byte[] buffer, int start, decimal value)
	{
		value.GetBits(out var isNegative, out var scale, out var low, out var mid, out var high);
		buffer.WriteByte(start, (byte)(scale | (isNegative ? 128 : 0)));
		buffer.WriteUInt32(start + 1, low);
		buffer.WriteUInt32(start + 5, mid);
		buffer.WriteUInt32(start + 9, high);
	}

	public unsafe static void WriteGuid(this byte[] buffer, int start, Guid value)
	{
		fixed (byte* ptr = &buffer[start])
		{
			byte* ptr2 = (byte*)(&value);
			uint num = *(uint*)ptr2;
			*ptr = (byte)num;
			ptr[1] = (byte)(num >> 8);
			ptr[2] = (byte)(num >> 16);
			ptr[3] = (byte)(num >> 24);
			ushort num2 = ((ushort*)ptr2)[2];
			ptr[4] = (byte)num2;
			ptr[5] = (byte)(num2 >> 8);
			ushort num3 = ((ushort*)ptr2)[3];
			ptr[6] = (byte)num3;
			ptr[7] = (byte)(num3 >> 8);
			ptr[8] = ptr2[8];
			ptr[9] = ptr2[9];
			ptr[10] = ptr2[10];
			ptr[11] = ptr2[11];
			ptr[12] = ptr2[12];
			ptr[13] = ptr2[13];
			ptr[14] = ptr2[14];
			ptr[15] = ptr2[15];
		}
	}

	public unsafe static void WriteUTF8(this byte[] buffer, int start, char* charPtr, int charCount, int byteCount, bool allowUnpairedSurrogates)
	{
		char* ptr = charPtr + charCount;
		fixed (byte* ptr2 = &buffer[0])
		{
			byte* ptr3 = ptr2 + start;
			if (byteCount == charCount)
			{
				while (charPtr < ptr)
				{
					*(ptr3++) = (byte)(*(charPtr++));
				}
				return;
			}
			while (charPtr < ptr)
			{
				char c = *(charPtr++);
				if (c < '\u0080')
				{
					*(ptr3++) = (byte)c;
					continue;
				}
				if (c < 'ࠀ')
				{
					*ptr3 = (byte)((((int)c >> 6) & 0x1F) | 0xC0);
					ptr3[1] = (byte)((c & 0x3F) | 0x80);
					ptr3 += 2;
					continue;
				}
				if (IsSurrogateChar(c))
				{
					if (IsHighSurrogateChar(c) && charPtr < ptr && IsLowSurrogateChar(*charPtr))
					{
						char num = c;
						int num2 = *(charPtr++);
						int num3 = (num - 55296 << 10) + num2 - 56320 + 65536;
						*ptr3 = (byte)(((num3 >> 18) & 7) | 0xF0);
						ptr3[1] = (byte)(((num3 >> 12) & 0x3F) | 0x80);
						ptr3[2] = (byte)(((num3 >> 6) & 0x3F) | 0x80);
						ptr3[3] = (byte)((num3 & 0x3F) | 0x80);
						ptr3 += 4;
						continue;
					}
					if (!allowUnpairedSurrogates)
					{
						c = '\ufffd';
					}
				}
				*ptr3 = (byte)((((int)c >> 12) & 0xF) | 0xE0);
				ptr3[1] = (byte)((((int)c >> 6) & 0x3F) | 0x80);
				ptr3[2] = (byte)((c & 0x3F) | 0x80);
				ptr3 += 3;
			}
		}
	}

	internal unsafe static int GetUTF8ByteCount(string str)
	{
		fixed (char* str2 = str)
		{
			return GetUTF8ByteCount(str2, str.Length);
		}
	}

	internal unsafe static int GetUTF8ByteCount(char* str, int charCount)
	{
		char* remainder;
		return GetUTF8ByteCount(str, charCount, int.MaxValue, out remainder);
	}

	internal unsafe static int GetUTF8ByteCount(char* str, int charCount, int byteLimit, out char* remainder)
	{
		char* ptr = str + charCount;
		char* ptr2 = str;
		int num = 0;
		while (ptr2 < ptr)
		{
			char c = *(ptr2++);
			int num2;
			if (c < '\u0080')
			{
				num2 = 1;
			}
			else if (c < 'ࠀ')
			{
				num2 = 2;
			}
			else if (IsHighSurrogateChar(c) && ptr2 < ptr && IsLowSurrogateChar(*ptr2))
			{
				num2 = 4;
				ptr2++;
			}
			else
			{
				num2 = 3;
			}
			if (num + num2 > byteLimit)
			{
				ptr2 -= ((num2 < 4) ? 1 : 2);
				break;
			}
			num += num2;
		}
		remainder = ptr2;
		return num;
	}

	internal static bool IsSurrogateChar(int c)
	{
		return (uint)(c - 55296) <= 2047u;
	}

	internal static bool IsHighSurrogateChar(int c)
	{
		return (uint)(c - 55296) <= 1023u;
	}

	internal static bool IsLowSurrogateChar(int c)
	{
		return (uint)(c - 56320) <= 1023u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ValidateRange(int bufferLength, int start, int byteCount, string byteCountParameterName)
	{
		if (start < 0 || start > bufferLength)
		{
			System.Reflection.Throw.ArgumentOutOfRange("start");
		}
		if (byteCount < 0 || byteCount > bufferLength - start)
		{
			System.Reflection.Throw.ArgumentOutOfRange(byteCountParameterName);
		}
	}

	internal static int GetUserStringByteLength(int characterCount)
	{
		return characterCount * 2 + 1;
	}

	internal static byte GetUserStringTrailingByte(string str)
	{
		foreach (char c in str)
		{
			if (c >= '\u007f')
			{
				return 1;
			}
			switch ((int)c)
			{
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 23:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
			case 39:
			case 45:
				return 1;
			}
		}
		return 0;
	}
}
