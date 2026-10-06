using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Skin_generation_params", false, null)]
public struct SkinGenerationParams
{
	public int _skinMeshesVisibilityMask;

	public Equipment.UnderwearTypes _underwearType;

	public int _bodyMeshType;

	public int _hairCoverType;

	public int _beardCoverType;

	public int _bodyDeformType;

	[MarshalAs(UnmanagedType.U1)]
	public bool _prepareImmediately;

	[MarshalAs(UnmanagedType.U1)]
	public bool _useTranslucency;

	[MarshalAs(UnmanagedType.U1)]
	public bool _useTesselation;

	public float _faceDirtAmount;

	public int _gender;

	public int _race;

	public int _faceCacheId;

	public static SkinGenerationParams Create()
	{
		SkinGenerationParams result = default(SkinGenerationParams);
		result._skinMeshesVisibilityMask = 481;
		result._underwearType = Equipment.UnderwearTypes.FullUnderwear;
		result._bodyMeshType = 0;
		result._hairCoverType = 0;
		result._beardCoverType = 0;
		result._prepareImmediately = false;
		result._bodyDeformType = -1;
		result._faceDirtAmount = 0f;
		result._gender = 0;
		result._race = 0;
		result._useTranslucency = false;
		result._useTesselation = false;
		result._faceCacheId = 0;
		return result;
	}

	public SkinGenerationParams(int skinMeshesVisibilityMask, Equipment.UnderwearTypes underwearType, int bodyMeshType, int hairCoverType, int beardCoverType, int bodyDeformType, bool prepareImmediately, float faceDirtAmount, int gender, int race, bool useTranslucency, bool useTesselation, int faceCacheID)
	{
		_skinMeshesVisibilityMask = skinMeshesVisibilityMask;
		_underwearType = underwearType;
		_bodyMeshType = bodyMeshType;
		_hairCoverType = hairCoverType;
		_beardCoverType = beardCoverType;
		_bodyDeformType = bodyDeformType;
		_prepareImmediately = prepareImmediately;
		_faceDirtAmount = faceDirtAmount;
		_gender = gender;
		_race = race;
		_useTranslucency = useTranslucency;
		_useTesselation = useTesselation;
		_faceCacheId = faceCacheID;
	}
}
