using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC;

public class NavalMissionState : MissionState
{
	public new static Mission OpenNew(string missionName, MissionInitializerRecord rec, InitializeMissionBehaviorsDelegate handler, bool addDefaultMissionBehaviors = true, bool needsMemoryCleanup = true)
	{
		Debug.Print("Opening new mission " + missionName + " " + rec.SceneLevels + ".\n");
		if (!GameNetwork.IsClientOrReplay && !GameNetwork.IsServer)
		{
			MBCommon.CurrentGameType = (MissionState.IsRecordingActive() ? MBCommon.GameType.SingleRecord : MBCommon.GameType.Single);
		}
		Game.Current.OnMissionIsStarting(missionName, rec);
		NavalMissionState navalMissionState = Game.Current.GameStateManager.CreateState<NavalMissionState>();
		Mission mission = navalMissionState.HandleOpenNew(missionName, rec, handler, addDefaultMissionBehaviors, needsMemoryCleanup);
		mission.SetCloseProximityWaveSoundsEnabled(value: true);
		mission.ForceDisableOcclusion(value: true);
		Game.Current.GameStateManager.PushState(navalMissionState);
		return mission;
	}
}
