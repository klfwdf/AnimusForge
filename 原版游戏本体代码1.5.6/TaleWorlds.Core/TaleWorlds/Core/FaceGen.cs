using System;

namespace TaleWorlds.Core;

public static class FaceGen
{
	public const string MonsterSuffixSettlement = "_settlement";

	public const string MonsterSuffixSettlementSlow = "_settlement_slow";

	public const string MonsterSuffixSettlementFast = "_settlement_fast";

	public const string MonsterSuffixChild = "_child";

	public static bool ShowDebugValues;

	public static bool UpdateDeformKeys;

	private static IFaceGen _instance;

	public static void SetInstance(IFaceGen faceGen)
	{
		_instance = faceGen;
	}

	public static BodyProperties GetRandomBodyProperties(int race, bool isFemale, BodyProperties bodyPropertiesMin, BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tatooTags, float variationAmount)
	{
		if (_instance != null)
		{
			return _instance.GetRandomBodyProperties(race, isFemale, bodyPropertiesMin, bodyPropertiesMax, hairCoverType, seed, hairTags, beardTags, tatooTags, variationAmount);
		}
		return bodyPropertiesMin;
	}

	public static int GetRaceCount()
	{
		return _instance?.GetRaceCount() ?? 0;
	}

	public static int GetRaceOrDefault(string raceId)
	{
		return _instance?.GetRaceOrDefault(raceId) ?? 0;
	}

	public static string GetBaseMonsterNameFromRace(int race)
	{
		return _instance?.GetBaseMonsterNameFromRace(race) ?? null;
	}

	public static string[] GetRaceNames()
	{
		return _instance?.GetRaceNames() ?? null;
	}

	public static Monster GetMonster(string monsterID)
	{
		return _instance?.GetMonster(monsterID);
	}

	public static Monster GetMonsterWithSuffix(int race, string suffix)
	{
		return _instance?.GetMonsterWithSuffix(race, suffix);
	}

	public static Monster GetBaseMonsterFromRace(int race)
	{
		return _instance?.GetBaseMonsterFromRace(race);
	}

	public static void GenerateParentKey(BodyProperties childBodyProperties, int race, ref BodyProperties motherBodyProperties, ref BodyProperties fatherBodyProperties)
	{
		_instance?.GenerateParentBody(childBodyProperties, race, ref motherBodyProperties, ref fatherBodyProperties);
	}

	public static void SetHair(ref BodyProperties bodyProperties, int hair, int beard, int tattoo)
	{
		_instance?.SetHair(ref bodyProperties, hair, beard, tattoo);
	}

	public static void SetBody(ref BodyProperties bodyProperties, int build, int weight)
	{
		_instance?.SetBody(ref bodyProperties, build, weight);
	}

	public static void SetPigmentation(ref BodyProperties bodyProperties, int skinColor, int hairColor, int eyeColor)
	{
		_instance?.SetPigmentation(ref bodyProperties, skinColor, hairColor, eyeColor);
	}

	public static BodyProperties GetBodyPropertiesWithAge(ref BodyProperties originalBodyProperties, float age)
	{
		if (_instance != null)
		{
			return _instance.GetBodyPropertiesWithAge(ref originalBodyProperties, age);
		}
		return originalBodyProperties;
	}

	public static BodyMeshMaturityType GetMaturityTypeWithAge(float age)
	{
		if (_instance != null)
		{
			return _instance.GetMaturityTypeWithAge(age);
		}
		return BodyMeshMaturityType.Child;
	}

	public static int[] GetHairIndicesByTag(int race, int curGender, float age, string tag)
	{
		if (_instance != null)
		{
			return _instance.GetHairIndicesByTag(race, curGender, age, tag);
		}
		return Array.Empty<int>();
	}

	public static int[] GetFacialIndicesByTag(int race, int curGender, float age, string tag)
	{
		if (_instance != null)
		{
			return _instance.GetFacialIndicesByTag(race, curGender, age, tag);
		}
		return Array.Empty<int>();
	}

	public static int[] GetTattooIndicesByTag(int race, int curGender, float age, string tag)
	{
		if (_instance != null)
		{
			return _instance.GetTattooIndicesByTag(race, curGender, age, tag);
		}
		return Array.Empty<int>();
	}

	public static float GetTattooZeroProbability(int race, int curGender, float age)
	{
		if (_instance != null)
		{
			return _instance.GetTattooZeroProbability(race, curGender, age);
		}
		return 0f;
	}
}
