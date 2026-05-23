using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000120 RID: 288
	public class DefaultIssueModel : IssueModel
	{
		// Token: 0x17000668 RID: 1640
		// (get) Token: 0x06001817 RID: 6167 RVA: 0x00073A25 File Offset: 0x00071C25
		public override int IssueOwnerCoolDownInDays
		{
			get
			{
				return 30;
			}
		}

		// Token: 0x06001818 RID: 6168 RVA: 0x00073A29 File Offset: 0x00071C29
		public override float GetIssueDifficultyMultiplier()
		{
			return MBMath.ClampFloat(Campaign.Current.PlayerProgress, 0.1f, 1f);
		}

		// Token: 0x06001819 RID: 6169 RVA: 0x00073A44 File Offset: 0x00071C44
		public override void GetIssueEffectsOfSettlement(IssueEffect issueEffect, Settlement settlement, ref ExplainedNumber explainedNumber)
		{
			Hero leader = settlement.OwnerClan.Leader;
			if (leader != null && leader.IsAlive && leader.Issue != null)
			{
				this.GetIssueEffectOfHeroInternal(issueEffect, leader, ref explainedNumber, DefaultIssueModel.SettlementIssuesText);
			}
			foreach (Hero hero in settlement.HeroesWithoutParty)
			{
				if (hero.Issue != null)
				{
					this.GetIssueEffectOfHeroInternal(issueEffect, hero, ref explainedNumber, DefaultIssueModel.SettlementIssuesText);
				}
			}
			if (settlement.IsTown || settlement.IsCastle)
			{
				foreach (Village village in settlement.BoundVillages)
				{
					foreach (Hero hero2 in village.Settlement.Notables)
					{
						if (hero2.Issue != null)
						{
							this.GetIssueEffectOfHeroInternal(issueEffect, hero2, ref explainedNumber, DefaultIssueModel.RelatedSettlementIssuesText);
						}
					}
				}
			}
		}

		// Token: 0x0600181A RID: 6170 RVA: 0x00073B78 File Offset: 0x00071D78
		public override void GetIssueEffectOfHero(IssueEffect issueEffect, Hero hero, ref ExplainedNumber explainedNumber)
		{
			this.GetIssueEffectOfHeroInternal(issueEffect, hero, ref explainedNumber, DefaultIssueModel.HeroIssueText);
		}

		// Token: 0x0600181B RID: 6171 RVA: 0x00073B88 File Offset: 0x00071D88
		public override void GetIssueEffectOfClan(IssueEffect issueEffect, Clan clan, ref ExplainedNumber explainedNumber)
		{
			float num = 0f;
			foreach (Hero hero in clan.AliveLords)
			{
				if (hero.Issue != null)
				{
					IssueBase issue = hero.Issue;
					num += issue.GetActiveIssueEffectAmount(issueEffect);
				}
			}
			explainedNumber.Add(num, DefaultIssueModel.ClanIssuesText, null);
		}

		// Token: 0x0600181C RID: 6172 RVA: 0x00073C00 File Offset: 0x00071E00
		public override ValueTuple<int, int> GetCausalityForHero(Hero alternativeSolutionHero, IssueBase issue)
		{
			ValueTuple<SkillObject, int> issueAlternativeSolutionSkill = this.GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
			int skillValue = alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1);
			float num = 0.8f;
			if (skillValue != 0)
			{
				num = (float)(issueAlternativeSolutionSkill.Item2 / skillValue) * 0.1f;
			}
			num = MBMath.ClampFloat(num, 0.2f, 0.8f);
			int num2 = MathF.Ceiling((float)issue.GetTotalAlternativeSolutionNeededMenCount() * num);
			return new ValueTuple<int, int>(MBMath.ClampInt(2 * (num2 / 3), 1, num2), num2);
		}

		// Token: 0x0600181D RID: 6173 RVA: 0x00073C70 File Offset: 0x00071E70
		public override float GetFailureRiskForHero(Hero alternativeSolutionHero, IssueBase issue)
		{
			ValueTuple<SkillObject, int> issueAlternativeSolutionSkill = this.GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
			return MBMath.ClampFloat((float)(issueAlternativeSolutionSkill.Item2 - alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1)) * 0.5f / 100f, 0f, 0.9f);
		}

		// Token: 0x0600181E RID: 6174 RVA: 0x00073CB8 File Offset: 0x00071EB8
		public override CampaignTime GetDurationOfResolutionForHero(Hero alternativeSolutionHero, IssueBase issue)
		{
			ValueTuple<SkillObject, int> issueAlternativeSolutionSkill = this.GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
			int skillValue = alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1);
			float num = 10f;
			if (skillValue != 0)
			{
				num = MBMath.ClampFloat((float)(issueAlternativeSolutionSkill.Item2 / skillValue), 0f, 10f);
			}
			return CampaignTime.Days((float)issue.GetBaseAlternativeSolutionDurationInDays() + 2f * num);
		}

		// Token: 0x0600181F RID: 6175 RVA: 0x00073D14 File Offset: 0x00071F14
		public override int GetTroopsRequiredForHero(Hero alternativeSolutionHero, IssueBase issue)
		{
			ValueTuple<SkillObject, int> issueAlternativeSolutionSkill = this.GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
			int skillValue = alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1);
			float num = 1.2f;
			if (skillValue != 0)
			{
				num = (float)issueAlternativeSolutionSkill.Item2 / (float)skillValue;
			}
			num = MBMath.ClampFloat(num, 0.2f, 1.2f);
			return (int)((float)issue.AlternativeSolutionBaseNeededMenCount * num);
		}

		// Token: 0x06001820 RID: 6176 RVA: 0x00073D66 File Offset: 0x00071F66
		public override ValueTuple<SkillObject, int> GetIssueAlternativeSolutionSkill(Hero hero, IssueBase issue)
		{
			return issue.GetAlternativeSolutionSkill(hero);
		}

		// Token: 0x06001821 RID: 6177 RVA: 0x00073D70 File Offset: 0x00071F70
		private void GetIssueEffectOfHeroInternal(IssueEffect issueEffect, Hero hero, ref ExplainedNumber explainedNumber, TextObject customText)
		{
			float activeIssueEffectAmount = hero.Issue.GetActiveIssueEffectAmount(issueEffect);
			if (!activeIssueEffectAmount.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				explainedNumber.Add(activeIssueEffectAmount, customText, null);
			}
		}

		// Token: 0x06001822 RID: 6178 RVA: 0x00073DA6 File Offset: 0x00071FA6
		public override bool CanTroopsReturnFromAlternativeSolution()
		{
			return !Hero.MainHero.IsPrisoner && (!MobileParty.MainParty.IsCurrentlyAtSea || Settlement.CurrentSettlement != null) && MobileParty.MainParty.MapEvent == null;
		}

		// Token: 0x040007DE RID: 2014
		private static readonly TextObject SettlementIssuesText = new TextObject("{=EQLgVYk0}Settlement Issues", null);

		// Token: 0x040007DF RID: 2015
		private static readonly TextObject HeroIssueText = GameTexts.FindText("str_issues", null);

		// Token: 0x040007E0 RID: 2016
		private static readonly TextObject RelatedSettlementIssuesText = new TextObject("{=umNyHc3A}Bound Village Issues", null);

		// Token: 0x040007E1 RID: 2017
		private static readonly TextObject ClanIssuesText = new TextObject("{=jdl8G8JS}Clan Issues", null);
	}
}
