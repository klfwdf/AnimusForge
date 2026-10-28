namespace System.Reflection.Metadata;

internal static class StringUtils
{
	internal static int IgnoreCaseMask(bool ignoreCase)
	{
		if (!ignoreCase)
		{
			return 255;
		}
		return 32;
	}

	internal static bool IsEqualAscii(int a, int b, int ignoreCaseMask)
	{
		if (a != b)
		{
			if ((a | 0x20) == (b | 0x20))
			{
				return (uint)((a | ignoreCaseMask) - 97) <= 25u;
			}
			return false;
		}
		return true;
	}
}
