using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public struct MissionInitializerRecord : ISerializableObject
{
	public int TerrainType;

	public float DamageToFriendsMultiplier;

	public float DamageFromPlayerToFriendsMultiplier;

	[MarshalAs(UnmanagedType.U1)]
	public bool NeedsRandomTerrain;

	public int RandomTerrainSeed;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string SceneName;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string SceneLevels;

	[MarshalAs(UnmanagedType.U1)]
	public bool PlayingInCampaignMode;

	[MarshalAs(UnmanagedType.U1)]
	public bool EnableSceneRecording;

	public int SceneUpgradeLevel;

	[MarshalAs(UnmanagedType.U1)]
	public bool SceneHasMapPatch;

	public Vec2 PatchCoordinates;

	public Vec2 PatchEncounterDir;

	[MarshalAs(UnmanagedType.U1)]
	public bool DoNotUseLoadingScreen;

	[MarshalAs(UnmanagedType.U1)]
	public bool DisableDynamicPointlightShadows;

	[MarshalAs(UnmanagedType.I1)]
	public bool DisableCorpseFadeOut;

	public int DecalAtlasGroup;

	public AtmosphereInfo AtmosphereOnCampaign;

	public MissionInitializerRecord(string name)
	{
		TerrainType = -1;
		DamageToFriendsMultiplier = 1f;
		DamageFromPlayerToFriendsMultiplier = 1f;
		NeedsRandomTerrain = false;
		RandomTerrainSeed = 0;
		SceneName = name;
		SceneLevels = "";
		PlayingInCampaignMode = false;
		EnableSceneRecording = false;
		SceneUpgradeLevel = 0;
		SceneHasMapPatch = false;
		PatchCoordinates = Vec2.Zero;
		PatchEncounterDir = Vec2.Zero;
		DoNotUseLoadingScreen = false;
		DisableDynamicPointlightShadows = false;
		DecalAtlasGroup = 0;
		AtmosphereOnCampaign = AtmosphereInfo.GetInvalidAtmosphereInfo();
		DisableCorpseFadeOut = false;
	}

	void ISerializableObject.DeserializeFrom(IReader reader)
	{
		SceneName = reader.ReadString();
		SceneLevels = reader.ReadString();
		reader.ReadFloat();
		NeedsRandomTerrain = reader.ReadBool();
		RandomTerrainSeed = reader.ReadInt();
		EnableSceneRecording = reader.ReadBool();
		SceneUpgradeLevel = reader.ReadInt();
		PlayingInCampaignMode = reader.ReadBool();
		DisableDynamicPointlightShadows = reader.ReadBool();
		DoNotUseLoadingScreen = reader.ReadBool();
		DisableCorpseFadeOut = reader.ReadBool();
		if (reader.ReadBool())
		{
			AtmosphereOnCampaign = AtmosphereInfo.GetInvalidAtmosphereInfo();
			AtmosphereOnCampaign.DeserializeFrom(reader);
		}
	}

	void ISerializableObject.SerializeTo(IWriter writer)
	{
		writer.WriteString(SceneName);
		writer.WriteString(SceneLevels);
		writer.WriteFloat(6f);
		writer.WriteBool(NeedsRandomTerrain);
		writer.WriteInt(RandomTerrainSeed);
		writer.WriteBool(EnableSceneRecording);
		writer.WriteInt(SceneUpgradeLevel);
		writer.WriteBool(PlayingInCampaignMode);
		writer.WriteBool(DisableDynamicPointlightShadows);
		writer.WriteBool(DoNotUseLoadingScreen);
		writer.WriteBool(DisableCorpseFadeOut);
		writer.WriteInt(DecalAtlasGroup);
		bool isValid = AtmosphereOnCampaign.IsValid;
		writer.WriteBool(isValid);
		if (isValid)
		{
			AtmosphereOnCampaign.SerializeTo(writer);
		}
	}
}
