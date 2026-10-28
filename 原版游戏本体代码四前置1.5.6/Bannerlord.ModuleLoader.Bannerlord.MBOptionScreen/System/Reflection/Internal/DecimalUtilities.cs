namespace System.Reflection.Internal;

internal static class DecimalUtilities
{
	public static int GetScale(this decimal value)
	{
		return (byte)(decimal.GetBits(value)[3] >> 16);
	}

	public static void GetBits(this decimal value, out bool isNegative, out byte scale, out uint low, out uint mid, out uint high)
	{
		int[] bits = decimal.GetBits(value);
		low = (uint)bits[0];
		mid = (uint)bits[1];
		high = (uint)bits[2];
		scale = (byte)(bits[3] >> 16);
		isNegative = (bits[3] & 0x80000000u) != 0;
	}
}
