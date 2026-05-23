using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SettlementGarrisonModel : MBGameModel<SettlementGarrisonModel>
{
	public abstract int GetMaximumDailyAutoRecruitmentCount(Town town);

	public abstract ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false);

	public abstract int FindNumberOfTroopsToTakeFromGarrison(MobileParty mobileParty, Settlement settlement, float idealGarrisonStrengthPerWalledCenter = 0f);

	public abstract int FindNumberOfTroopsToLeaveToGarrison(MobileParty mobileParty, Settlement settlement);

	public abstract float GetMaximumDailyRepairAmount(Settlement settlement);
}
