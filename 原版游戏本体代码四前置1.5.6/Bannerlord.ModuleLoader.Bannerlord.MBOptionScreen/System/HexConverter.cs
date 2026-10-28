using System.Runtime.CompilerServices;

namespace System;

internal static class HexConverter
{
	public enum Casing : uint
	{
		Upper = 0u,
		Lower = 8224u
	}

	public static ReadOnlySpan<byte> CharToHexLookup => new byte[256]
	{
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 0, 1,
		2, 3, 4, 5, 6, 7, 8, 9, 255, 255,
		255, 255, 255, 255, 255, 10, 11, 12, 13, 14,
		15, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 10, 11, 12,
		13, 14, 15, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ToBytesBuffer(byte value, Span<byte> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
	{
		uint num = (uint)(((value & 0xF0) << 4) + (value & 0xF) - 35209);
		uint num2 = ((((0 - num) & 0x7070) >> 4) + num + 47545) | (uint)casing;
		buffer[startingIndex + 1] = (byte)num2;
		buffer[startingIndex] = (byte)(num2 >> 8);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
	{
		uint num = (uint)(((value & 0xF0) << 4) + (value & 0xF) - 35209);
		uint num2 = ((((0 - num) & 0x7070) >> 4) + num + 47545) | (uint)casing;
		buffer[startingIndex + 1] = (char)(num2 & 0xFF);
		buffer[startingIndex] = (char)(num2 >> 8);
	}

	public static void EncodeToUtf16(ReadOnlySpan<byte> bytes, Span<char> chars, Casing casing = Casing.Upper)
	{
		for (int i = 0; i < bytes.Length; i++)
		{
			ToCharsBuffer(bytes[i], chars, i * 2, casing);
		}
	}

	public static string ToString(ReadOnlySpan<byte> bytes, Casing casing = Casing.Upper)
	{
		Span<char> span = ((bytes.Length <= 16) ? stackalloc char[bytes.Length * 2] : new char[bytes.Length * 2].AsSpan());
		Span<char> buffer = span;
		int num = 0;
		ReadOnlySpan<byte> readOnlySpan = bytes;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			ToCharsBuffer(readOnlySpan[i], buffer, num, casing);
			num += 2;
		}
		return buffer.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char ToCharUpper(int value)
	{
		value &= 0xF;
		value += 48;
		if (value > 57)
		{
			value += 7;
		}
		return (char)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char ToCharLower(int value)
	{
		value &= 0xF;
		value += 48;
		if (value > 57)
		{
			value += 39;
		}
		return (char)value;
	}

	public static bool TryDecodeFromUtf16(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
	{
		return TryDecodeFromUtf16_Scalar(chars, bytes, out charsProcessed);
	}

	private static bool TryDecodeFromUtf16_Scalar(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		while (num2 < bytes.Length)
		{
			num3 = FromChar(chars[num + 1]);
			num4 = FromChar(chars[num]);
			if ((num3 | num4) == 255)
			{
				break;
			}
			bytes[num2++] = (byte)((num4 << 4) | num3);
			num += 2;
		}
		if (num3 == 255)
		{
			num++;
		}
		charsProcessed = num;
		return (num3 | num4) != 255;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FromChar(int c)
	{
		if (c < CharToHexLookup.Length)
		{
			return CharToHexLookup[c];
		}
		return 255;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FromUpperChar(int c)
	{
		if (c <= 71)
		{
			return CharToHexLookup[c];
		}
		return 255;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FromLowerChar(int c)
	{
		switch (c)
		{
		case 48:
		case 49:
		case 50:
		case 51:
		case 52:
		case 53:
		case 54:
		case 55:
		case 56:
		case 57:
			return c - 48;
		case 97:
		case 98:
		case 99:
		case 100:
		case 101:
		case 102:
			return c - 97 + 10;
		default:
			return 255;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsHexChar(int c)
	{
		if (IntPtr.Size == 8)
		{
			ulong num = (uint)(c - 48);
			long num2 = -17875860044349952L << (int)num;
			ulong num3 = num - 64;
			return (long)((ulong)num2 & num3) < 0L;
		}
		return FromChar(c) != 255;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsHexUpperChar(int c)
	{
		if ((uint)(c - 48) > 9u)
		{
			return (uint)(c - 65) <= 5u;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsHexLowerChar(int c)
	{
		if ((uint)(c - 48) > 9u)
		{
			return (uint)(c - 97) <= 5u;
		}
		return true;
	}
}
