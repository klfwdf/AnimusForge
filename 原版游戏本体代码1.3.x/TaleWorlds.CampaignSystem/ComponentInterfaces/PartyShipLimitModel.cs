using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001BA RID: 442
	public abstract class PartyShipLimitModel : MBGameModel<PartyShipLimitModel>
	{
		// Token: 0x06001D74 RID: 7540
		public abstract int GetIdealShipNumber(MobileParty mobileParty);

		// Token: 0x06001D75 RID: 7541
		public abstract int GetIdealShipNumber(Clan clan);

		// Token: 0x06001D76 RID: 7542
		public abstract float GetShipPriority(MobileParty mobileParty, Ship ship, bool isSelling);
	}
}
