using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000146 RID: 326
	public class DefaultSceneModel : SceneModel
	{
		// Token: 0x06001987 RID: 6535 RVA: 0x0007F8F4 File Offset: 0x0007DAF4
		public override string GetBattleSceneForMapPatch(MapPatchData mapPatch, bool isNavalEncounter)
		{
			MBList<SingleplayerBattleSceneData> mblist = (from scene in GameSceneDataManager.Instance.SingleplayerBattleScenes
				where scene.MapIndices.Contains(mapPatch.sceneIndex) && scene.IsNaval == isNavalEncounter
				select scene).ToMBList<SingleplayerBattleSceneData>();
			string sceneID;
			if (mblist.IsEmpty<SingleplayerBattleSceneData>())
			{
				IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
				CampaignVec2 position = MobileParty.MainParty.Position;
				TerrainType currentPositionTerrainType;
				mapSceneWrapper.GetEnvironmentTerrainTypesCount(position, out currentPositionTerrainType);
				mblist = (from scene in GameSceneDataManager.Instance.SingleplayerBattleScenes
					where scene.Terrain == currentPositionTerrainType && scene.IsNaval == isNavalEncounter
					select scene).ToMBList<SingleplayerBattleSceneData>();
				if (mblist.IsEmpty<SingleplayerBattleSceneData>())
				{
					Debug.FailedAssert("Battle scene for map patch with scene index " + mapPatch.sceneIndex + " does not exist. Picking a random scene", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSceneModel.cs", "GetBattleSceneForMapPatch", 35);
					mblist = (from scene in GameSceneDataManager.Instance.SingleplayerBattleScenes
						where scene.IsNaval == isNavalEncounter
						select scene).ToMBList<SingleplayerBattleSceneData>();
					if (mblist.IsEmpty<SingleplayerBattleSceneData>())
					{
						Debug.FailedAssert("naval battles scene mismatch. Picking a random scene", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSceneModel.cs", "GetBattleSceneForMapPatch", 40);
						mblist = GameSceneDataManager.Instance.SingleplayerBattleScenes.ToMBList<SingleplayerBattleSceneData>();
					}
				}
				sceneID = mblist.GetRandomElement<SingleplayerBattleSceneData>().SceneID;
			}
			else if (mblist.Count > 1)
			{
				if (isNavalEncounter)
				{
					IMapScene mapSceneWrapper2 = Campaign.Current.MapSceneWrapper;
					CampaignVec2 position = MobileParty.MainParty.Position;
					TerrainType currentPositionTerrainType;
					mapSceneWrapper2.GetEnvironmentTerrainTypesCount(position, out currentPositionTerrainType);
					List<SingleplayerBattleSceneData> list = (from scene in mblist
						where scene.Terrain == currentPositionTerrainType
						select scene).ToList<SingleplayerBattleSceneData>();
					if (!list.IsEmpty<SingleplayerBattleSceneData>())
					{
						sceneID = list.GetRandomElement<SingleplayerBattleSceneData>().SceneID;
					}
					else
					{
						sceneID = mblist.GetRandomElement<SingleplayerBattleSceneData>().SceneID;
					}
				}
				else
				{
					Debug.FailedAssert("Multiple battle scenes for map patch with scene index " + mapPatch.sceneIndex + " are defined. Picking a matching scene randomly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSceneModel.cs", "GetBattleSceneForMapPatch", 67);
					sceneID = mblist.GetRandomElement<SingleplayerBattleSceneData>().SceneID;
				}
			}
			else
			{
				sceneID = mblist[0].SceneID;
			}
			return sceneID;
		}

		// Token: 0x06001988 RID: 6536 RVA: 0x0007FB20 File Offset: 0x0007DD20
		public override string GetConversationSceneForMapPosition(CampaignVec2 campaignPosition)
		{
			TerrainType currentPositionTerrainType;
			List<TerrainType> environmentTerrainTypesCount = Campaign.Current.MapSceneWrapper.GetEnvironmentTerrainTypesCount(campaignPosition, out currentPositionTerrainType);
			TerrainType terrain = DefaultSceneModel.GetTerrainByCount(environmentTerrainTypesCount, currentPositionTerrainType);
			return (GameSceneDataManager.Instance.ConversationScenes.Any((ConversationSceneData scene) => scene.Terrain == terrain) ? GameSceneDataManager.Instance.ConversationScenes.GetRandomElementWithPredicate((ConversationSceneData scene) => scene.Terrain == terrain) : GameSceneDataManager.Instance.ConversationScenes.GetRandomElement<ConversationSceneData>()).SceneID;
		}

		// Token: 0x06001989 RID: 6537 RVA: 0x0007FBA8 File Offset: 0x0007DDA8
		private static TerrainType GetTerrainByCount(List<TerrainType> terrainTypeSamples, TerrainType currentPositionTerrainType)
		{
			for (int i = 0; i < terrainTypeSamples.Count; i++)
			{
				if (terrainTypeSamples[i] == TerrainType.Snow)
				{
					terrainTypeSamples[i] = TerrainType.Plain;
				}
			}
			if (DefaultSceneModel._conversationTerrains.Contains(currentPositionTerrainType))
			{
				int num = (int)((float)terrainTypeSamples.Count * 0.33f);
				for (int j = 0; j < num; j++)
				{
					terrainTypeSamples.Add(currentPositionTerrainType);
				}
			}
			Dictionary<TerrainType, int> dictionary = new Dictionary<TerrainType, int>();
			foreach (TerrainType terrainType in terrainTypeSamples)
			{
				if (DefaultSceneModel._conversationTerrains.Contains(terrainType))
				{
					if (!dictionary.ContainsKey(terrainType))
					{
						dictionary.Add(terrainType, 1);
					}
					else
					{
						Dictionary<TerrainType, int> dictionary2 = dictionary;
						TerrainType key = terrainType;
						int num2 = dictionary2[key];
						dictionary2[key] = num2 + 1;
					}
				}
			}
			if (dictionary.Count > 0)
			{
				return (from t in dictionary
					orderby t.Value descending
					select t).First<KeyValuePair<TerrainType, int>>().Key;
			}
			return TerrainType.Plain;
		}

		// Token: 0x0600198B RID: 6539 RVA: 0x0007FCCC File Offset: 0x0007DECC
		// Note: this type is marked as 'beforefieldinit'.
		static DefaultSceneModel()
		{
			TerrainType[] array = new TerrainType[9];
			RuntimeHelpers.InitializeArray(array, fieldof(<PrivateImplementationDetails>.75FB24E28A16E3036FD9D109C4AA65CF9A3C638A).FieldHandle);
			DefaultSceneModel._conversationTerrains = array;
		}

		// Token: 0x0400086C RID: 2156
		private static readonly TerrainType[] _conversationTerrains;
	}
}
