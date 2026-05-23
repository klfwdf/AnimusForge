using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class DailyTroopXpBonusModel : MBGameModel<DailyTroopXpBonusModel>
{
	public abstract int CalculateDailyTroopXpBonus(Town town);

	public abstract float CalculateGarrisonXpBonusMultiplier(Town town);
}
