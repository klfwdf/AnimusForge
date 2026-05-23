using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public interface IMissionSystemHandler
{
	void OnMissionAfterStarting(Mission mission);

	void OnMissionLoadingFinished(Mission mission);

	void BeforeMissionTick(Mission mission, float realDt);

	void AfterMissionTick(Mission mission, float realDt);

	void UpdateCamera(Mission mission, float realDt);

	bool RenderIsReady();

	IEnumerable<MissionBehavior> OnAddBehaviors(IEnumerable<MissionBehavior> behaviors, Mission mission, string missionName, bool addDefaultMissionBehaviors);
}
