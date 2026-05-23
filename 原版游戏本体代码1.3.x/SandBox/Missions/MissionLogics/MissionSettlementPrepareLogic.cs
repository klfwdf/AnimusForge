using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200007C RID: 124
	public class MissionSettlementPrepareLogic : MissionLogic
	{
		// Token: 0x06000519 RID: 1305 RVA: 0x000224CA File Offset: 0x000206CA
		public override void AfterStart()
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign && Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsTown || Settlement.CurrentSettlement.IsCastle))
			{
				this.OpenGates();
			}
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x00022500 File Offset: 0x00020700
		private void OpenGates()
		{
			foreach (CastleGate castleGate in Mission.Current.ActiveMissionObjects.FindAllWithType<CastleGate>().ToList<CastleGate>())
			{
				castleGate.OpenDoorAndDisableGateForCivilianMission();
			}
		}
	}
}
