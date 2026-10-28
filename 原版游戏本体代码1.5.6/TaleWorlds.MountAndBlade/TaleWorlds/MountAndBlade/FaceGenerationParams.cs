using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Face_generation_params", false, null)]
public struct FaceGenerationParams
{
	public int Seed;

	public int CurrentBeard;

	public int CurrentHair;

	public int CurrentEyebrow;

	public int CurrentRace;

	public int CurrentGender;

	public int CurrentFaceTexture;

	public int CurrentMouthTexture;

	public int CurrentFaceTattoo;

	public int CurrentVoice;

	public int HairFilter;

	public int BeardFilter;

	public int TattooFilter;

	public int FaceTextureFilter;

	public float TattooZeroProbability;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 320)]
	public float[] KeyWeights;

	public float CurrentAge;

	public float CurrentWeight;

	public float CurrentBuild;

	public float CurrentSkinColorOffset;

	public float CurrentHairColorOffset;

	public float CurrentEyeColorOffset;

	public float FaceDirtAmount;

	public float CurrentFaceTattooColorOffset1;

	public float HeightMultiplier;

	public float VoicePitch;

	[MarshalAs(UnmanagedType.U1)]
	public bool IsHairFlipped;

	[MarshalAs(UnmanagedType.U1)]
	public bool UseCache;

	[MarshalAs(UnmanagedType.U1)]
	public bool UseGpuMorph;

	[MarshalAs(UnmanagedType.U1)]
	public bool Padding2;

	public int FaceCacheId;

	public static FaceGenerationParams Create()
	{
		FaceGenerationParams result = default(FaceGenerationParams);
		result.Seed = 0;
		result.CurrentBeard = 0;
		result.CurrentHair = 0;
		result.CurrentEyebrow = 0;
		result.IsHairFlipped = false;
		result.CurrentRace = 0;
		result.CurrentGender = 0;
		result.CurrentFaceTexture = 0;
		result.CurrentMouthTexture = 0;
		result.CurrentFaceTattoo = 0;
		result.CurrentVoice = 0;
		result.HairFilter = 0;
		result.BeardFilter = 0;
		result.TattooFilter = 0;
		result.FaceTextureFilter = 0;
		result.TattooZeroProbability = 0f;
		result.KeyWeights = new float[320];
		result.CurrentAge = 0f;
		result.CurrentWeight = 0f;
		result.CurrentBuild = 0f;
		result.CurrentSkinColorOffset = 0f;
		result.CurrentHairColorOffset = 0f;
		result.CurrentEyeColorOffset = 0f;
		result.FaceDirtAmount = 0f;
		result.CurrentFaceTattooColorOffset1 = 0f;
		result.HeightMultiplier = 0f;
		result.VoicePitch = 0f;
		result.UseCache = false;
		result.UseGpuMorph = false;
		result.Padding2 = false;
		result.FaceCacheId = 0;
		return result;
	}

	public void SetRaceGenderAndAdjustParams(int race, int gender, int curAge)
	{
		CurrentGender = gender;
		CurrentRace = race;
		int hairNum = 0;
		int beardNum = 0;
		int faceTextureNum = 0;
		int mouthTextureNum = 0;
		int eyebrowNum = 0;
		int soundNum = 0;
		int faceTattooNum = 0;
		float scale = 0f;
		MBBodyProperties.GetParamsMax(race, gender, curAge, ref hairNum, ref beardNum, ref faceTextureNum, ref mouthTextureNum, ref faceTattooNum, ref soundNum, ref eyebrowNum, ref scale);
		CurrentHair = MBMath.ClampInt(CurrentHair, 0, hairNum - 1);
		CurrentBeard = MBMath.ClampInt(CurrentBeard, 0, beardNum - 1);
		CurrentFaceTexture = MBMath.ClampInt(CurrentFaceTexture, 0, faceTextureNum - 1);
		CurrentMouthTexture = MBMath.ClampInt(CurrentMouthTexture, 0, mouthTextureNum - 1);
		CurrentFaceTattoo = MBMath.ClampInt(CurrentFaceTattoo, 0, faceTattooNum - 1);
		CurrentVoice = MBMath.ClampInt(CurrentVoice, 0, soundNum - 1);
		VoicePitch = MBMath.ClampFloat(VoicePitch, 0f, 1f);
		CurrentEyebrow = MBMath.ClampInt(CurrentEyebrow, 0, eyebrowNum - 1);
	}

	public void SetRandomParamsExceptKeys(int race, int gender, int minAge, out float scale)
	{
		int hairNum = 0;
		int beardNum = 0;
		int faceTextureNum = 0;
		int mouthTextureNum = 0;
		int eyebrowNum = 0;
		int soundNum = 0;
		int faceTattooNum = 0;
		scale = 0f;
		MBBodyProperties.GetParamsMax(race, gender, minAge, ref hairNum, ref beardNum, ref faceTextureNum, ref mouthTextureNum, ref faceTattooNum, ref soundNum, ref eyebrowNum, ref scale);
		CurrentHair = MBRandom.RandomInt(hairNum);
		CurrentBeard = MBRandom.RandomInt(beardNum);
		CurrentFaceTexture = MBRandom.RandomInt(faceTextureNum);
		CurrentMouthTexture = MBRandom.RandomInt(mouthTextureNum);
		CurrentFaceTattoo = MBRandom.RandomInt(faceTattooNum);
		CurrentVoice = MBRandom.RandomInt(soundNum);
		VoicePitch = MBRandom.RandomFloat;
		CurrentEyebrow = MBRandom.RandomInt(eyebrowNum);
		CurrentSkinColorOffset = MBRandom.RandomFloat;
		CurrentHairColorOffset = MBRandom.RandomFloat;
		CurrentEyeColorOffset = MBRandom.RandomFloat;
		CurrentFaceTattooColorOffset1 = MBRandom.RandomFloat;
		HeightMultiplier = MBRandom.RandomFloat;
	}
}
