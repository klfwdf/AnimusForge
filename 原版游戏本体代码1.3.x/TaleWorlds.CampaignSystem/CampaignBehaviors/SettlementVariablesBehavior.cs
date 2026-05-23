using System;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200043F RID: 1087
	public class SettlementVariablesBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600453C RID: 17724 RVA: 0x001569CC File Offset: 0x00154BCC
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.HourlyTickSettlement));
		}

		// Token: 0x0600453D RID: 17725 RVA: 0x001569E5 File Offset: 0x00154BE5
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600453E RID: 17726 RVA: 0x001569E8 File Offset: 0x00154BE8
		private void HourlyTickSettlement(Settlement settlement)
		{
			if (settlement.LastAttackerParty != null && settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null && settlement.LastThreatTime.ElapsedDaysUntilNow > this._resetLastAttackerPartyAsDays)
			{
				settlement.LastAttackerParty = null;
			}
		}

		// Token: 0x0400136D RID: 4973
		private float _resetLastAttackerPartyAsDays = 1f;
	}
}
