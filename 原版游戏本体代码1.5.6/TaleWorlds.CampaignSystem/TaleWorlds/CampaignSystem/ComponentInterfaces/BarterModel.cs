using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BarterModel : MBGameModel<BarterModel>
{
	public abstract int BarterCooldownWithHeroInDays { get; }

	public abstract float MaximumPercentageOfNpcGoldToSpendAtBarter { get; }

	public abstract int CalculateOverpayRelationIncreaseCosts(Hero hero, float overpayAmount);

	public abstract ExplainedNumber GetBarterPenalty(IFaction faction, ItemBarterable itemBarterable, Hero otherHero, PartyBase otherParty);
}
