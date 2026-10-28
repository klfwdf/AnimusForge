using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Text;

internal static class Base64
{
	private static readonly sbyte[] s_decodingMap = new sbyte[256]
	{
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, 62, -1, -1, -1, 63, 52, 53,
		54, 55, 56, 57, 58, 59, 60, 61, -1, -1,
		-1, -1, -1, -1, -1, 0, 1, 2, 3, 4,
		5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
		25, -1, -1, -1, -1, -1, -1, 26, 27, 28,
		29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
		39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
		49, 50, 51, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1
	};

	private static readonly byte[] s_encodingMap = new byte[64]
	{
		65, 66, 67, 68, 69, 70, 71, 72, 73, 74,
		75, 76, 77, 78, 79, 80, 81, 82, 83, 84,
		85, 86, 87, 88, 89, 90, 97, 98, 99, 100,
		101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
		111, 112, 113, 114, 115, 116, 117, 118, 119, 120,
		121, 122, 48, 49, 50, 51, 52, 53, 54, 55,
		56, 57, 43, 47
	};

	private const byte EncodingPad = 61;

	private const int MaximumEncodeLength = 1610612733;

	public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
	{
		ref byte reference = ref MemoryMarshal.GetReference(utf8);
		ref byte reference2 = ref MemoryMarshal.GetReference(bytes);
		int num = utf8.Length & -4;
		int length = bytes.Length;
		int num2 = 0;
		int num3 = 0;
		if (utf8.Length != 0)
		{
			ref sbyte reference3 = ref s_decodingMap[0];
			int num4 = (isFinalBlock ? 4 : 0);
			int num5 = 0;
			num5 = ((length < GetMaxDecodedFromUtf8Length(num)) ? (length / 3 * 4) : (num - num4));
			while (true)
			{
				if (num2 < num5)
				{
					int num6 = Decode(ref Unsafe.Add(ref reference, num2), ref reference3);
					if (num6 >= 0)
					{
						WriteThreeLowOrderBytes(ref Unsafe.Add(ref reference2, num3), num6);
						num3 += 3;
						num2 += 4;
						continue;
					}
				}
				else
				{
					if (num5 != num - num4)
					{
						goto IL_0205;
					}
					if (num2 == num)
					{
						if (!isFinalBlock)
						{
							bytesConsumed = num2;
							bytesWritten = num3;
							return OperationStatus.NeedMoreData;
						}
					}
					else
					{
						int elementOffset = Unsafe.Add(ref reference, num - 4);
						int elementOffset2 = Unsafe.Add(ref reference, num - 3);
						int num7 = Unsafe.Add(ref reference, num - 2);
						int num8 = Unsafe.Add(ref reference, num - 1);
						elementOffset = Unsafe.Add(ref reference3, elementOffset);
						elementOffset2 = Unsafe.Add(ref reference3, elementOffset2);
						elementOffset <<= 18;
						elementOffset2 <<= 12;
						elementOffset |= elementOffset2;
						if (num8 != 61)
						{
							num7 = Unsafe.Add(ref reference3, num7);
							num8 = Unsafe.Add(ref reference3, num8);
							num7 <<= 6;
							elementOffset |= num8;
							elementOffset |= num7;
							if (elementOffset >= 0)
							{
								if (num3 <= length - 3)
								{
									WriteThreeLowOrderBytes(ref Unsafe.Add(ref reference2, num3), elementOffset);
									num3 += 3;
									goto IL_01eb;
								}
								goto IL_0205;
							}
						}
						else if (num7 != 61)
						{
							num7 = Unsafe.Add(ref reference3, num7);
							num7 <<= 6;
							elementOffset |= num7;
							if (elementOffset >= 0)
							{
								if (num3 <= length - 2)
								{
									Unsafe.Add(ref reference2, num3) = (byte)(elementOffset >> 16);
									Unsafe.Add(ref reference2, num3 + 1) = (byte)(elementOffset >> 8);
									num3 += 2;
									goto IL_01eb;
								}
								goto IL_0205;
							}
						}
						else if (elementOffset >= 0)
						{
							if (num3 <= length - 1)
							{
								Unsafe.Add(ref reference2, num3) = (byte)(elementOffset >> 16);
								num3++;
								goto IL_01eb;
							}
							goto IL_0205;
						}
					}
				}
				goto IL_022b;
				IL_01eb:
				num2 += 4;
				if (num == utf8.Length)
				{
					break;
				}
				goto IL_022b;
				IL_022b:
				bytesConsumed = num2;
				bytesWritten = num3;
				return OperationStatus.InvalidData;
				IL_0205:
				if (!((num != utf8.Length) & isFinalBlock))
				{
					bytesConsumed = num2;
					bytesWritten = num3;
					return OperationStatus.DestinationTooSmall;
				}
				goto IL_022b;
			}
		}
		bytesConsumed = num2;
		bytesWritten = num3;
		return OperationStatus.Done;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetMaxDecodedFromUtf8Length(int length)
	{
		if (length < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.length);
		}
		return (length >> 2) * 3;
	}

	public static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten)
	{
		int length = buffer.Length;
		int num = 0;
		int num2 = 0;
		if (length == (length >> 2) * 4)
		{
			if (length == 0)
			{
				goto IL_016d;
			}
			ref byte reference = ref MemoryMarshal.GetReference(buffer);
			ref sbyte reference2 = ref s_decodingMap[0];
			while (num < length - 4)
			{
				int num3 = Decode(ref Unsafe.Add(ref reference, num), ref reference2);
				if (num3 >= 0)
				{
					WriteThreeLowOrderBytes(ref Unsafe.Add(ref reference, num2), num3);
					num2 += 3;
					num += 4;
					continue;
				}
				goto IL_0172;
			}
			int elementOffset = Unsafe.Add(ref reference, length - 4);
			int elementOffset2 = Unsafe.Add(ref reference, length - 3);
			int num4 = Unsafe.Add(ref reference, length - 2);
			int num5 = Unsafe.Add(ref reference, length - 1);
			elementOffset = Unsafe.Add(ref reference2, elementOffset);
			elementOffset2 = Unsafe.Add(ref reference2, elementOffset2);
			elementOffset <<= 18;
			elementOffset2 <<= 12;
			elementOffset |= elementOffset2;
			if (num5 != 61)
			{
				num4 = Unsafe.Add(ref reference2, num4);
				num5 = Unsafe.Add(ref reference2, num5);
				num4 <<= 6;
				elementOffset |= num5;
				elementOffset |= num4;
				if (elementOffset >= 0)
				{
					WriteThreeLowOrderBytes(ref Unsafe.Add(ref reference, num2), elementOffset);
					num2 += 3;
					goto IL_016d;
				}
			}
			else if (num4 != 61)
			{
				num4 = Unsafe.Add(ref reference2, num4);
				num4 <<= 6;
				elementOffset |= num4;
				if (elementOffset >= 0)
				{
					Unsafe.Add(ref reference, num2) = (byte)(elementOffset >> 16);
					Unsafe.Add(ref reference, num2 + 1) = (byte)(elementOffset >> 8);
					num2 += 2;
					goto IL_016d;
				}
			}
			else if (elementOffset >= 0)
			{
				Unsafe.Add(ref reference, num2) = (byte)(elementOffset >> 16);
				num2++;
				goto IL_016d;
			}
		}
		goto IL_0172;
		IL_0172:
		bytesWritten = num2;
		return OperationStatus.InvalidData;
		IL_016d:
		bytesWritten = num2;
		return OperationStatus.Done;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Decode(ref byte encodedBytes, ref sbyte decodingMap)
	{
		int elementOffset = encodedBytes;
		int elementOffset2 = Unsafe.Add(ref encodedBytes, 1);
		int elementOffset3 = Unsafe.Add(ref encodedBytes, 2);
		int elementOffset4 = Unsafe.Add(ref encodedBytes, 3);
		elementOffset = Unsafe.Add(ref decodingMap, elementOffset);
		elementOffset2 = Unsafe.Add(ref decodingMap, elementOffset2);
		elementOffset3 = Unsafe.Add(ref decodingMap, elementOffset3);
		elementOffset4 = Unsafe.Add(ref decodingMap, elementOffset4);
		elementOffset <<= 18;
		elementOffset2 <<= 12;
		elementOffset3 <<= 6;
		elementOffset |= elementOffset4;
		elementOffset2 |= elementOffset3;
		return elementOffset | elementOffset2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteThreeLowOrderBytes(ref byte destination, int value)
	{
		destination = (byte)(value >> 16);
		Unsafe.Add(ref destination, 1) = (byte)(value >> 8);
		Unsafe.Add(ref destination, 2) = (byte)value;
	}

	public static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
	{
		ref byte reference = ref MemoryMarshal.GetReference(bytes);
		ref byte reference2 = ref MemoryMarshal.GetReference(utf8);
		int length = bytes.Length;
		int length2 = utf8.Length;
		int num = 0;
		num = ((length > 1610612733 || length2 < GetMaxEncodedToUtf8Length(length)) ? ((length2 >> 2) * 3 - 2) : (length - 2));
		int i = 0;
		int num2 = 0;
		int num3 = 0;
		ref byte encodingMap = ref s_encodingMap[0];
		for (; i < num; i += 3)
		{
			num3 = Encode(ref Unsafe.Add(ref reference, i), ref encodingMap);
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference2, num2), num3);
			num2 += 4;
		}
		if (num == length - 2)
		{
			if (isFinalBlock)
			{
				if (i == length - 1)
				{
					num3 = EncodeAndPadTwo(ref Unsafe.Add(ref reference, i), ref encodingMap);
					Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference2, num2), num3);
					num2 += 4;
					i++;
				}
				else if (i == length - 2)
				{
					num3 = EncodeAndPadOne(ref Unsafe.Add(ref reference, i), ref encodingMap);
					Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference2, num2), num3);
					num2 += 4;
					i += 2;
				}
				bytesConsumed = i;
				bytesWritten = num2;
				return OperationStatus.Done;
			}
			bytesConsumed = i;
			bytesWritten = num2;
			return OperationStatus.NeedMoreData;
		}
		bytesConsumed = i;
		bytesWritten = num2;
		return OperationStatus.DestinationTooSmall;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetMaxEncodedToUtf8Length(int length)
	{
		if ((uint)length > 1610612733u)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.length);
		}
		return (length + 2) / 3 * 4;
	}

	public static OperationStatus EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
	{
		int maxEncodedToUtf8Length = GetMaxEncodedToUtf8Length(dataLength);
		if (buffer.Length >= maxEncodedToUtf8Length)
		{
			int num = dataLength - dataLength / 3 * 3;
			int num2 = maxEncodedToUtf8Length - 4;
			int num3 = dataLength - num;
			int num4 = 0;
			ref byte encodingMap = ref s_encodingMap[0];
			ref byte reference = ref MemoryMarshal.GetReference(buffer);
			switch (num)
			{
			case 1:
				num4 = EncodeAndPadTwo(ref Unsafe.Add(ref reference, num3), ref encodingMap);
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference, num2), num4);
				num2 -= 4;
				break;
			default:
				num4 = EncodeAndPadOne(ref Unsafe.Add(ref reference, num3), ref encodingMap);
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference, num2), num4);
				num2 -= 4;
				break;
			case 0:
				break;
			}
			for (num3 -= 3; num3 >= 0; num3 -= 3)
			{
				num4 = Encode(ref Unsafe.Add(ref reference, num3), ref encodingMap);
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference, num2), num4);
				num2 -= 4;
			}
			bytesWritten = maxEncodedToUtf8Length;
			return OperationStatus.Done;
		}
		bytesWritten = 0;
		return OperationStatus.DestinationTooSmall;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Encode(ref byte threeBytes, ref byte encodingMap)
	{
		int num = (threeBytes << 16) | (Unsafe.Add(ref threeBytes, 1) << 8) | Unsafe.Add(ref threeBytes, 2);
		int num2 = Unsafe.Add(ref encodingMap, num >> 18);
		int num3 = Unsafe.Add(ref encodingMap, (num >> 12) & 0x3F);
		int num4 = Unsafe.Add(ref encodingMap, (num >> 6) & 0x3F);
		int num5 = Unsafe.Add(ref encodingMap, num & 0x3F);
		return num2 | (num3 << 8) | (num4 << 16) | (num5 << 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int EncodeAndPadOne(ref byte twoBytes, ref byte encodingMap)
	{
		int num = (twoBytes << 16) | (Unsafe.Add(ref twoBytes, 1) << 8);
		int num2 = Unsafe.Add(ref encodingMap, num >> 18);
		int num3 = Unsafe.Add(ref encodingMap, (num >> 12) & 0x3F);
		int num4 = Unsafe.Add(ref encodingMap, (num >> 6) & 0x3F);
		return num2 | (num3 << 8) | (num4 << 16) | 0x3D000000;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int EncodeAndPadTwo(ref byte oneByte, ref byte encodingMap)
	{
		int num = oneByte << 8;
		int num2 = Unsafe.Add(ref encodingMap, num >> 10);
		int num3 = Unsafe.Add(ref encodingMap, (num >> 4) & 0x3F);
		return num2 | (num3 << 8) | 0x3D0000 | 0x3D000000;
	}
}
