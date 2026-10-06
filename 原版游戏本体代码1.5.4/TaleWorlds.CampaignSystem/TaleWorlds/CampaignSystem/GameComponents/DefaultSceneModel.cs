using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSceneModel : SceneModel
{
	private static readonly TerrainType[] _conversationTerrains = new TerrainType[9]
	{
		TerrainType.Plain,
		TerrainType.Desert,
		TerrainType.Swamp,
		TerrainType.Steppe,
		TerrainType.OpenSea,
		TerrainType.CoastalSea,
		TerrainType.Lake,
		TerrainType.River,
		TerrainType.Water
	};

	public override string GetBattleSceneForMapPatch(MapPatchData mapPatch, bool isNavalEncounter)
	{
		string text = "";
		MBList<SingleplayerBattleSceneData> mBList = GameSceneDataManager.Instance.SingleplayerBattleScenes.Where((SingleplayerBattleSceneData scene) => scene.MapIndices.Contains(mapPatch.sceneIndex) && scene.IsNaval == isNavalEncounter).ToMBList();
		if (mBList.IsEmpty())
		{
			Campaign.Current.MapSceneWrapper.GetEnvironmentTerrainTypesCount(MobileParty.MainParty.Position, out var currentPositionTerrainType);
			mBList = GameSceneDataManager.Instance.SingleplayerBattleScenes.Where((SingleplayerBattleSceneData scene) => scene.Terrain == currentPositionTerrainType && scene.IsNaval == isNavalEncounter).ToMBList();
			if (mBList.IsEmpty())
			{
				Debug.FailedAssert("Battle scene for map patch with scene index " + mapPatch.sceneIndex + " does not exist. Picking a random scene", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSceneModel.cs", "GetBattleSceneForMapPatch", 35);
				mBList = GameSceneDataManager.Instance.SingleplayerBattleScenes.Where((SingleplayerBattleSceneData scene) => scene.IsNaval == isNavalEncounter).ToMBList();
				if (mBList.IsEmpty())
				{
					Debug.FailedAssert("naval battles scene mismatch. Picking a random scene", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSceneModel.cs", "GetBattleSceneForMapPatch", 40);
					mBList = GameSceneDataManager.Instance.SingleplayerBattleScenes.ToMBList();
				}
			}
			return mBList.GetRandomElement().SceneID;
		}
		if (mBList.Count > 1)
		{
			if (isNavalEncounter)
			{
				Campaign.Current.MapSceneWrapper.GetEnvironmentTerrainTypesCount(MobileParty.MainParty.Position, out var currentPositionTerrainType2);
				List<SingleplayerBattleSceneData> list = mBList.Where((SingleplayerBattleSceneData scene) => scene.Terrain == currentPositionTerrainType2).ToList();
				if (!list.IsEmpty())
				{
					return list.GetRandomElement().SceneID;
				}
				return mBList.GetRandomElement().SceneID;
			}
			Debug.FailedAssert("Multiple battle scenes for map patch with scene index " + mapPatch.sceneIndex + " are defined. Picking a matching scene randomly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSceneModel.cs", "GetBattleSceneForMapPatch", 67);
			return mBList.GetRandomElement().SceneID;
		}
		return mBList[0].SceneID;
	}

	public override string GetConversationSceneForMapPosition(CampaignVec2 campaignPosition)
	{
		TerrainType currentPositionTerrainType;
		List<TerrainType> environmentTerrainTypesCount = Campaign.Current.MapSceneWrapper.GetEnvironmentTerrainTypesCount(in campaignPosition, out currentPositionTerrainType);
		TerrainType terrain = GetTerrainByCount(environmentTerrainTypesCount, currentPositionTerrainType);
		return (GameSceneDataManager.Instance.ConversationScenes.Any((ConversationSceneData scene) => scene.Terrain == terrain) ? GameSceneDataManager.Instance.ConversationScenes.GetRandomElementWithPredicate((ConversationSceneData scene) => scene.Terrain == terrain) : GameSceneDataManager.Instance.ConversationScenes.GetRandomElement()).SceneID;
	}

	private static TerrainType GetTerrainByCount(List<TerrainType> terrainTypeSamples, TerrainType currentPositionTerrainType)
	{
		for (int i = 0; i < terrainTypeSamples.Count; i++)
		{
			if (terrainTypeSamples[i] == TerrainType.Snow)
			{
				terrainTypeSamples[i] = TerrainType.Plain;
			}
		}
		if (_conversationTerrains.Contains(currentPositionTerrainType))
		{
			int num = (int)((float)terrainTypeSamples.Count * 0.33f);
			for (int j = 0; j < num; j++)
			{
				terrainTypeSamples.Add(currentPositionTerrainType);
			}
		}
		Dictionary<TerrainType, int> dictionary = new Dictionary<TerrainType, int>();
		foreach (TerrainType terrainTypeSample in terrainTypeSamples)
		{
			if (_conversationTerrains.Contains(terrainTypeSample))
			{
				if (!dictionary.ContainsKey(terrainTypeSample))
				{
					dictionary.Add(terrainTypeSample, 1);
				}
				else
				{
					dictionary[terrainTypeSample]++;
				}
			}
		}
		if (dictionary.Count > 0)
		{
			return dictionary.OrderByDescending((KeyValuePair<TerrainType, int> t) => t.Value).First().Key;
		}
		return TerrainType.Plain;
	}
}
