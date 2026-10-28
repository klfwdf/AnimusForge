using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.Common.Helpers;

public static class ElegantPairHelper
{
	public static long Pair(int a, int b)
	{
		ulong num = ((a >= 0) ? (2uL * (ulong)a) : ((ulong)(-2L * (long)a - 1)));
		ulong num2 = ((b >= 0) ? (2uL * (ulong)b) : ((ulong)(-2L * (long)b - 1)));
		long num3 = (long)(((num >= num2) ? (num * num + num + num2) : (num + num2 * num2)) / 2);
		if ((a >= 0 || b >= 0) && (a < 0 || b < 0))
		{
			return -num3 - 1;
		}
		return num3;
	}

	public static ulong Pair(uint a, uint b)
	{
		ulong num = a;
		ulong num2 = b;
		if (num < num2)
		{
			return num + num2 * num2;
		}
		return num * num + num + num2;
	}

	public static int Pair(short a, short b)
	{
		uint num = (uint)((a >= 0) ? (2 * a) : (-2 * a - 1));
		uint num2 = (uint)((b >= 0) ? (2 * b) : (-2 * b - 1));
		int num3 = (int)(((num >= num2) ? (num * num + num + num2) : (num + num2 * num2)) / 2);
		if ((a >= 0 || b >= 0) && (a < 0 || b < 0))
		{
			return -num3 - 1;
		}
		return num3;
	}

	public static uint Pair(ushort a, ushort b)
	{
		if ((uint)a < (uint)b)
		{
			return (uint)(a + b * b);
		}
		return (uint)(a * a + a + b);
	}

	public static ulong Pair(MBGUID a, MBGUID b)
	{
		return Pair(a.InternalValue, b.InternalValue);
	}

	public static (uint A, uint B) UnPair(ulong pairValue)
	{
		ulong num = IntegerSqrt(pairValue);
		ulong num2 = pairValue - num * num;
		if (num2 >= num)
		{
			return (A: (uint)num, B: (uint)(num2 - num));
		}
		return (A: (uint)num2, B: (uint)num);
	}

	public static (int A, int B) UnPair(long pairValue)
	{
		long num = ((pairValue >= 0) ? pairValue : (-pairValue - 1));
		ulong num2 = (ulong)(2 * num);
		(int, int) signedTuple = GetSignedTuple(UnPair(num2));
		if (Pair(signedTuple.Item1, signedTuple.Item2) != pairValue)
		{
			return GetSignedTuple(UnPair(num2 + 1));
		}
		return signedTuple;
	}

	public static (ushort A, ushort B) UnPair(uint pairValue)
	{
		uint num = IntegerSqrt(pairValue);
		uint num2 = pairValue - num * num;
		if (num2 >= num)
		{
			return (A: (ushort)num, B: (ushort)(num2 - num));
		}
		return (A: (ushort)num2, B: (ushort)num);
	}

	public static (short A, short B) UnPair(int pairValue)
	{
		int num = ((pairValue >= 0) ? pairValue : (-pairValue - 1));
		uint num2 = (uint)(2 * num);
		(ushort, ushort) tuple = UnPair(num2);
		(int, int) signedTuple = GetSignedTuple((A: tuple.Item1, B: tuple.Item2));
		(short, short) result = ((short)signedTuple.Item1, (short)signedTuple.Item2);
		if (Pair(result.Item1, result.Item2) != pairValue)
		{
			tuple = UnPair(num2 + 1);
			signedTuple = GetSignedTuple((A: tuple.Item1, B: tuple.Item2));
			return (A: (short)signedTuple.Item1, B: (short)signedTuple.Item2);
		}
		return result;
	}

	public static (MBGUID A, MBGUID B) UnPairMBGUID(ulong pairValue)
	{
		var (id, id2) = UnPair(pairValue);
		return (A: new MBGUID(id), B: new MBGUID(id2));
	}

	private static ulong IntegerSqrt(ulong a)
	{
		ulong num = 0uL;
		ulong num2 = 4294967296uL;
		ulong num3;
		while (true)
		{
			if (num2 <= 1 + num)
			{
				return num;
			}
			num3 = num + (num2 - num) / 2;
			ulong num4 = num3 * num3;
			if (num4 == a)
			{
				break;
			}
			if (num4 > a)
			{
				num2 = num3;
			}
			else
			{
				num = num3;
			}
		}
		return num3;
	}

	private static uint IntegerSqrt(uint a)
	{
		uint num = 0u;
		uint num2 = 65536u;
		uint num3;
		while (true)
		{
			if (num2 <= 1 + num)
			{
				return num;
			}
			num3 = num + (num2 - num) / 2;
			uint num4 = num3 * num3;
			if (num4 == a)
			{
				break;
			}
			if (num4 > a)
			{
				num2 = num3;
			}
			else
			{
				num = num3;
			}
		}
		return num3;
	}

	private static (int A, int B) GetSignedTuple((uint A, uint B) unsignedTuple)
	{
		return (A: (int)((unsignedTuple.A % 2 == 0) ? ((int)unsignedTuple.A / 2) : ((unsignedTuple.A + 1) / -2)), B: (int)((unsignedTuple.B % 2 == 0) ? ((int)unsignedTuple.B / 2) : ((unsignedTuple.B + 1) / -2)));
	}
}
