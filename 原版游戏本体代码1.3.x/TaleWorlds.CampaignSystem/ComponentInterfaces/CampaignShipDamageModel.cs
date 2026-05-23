using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200018D RID: 397
	public abstract class CampaignShipDamageModel : MBGameModel<CampaignShipDamageModel>
	{
		// Token: 0x06001BED RID: 7149
		public abstract int GetHourlyShipDamage(MobileParty owner, Ship ship);

		// Token: 0x06001BEE RID: 7150
		public abstract float GetEstimatedSafeSailDuration(MobileParty mobileParty);

		// Token: 0x06001BEF RID: 7151
		public abstract float GetShipDamage(Ship ship, Ship rammingShip, float rawDamage);
	}
}
