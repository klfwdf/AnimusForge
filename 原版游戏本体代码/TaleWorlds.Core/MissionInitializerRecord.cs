using System;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x020000B8 RID: 184
	public struct MissionInitializerRecord : ISerializableObject
	{
		// Token: 0x0600098D RID: 2445 RVA: 0x0001F374 File Offset: 0x0001D574
		public MissionInitializerRecord(string name)
		{
			this.TerrainType = -1;
			this.DamageToFriendsMultiplier = 1f;
			this.DamageFromPlayerToFriendsMultiplier = 1f;
			this.NeedsRandomTerrain = false;
			this.RandomTerrainSeed = 0;
			this.SceneName = name;
			this.SceneLevels = "";
			this.PlayingInCampaignMode = false;
			this.EnableSceneRecording = false;
			this.SceneUpgradeLevel = 0;
			this.SceneHasMapPatch = false;
			this.PatchCoordinates = Vec2.Zero;
			this.PatchEncounterDir = Vec2.Zero;
			this.DoNotUseLoadingScreen = false;
			this.DisableDynamicPointlightShadows = false;
			this.DecalAtlasGroup = 0;
			this.AtmosphereOnCampaign = AtmosphereInfo.GetInvalidAtmosphereInfo();
			this.DisableCorpseFadeOut = false;
		}

		// Token: 0x0600098E RID: 2446 RVA: 0x0001F418 File Offset: 0x0001D618
		void ISerializableObject.DeserializeFrom(IReader reader)
		{
			this.SceneName = reader.ReadString();
			this.SceneLevels = reader.ReadString();
			reader.ReadFloat();
			this.NeedsRandomTerrain = reader.ReadBool();
			this.RandomTerrainSeed = reader.ReadInt();
			this.EnableSceneRecording = reader.ReadBool();
			this.SceneUpgradeLevel = reader.ReadInt();
			this.PlayingInCampaignMode = reader.ReadBool();
			this.DisableDynamicPointlightShadows = reader.ReadBool();
			this.DoNotUseLoadingScreen = reader.ReadBool();
			this.DisableCorpseFadeOut = reader.ReadBool();
			if (reader.ReadBool())
			{
				this.AtmosphereOnCampaign = AtmosphereInfo.GetInvalidAtmosphereInfo();
				this.AtmosphereOnCampaign.DeserializeFrom(reader);
			}
		}

		// Token: 0x0600098F RID: 2447 RVA: 0x0001F4C4 File Offset: 0x0001D6C4
		void ISerializableObject.SerializeTo(IWriter writer)
		{
			writer.WriteString(this.SceneName);
			writer.WriteString(this.SceneLevels);
			writer.WriteFloat(6f);
			writer.WriteBool(this.NeedsRandomTerrain);
			writer.WriteInt(this.RandomTerrainSeed);
			writer.WriteBool(this.EnableSceneRecording);
			writer.WriteInt(this.SceneUpgradeLevel);
			writer.WriteBool(this.PlayingInCampaignMode);
			writer.WriteBool(this.DisableDynamicPointlightShadows);
			writer.WriteBool(this.DoNotUseLoadingScreen);
			writer.WriteBool(this.DisableCorpseFadeOut);
			writer.WriteInt(this.DecalAtlasGroup);
			bool isValid = this.AtmosphereOnCampaign.IsValid;
			writer.WriteBool(isValid);
			if (isValid)
			{
				this.AtmosphereOnCampaign.SerializeTo(writer);
			}
		}

		// Token: 0x04000542 RID: 1346
		public int TerrainType;

		// Token: 0x04000543 RID: 1347
		public float DamageToFriendsMultiplier;

		// Token: 0x04000544 RID: 1348
		public float DamageFromPlayerToFriendsMultiplier;

		// Token: 0x04000545 RID: 1349
		[MarshalAs(UnmanagedType.U1)]
		public bool NeedsRandomTerrain;

		// Token: 0x04000546 RID: 1350
		public int RandomTerrainSeed;

		// Token: 0x04000547 RID: 1351
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
		public string SceneName;

		// Token: 0x04000548 RID: 1352
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
		public string SceneLevels;

		// Token: 0x04000549 RID: 1353
		[MarshalAs(UnmanagedType.U1)]
		public bool PlayingInCampaignMode;

		// Token: 0x0400054A RID: 1354
		[MarshalAs(UnmanagedType.U1)]
		public bool EnableSceneRecording;

		// Token: 0x0400054B RID: 1355
		public int SceneUpgradeLevel;

		// Token: 0x0400054C RID: 1356
		[MarshalAs(UnmanagedType.U1)]
		public bool SceneHasMapPatch;

		// Token: 0x0400054D RID: 1357
		public Vec2 PatchCoordinates;

		// Token: 0x0400054E RID: 1358
		public Vec2 PatchEncounterDir;

		// Token: 0x0400054F RID: 1359
		[MarshalAs(UnmanagedType.U1)]
		public bool DoNotUseLoadingScreen;

		// Token: 0x04000550 RID: 1360
		[MarshalAs(UnmanagedType.U1)]
		public bool DisableDynamicPointlightShadows;

		// Token: 0x04000551 RID: 1361
		[MarshalAs(UnmanagedType.I1)]
		public bool DisableCorpseFadeOut;

		// Token: 0x04000552 RID: 1362
		public int DecalAtlasGroup;

		// Token: 0x04000553 RID: 1363
		public AtmosphereInfo AtmosphereOnCampaign;
	}
}
