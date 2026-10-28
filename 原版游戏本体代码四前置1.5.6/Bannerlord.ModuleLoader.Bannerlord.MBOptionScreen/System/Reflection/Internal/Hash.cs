namespace System.Reflection.Internal;

internal static class Hash
{
	internal const int FnvOffsetBias = -2128831035;

	internal const int FnvPrime = 16777619;

	internal static int Combine(int newKey, int currentKey)
	{
		return currentKey * -1521134295 + newKey;
	}

	internal static int Combine(uint newKey, int currentKey)
	{
		return currentKey * -1521134295 + (int)newKey;
	}

	internal static int Combine(bool newKeyPart, int currentKey)
	{
		return Combine(currentKey, newKeyPart ? 1 : 0);
	}

	internal static int GetFNVHashCode(ReadOnlySpan<byte> data)
	{
		int num = -2128831035;
		for (int i = 0; i < data.Length; i++)
		{
			num = (num ^ data[i]) * 16777619;
		}
		return num;
	}
}
