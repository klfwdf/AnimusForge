namespace System.Numerics.Hashing;

internal static class _003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003EHashHelpers
{
	public static readonly int RandomSeed = Guid.NewGuid().GetHashCode();

	public static int Combine(int h1, int h2)
	{
		uint num = (uint)((h1 << 5) | (h1 >>> 27));
		return ((int)num + h1) ^ h2;
	}
}
