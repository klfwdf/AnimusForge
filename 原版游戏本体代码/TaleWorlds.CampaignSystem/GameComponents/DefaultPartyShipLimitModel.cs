using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000135 RID: 309
	public class DefaultPartyShipLimitModel : PartyShipLimitModel
	{
		// Token: 0x0600190B RID: 6411 RVA: 0x0007B42A File Offset: 0x0007962A
		public override int GetIdealShipNumber(MobileParty mobileParty)
		{
			return 0;
		}

		// Token: 0x0600190C RID: 6412 RVA: 0x0007B42D File Offset: 0x0007962D
		public override int GetIdealShipNumber(Clan clan)
		{
			return 0;
		}

		// Token: 0x0600190D RID: 6413 RVA: 0x0007B430 File Offset: 0x00079630
		public override float GetShipPriority(MobileParty mobileParty, Ship ship, bool isSelling)
		{
			return 0f;
		}
	}
}
