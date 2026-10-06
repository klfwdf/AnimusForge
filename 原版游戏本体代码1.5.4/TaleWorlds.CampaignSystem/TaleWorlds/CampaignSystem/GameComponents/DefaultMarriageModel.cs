using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMarriageModel : MarriageModel
{
	private const float BaseMarriageChanceForNpcs = 0.002f;

	public override int MinimumMarriageAgeMale => 18;

	public override int MinimumMarriageAgeFemale => 18;

	public override bool IsCoupleSuitableForMarriage(Hero firstHero, Hero secondHero)
	{
		if (!IsClanSuitableForMarriage(firstHero.Clan) || !IsClanSuitableForMarriage(secondHero.Clan) || (firstHero.Clan?.Leader == firstHero && secondHero.Clan?.Leader == secondHero) || firstHero.IsFemale == secondHero.IsFemale || AreHeroesRelated(firstHero, secondHero, 3))
		{
			return false;
		}
		Hero courtedHeroInOtherClan = Romance.GetCourtedHeroInOtherClan(firstHero, secondHero);
		if (courtedHeroInOtherClan != null && courtedHeroInOtherClan != secondHero)
		{
			return false;
		}
		Hero courtedHeroInOtherClan2 = Romance.GetCourtedHeroInOtherClan(secondHero, firstHero);
		if (courtedHeroInOtherClan2 != null && courtedHeroInOtherClan2 != firstHero)
		{
			return false;
		}
		if (firstHero.CanMarry())
		{
			return secondHero.CanMarry();
		}
		return false;
	}

	public override bool IsClanSuitableForMarriage(Clan clan)
	{
		if (clan != null && !clan.IsBanditFaction && !clan.IsRebelClan)
		{
			return !clan.IsEliminated;
		}
		return false;
	}

	public override float NpcCoupleMarriageChance(Hero firstHero, Hero secondHero)
	{
		if (IsCoupleSuitableForMarriage(firstHero, secondHero))
		{
			float num = 0.002f;
			num *= 1f + (firstHero.Age - (float)Campaign.Current.Models.AgeModel.HeroComesOfAge) / 50f;
			num *= 1f + (secondHero.Age - (float)Campaign.Current.Models.AgeModel.HeroComesOfAge) / 50f;
			num *= 1f - MathF.Abs(secondHero.Age - firstHero.Age) / 50f;
			if (firstHero.Clan.Kingdom != secondHero.Clan.Kingdom)
			{
				num *= 0.5f;
			}
			float num2 = 0.5f + (float)firstHero.Clan.GetRelationWithClan(secondHero.Clan) / 200f;
			return num * num2;
		}
		return 0f;
	}

	public override bool ShouldNpcMarriageBetweenClansBeAllowed(Clan consideringClan, Clan targetClan)
	{
		if (targetClan != consideringClan && !consideringClan.IsAtWarWith(targetClan))
		{
			return consideringClan.GetRelationWithClan(targetClan) >= -50;
		}
		return false;
	}

	public override List<Hero> GetAdultChildrenSuitableForMarriage(Hero hero)
	{
		List<Hero> list = new List<Hero>();
		foreach (Hero child in hero.Children)
		{
			if (child.CanMarry())
			{
				list.Add(child);
			}
		}
		return list;
	}

	private bool AreHeroesRelatedAux1(Hero firstHero, Hero secondHero, int ancestorDepth)
	{
		if (firstHero != secondHero)
		{
			if (ancestorDepth > 0)
			{
				if (secondHero.Mother == null || !AreHeroesRelatedAux1(firstHero, secondHero.Mother, ancestorDepth - 1))
				{
					if (secondHero.Father != null)
					{
						return AreHeroesRelatedAux1(firstHero, secondHero.Father, ancestorDepth - 1);
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private bool AreHeroesRelatedAux2(Hero firstHero, Hero secondHero, int ancestorDepth, int secondAncestorDepth)
	{
		if (!AreHeroesRelatedAux1(firstHero, secondHero, secondAncestorDepth))
		{
			if (ancestorDepth > 0)
			{
				if (firstHero.Mother == null || !AreHeroesRelatedAux2(firstHero.Mother, secondHero, ancestorDepth - 1, secondAncestorDepth))
				{
					if (firstHero.Father != null)
					{
						return AreHeroesRelatedAux2(firstHero.Father, secondHero, ancestorDepth - 1, secondAncestorDepth);
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private bool AreHeroesRelated(Hero firstHero, Hero secondHero, int ancestorDepth)
	{
		return AreHeroesRelatedAux2(firstHero, secondHero, ancestorDepth, ancestorDepth);
	}

	public override int GetEffectiveRelationIncrease(Hero firstHero, Hero secondHero)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(20f);
		SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.CharmRelationBonus, firstHero.IsFemale ? secondHero.CharacterObject : firstHero.CharacterObject, ref explainedNumber);
		return MathF.Round(explainedNumber.ResultNumber);
	}

	public override bool IsSuitableForMarriage(Hero maidenOrSuitor)
	{
		if (maidenOrSuitor.IsActive && maidenOrSuitor.Spouse == null && maidenOrSuitor.IsLord && !maidenOrSuitor.IsMinorFactionHero && !maidenOrSuitor.IsNotable && !maidenOrSuitor.IsTemplate && maidenOrSuitor.PartyBelongedTo?.MapEvent == null && maidenOrSuitor.PartyBelongedTo?.Army == null)
		{
			IMarriageOfferCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IMarriageOfferCampaignBehavior>();
			if (campaignBehavior != null && campaignBehavior.IsHeroEngaged(maidenOrSuitor))
			{
				return false;
			}
			if (maidenOrSuitor.IsFemale)
			{
				return maidenOrSuitor.CharacterObject.Age >= (float)MinimumMarriageAgeFemale;
			}
			return maidenOrSuitor.CharacterObject.Age >= (float)MinimumMarriageAgeMale;
		}
		return false;
	}

	public override Clan GetClanAfterMarriage(Hero firstHero, Hero secondHero)
	{
		if (firstHero.IsHumanPlayerCharacter)
		{
			return firstHero.Clan;
		}
		if (secondHero.IsHumanPlayerCharacter)
		{
			return secondHero.Clan;
		}
		if (firstHero.Clan.Leader == firstHero)
		{
			return firstHero.Clan;
		}
		if (secondHero.Clan.Leader == secondHero)
		{
			return secondHero.Clan;
		}
		if (!firstHero.IsFemale)
		{
			return firstHero.Clan;
		}
		return secondHero.Clan;
	}
}
