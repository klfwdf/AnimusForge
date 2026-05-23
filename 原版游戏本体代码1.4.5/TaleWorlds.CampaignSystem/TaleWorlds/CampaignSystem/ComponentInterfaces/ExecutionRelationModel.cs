using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ExecutionRelationModel : MBGameModel<ExecutionRelationModel>
{
	public abstract int HeroKillingHeroClanRelationPenalty { get; }

	public abstract int HeroKillingHeroFriendRelationPenalty { get; }

	public abstract int PlayerExecutingHeroFactionRelationPenaltyDishonorable { get; }

	public abstract int PlayerExecutingHeroClanRelationPenaltyDishonorable { get; }

	public abstract int PlayerExecutingHeroFriendRelationPenaltyDishonorable { get; }

	public abstract int PlayerExecutingHeroHonorPenalty { get; }

	public abstract int PlayerExecutingHeroFactionRelationPenalty { get; }

	public abstract int PlayerExecutingHeroHonorableNobleRelationPenalty { get; }

	public abstract int PlayerExecutingHeroClanRelationPenalty { get; }

	public abstract int PlayerExecutingHeroFriendRelationPenalty { get; }

	public abstract int GetRelationChangeForExecutingHero(Hero victim, Hero hero, out bool showQuickNotification);
}
