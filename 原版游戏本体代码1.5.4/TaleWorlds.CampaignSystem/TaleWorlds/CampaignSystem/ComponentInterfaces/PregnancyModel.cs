using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PregnancyModel : MBGameModel<PregnancyModel>
{
	public abstract float PregnancyDurationInDays { get; }

	public abstract float MaternalMortalityProbabilityInLabor { get; }

	public abstract float StillbirthProbability { get; }

	public abstract float DeliveringFemaleOffspringProbability { get; }

	public abstract float DeliveringTwinsProbability { get; }

	public abstract float GetDailyChanceOfPregnancyForHero(Hero hero);
}
