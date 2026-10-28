using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBFaceGen : IMBFaceGen
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool EnforceConstraintsDelegate(ref FaceGenerationParams faceGenerationParams);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FlushFaceCacheDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetDeformKeyDataDelegate(int keyNo, ref DeformKeyData deformKeyData, int race, int gender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFaceGenInstancesLengthDelegate(int race, int gender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFacialIndicesByTagDelegate(int race, int curGender, float age, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetHairColorCountDelegate(int race, int curGender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetHairColorGradientPointsDelegate(int race, int curGender, float age, IntPtr colors);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetHairIndicesByTagDelegate(int race, int curGender, float age, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMaturityTypeDelegate(float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumEditableDeformKeysDelegate(int race, [MarshalAs(UnmanagedType.U1)] bool initialGender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetParamsFromKeyDelegate(ref FaceGenerationParams faceGenerationParams, ref BodyProperties bodyProperties, [MarshalAs(UnmanagedType.U1)] bool earsAreHidden, [MarshalAs(UnmanagedType.U1)] bool mouthHidden);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetParamsMaxDelegate(int race, int curGender, float curAge, ref int hairNum, ref int beardNum, ref int faceTextureNum, ref int mouthTextureNum, ref int faceTattooNum, ref int soundNum, ref int eyebrowNum, ref float scale);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetRaceIdsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetRandomBodyPropertiesDelegate(int race, int gender, ref BodyProperties bodyPropertiesMin, ref BodyProperties bodyPropertiesMax, int hairCoverType, int seed, byte[] hairTags, byte[] beardTags, byte[] tatooTags, float variationAmount, ref BodyProperties outBodyProperties);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetScaleFromKeyDelegate(int race, int gender, ref BodyProperties initialBodyProperties);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSkinColorCountDelegate(int race, int curGender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetSkinColorGradientPointsDelegate(int race, int curGender, float age, IntPtr colors);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetTatooColorCountDelegate(int race, int curGender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetTatooColorGradientPointsDelegate(int race, int curGender, float age, IntPtr colors);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetTattooIndicesByTagDelegate(int race, int curGender, float age, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVoiceRecordsCountDelegate(int race, int curGender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetVoiceTypeUsableForPlayerDataDelegate(int race, int curGender, float age, IntPtr aiArray);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetZeroProbabilitiesDelegate(int race, int curGender, float curAge, ref float tattooZeroProbability);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ProduceNumericKeyWithDefaultValuesDelegate(ref BodyProperties initialBodyProperties, [MarshalAs(UnmanagedType.U1)] bool earsAreHidden, [MarshalAs(UnmanagedType.U1)] bool mouthIsHidden, int race, int gender, float age);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ProduceNumericKeyWithParamsDelegate(ref FaceGenerationParams faceGenerationParams, [MarshalAs(UnmanagedType.U1)] bool earsAreHidden, [MarshalAs(UnmanagedType.U1)] bool mouthIsHidden, ref BodyProperties bodyProperties);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TransformFaceKeysToDefaultFaceDelegate(ref FaceGenerationParams faceGenerationParams);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static EnforceConstraintsDelegate call_EnforceConstraintsDelegate;

	public static FlushFaceCacheDelegate call_FlushFaceCacheDelegate;

	public static GetDeformKeyDataDelegate call_GetDeformKeyDataDelegate;

	public static GetFaceGenInstancesLengthDelegate call_GetFaceGenInstancesLengthDelegate;

	public static GetFacialIndicesByTagDelegate call_GetFacialIndicesByTagDelegate;

	public static GetHairColorCountDelegate call_GetHairColorCountDelegate;

	public static GetHairColorGradientPointsDelegate call_GetHairColorGradientPointsDelegate;

	public static GetHairIndicesByTagDelegate call_GetHairIndicesByTagDelegate;

	public static GetMaturityTypeDelegate call_GetMaturityTypeDelegate;

	public static GetNumEditableDeformKeysDelegate call_GetNumEditableDeformKeysDelegate;

	public static GetParamsFromKeyDelegate call_GetParamsFromKeyDelegate;

	public static GetParamsMaxDelegate call_GetParamsMaxDelegate;

	public static GetRaceIdsDelegate call_GetRaceIdsDelegate;

	public static GetRandomBodyPropertiesDelegate call_GetRandomBodyPropertiesDelegate;

	public static GetScaleFromKeyDelegate call_GetScaleFromKeyDelegate;

	public static GetSkinColorCountDelegate call_GetSkinColorCountDelegate;

	public static GetSkinColorGradientPointsDelegate call_GetSkinColorGradientPointsDelegate;

	public static GetTatooColorCountDelegate call_GetTatooColorCountDelegate;

	public static GetTatooColorGradientPointsDelegate call_GetTatooColorGradientPointsDelegate;

	public static GetTattooIndicesByTagDelegate call_GetTattooIndicesByTagDelegate;

	public static GetVoiceRecordsCountDelegate call_GetVoiceRecordsCountDelegate;

	public static GetVoiceTypeUsableForPlayerDataDelegate call_GetVoiceTypeUsableForPlayerDataDelegate;

	public static GetZeroProbabilitiesDelegate call_GetZeroProbabilitiesDelegate;

	public static ProduceNumericKeyWithDefaultValuesDelegate call_ProduceNumericKeyWithDefaultValuesDelegate;

	public static ProduceNumericKeyWithParamsDelegate call_ProduceNumericKeyWithParamsDelegate;

	public static TransformFaceKeysToDefaultFaceDelegate call_TransformFaceKeysToDefaultFaceDelegate;

	public bool EnforceConstraints(ref FaceGenerationParams faceGenerationParams)
	{
		return call_EnforceConstraintsDelegate(ref faceGenerationParams);
	}

	public void FlushFaceCache()
	{
		call_FlushFaceCacheDelegate();
	}

	public void GetDeformKeyData(int keyNo, ref DeformKeyData deformKeyData, int race, int gender, float age)
	{
		call_GetDeformKeyDataDelegate(keyNo, ref deformKeyData, race, gender, age);
	}

	public int GetFaceGenInstancesLength(int race, int gender, float age)
	{
		return call_GetFaceGenInstancesLengthDelegate(race, gender, age);
	}

	public string GetFacialIndicesByTag(int race, int curGender, float age, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetFacialIndicesByTagDelegate(race, curGender, age, array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetHairColorCount(int race, int curGender, float age)
	{
		return call_GetHairColorCountDelegate(race, curGender, age);
	}

	public void GetHairColorGradientPoints(int race, int curGender, float age, Vec3[] colors)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(colors);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetHairColorGradientPointsDelegate(race, curGender, age, pointer);
		pinnedArrayData.Dispose();
	}

	public string GetHairIndicesByTag(int race, int curGender, float age, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetHairIndicesByTagDelegate(race, curGender, age, array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetMaturityType(float age)
	{
		return call_GetMaturityTypeDelegate(age);
	}

	public int GetNumEditableDeformKeys(int race, bool initialGender, float age)
	{
		return call_GetNumEditableDeformKeysDelegate(race, initialGender, age);
	}

	public void GetParamsFromKey(ref FaceGenerationParams faceGenerationParams, ref BodyProperties bodyProperties, bool earsAreHidden, bool mouthHidden)
	{
		call_GetParamsFromKeyDelegate(ref faceGenerationParams, ref bodyProperties, earsAreHidden, mouthHidden);
	}

	public void GetParamsMax(int race, int curGender, float curAge, ref int hairNum, ref int beardNum, ref int faceTextureNum, ref int mouthTextureNum, ref int faceTattooNum, ref int soundNum, ref int eyebrowNum, ref float scale)
	{
		call_GetParamsMaxDelegate(race, curGender, curAge, ref hairNum, ref beardNum, ref faceTextureNum, ref mouthTextureNum, ref faceTattooNum, ref soundNum, ref eyebrowNum, ref scale);
	}

	public string GetRaceIds()
	{
		if (call_GetRaceIdsDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void GetRandomBodyProperties(int race, int gender, ref BodyProperties bodyPropertiesMin, ref BodyProperties bodyPropertiesMax, int hairCoverType, int seed, string hairTags, string beardTags, string tatooTags, float variationAmount, ref BodyProperties outBodyProperties)
	{
		byte[] array = null;
		if (hairTags != null)
		{
			int byteCount = _utf8.GetByteCount(hairTags);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(hairTags, 0, hairTags.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (beardTags != null)
		{
			int byteCount2 = _utf8.GetByteCount(beardTags);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(beardTags, 0, beardTags.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (tatooTags != null)
		{
			int byteCount3 = _utf8.GetByteCount(tatooTags);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(tatooTags, 0, tatooTags.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		call_GetRandomBodyPropertiesDelegate(race, gender, ref bodyPropertiesMin, ref bodyPropertiesMax, hairCoverType, seed, array, array2, array3, variationAmount, ref outBodyProperties);
	}

	public float GetScaleFromKey(int race, int gender, ref BodyProperties initialBodyProperties)
	{
		return call_GetScaleFromKeyDelegate(race, gender, ref initialBodyProperties);
	}

	public int GetSkinColorCount(int race, int curGender, float age)
	{
		return call_GetSkinColorCountDelegate(race, curGender, age);
	}

	public void GetSkinColorGradientPoints(int race, int curGender, float age, Vec3[] colors)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(colors);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetSkinColorGradientPointsDelegate(race, curGender, age, pointer);
		pinnedArrayData.Dispose();
	}

	public int GetTatooColorCount(int race, int curGender, float age)
	{
		return call_GetTatooColorCountDelegate(race, curGender, age);
	}

	public void GetTatooColorGradientPoints(int race, int curGender, float age, Vec3[] colors)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(colors);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetTatooColorGradientPointsDelegate(race, curGender, age, pointer);
		pinnedArrayData.Dispose();
	}

	public string GetTattooIndicesByTag(int race, int curGender, float age, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		if (call_GetTattooIndicesByTagDelegate(race, curGender, age, array) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetVoiceRecordsCount(int race, int curGender, float age)
	{
		return call_GetVoiceRecordsCountDelegate(race, curGender, age);
	}

	public void GetVoiceTypeUsableForPlayerData(int race, int curGender, float age, bool[] aiArray)
	{
		PinnedArrayData<bool> pinnedArrayData = new PinnedArrayData<bool>(aiArray);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetVoiceTypeUsableForPlayerDataDelegate(race, curGender, age, pointer);
		pinnedArrayData.Dispose();
	}

	public void GetZeroProbabilities(int race, int curGender, float curAge, ref float tattooZeroProbability)
	{
		call_GetZeroProbabilitiesDelegate(race, curGender, curAge, ref tattooZeroProbability);
	}

	public void ProduceNumericKeyWithDefaultValues(ref BodyProperties initialBodyProperties, bool earsAreHidden, bool mouthIsHidden, int race, int gender, float age)
	{
		call_ProduceNumericKeyWithDefaultValuesDelegate(ref initialBodyProperties, earsAreHidden, mouthIsHidden, race, gender, age);
	}

	public void ProduceNumericKeyWithParams(ref FaceGenerationParams faceGenerationParams, bool earsAreHidden, bool mouthIsHidden, ref BodyProperties bodyProperties)
	{
		call_ProduceNumericKeyWithParamsDelegate(ref faceGenerationParams, earsAreHidden, mouthIsHidden, ref bodyProperties);
	}

	public void TransformFaceKeysToDefaultFace(ref FaceGenerationParams faceGenerationParams)
	{
		call_TransformFaceKeysToDefaultFaceDelegate(ref faceGenerationParams);
	}
}
