using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200006C RID: 108
	public class HouseMissionController : MissionLogic
	{
		// Token: 0x06000466 RID: 1126 RVA: 0x0001A6A6 File Offset: 0x000188A6
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x0001A6BF File Offset: 0x000188BF
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = true;
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x0001A6D3 File Offset: 0x000188D3
		public override void EarlyStart()
		{
		}

		// Token: 0x06000469 RID: 1129 RVA: 0x0001A6D8 File Offset: 0x000188D8
		public override void AfterStart()
		{
			base.AfterStart();
			base.Mission.SetMissionMode(MissionMode.StartUp, true);
			base.Mission.IsInventoryAccessible = !Campaign.Current.IsMainHeroDisguised;
			base.Mission.IsQuestScreenAccessible = true;
			SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.DoesMissionRequireCivilianEquipment, true, true, false, "");
			this._missionAgentHandler.SpawnLocationCharacters(null);
		}

		// Token: 0x0400025C RID: 604
		private MissionAgentHandler _missionAgentHandler;
	}
}
