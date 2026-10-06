using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultIssueModel : IssueModel
{
	private static readonly TextObject SettlementIssuesText = new TextObject("{=EQLgVYk0}Settlement Issues");

	private static readonly TextObject HeroIssueText = GameTexts.FindText("str_issues");

	private static readonly TextObject RelatedSettlementIssuesText = new TextObject("{=umNyHc3A}Bound Village Issues");

	private static readonly TextObject ClanIssuesText = new TextObject("{=jdl8G8JS}Clan Issues");

	public override int IssueOwnerCoolDownInDays => 30;

	public override float GetIssueDifficultyMultiplier()
	{
		return MBMath.ClampFloat(Campaign.Current.PlayerProgress, 0.1f, 1f);
	}

	public override void GetIssueEffectsOfSettlement(IssueEffect issueEffect, Settlement settlement, ref ExplainedNumber explainedNumber)
	{
		foreach (Hero aliveLord in settlement.OwnerClan.AliveLords)
		{
			if (aliveLord.Issue != null)
			{
				GetIssueEffectOfHeroInternal(issueEffect, aliveLord, ref explainedNumber, SettlementIssuesText);
			}
		}
		foreach (Hero item in settlement.HeroesWithoutParty)
		{
			if (item.Issue != null)
			{
				GetIssueEffectOfHeroInternal(issueEffect, item, ref explainedNumber, SettlementIssuesText);
			}
		}
		if (!settlement.IsTown && !settlement.IsCastle)
		{
			return;
		}
		foreach (Village boundVillage in settlement.BoundVillages)
		{
			foreach (Hero notable in boundVillage.Settlement.Notables)
			{
				if (notable.Issue != null)
				{
					GetIssueEffectOfHeroInternal(issueEffect, notable, ref explainedNumber, RelatedSettlementIssuesText);
				}
			}
		}
	}

	public override void GetIssueEffectOfHero(IssueEffect issueEffect, Hero hero, ref ExplainedNumber explainedNumber)
	{
		GetIssueEffectOfHeroInternal(issueEffect, hero, ref explainedNumber, HeroIssueText);
	}

	public override void GetIssueEffectOfClan(IssueEffect issueEffect, Clan clan, ref ExplainedNumber explainedNumber)
	{
		float num = 0f;
		foreach (Hero aliveLord in clan.AliveLords)
		{
			if (aliveLord.Issue != null)
			{
				IssueBase issue = aliveLord.Issue;
				num += issue.GetActiveIssueEffectAmount(issueEffect);
			}
		}
		explainedNumber.Add(num, ClanIssuesText);
	}

	public override (int, int) GetCausalityForHero(Hero alternativeSolutionHero, IssueBase issue)
	{
		(SkillObject, int) issueAlternativeSolutionSkill = GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
		int skillValue = alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1);
		float value = 0.8f;
		if (skillValue != 0)
		{
			value = (float)(issueAlternativeSolutionSkill.Item2 / skillValue) * 0.1f;
		}
		value = MBMath.ClampFloat(value, 0.2f, 0.8f);
		int num = MathF.Ceiling((float)issue.GetTotalAlternativeSolutionNeededMenCount() * value);
		return (MBMath.ClampInt(2 * (num / 3), 1, num), num);
	}

	public override float GetFailureRiskForHero(Hero alternativeSolutionHero, IssueBase issue)
	{
		(SkillObject, int) issueAlternativeSolutionSkill = GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
		return MBMath.ClampFloat((float)(issueAlternativeSolutionSkill.Item2 - alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1)) * 0.5f / 100f, 0f, 0.9f);
	}

	public override CampaignTime GetDurationOfResolutionForHero(Hero alternativeSolutionHero, IssueBase issue)
	{
		(SkillObject, int) issueAlternativeSolutionSkill = GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
		int skillValue = alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1);
		float num = 10f;
		if (skillValue != 0)
		{
			num = MBMath.ClampFloat(issueAlternativeSolutionSkill.Item2 / skillValue, 0f, 10f);
		}
		return CampaignTime.Days((float)issue.GetBaseAlternativeSolutionDurationInDays() + 2f * num);
	}

	public override int GetTroopsRequiredForHero(Hero alternativeSolutionHero, IssueBase issue)
	{
		(SkillObject, int) issueAlternativeSolutionSkill = GetIssueAlternativeSolutionSkill(alternativeSolutionHero, issue);
		int skillValue = alternativeSolutionHero.GetSkillValue(issueAlternativeSolutionSkill.Item1);
		float value = 1.2f;
		if (skillValue != 0)
		{
			value = (float)issueAlternativeSolutionSkill.Item2 / (float)skillValue;
		}
		value = MBMath.ClampFloat(value, 0.2f, 1.2f);
		return (int)((float)issue.AlternativeSolutionBaseNeededMenCount * value);
	}

	public override (SkillObject, int) GetIssueAlternativeSolutionSkill(Hero hero, IssueBase issue)
	{
		return issue.GetAlternativeSolutionSkill(hero);
	}

	private void GetIssueEffectOfHeroInternal(IssueEffect issueEffect, Hero hero, ref ExplainedNumber explainedNumber, TextObject customText)
	{
		float activeIssueEffectAmount = hero.Issue.GetActiveIssueEffectAmount(issueEffect);
		if (!activeIssueEffectAmount.ApproximatelyEqualsTo(0f))
		{
			explainedNumber.Add(activeIssueEffectAmount, customText);
		}
	}

	public override bool CanTroopsReturnFromAlternativeSolution()
	{
		if (!Hero.MainHero.IsPrisoner && (!MobileParty.MainParty.IsCurrentlyAtSea || Settlement.CurrentSettlement != null))
		{
			return MobileParty.MainParty.MapEvent == null;
		}
		return false;
	}
}
