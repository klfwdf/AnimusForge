using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MBBodyProperties
{
	public enum GenerationType
	{
		FromMother,
		FromFather,
		Count
	}

	public static int GetNumEditableDeformKeys(int race, bool initialGender, int age)
	{
		return MBAPI.IMBFaceGen.GetNumEditableDeformKeys(race, initialGender, age);
	}

	public static void GetParamsFromKey(ref FaceGenerationParams faceGenerationParams, BodyProperties bodyProperties, bool earsAreHidden, bool mouthHidden)
	{
		MBAPI.IMBFaceGen.GetParamsFromKey(ref faceGenerationParams, ref bodyProperties, earsAreHidden, mouthHidden);
	}

	public static void GetParamsMax(int race, int curGender, int curAge, ref int hairNum, ref int beardNum, ref int faceTextureNum, ref int mouthTextureNum, ref int faceTattooNum, ref int soundNum, ref int eyebrowNum, ref float scale)
	{
		MBAPI.IMBFaceGen.GetParamsMax(race, curGender, curAge, ref hairNum, ref beardNum, ref faceTextureNum, ref mouthTextureNum, ref faceTattooNum, ref soundNum, ref eyebrowNum, ref scale);
	}

	public static void GetZeroProbabilities(int race, int curGender, float curAge, ref float tattooZeroProbability)
	{
		MBAPI.IMBFaceGen.GetZeroProbabilities(race, curGender, curAge, ref tattooZeroProbability);
	}

	public static void ProduceNumericKeyWithParams(FaceGenerationParams faceGenerationParams, bool earsAreHidden, bool mouthIsHidden, ref BodyProperties bodyProperties)
	{
		MBAPI.IMBFaceGen.ProduceNumericKeyWithParams(ref faceGenerationParams, earsAreHidden, mouthIsHidden, ref bodyProperties);
	}

	public static void TransformFaceKeysToDefaultFace(ref FaceGenerationParams faceGenerationParams)
	{
		MBAPI.IMBFaceGen.TransformFaceKeysToDefaultFace(ref faceGenerationParams);
	}

	public static void ProduceNumericKeyWithDefaultValues(ref BodyProperties initialBodyProperties, bool earsAreHidden, bool mouthIsHidden, int race, int gender, int age)
	{
		MBAPI.IMBFaceGen.ProduceNumericKeyWithDefaultValues(ref initialBodyProperties, earsAreHidden, mouthIsHidden, race, gender, age);
	}

	public static BodyProperties GetRandomBodyProperties(int race, bool isFemale, BodyProperties bodyPropertiesMin, BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tatooTags, float variationAmount)
	{
		BodyProperties outBodyProperties = default(BodyProperties);
		MBAPI.IMBFaceGen.GetRandomBodyProperties(race, isFemale ? 1 : 0, ref bodyPropertiesMin, ref bodyPropertiesMax, hairCoverType, seed, hairTags, beardTags, tatooTags, variationAmount, ref outBodyProperties);
		return outBodyProperties;
	}

	public static DeformKeyData GetDeformKeyData(int keyNo, int race, int gender, int age)
	{
		DeformKeyData deformKeyData = default(DeformKeyData);
		MBAPI.IMBFaceGen.GetDeformKeyData(keyNo, ref deformKeyData, race, gender, age);
		return deformKeyData;
	}

	public static int GetFaceGenInstancesLength(int race, int gender, int age)
	{
		return MBAPI.IMBFaceGen.GetFaceGenInstancesLength(race, gender, age);
	}

	public static bool EnforceConstraints(ref FaceGenerationParams faceGenerationParams)
	{
		return MBAPI.IMBFaceGen.EnforceConstraints(ref faceGenerationParams);
	}

	public static float GetScaleFromKey(int race, int gender, BodyProperties bodyProperties)
	{
		return MBAPI.IMBFaceGen.GetScaleFromKey(race, gender, ref bodyProperties);
	}

	public static int GetHairColorCount(int race, int curGender, int age)
	{
		return MBAPI.IMBFaceGen.GetHairColorCount(race, curGender, age);
	}

	public static List<uint> GetHairColorGradientPoints(int race, int curGender, int age)
	{
		int hairColorCount = GetHairColorCount(race, curGender, age);
		List<uint> list = new List<uint>();
		Vec3[] array = new Vec3[hairColorCount];
		MBAPI.IMBFaceGen.GetHairColorGradientPoints(race, curGender, age, array);
		Vec3[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Vec3 vec = array2[i];
			list.Add(MBMath.ColorFromRGBA(vec.x, vec.y, vec.z, 1f));
		}
		return list;
	}

	public static int GetTatooColorCount(int race, int curGender, int age)
	{
		return MBAPI.IMBFaceGen.GetTatooColorCount(race, curGender, age);
	}

	public static List<uint> GetTatooColorGradientPoints(int race, int curGender, int age)
	{
		int tatooColorCount = GetTatooColorCount(race, curGender, age);
		List<uint> list = new List<uint>();
		Vec3[] array = new Vec3[tatooColorCount];
		MBAPI.IMBFaceGen.GetTatooColorGradientPoints(race, curGender, age, array);
		Vec3[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Vec3 vec = array2[i];
			list.Add(MBMath.ColorFromRGBA(vec.x, vec.y, vec.z, 1f));
		}
		return list;
	}

	public static int GetSkinColorCount(int race, int curGender, int age)
	{
		return MBAPI.IMBFaceGen.GetSkinColorCount(race, curGender, age);
	}

	public static BodyMeshMaturityType GetMaturityType(float age)
	{
		return (BodyMeshMaturityType)MBAPI.IMBFaceGen.GetMaturityType(age);
	}

	public static void FlushFaceCache()
	{
		MBAPI.IMBFaceGen.FlushFaceCache();
	}

	public static string[] GetRaceIds()
	{
		return MBAPI.IMBFaceGen.GetRaceIds().Split(new char[1] { ';' });
	}

	public static int[] GetHairIndicesByTag(int race, int curGender, float age, string tag)
	{
		int result;
		return (from x in MBAPI.IMBFaceGen.GetHairIndicesByTag(race, curGender, age, tag).Split(new char[1] { ',' })
			where int.TryParse(x, out result)
			select x).Select(int.Parse).ToArray();
	}

	public static int[] GetFacialIndicesByTag(int race, int curGender, float age, string tag)
	{
		int result;
		return (from x in MBAPI.IMBFaceGen.GetFacialIndicesByTag(race, curGender, age, tag).Split(new char[1] { ',' })
			where int.TryParse(x, out result)
			select x).Select(int.Parse).ToArray();
	}

	public static int[] GetTattooIndicesByTag(int race, int curGender, float age, string tag)
	{
		int result;
		return (from x in MBAPI.IMBFaceGen.GetTattooIndicesByTag(race, curGender, age, tag).Split(new char[1] { ',' })
			where int.TryParse(x, out result)
			select x).Select(int.Parse).ToArray();
	}

	public static List<uint> GetSkinColorGradientPoints(int race, int curGender, int age)
	{
		int skinColorCount = GetSkinColorCount(race, curGender, age);
		List<uint> list = new List<uint>();
		Vec3[] array = new Vec3[skinColorCount];
		MBAPI.IMBFaceGen.GetSkinColorGradientPoints(race, curGender, age, array);
		Vec3[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Vec3 vec = array2[i];
			list.Add(MBMath.ColorFromRGBA(vec.x, vec.y, vec.z, 1f));
		}
		return list;
	}

	public static List<bool> GetVoiceTypeUsableForPlayerData(int race, int curGender, float age, int voiceTypeCount)
	{
		bool[] array = new bool[voiceTypeCount];
		MBAPI.IMBFaceGen.GetVoiceTypeUsableForPlayerData(race, curGender, age, array);
		return new List<bool>(array);
	}

	public static void SetHair(ref BodyProperties bodyProperties, int hair, int beard, int tattoo)
	{
		FaceGenerationParams faceGenerationParams = FaceGenerationParams.Create();
		GetParamsFromKey(ref faceGenerationParams, bodyProperties, earsAreHidden: false, mouthHidden: false);
		if (hair > -1)
		{
			faceGenerationParams.CurrentHair = hair;
		}
		if (beard > -1)
		{
			faceGenerationParams.CurrentBeard = beard;
		}
		if (tattoo > -1)
		{
			faceGenerationParams.CurrentFaceTattoo = tattoo;
		}
		ProduceNumericKeyWithParams(faceGenerationParams, earsAreHidden: false, mouthIsHidden: false, ref bodyProperties);
	}

	public static void SetBody(ref BodyProperties bodyProperties, int build, int weight)
	{
		FaceGenerationParams faceGenerationParams = FaceGenerationParams.Create();
		GetParamsFromKey(ref faceGenerationParams, bodyProperties, earsAreHidden: false, mouthHidden: false);
		_ = -1;
		_ = -1;
		ProduceNumericKeyWithParams(faceGenerationParams, earsAreHidden: false, mouthIsHidden: false, ref bodyProperties);
	}

	public static void SetPigmentation(ref BodyProperties bodyProperties, int skinColor, int hairColor, int eyeColor)
	{
		FaceGenerationParams faceGenerationParams = FaceGenerationParams.Create();
		GetParamsFromKey(ref faceGenerationParams, bodyProperties, earsAreHidden: false, mouthHidden: false);
		_ = -1;
		_ = -1;
		_ = -1;
		ProduceNumericKeyWithParams(faceGenerationParams, earsAreHidden: false, mouthIsHidden: false, ref bodyProperties);
	}

	public static void GenerateParentKey(BodyProperties childBodyProperties, int race, ref BodyProperties motherBodyProperties, ref BodyProperties fatherBodyProperties)
	{
		FaceGenerationParams faceGenerationParams = FaceGenerationParams.Create();
		FaceGenerationParams faceGenerationParams2 = FaceGenerationParams.Create();
		FaceGenerationParams faceGenerationParams3 = FaceGenerationParams.Create();
		GenerationType[] array = new GenerationType[4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (GenerationType)MBRandom.RandomInt(2);
		}
		GetParamsFromKey(ref faceGenerationParams, childBodyProperties, earsAreHidden: false, mouthHidden: false);
		int faceGenInstancesLength = GetFaceGenInstancesLength(race, faceGenerationParams.CurrentGender, (int)faceGenerationParams.CurrentAge);
		for (int j = 0; j < faceGenInstancesLength; j++)
		{
			DeformKeyData deformKeyData = GetDeformKeyData(j, race, faceGenerationParams.CurrentGender, (int)faceGenerationParams.CurrentAge);
			if (deformKeyData.GroupId >= 0 && deformKeyData.GroupId != 0 && deformKeyData.GroupId != 5 && deformKeyData.GroupId != 6)
			{
				float num = MBRandom.RandomFloat * MathF.Min(faceGenerationParams.KeyWeights[j], 1f - faceGenerationParams.KeyWeights[j]);
				if (array[deformKeyData.GroupId - 1] == GenerationType.FromMother)
				{
					faceGenerationParams3.KeyWeights[j] = faceGenerationParams.KeyWeights[j];
					faceGenerationParams2.KeyWeights[j] = faceGenerationParams.KeyWeights[j] + num;
				}
				else if (array[deformKeyData.GroupId - 1] == GenerationType.FromFather)
				{
					faceGenerationParams2.KeyWeights[j] = faceGenerationParams.KeyWeights[j];
					faceGenerationParams3.KeyWeights[j] = faceGenerationParams.KeyWeights[j] + num;
				}
				else
				{
					faceGenerationParams3.KeyWeights[j] = faceGenerationParams.KeyWeights[j] + num;
					faceGenerationParams2.KeyWeights[j] = faceGenerationParams.KeyWeights[j] - num;
				}
			}
		}
		faceGenerationParams2.CurrentAge = faceGenerationParams.CurrentAge + (float)MBRandom.RandomInt(18, 25);
		faceGenerationParams2.SetRandomParamsExceptKeys(race, 0, (int)faceGenerationParams2.CurrentAge, out var _);
		faceGenerationParams2.CurrentFaceTattoo = 0;
		faceGenerationParams3.CurrentAge = faceGenerationParams.CurrentAge + (float)MBRandom.RandomInt(18, 22);
		faceGenerationParams3.SetRandomParamsExceptKeys(race, 1, (int)faceGenerationParams3.CurrentAge, out var _);
		faceGenerationParams3.CurrentFaceTattoo = 0;
		faceGenerationParams3.HeightMultiplier = faceGenerationParams2.HeightMultiplier * MBRandom.RandomFloatRanged(0.7f, 0.9f);
		if (faceGenerationParams3.CurrentHair == 0)
		{
			faceGenerationParams3.CurrentHair = 1;
		}
		float num2 = MBRandom.RandomFloat * MathF.Min(faceGenerationParams.CurrentSkinColorOffset, 1f - faceGenerationParams.CurrentSkinColorOffset);
		float num3 = MBRandom.RandomFloat * MathF.Min(faceGenerationParams.CurrentHairColorOffset, 1f - faceGenerationParams.CurrentHairColorOffset);
		int num4 = MBRandom.RandomInt(2);
		if (num4 == 1)
		{
			faceGenerationParams2.CurrentSkinColorOffset = faceGenerationParams.CurrentSkinColorOffset + num2;
			faceGenerationParams3.CurrentSkinColorOffset = faceGenerationParams.CurrentSkinColorOffset - num2;
		}
		else
		{
			faceGenerationParams2.CurrentSkinColorOffset = faceGenerationParams.CurrentSkinColorOffset - num2;
			faceGenerationParams3.CurrentSkinColorOffset = faceGenerationParams.CurrentSkinColorOffset + num2;
		}
		if (num4 == 1)
		{
			faceGenerationParams2.CurrentHairColorOffset = faceGenerationParams.CurrentHairColorOffset + num3;
			faceGenerationParams3.CurrentHairColorOffset = faceGenerationParams.CurrentHairColorOffset - num3;
		}
		else
		{
			faceGenerationParams2.CurrentHairColorOffset = faceGenerationParams.CurrentHairColorOffset - num3;
			faceGenerationParams3.CurrentHairColorOffset = faceGenerationParams.CurrentHairColorOffset + num3;
		}
		ProduceNumericKeyWithParams(faceGenerationParams3, earsAreHidden: false, mouthIsHidden: false, ref motherBodyProperties);
		ProduceNumericKeyWithParams(faceGenerationParams2, earsAreHidden: false, mouthIsHidden: false, ref fatherBodyProperties);
	}

	public static BodyProperties GetBodyPropertiesWithAge(ref BodyProperties bodyProperties, float age)
	{
		FaceGenerationParams faceGenerationParams = default(FaceGenerationParams);
		GetParamsFromKey(ref faceGenerationParams, bodyProperties, earsAreHidden: false, mouthHidden: false);
		faceGenerationParams.CurrentAge = age;
		BodyProperties bodyProperties2 = default(BodyProperties);
		ProduceNumericKeyWithParams(faceGenerationParams, earsAreHidden: false, mouthIsHidden: false, ref bodyProperties2);
		return bodyProperties2;
	}
}
