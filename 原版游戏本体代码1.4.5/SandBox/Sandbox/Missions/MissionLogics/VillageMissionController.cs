using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class VillageMissionController : MissionLogic
{
	public override void OnCreated()
	{
		base.OnCreated();
		base.Mission.DoesMissionRequireCivilianEquipment = false;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		bool isNight = Campaign.Current.IsNight;
		base.Mission.IsInventoryAccessible = true;
		base.Mission.IsQuestScreenAccessible = true;
		MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.DoesMissionRequireCivilianEquipment);
		missionBehavior.SpawnLocationCharacters();
		SandBoxHelpers.MissionHelper.SpawnHorses();
		if (!isNight)
		{
			SandBoxHelpers.MissionHelper.SpawnSheeps();
			SandBoxHelpers.MissionHelper.SpawnCows();
			SandBoxHelpers.MissionHelper.SpawnHogs();
			SandBoxHelpers.MissionHelper.SpawnGeese();
			SandBoxHelpers.MissionHelper.SpawnChicken();
		}
	}
}
