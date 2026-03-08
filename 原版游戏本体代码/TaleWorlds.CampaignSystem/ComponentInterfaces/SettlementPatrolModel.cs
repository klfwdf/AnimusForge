using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000185 RID: 389
	public abstract class SettlementPatrolModel : MBGameModel<SettlementPatrolModel>
	{
		// Token: 0x06001BA4 RID: 7076
		public abstract CampaignTime GetPatrolPartySpawnDuration(Settlement settlement, bool naval);

		// Token: 0x06001BA5 RID: 7077
		public abstract bool CanSettlementHavePatrolParties(Settlement settlement, bool naval);

		// Token: 0x06001BA6 RID: 7078
		public abstract PartyTemplateObject GetPartyTemplateForPatrolParty(Settlement settlement, bool naval);
	}
}
