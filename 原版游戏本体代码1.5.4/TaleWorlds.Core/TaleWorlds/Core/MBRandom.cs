using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public static class MBRandom
{
	public const int MaxSeed = 2000;

	private static MBFastRandom _internalRandom = null;

	private static readonly MBFastRandom NondeterministicRandom = new MBFastRandom();

	private static MBFastRandom Random
	{
		get
		{
			if (Game.Current != null)
			{
				return Game.Current.RandomGenerator;
			}
			if (_internalRandom == null)
			{
				_internalRandom = new MBFastRandom();
			}
			return _internalRandom;
		}
	}

	public static float RandomFloat => Random.NextFloat();

	public static float RandomFloatNormal
	{
		get
		{
			int num = 4;
			float num2;
			float num4;
			do
			{
				num2 = 2f * RandomFloat - 1f;
				float num3 = 2f * RandomFloat - 1f;
				num4 = num2 * num2 + num3 * num3;
				num--;
			}
			while (num4 >= 1f || (num4 == 0f && num > 0));
			return num2 * num4 * 1f;
		}
	}

	public static float NondeterministicRandomFloat => NondeterministicRandom.NextFloat();

	public static int NondeterministicRandomInt => NondeterministicRandom.Next();

	public static float RandomFloatRanged(float maxVal)
	{
		return RandomFloat * maxVal;
	}

	public static float RandomFloatRanged(float minVal, float maxVal)
	{
		return minVal + RandomFloat * (maxVal - minVal);
	}

	public static int RandomInt()
	{
		return Random.Next();
	}

	public static int RandomInt(int maxValue)
	{
		return Random.Next(maxValue);
	}

	public static int RandomInt(int minValue, int maxValue)
	{
		return Random.Next(minValue, maxValue);
	}

	public static int RoundRandomized(float f)
	{
		int num = TaleWorlds.Library.MathF.Floor(f);
		float num2 = f - (float)num;
		if (RandomFloat < num2)
		{
			num++;
		}
		return num;
	}

	public static T ChooseWeighted<T>(IReadOnlyList<(T, float)> weightList)
	{
		int chosenIndex;
		return ChooseWeighted(weightList, out chosenIndex);
	}

	public static T ChooseWeighted<T>(IReadOnlyList<(T, float)> weightList, out int chosenIndex)
	{
		chosenIndex = -1;
		float num = weightList.Sum(((T, float) x) => x.Item2);
		float num2 = RandomFloat * num;
		for (int num3 = 0; num3 < weightList.Count; num3++)
		{
			num2 -= weightList[num3].Item2;
			if (num2 <= 0f)
			{
				chosenIndex = num3;
				return weightList[num3].Item1;
			}
		}
		if (weightList.Count > 0)
		{
			chosenIndex = 0;
			return weightList[0].Item1;
		}
		chosenIndex = -1;
		return default(T);
	}

	public static float RandomFloatGaussian(float center, float spread, float min, float max)
	{
		float a = 1f - RandomFloat;
		float num = 1f - RandomFloat;
		float num2 = TaleWorlds.Library.MathF.Sqrt(-2f * TaleWorlds.Library.MathF.Log(a)) * TaleWorlds.Library.MathF.Sin(System.MathF.PI * 2f * num);
		return TaleWorlds.Library.MathF.Clamp(center + spread * num2, min, max);
	}

	public static void SetSeed(uint seed, uint seed2)
	{
		Random.SetSeed(seed, seed2);
	}

	public static int RandomIntWithSeed(uint seed, uint seed2)
	{
		return MBFastRandom.GetRandomInt(seed, seed2);
	}

	public static float RandomFloatWithSeed(uint seed, uint seed2)
	{
		return MBFastRandom.GetRandomFloat(seed, seed2);
	}
}
