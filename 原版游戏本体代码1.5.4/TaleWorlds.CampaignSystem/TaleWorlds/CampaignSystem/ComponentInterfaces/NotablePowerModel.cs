using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class NotablePowerModel : MBGameModel<NotablePowerModel>
{
	public abstract int RegularNotableMaxPowerLevel { get; }

	public abstract int NotableDisappearPowerLimit { get; }

	public abstract ExplainedNumber CalculateDailyPowerChangeForHero(Hero hero, bool includeDescriptions = false);

	public abstract TextObject GetPowerRankName(Hero hero);

	public abstract float GetInfluenceBonusToClan(Hero hero);

	public abstract int GetInitialPower(Hero hero);

	public abstract int GetInitialNotableSupporterCost(Hero hero);
}
