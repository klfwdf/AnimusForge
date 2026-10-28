using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace TaleWorlds.MountAndBlade.View;

public class SimpleSceneTestWithMission
{
	private Mission _mission;

	private string _sceneName;

	private DecalAtlasGroup _customDecalGroup;

	public SimpleSceneTestWithMission(string sceneName, DecalAtlasGroup atlasGroup = DecalAtlasGroup.All)
	{
		_sceneName = sceneName;
		_customDecalGroup = atlasGroup;
		_mission = OpenSceneWithMission(_sceneName);
	}

	public bool LoadingFinished()
	{
		if (_mission.IsLoadingFinished)
		{
			return Utilities.GetNumberOfShaderCompilationsInProgress() == 0;
		}
		return false;
	}

	private Mission OpenSceneWithMission(string scene, string sceneLevels = "")
	{
		LoadingWindow.DisableGlobalLoadingWindow();
		return MissionState.OpenNew("SimpleSceneTestWithMission", new MissionInitializerRecord(scene)
		{
			PlayingInCampaignMode = false,
			AtmosphereOnCampaign = AtmosphereInfo.GetInvalidAtmosphereInfo(),
			DecalAtlasGroup = (int)_customDecalGroup,
			SceneLevels = sceneLevels
		}, (Mission missionController) => new MissionBehavior[6]
		{
			new MissionOptionsComponent(),
			new BasicLeaveMissionLogic(askBeforeLeave: false, 0),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new EquipmentControllerLeaveLogic()
		});
	}
}
