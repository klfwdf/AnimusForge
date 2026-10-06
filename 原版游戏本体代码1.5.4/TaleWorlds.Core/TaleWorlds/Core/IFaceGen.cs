namespace TaleWorlds.Core;

public interface IFaceGen
{
	BodyProperties GetRandomBodyProperties(int race, bool isFemale, BodyProperties bodyPropertiesMin, BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tatooTags, float variationAmount);

	void GenerateParentBody(BodyProperties childBodyProperties, int race, ref BodyProperties motherBodyProperties, ref BodyProperties fatherBodyProperties);

	void SetBody(ref BodyProperties bodyProperties, int build, int weight);

	void SetHair(ref BodyProperties bodyProperties, int hair, int beard, int tattoo);

	void SetPigmentation(ref BodyProperties bodyProperties, int skinColor, int hairColor, int eyeColor);

	BodyProperties GetBodyPropertiesWithAge(ref BodyProperties bodyProperties, float age);

	BodyMeshMaturityType GetMaturityTypeWithAge(float age);

	int GetRaceCount();

	int GetRaceOrDefault(string raceId);

	string GetBaseMonsterNameFromRace(int race);

	string[] GetRaceNames();

	Monster GetMonster(string monsterID);

	Monster GetMonsterWithSuffix(int race, string suffix);

	Monster GetBaseMonsterFromRace(int race);

	int[] GetHairIndicesByTag(int race, int curGender, float age, string tag);

	int[] GetFacialIndicesByTag(int race, int curGender, float age, string tag);

	int[] GetTattooIndicesByTag(int race, int curGender, float age, string tag);

	float GetTattooZeroProbability(int race, int curGender, float age);
}
