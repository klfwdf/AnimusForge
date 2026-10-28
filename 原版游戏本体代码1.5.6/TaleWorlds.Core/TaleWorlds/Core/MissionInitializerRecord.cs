using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public struct MissionInitializerRecord(string name) : ISerializableObject
{
	public int TerrainType = -1;

	public float DamageToFriendsMultiplier = 1f;

	public float DamageFromPlayerToFriendsMultiplier = 1f;

	[MarshalAs(UnmanagedType.U1)]
	public bool NeedsRandomTerrain = false;

	public int RandomTerrainSeed = 0;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string SceneName = name;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string SceneLevels = "";

	[MarshalAs(UnmanagedType.U1)]
	public bool PlayingInCampaignMode = false;

	[MarshalAs(UnmanagedType.U1)]
	public bool EnableSceneRecording = false;

	public int SceneUpgradeLevel = 0;

	[MarshalAs(UnmanagedType.U1)]
	public bool SceneHasMapPatch = false;

	public Vec2 PatchCoordinates = Vec2.Zero;

	public Vec2 PatchEncounterDir = Vec2.Zero;

	[MarshalAs(UnmanagedType.U1)]
	public bool DoNotUseLoadingScreen = false;

	[MarshalAs(UnmanagedType.U1)]
	public bool DisableDynamicPointlightShadows = false;

	[MarshalAs(UnmanagedType.I1)]
	public bool DisableCorpseFadeOut = false;

	public int DecalAtlasGroup = 0;

	public AtmosphereInfo AtmosphereOnCampaign = AtmosphereInfo.GetInvalidAtmosphereInfo();

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
