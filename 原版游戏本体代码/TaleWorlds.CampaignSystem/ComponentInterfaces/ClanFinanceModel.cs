using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D2 RID: 466
	public abstract class ClanFinanceModel : MBGameModel<ClanFinanceModel>
	{
		// Token: 0x17000788 RID: 1928
		// (get) Token: 0x06001E26 RID: 7718
		public abstract int PartyGoldLowerThreshold { get; }

		// Token: 0x06001E27 RID: 7719
		public abstract ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false);

		// Token: 0x06001E28 RID: 7720
		public abstract ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false);

		// Token: 0x06001E29 RID: 7721
		public abstract ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false);

		// Token: 0x06001E2A RID: 7722
		public abstract ExplainedNumber CalculateTownIncomeFromTariffs(Clan clan, Town town, bool applyWithdrawals = false);

		// Token: 0x06001E2B RID: 7723
		public abstract int CalculateTownIncomeFromProjects(Town town);

		// Token: 0x06001E2C RID: 7724
		public abstract int CalculateNotableDailyGoldChange(Hero hero, bool applyWithdrawals);

		// Token: 0x06001E2D RID: 7725
		public abstract int CalculateVillageIncome(Clan clan, Village village, bool applyWithdrawals = false);

		// Token: 0x06001E2E RID: 7726
		public abstract int CalculateOwnerIncomeFromCaravan(MobileParty caravan);

		// Token: 0x06001E2F RID: 7727
		public abstract int CalculateOwnerIncomeFromWorkshop(Workshop workshop);

		// Token: 0x06001E30 RID: 7728
		public abstract float RevenueSmoothenFraction();
	}
}
