using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Towns
{
	// Token: 0x0200008F RID: 143
	public class TownCenterMissionController : MissionLogic
	{
		// Token: 0x0600059F RID: 1439 RVA: 0x000252E0 File Offset: 0x000234E0
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = true;
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x000252F4 File Offset: 0x000234F4
		public override void AfterStart()
		{
			bool isNight = Campaign.Current.IsNight;
			base.Mission.SetMissionMode(MissionMode.StartUp, true);
			base.Mission.IsInventoryAccessible = !Campaign.Current.IsMainHeroDisguised;
			base.Mission.IsQuestScreenAccessible = true;
			MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
			SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.DoesMissionRequireCivilianEquipment, true, false, false, "");
			missionBehavior.SpawnLocationCharacters(null);
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
}
