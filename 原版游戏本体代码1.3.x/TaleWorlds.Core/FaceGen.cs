using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200005B RID: 91
	public static class FaceGen
	{
		// Token: 0x06000719 RID: 1817 RVA: 0x00018B64 File Offset: 0x00016D64
		public static void SetInstance(IFaceGen faceGen)
		{
			FaceGen._instance = faceGen;
		}

		// Token: 0x0600071A RID: 1818 RVA: 0x00018B6C File Offset: 0x00016D6C
		public static BodyProperties GetRandomBodyProperties(int race, bool isFemale, BodyProperties bodyPropertiesMin, BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tatooTags, float variationAmount)
		{
			if (FaceGen._instance != null)
			{
				return FaceGen._instance.GetRandomBodyProperties(race, isFemale, bodyPropertiesMin, bodyPropertiesMax, hairCoverType, seed, hairTags, beardTags, tatooTags, variationAmount);
			}
			return bodyPropertiesMin;
		}

		// Token: 0x0600071B RID: 1819 RVA: 0x00018B9C File Offset: 0x00016D9C
		public static int GetRaceCount()
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return 0;
			}
			return instance.GetRaceCount();
		}

		// Token: 0x0600071C RID: 1820 RVA: 0x00018BAE File Offset: 0x00016DAE
		public static int GetRaceOrDefault(string raceId)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return 0;
			}
			return instance.GetRaceOrDefault(raceId);
		}

		// Token: 0x0600071D RID: 1821 RVA: 0x00018BC1 File Offset: 0x00016DC1
		public static string GetBaseMonsterNameFromRace(int race)
		{
			IFaceGen instance = FaceGen._instance;
			return ((instance != null) ? instance.GetBaseMonsterNameFromRace(race) : null) ?? null;
		}

		// Token: 0x0600071E RID: 1822 RVA: 0x00018BDA File Offset: 0x00016DDA
		public static string[] GetRaceNames()
		{
			IFaceGen instance = FaceGen._instance;
			return ((instance != null) ? instance.GetRaceNames() : null) ?? null;
		}

		// Token: 0x0600071F RID: 1823 RVA: 0x00018BF2 File Offset: 0x00016DF2
		public static Monster GetMonster(string monsterID)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return null;
			}
			return instance.GetMonster(monsterID);
		}

		// Token: 0x06000720 RID: 1824 RVA: 0x00018C05 File Offset: 0x00016E05
		public static Monster GetMonsterWithSuffix(int race, string suffix)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return null;
			}
			return instance.GetMonsterWithSuffix(race, suffix);
		}

		// Token: 0x06000721 RID: 1825 RVA: 0x00018C19 File Offset: 0x00016E19
		public static Monster GetBaseMonsterFromRace(int race)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return null;
			}
			return instance.GetBaseMonsterFromRace(race);
		}

		// Token: 0x06000722 RID: 1826 RVA: 0x00018C2C File Offset: 0x00016E2C
		public static void GenerateParentKey(BodyProperties childBodyProperties, int race, ref BodyProperties motherBodyProperties, ref BodyProperties fatherBodyProperties)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return;
			}
			instance.GenerateParentBody(childBodyProperties, race, ref motherBodyProperties, ref fatherBodyProperties);
		}

		// Token: 0x06000723 RID: 1827 RVA: 0x00018C41 File Offset: 0x00016E41
		public static void SetHair(ref BodyProperties bodyProperties, int hair, int beard, int tattoo)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return;
			}
			instance.SetHair(ref bodyProperties, hair, beard, tattoo);
		}

		// Token: 0x06000724 RID: 1828 RVA: 0x00018C56 File Offset: 0x00016E56
		public static void SetBody(ref BodyProperties bodyProperties, int build, int weight)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return;
			}
			instance.SetBody(ref bodyProperties, build, weight);
		}

		// Token: 0x06000725 RID: 1829 RVA: 0x00018C6A File Offset: 0x00016E6A
		public static void SetPigmentation(ref BodyProperties bodyProperties, int skinColor, int hairColor, int eyeColor)
		{
			IFaceGen instance = FaceGen._instance;
			if (instance == null)
			{
				return;
			}
			instance.SetPigmentation(ref bodyProperties, skinColor, hairColor, eyeColor);
		}

		// Token: 0x06000726 RID: 1830 RVA: 0x00018C7F File Offset: 0x00016E7F
		public static BodyProperties GetBodyPropertiesWithAge(ref BodyProperties originalBodyProperties, float age)
		{
			if (FaceGen._instance != null)
			{
				return FaceGen._instance.GetBodyPropertiesWithAge(ref originalBodyProperties, age);
			}
			return originalBodyProperties;
		}

		// Token: 0x06000727 RID: 1831 RVA: 0x00018C9B File Offset: 0x00016E9B
		public static BodyMeshMaturityType GetMaturityTypeWithAge(float age)
		{
			if (FaceGen._instance != null)
			{
				return FaceGen._instance.GetMaturityTypeWithAge(age);
			}
			return BodyMeshMaturityType.Child;
		}

		// Token: 0x06000728 RID: 1832 RVA: 0x00018CB1 File Offset: 0x00016EB1
		public static int[] GetHairIndicesByTag(int race, int curGender, float age, string tag)
		{
			if (FaceGen._instance != null)
			{
				return FaceGen._instance.GetHairIndicesByTag(race, curGender, age, tag);
			}
			return Array.Empty<int>();
		}

		// Token: 0x06000729 RID: 1833 RVA: 0x00018CCE File Offset: 0x00016ECE
		public static int[] GetFacialIndicesByTag(int race, int curGender, float age, string tag)
		{
			if (FaceGen._instance != null)
			{
				return FaceGen._instance.GetFacialIndicesByTag(race, curGender, age, tag);
			}
			return Array.Empty<int>();
		}

		// Token: 0x0600072A RID: 1834 RVA: 0x00018CEB File Offset: 0x00016EEB
		public static int[] GetTattooIndicesByTag(int race, int curGender, float age, string tag)
		{
			if (FaceGen._instance != null)
			{
				return FaceGen._instance.GetTattooIndicesByTag(race, curGender, age, tag);
			}
			return Array.Empty<int>();
		}

		// Token: 0x0600072B RID: 1835 RVA: 0x00018D08 File Offset: 0x00016F08
		public static float GetTattooZeroProbability(int race, int curGender, float age)
		{
			if (FaceGen._instance != null)
			{
				return FaceGen._instance.GetTattooZeroProbability(race, curGender, age);
			}
			return 0f;
		}

		// Token: 0x0400038F RID: 911
		public const string MonsterSuffixSettlement = "_settlement";

		// Token: 0x04000390 RID: 912
		public const string MonsterSuffixSettlementSlow = "_settlement_slow";

		// Token: 0x04000391 RID: 913
		public const string MonsterSuffixSettlementFast = "_settlement_fast";

		// Token: 0x04000392 RID: 914
		public const string MonsterSuffixChild = "_child";

		// Token: 0x04000393 RID: 915
		public static bool ShowDebugValues;

		// Token: 0x04000394 RID: 916
		public static bool UpdateDeformKeys;

		// Token: 0x04000395 RID: 917
		private static IFaceGen _instance;
	}
}
