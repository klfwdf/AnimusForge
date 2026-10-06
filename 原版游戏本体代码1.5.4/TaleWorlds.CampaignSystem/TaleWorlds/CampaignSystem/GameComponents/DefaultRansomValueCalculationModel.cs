using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultRansomValueCalculationModel : RansomValueCalculationModel
{
	public override int PrisonerRansomValue(CharacterObject prisoner, Hero sellerHero = null)
	{
		int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(prisoner, null).RoundedResultNumber;
		float num = 0f;
		float num2 = 0f;
		float num3 = 1f;
		if (prisoner.HeroObject?.Clan != null)
		{
			num = (float)(prisoner.HeroObject.Clan.Tier + 2) * 200f * ((!prisoner.HeroObject.IsClanLeader) ? 1f : (prisoner.HeroObject.IsKingdomLeader ? 6f : 2.5f));
			num2 = MathF.Sqrt(MathF.Max(0, prisoner.HeroObject.Gold)) * 6f;
			if (prisoner.HeroObject.Clan.Kingdom != null)
			{
				int count = prisoner.HeroObject.Clan.Kingdom.Fiefs.Count;
				num3 = ((!prisoner.HeroObject.MapFaction.IsKingdomFaction) ? 1f : ((count < 8) ? (((float)count + 1f) / 9f) : (1f + MathF.Sqrt(count - 8) * 0.1f)));
			}
			else
			{
				num3 = 0.5f;
			}
		}
		float num4 = ((prisoner.HeroObject != null) ? (num + num2) : 0f);
		ExplainedNumber stat = new ExplainedNumber(((float)roundedResultNumber + num4) * ((!prisoner.IsHero) ? 0.25f : 1f) * num3);
		if (sellerHero != null)
		{
			if (!prisoner.IsHero)
			{
				if (sellerHero.GetPerkValue(DefaultPerks.Roguery.Manhunter) && sellerHero.PartyBelongedTo != null && !sellerHero.PartyBelongedTo.IsCurrentlyAtSea)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.Manhunter, BattleEnvironment.Any, sellerHero.CharacterObject, isPrimaryBonus: true, ref stat);
				}
				TraitEffectHelper.ApplyTraitEffect(sellerHero, DefaultPersonalityTraitEffects.MercyTroopRansomEffect, ref stat);
			}
			else
			{
				if (sellerHero.IsPartyLeader && sellerHero.GetPerkValue(DefaultPerks.Roguery.RansomBroker))
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.RansomBroker, sellerHero.PartyBelongedTo, isPrimaryBonus: true, ref stat);
				}
				Hero hero = sellerHero.Clan?.Leader;
				if (hero != null)
				{
					TraitEffectHelper.ApplyTraitEffect(hero, DefaultPersonalityTraitEffects.MercyLordRansomEffect, ref stat);
				}
			}
		}
		stat.LimitMin(1f);
		return stat.RoundedResultNumber;
	}
}
