using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.CustomBattle;

public struct NavalCustomBattleSceneData
{
	public string SceneID { get; private set; }

	public TextObject Name { get; private set; }

	public TerrainType Terrain { get; private set; }

	public string ForcedSceneLevel { get; private set; }

	public NavalCustomBattleSceneData(string sceneID, TextObject name, TerrainType terrain, string forcedSceneLevel)
	{
		SceneID = sceneID;
		Name = name;
		Terrain = terrain;
		ForcedSceneLevel = forcedSceneLevel;
	}
}
