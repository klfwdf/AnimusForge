using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public static class RandomOwnerExtensions
{
	public static int RandomIntWithSeed(this IRandomOwner obj, uint seed)
	{
		return MBRandom.RandomIntWithSeed((uint)obj.RandomValue, seed);
	}

	public static int RandomIntWithSeed(this IRandomOwner obj, uint seed, int max)
	{
		return obj.RandomIntWithSeed(seed, 0, max);
	}

	public static int RandomIntWithSeed(this IRandomOwner obj, uint seed, int min, int max)
	{
		return Random(obj.RandomIntWithSeed(seed), min, max);
	}

	public static float RandomFloatWithSeed(this IRandomOwner obj, uint seed)
	{
		return MBRandom.RandomFloatWithSeed((uint)obj.RandomValue, seed);
	}

	public static float RandomFloatWithSeed(this IRandomOwner obj, uint seed, float max)
	{
		return obj.RandomFloatWithSeed(seed, 0f, max);
	}

	public static float RandomFloatWithSeed(this IRandomOwner obj, uint seed, float min, float max)
	{
		return Random(obj.RandomFloatWithSeed(seed), min, max);
	}

	public static int RandomInt(this IRandomOwner obj)
	{
		return obj.RandomValue;
	}

	public static int RandomInt(this IRandomOwner obj, int max)
	{
		return obj.RandomInt(0, max);
	}

	public static int RandomInt(this IRandomOwner obj, int min, int max)
	{
		return Random(obj.RandomInt(), min, max);
	}

	public static float RandomFloat(this IRandomOwner obj)
	{
		return (float)obj.RandomValue / 2.1474836E+09f;
	}

	public static float RandomFloat(this IRandomOwner obj, float max)
	{
		return obj.RandomFloat(0f, max);
	}

	public static float RandomFloat(this IRandomOwner obj, float min, float max)
	{
		return Random(obj.RandomFloat(), min, max);
	}

	private static int Random(int randomValue, int min, int max)
	{
		int num = max - min;
		if (num == 0)
		{
			Debug.FailedAssert("invalid Random parameters", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\IRandomOwner.cs", "Random", 79);
			return 0;
		}
		return min + randomValue % num;
	}

	private static float Random(float randomValue, float min, float max)
	{
		float num = max - min;
		if (num <= float.Epsilon)
		{
			Debug.FailedAssert("invalid Random parameters", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\IRandomOwner.cs", "Random", 91);
			return min;
		}
		return min + randomValue * num;
	}
}
