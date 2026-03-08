using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000098 RID: 152
	public static class RandomOwnerExtensions
	{
		// Token: 0x060012B1 RID: 4785 RVA: 0x00054196 File Offset: 0x00052396
		public static int RandomIntWithSeed(this IRandomOwner obj, uint seed)
		{
			return MBRandom.RandomIntWithSeed((uint)obj.RandomValue, seed);
		}

		// Token: 0x060012B2 RID: 4786 RVA: 0x000541A4 File Offset: 0x000523A4
		public static int RandomIntWithSeed(this IRandomOwner obj, uint seed, int max)
		{
			return obj.RandomIntWithSeed(seed, 0, max);
		}

		// Token: 0x060012B3 RID: 4787 RVA: 0x000541AF File Offset: 0x000523AF
		public static int RandomIntWithSeed(this IRandomOwner obj, uint seed, int min, int max)
		{
			return RandomOwnerExtensions.Random(obj.RandomIntWithSeed(seed), min, max);
		}

		// Token: 0x060012B4 RID: 4788 RVA: 0x000541BF File Offset: 0x000523BF
		public static float RandomFloatWithSeed(this IRandomOwner obj, uint seed)
		{
			return MBRandom.RandomFloatWithSeed((uint)obj.RandomValue, seed);
		}

		// Token: 0x060012B5 RID: 4789 RVA: 0x000541CD File Offset: 0x000523CD
		public static float RandomFloatWithSeed(this IRandomOwner obj, uint seed, float max)
		{
			return obj.RandomFloatWithSeed(seed, 0f, max);
		}

		// Token: 0x060012B6 RID: 4790 RVA: 0x000541DC File Offset: 0x000523DC
		public static float RandomFloatWithSeed(this IRandomOwner obj, uint seed, float min, float max)
		{
			return RandomOwnerExtensions.Random(obj.RandomFloatWithSeed(seed), min, max);
		}

		// Token: 0x060012B7 RID: 4791 RVA: 0x000541EC File Offset: 0x000523EC
		public static int RandomInt(this IRandomOwner obj)
		{
			return obj.RandomValue;
		}

		// Token: 0x060012B8 RID: 4792 RVA: 0x000541F4 File Offset: 0x000523F4
		public static int RandomInt(this IRandomOwner obj, int max)
		{
			return obj.RandomInt(0, max);
		}

		// Token: 0x060012B9 RID: 4793 RVA: 0x000541FE File Offset: 0x000523FE
		public static int RandomInt(this IRandomOwner obj, int min, int max)
		{
			return RandomOwnerExtensions.Random(obj.RandomInt(), min, max);
		}

		// Token: 0x060012BA RID: 4794 RVA: 0x0005420D File Offset: 0x0005240D
		public static float RandomFloat(this IRandomOwner obj)
		{
			return (float)obj.RandomValue / 2.1474836E+09f;
		}

		// Token: 0x060012BB RID: 4795 RVA: 0x0005421C File Offset: 0x0005241C
		public static float RandomFloat(this IRandomOwner obj, float max)
		{
			return obj.RandomFloat(0f, max);
		}

		// Token: 0x060012BC RID: 4796 RVA: 0x0005422A File Offset: 0x0005242A
		public static float RandomFloat(this IRandomOwner obj, float min, float max)
		{
			return RandomOwnerExtensions.Random(obj.RandomFloat(), min, max);
		}

		// Token: 0x060012BD RID: 4797 RVA: 0x0005423C File Offset: 0x0005243C
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

		// Token: 0x060012BE RID: 4798 RVA: 0x00054270 File Offset: 0x00052470
		private static float Random(float randomValue, float min, float max)
		{
			float num = max - min;
			if (num <= 1E-45f)
			{
				Debug.FailedAssert("invalid Random parameters", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\IRandomOwner.cs", "Random", 91);
				return min;
			}
			return min + randomValue * num;
		}
	}
}
