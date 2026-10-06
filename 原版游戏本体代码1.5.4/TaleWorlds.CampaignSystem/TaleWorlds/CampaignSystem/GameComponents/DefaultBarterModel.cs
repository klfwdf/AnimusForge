using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBarterModel : BarterModel
{
	public override int BarterCooldownWithHeroInDays => 3;

	private int MaximumOverpayRelationBonus => 3;

	public override float MaximumPercentageOfNpcGoldToSpendAtBarter => 0.25f;

	public override int CalculateOverpayRelationIncreaseCosts(Hero hero, float overpayAmount)
	{
		int num = (int)hero.GetRelationWithPlayer();
		float num2 = MathF.Clamp(num + MaximumOverpayRelationBonus, -100f, 100f);
		float num3 = 0f;
		for (int i = num; (float)i < num2; i++)
		{
			int num4 = 1000 + 100 * (i * i);
			if (overpayAmount >= (float)num4)
			{
				overpayAmount -= (float)num4;
				num3 += 1f;
				continue;
			}
			if (MBRandom.RandomFloat <= overpayAmount / (float)num4)
			{
				num3 += 1f;
			}
			break;
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.Tribute))
		{
			num3 *= 1f + DefaultPerks.Charm.Tribute.PrimaryBonus;
		}
		return MathF.Ceiling(num3);
	}

	public override ExplainedNumber GetBarterPenalty(IFaction faction, ItemBarterable itemBarterable, Hero otherHero, PartyBase otherParty)
	{
		ExplainedNumber result;
		if (faction == otherHero?.Clan || faction == otherHero?.MapFaction || faction == otherParty?.MapFaction)
		{
			result = new ExplainedNumber(0.4f);
			if (otherHero != null && itemBarterable.OriginalOwner != null && otherHero != itemBarterable.OriginalOwner && otherHero.MapFaction != null && otherHero.IsPartyLeader)
			{
				if (otherHero.Culture == itemBarterable.OriginalOwner?.Culture)
				{
					if (itemBarterable.OriginalOwner.GetPerkValue(DefaultPerks.Charm.EffortForThePeople))
					{
						result.AddFactor(0f - DefaultPerks.Charm.EffortForThePeople.SecondaryBonus);
					}
				}
				else if (itemBarterable.OriginalOwner.GetPerkValue(DefaultPerks.Charm.SlickNegotiator))
				{
					result.AddFactor(0f - DefaultPerks.Charm.SlickNegotiator.SecondaryBonus);
				}
				if (itemBarterable.OriginalOwner.GetPerkValue(DefaultPerks.Trade.SelfMadeMan))
				{
					result.AddFactor(0f - DefaultPerks.Trade.SelfMadeMan.PrimaryBonus);
				}
			}
		}
		else if (faction == itemBarterable.OriginalOwner?.Clan || faction == itemBarterable.OriginalOwner?.MapFaction || faction == itemBarterable.OriginalParty?.MapFaction)
		{
			result = ((itemBarterable.ItemRosterElement.EquipmentElement.Item.IsAnimal || itemBarterable.ItemRosterElement.EquipmentElement.Item.IsMountable) ? new ExplainedNumber(-8.4f) : ((!itemBarterable.ItemRosterElement.EquipmentElement.Item.IsFood) ? new ExplainedNumber(-2.1f) : new ExplainedNumber(-12.6f)));
			if (otherHero != null && otherHero != itemBarterable.OriginalOwner && otherHero.MapFaction != null && otherHero.IsPartyLeader)
			{
				if (otherHero.Culture == itemBarterable.OriginalOwner?.Culture)
				{
					if (otherHero.GetPerkValue(DefaultPerks.Charm.EffortForThePeople))
					{
						result.AddFactor(DefaultPerks.Charm.EffortForThePeople.SecondaryBonus);
					}
				}
				else if (otherHero.GetPerkValue(DefaultPerks.Charm.SlickNegotiator))
				{
					result.AddFactor(DefaultPerks.Charm.SlickNegotiator.SecondaryBonus);
				}
				if (otherHero.GetPerkValue(DefaultPerks.Trade.SelfMadeMan))
				{
					result.AddFactor(DefaultPerks.Trade.SelfMadeMan.PrimaryBonus);
				}
			}
		}
		else
		{
			result = new ExplainedNumber(0f, includeDescriptions: false, null);
		}
		return result;
	}
}
