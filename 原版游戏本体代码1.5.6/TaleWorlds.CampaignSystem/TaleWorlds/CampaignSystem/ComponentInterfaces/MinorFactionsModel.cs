using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MinorFactionsModel : MBGameModel<MinorFactionsModel>
{
	public abstract float DailyMinorFactionHeroSpawnChance { get; }

	public abstract int MinorFactionHeroLimit { get; }

	public abstract int GetMercenaryAwardFactorToJoinKingdom(Clan mercenaryClan, Kingdom kingdom, bool neededAmountForClanToJoinCalculation = false);
}
