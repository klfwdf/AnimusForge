using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen;

internal static class Hashing
{
	private const uint Hash1Start = 352654597u;

	private const uint Factor = 1566083941u;

	public unsafe static int GetHashCodeOrdinal(ReadOnlySpan<char> s)
	{
		int num = s.Length;
		fixed (char* reference = &MemoryMarshal.GetReference(s))
		{
			switch (num)
			{
			case 0:
				return 757602046;
			case 1:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *reference;
				return (int)(352654597 + num3 * 1566083941);
			}
			case 2:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *reference;
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ reference[1];
				return (int)(352654597 + num3 * 1566083941);
			}
			case 3:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *reference;
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ reference[1];
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ reference[2];
				return (int)(352654597 + num3 * 1566083941);
			}
			case 4:
			{
				uint num2 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *(uint*)reference;
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ ((uint*)reference)[1];
				return (int)(num2 + num3 * 1566083941);
			}
			default:
			{
				uint num2 = 352654597u;
				uint num3 = num2;
				uint* ptr = (uint*)reference;
				while (num >= 4)
				{
					num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ *ptr;
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ ptr[1];
					ptr += 2;
					num -= 4;
				}
				char* ptr2 = (char*)ptr;
				while (num-- > 0)
				{
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ *(ptr2++);
				}
				return (int)(num2 + num3 * 1566083941);
			}
			}
		}
	}

	public unsafe static int GetHashCodeOrdinalIgnoreCaseAscii(ReadOnlySpan<char> s)
	{
		int num = s.Length;
		fixed (char* reference = &MemoryMarshal.GetReference(s))
		{
			switch (num)
			{
			case 0:
				return 757602046;
			case 1:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (uint)(*reference | 0x20);
				return (int)(352654597 + num3 * 1566083941);
			}
			case 2:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (uint)(*reference | 0x20);
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (uint)(reference[1] | 0x20);
				return (int)(352654597 + num3 * 1566083941);
			}
			case 3:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (uint)(*reference | 0x20);
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (uint)(reference[1] | 0x20);
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (uint)(reference[2] | 0x20);
				return (int)(352654597 + num3 * 1566083941);
			}
			case 4:
			{
				uint num2 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (*(uint*)reference | 0x200020);
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (((uint*)reference)[1] | 0x200020);
				return (int)(num2 + num3 * 1566083941);
			}
			default:
			{
				uint num2 = 352654597u;
				uint num3 = num2;
				uint* ptr = (uint*)reference;
				while (num >= 4)
				{
					num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ (*ptr | 0x200020);
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (ptr[1] | 0x200020);
					ptr += 2;
					num -= 4;
				}
				char* ptr2 = (char*)ptr;
				while (num-- > 0)
				{
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (uint)(*ptr2 | 0x200020);
					ptr2++;
				}
				return (int)(num2 + num3 * 1566083941);
			}
			}
		}
	}

	public static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> s)
	{
		int length = s.Length;
		char[] array = null;
		Span<char> span = ((length > 256) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(length))) : stackalloc char[256]);
		Span<char> destination = span;
		length = s.ToUpperInvariant(destination);
		int hashCodeOrdinal = GetHashCodeOrdinal(destination.Slice(0, length));
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return hashCodeOrdinal;
	}
}
