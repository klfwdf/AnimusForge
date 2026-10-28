namespace System.Numerics.Hashing;

internal static class HashHelpers
{
	public static readonly int RandomSeed = Guid.NewGuid().GetHashCode();

	public static int Combine(int h1, int h2)
	{
		uint num = (uint)((h1 << 5) | (h1 >>> 27));
		return ((int)num + h1) ^ h2;
	}
}
