using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000FC RID: 252
	public class DefaultCampaignShipDamageModel : CampaignShipDamageModel
	{
		// Token: 0x06001682 RID: 5762 RVA: 0x00067433 File Offset: 0x00065633
		public override int GetHourlyShipDamage(MobileParty owner, Ship ship)
		{
			return 0;
		}

		// Token: 0x06001683 RID: 5763 RVA: 0x00067436 File Offset: 0x00065636
		public override float GetEstimatedSafeSailDuration(MobileParty mobileParty)
		{
			return 0f;
		}

		// Token: 0x06001684 RID: 5764 RVA: 0x0006743D File Offset: 0x0006563D
		public override float GetShipDamage(Ship ship, Ship rammingShip, float rawDamage)
		{
			return rawDamage;
		}
	}
}
