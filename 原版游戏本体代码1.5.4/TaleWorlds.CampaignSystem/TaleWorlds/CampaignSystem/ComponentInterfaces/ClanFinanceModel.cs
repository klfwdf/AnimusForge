using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ClanFinanceModel : MBGameModel<ClanFinanceModel>
{
	public abstract int PartyGoldLowerThreshold { get; }

	public abstract ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false);

	public abstract ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false);

	public abstract ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false);

	public abstract ExplainedNumber CalculateTownIncomeFromTariffs(Clan clan, Town town, bool applyWithdrawals = false);

	public abstract int CalculateTownIncomeFromProjects(Town town);

	public abstract int CalculateNotableDailyGoldChange(Hero hero, bool applyWithdrawals);

	public abstract int CalculateVillageIncome(Clan clan, Village village, bool applyWithdrawals = false);

	public abstract int CalculateOwnerIncomeFromCaravan(MobileParty caravan);

	public abstract int CalculateOwnerIncomeFromWorkshop(Workshop workshop);

	public abstract float RevenueSmoothenFraction();
}
