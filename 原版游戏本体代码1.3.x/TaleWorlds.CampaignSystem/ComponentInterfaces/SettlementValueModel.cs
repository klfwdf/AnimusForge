using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C7 RID: 455
	public abstract class SettlementValueModel : MBGameModel<SettlementValueModel>
	{
		// Token: 0x06001DBD RID: 7613
		public abstract Settlement FindMostSuitableHomeSettlement(Clan clan);

		// Token: 0x06001DBE RID: 7614
		public abstract float CalculateSettlementValueForFaction(Settlement settlement, IFaction faction);

		// Token: 0x06001DBF RID: 7615
		public abstract float CalculateSettlementBaseValue(Settlement settlement);

		// Token: 0x06001DC0 RID: 7616
		public abstract float CalculateSettlementValueForEnemyHero(Settlement settlement, Hero hero);
	}
}
