using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x020000B3 RID: 179
	public static class MBRandom
	{
		// Token: 0x17000321 RID: 801
		// (get) Token: 0x06000952 RID: 2386 RVA: 0x0001E83A File Offset: 0x0001CA3A
		private static MBFastRandom Random
		{
			get
			{
				if (Game.Current != null)
				{
					return Game.Current.RandomGenerator;
				}
				if (MBRandom._internalRandom == null)
				{
					MBRandom._internalRandom = new MBFastRandom();
				}
				return MBRandom._internalRandom;
			}
		}

		// Token: 0x17000322 RID: 802
		// (get) Token: 0x06000953 RID: 2387 RVA: 0x0001E864 File Offset: 0x0001CA64
		public static float RandomFloat
		{
			get
			{
				return MBRandom.Random.NextFloat();
			}
		}

		// Token: 0x06000954 RID: 2388 RVA: 0x0001E870 File Offset: 0x0001CA70
		public static float RandomFloatRanged(float maxVal)
		{
			return MBRandom.RandomFloat * maxVal;
		}

		// Token: 0x06000955 RID: 2389 RVA: 0x0001E879 File Offset: 0x0001CA79
		public static float RandomFloatRanged(float minVal, float maxVal)
		{
			return minVal + MBRandom.RandomFloat * (maxVal - minVal);
		}

		// Token: 0x17000323 RID: 803
		// (get) Token: 0x06000956 RID: 2390 RVA: 0x0001E888 File Offset: 0x0001CA88
		public static float RandomFloatNormal
		{
			get
			{
				int num = 4;
				float num2;
				float num4;
				do
				{
					num2 = 2f * MBRandom.RandomFloat - 1f;
					float num3 = 2f * MBRandom.RandomFloat - 1f;
					num4 = num2 * num2 + num3 * num3;
					num--;
				}
				while (num4 >= 1f || (num4 == 0f && num > 0));
				return num2 * num4 * 1f;
			}
		}

		// Token: 0x06000957 RID: 2391 RVA: 0x0001E8E4 File Offset: 0x0001CAE4
		public static int RandomInt()
		{
			return MBRandom.Random.Next();
		}

		// Token: 0x06000958 RID: 2392 RVA: 0x0001E8F0 File Offset: 0x0001CAF0
		public static int RandomInt(int maxValue)
		{
			return MBRandom.Random.Next(maxValue);
		}

		// Token: 0x06000959 RID: 2393 RVA: 0x0001E8FD File Offset: 0x0001CAFD
		public static int RandomInt(int minValue, int maxValue)
		{
			return MBRandom.Random.Next(minValue, maxValue);
		}

		// Token: 0x0600095A RID: 2394 RVA: 0x0001E90C File Offset: 0x0001CB0C
		public static int RoundRandomized(float f)
		{
			int num = MathF.Floor(f);
			float num2 = f - (float)num;
			if (MBRandom.RandomFloat < num2)
			{
				num++;
			}
			return num;
		}

		// Token: 0x0600095B RID: 2395 RVA: 0x0001E934 File Offset: 0x0001CB34
		public static T ChooseWeighted<T>(IReadOnlyList<ValueTuple<T, float>> weightList)
		{
			int num;
			return MBRandom.ChooseWeighted<T>(weightList, out num);
		}

		// Token: 0x0600095C RID: 2396 RVA: 0x0001E94C File Offset: 0x0001CB4C
		public static T ChooseWeighted<T>(IReadOnlyList<ValueTuple<T, float>> weightList, out int chosenIndex)
		{
			chosenIndex = -1;
			float num = weightList.Sum((ValueTuple<T, float> x) => x.Item2);
			float num2 = MBRandom.RandomFloat * num;
			for (int i = 0; i < weightList.Count; i++)
			{
				num2 -= weightList[i].Item2;
				if (num2 <= 0f)
				{
					chosenIndex = i;
					return weightList[i].Item1;
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

		// Token: 0x0600095D RID: 2397 RVA: 0x0001E9E8 File Offset: 0x0001CBE8
		public static float RandomFloatGaussian(float center, float spread, float min, float max)
		{
			float a = 1f - MBRandom.RandomFloat;
			float num = 1f - MBRandom.RandomFloat;
			float num2 = MathF.Sqrt(-2f * MathF.Log(a)) * MathF.Sin(6.2831855f * num);
			return MathF.Clamp(center + spread * num2, min, max);
		}

		// Token: 0x0600095E RID: 2398 RVA: 0x0001EA38 File Offset: 0x0001CC38
		public static void SetSeed(uint seed, uint seed2)
		{
			MBRandom.Random.SetSeed(seed, seed2);
		}

		// Token: 0x17000324 RID: 804
		// (get) Token: 0x0600095F RID: 2399 RVA: 0x0001EA46 File Offset: 0x0001CC46
		public static float NondeterministicRandomFloat
		{
			get
			{
				return MBRandom.NondeterministicRandom.NextFloat();
			}
		}

		// Token: 0x17000325 RID: 805
		// (get) Token: 0x06000960 RID: 2400 RVA: 0x0001EA52 File Offset: 0x0001CC52
		public static int NondeterministicRandomInt
		{
			get
			{
				return MBRandom.NondeterministicRandom.Next();
			}
		}

		// Token: 0x06000961 RID: 2401 RVA: 0x0001EA5E File Offset: 0x0001CC5E
		public static int RandomIntWithSeed(uint seed, uint seed2)
		{
			return MBFastRandom.GetRandomInt(seed, seed2);
		}

		// Token: 0x06000962 RID: 2402 RVA: 0x0001EA67 File Offset: 0x0001CC67
		public static float RandomFloatWithSeed(uint seed, uint seed2)
		{
			return MBFastRandom.GetRandomFloat(seed, seed2);
		}

		// Token: 0x0400052E RID: 1326
		public const int MaxSeed = 2000;

		// Token: 0x0400052F RID: 1327
		private static MBFastRandom _internalRandom = null;

		// Token: 0x04000530 RID: 1328
		private static readonly MBFastRandom NondeterministicRandom = new MBFastRandom();
	}
}
