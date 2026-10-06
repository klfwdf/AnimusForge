using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class MissionCustomBattlePreloadView : MissionView
{
	private PreloadHelper _helperInstance = new PreloadHelper();

	private bool _preloadDone;

	public override void OnPreMissionTick(float dt)
	{
		if (_preloadDone)
		{
			return;
		}
		MissionCombatantsLogic missionBehavior = base.Mission.GetMissionBehavior<MissionCombatantsLogic>();
		List<BasicCharacterObject> list = new List<BasicCharacterObject>();
		foreach (IBattleCombatant allCombatant in missionBehavior.GetAllCombatants())
		{
			list.AddRange(((CustomBattleCombatant)allCombatant).Characters);
		}
		_helperInstance.PreloadCharacters(list);
		SiegeDeploymentMissionController missionBehavior2 = Mission.Current.GetMissionBehavior<SiegeDeploymentMissionController>();
		if (missionBehavior2 != null)
		{
			_helperInstance.PreloadItems(missionBehavior2.GetSiegeMissiles());
		}
		_preloadDone = true;
	}

	public override void OnSceneRenderingStarted()
	{
		_helperInstance.WaitForMeshesToBeLoaded();
	}

	public override void OnMissionStateDeactivated()
	{
		base.OnMissionStateDeactivated();
		_helperInstance.Clear();
	}
}
