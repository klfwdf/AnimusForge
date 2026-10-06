using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public struct ConversationSceneData
{
	public string SceneID { get; private set; }

	public TerrainType Terrain { get; private set; }

	public List<TerrainType> TerrainTypes { get; private set; }

	public ForestDensity ForestDensity { get; private set; }

	public ConversationSceneData(string sceneID, TerrainType terrain, List<TerrainType> terrainTypes, ForestDensity forestDensity)
	{
		SceneID = sceneID;
		Terrain = terrain;
		TerrainTypes = terrainTypes;
		ForestDensity = forestDensity;
	}
}
