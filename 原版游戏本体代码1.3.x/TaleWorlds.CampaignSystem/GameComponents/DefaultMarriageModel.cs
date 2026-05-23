using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000129 RID: 297
	public class DefaultMarriageModel : MarriageModel
	{
		// Token: 0x17000677 RID: 1655
		// (get) Token: 0x06001896 RID: 6294 RVA: 0x00076990 File Offset: 0x00074B90
		public override int MinimumMarriageAgeMale
		{
			get
			{
				return 18;
			}
		}

		// Token: 0x17000678 RID: 1656
		// (get) Token: 0x06001897 RID: 6295 RVA: 0x00076994 File Offset: 0x00074B94
		public override int MinimumMarriageAgeFemale
		{
			get
			{
				return 18;
			}
		}

		// Token: 0x06001898 RID: 6296 RVA: 0x00076998 File Offset: 0x00074B98
		public override bool IsCoupleSuitableForMarriage(Hero firstHero, Hero secondHero)
		{
			if (this.IsClanSuitableForMarriage(firstHero.Clan) && this.IsClanSuitableForMarriage(secondHero.Clan))
			{
				Clan clan = firstHero.Clan;
				if (((clan != null) ? clan.Leader : null) == firstHero)
				{
					Clan clan2 = secondHero.Clan;
					if (((clan2 != null) ? clan2.Leader : null) == secondHero)
					{
						return false;
					}
				}
				if (firstHero.IsFemale != secondHero.IsFemale && !this.AreHeroesRelated(firstHero, secondHero, 3))
				{
					Hero courtedHeroInOtherClan = Romance.GetCourtedHeroInOtherClan(firstHero, secondHero);
					if (courtedHeroInOtherClan != null && courtedHeroInOtherClan != secondHero)
					{
						return false;
					}
					Hero courtedHeroInOtherClan2 = Romance.GetCourtedHeroInOtherClan(secondHero, firstHero);
					return (courtedHeroInOtherClan2 == null || courtedHeroInOtherClan2 == firstHero) && firstHero.CanMarry() && secondHero.CanMarry();
				}
			}
			return false;
		}

		// Token: 0x06001899 RID: 6297 RVA: 0x00076A38 File Offset: 0x00074C38
		public override bool IsClanSuitableForMarriage(Clan clan)
		{
			return clan != null && !clan.IsBanditFaction && !clan.IsRebelClan && !clan.IsEliminated;
		}

		// Token: 0x0600189A RID: 6298 RVA: 0x00076A58 File Offset: 0x00074C58
		public override float NpcCoupleMarriageChance(Hero firstHero, Hero secondHero)
		{
			if (this.IsCoupleSuitableForMarriage(firstHero, secondHero))
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

		// Token: 0x0600189B RID: 6299 RVA: 0x00076B37 File Offset: 0x00074D37
		public override bool ShouldNpcMarriageBetweenClansBeAllowed(Clan consideringClan, Clan targetClan)
		{
			return targetClan != consideringClan && !consideringClan.IsAtWarWith(targetClan) && consideringClan.GetRelationWithClan(targetClan) >= -50;
		}

		// Token: 0x0600189C RID: 6300 RVA: 0x00076B58 File Offset: 0x00074D58
		public override List<Hero> GetAdultChildrenSuitableForMarriage(Hero hero)
		{
			List<Hero> list = new List<Hero>();
			foreach (Hero hero2 in hero.Children)
			{
				if (hero2.CanMarry())
				{
					list.Add(hero2);
				}
			}
			return list;
		}

		// Token: 0x0600189D RID: 6301 RVA: 0x00076BBC File Offset: 0x00074DBC
		private bool AreHeroesRelatedAux1(Hero firstHero, Hero secondHero, int ancestorDepth)
		{
			return firstHero == secondHero || (ancestorDepth > 0 && ((secondHero.Mother != null && this.AreHeroesRelatedAux1(firstHero, secondHero.Mother, ancestorDepth - 1)) || (secondHero.Father != null && this.AreHeroesRelatedAux1(firstHero, secondHero.Father, ancestorDepth - 1))));
		}

		// Token: 0x0600189E RID: 6302 RVA: 0x00076C0C File Offset: 0x00074E0C
		private bool AreHeroesRelatedAux2(Hero firstHero, Hero secondHero, int ancestorDepth, int secondAncestorDepth)
		{
			return this.AreHeroesRelatedAux1(firstHero, secondHero, secondAncestorDepth) || (ancestorDepth > 0 && ((firstHero.Mother != null && this.AreHeroesRelatedAux2(firstHero.Mother, secondHero, ancestorDepth - 1, secondAncestorDepth)) || (firstHero.Father != null && this.AreHeroesRelatedAux2(firstHero.Father, secondHero, ancestorDepth - 1, secondAncestorDepth))));
		}

		// Token: 0x0600189F RID: 6303 RVA: 0x00076C67 File Offset: 0x00074E67
		private bool AreHeroesRelated(Hero firstHero, Hero secondHero, int ancestorDepth)
		{
			return this.AreHeroesRelatedAux2(firstHero, secondHero, ancestorDepth, ancestorDepth);
		}

		// Token: 0x060018A0 RID: 6304 RVA: 0x00076C74 File Offset: 0x00074E74
		public override int GetEffectiveRelationIncrease(Hero firstHero, Hero secondHero)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(20f, false, null);
			SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.CharmRelationBonus, firstHero.IsFemale ? secondHero.CharacterObject : firstHero.CharacterObject, ref explainedNumber);
			return MathF.Round(explainedNumber.ResultNumber);
		}

		// Token: 0x060018A1 RID: 6305 RVA: 0x00076CC0 File Offset: 0x00074EC0
		public override bool IsSuitableForMarriage(Hero maidenOrSuitor)
		{
			if (maidenOrSuitor.IsActive && maidenOrSuitor.Spouse == null && maidenOrSuitor.IsLord && !maidenOrSuitor.IsMinorFactionHero && !maidenOrSuitor.IsNotable && !maidenOrSuitor.IsTemplate)
			{
				MobileParty partyBelongedTo = maidenOrSuitor.PartyBelongedTo;
				if (((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) == null)
				{
					MobileParty partyBelongedTo2 = maidenOrSuitor.PartyBelongedTo;
					if (((partyBelongedTo2 != null) ? partyBelongedTo2.Army : null) == null)
					{
						IMarriageOfferCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IMarriageOfferCampaignBehavior>();
						if (campaignBehavior != null && campaignBehavior.IsHeroEngaged(maidenOrSuitor))
						{
							return false;
						}
						if (maidenOrSuitor.IsFemale)
						{
							return maidenOrSuitor.CharacterObject.Age >= (float)this.MinimumMarriageAgeFemale;
						}
						return maidenOrSuitor.CharacterObject.Age >= (float)this.MinimumMarriageAgeMale;
					}
				}
			}
			return false;
		}

		// Token: 0x060018A2 RID: 6306 RVA: 0x00076D88 File Offset: 0x00074F88
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

		// Token: 0x040007FA RID: 2042
		private const float BaseMarriageChanceForNpcs = 0.002f;
	}
}
