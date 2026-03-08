using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000083 RID: 131
	public interface IFaceGen
	{
		// Token: 0x06000880 RID: 2176
		BodyProperties GetRandomBodyProperties(int race, bool isFemale, BodyProperties bodyPropertiesMin, BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tatooTags, float variationAmount);

		// Token: 0x06000881 RID: 2177
		void GenerateParentBody(BodyProperties childBodyProperties, int race, ref BodyProperties motherBodyProperties, ref BodyProperties fatherBodyProperties);

		// Token: 0x06000882 RID: 2178
		void SetBody(ref BodyProperties bodyProperties, int build, int weight);

		// Token: 0x06000883 RID: 2179
		void SetHair(ref BodyProperties bodyProperties, int hair, int beard, int tattoo);

		// Token: 0x06000884 RID: 2180
		void SetPigmentation(ref BodyProperties bodyProperties, int skinColor, int hairColor, int eyeColor);

		// Token: 0x06000885 RID: 2181
		BodyProperties GetBodyPropertiesWithAge(ref BodyProperties bodyProperties, float age);

		// Token: 0x06000886 RID: 2182
		BodyMeshMaturityType GetMaturityTypeWithAge(float age);

		// Token: 0x06000887 RID: 2183
		int GetRaceCount();

		// Token: 0x06000888 RID: 2184
		int GetRaceOrDefault(string raceId);

		// Token: 0x06000889 RID: 2185
		string GetBaseMonsterNameFromRace(int race);

		// Token: 0x0600088A RID: 2186
		string[] GetRaceNames();

		// Token: 0x0600088B RID: 2187
		Monster GetMonster(string monsterID);

		// Token: 0x0600088C RID: 2188
		Monster GetMonsterWithSuffix(int race, string suffix);

		// Token: 0x0600088D RID: 2189
		Monster GetBaseMonsterFromRace(int race);

		// Token: 0x0600088E RID: 2190
		int[] GetHairIndicesByTag(int race, int curGender, float age, string tag);

		// Token: 0x0600088F RID: 2191
		int[] GetFacialIndicesByTag(int race, int curGender, float age, string tag);

		// Token: 0x06000890 RID: 2192
		int[] GetTattooIndicesByTag(int race, int curGender, float age, string tag);

		// Token: 0x06000891 RID: 2193
		float GetTattooZeroProbability(int race, int curGender, float age);
	}
}
