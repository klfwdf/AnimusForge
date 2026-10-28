namespace System.Reflection.Internal;

internal static class BitArithmetic
{
	internal static int CountBits(int v)
	{
		return CountBits((uint)v);
	}

	internal static int CountBits(uint v)
	{
		v -= (v >> 1) & 0x55555555;
		v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
		return (int)(((v + (v >> 4)) & 0xF0F0F0F) * 16843009) >> 24;
	}

	internal static int CountBits(ulong v)
	{
		v -= (v >> 1) & 0x5555555555555555L;
		v = (v & 0x3333333333333333L) + ((v >> 2) & 0x3333333333333333L);
		return (int)(((v + (v >> 4)) & 0xF0F0F0F0F0F0F0FL) * 72340172838076673L >> 56);
	}

	internal static uint Align(uint position, uint alignment)
	{
		uint num = position & ~(alignment - 1);
		if (num == position)
		{
			return num;
		}
		return num + alignment;
	}

	internal static int Align(int position, int alignment)
	{
		int num = position & ~(alignment - 1);
		if (num == position)
		{
			return num;
		}
		return num + alignment;
	}
}
