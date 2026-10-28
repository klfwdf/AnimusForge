using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class IssueModel : MBGameModel<IssueModel>
{
	public abstract int IssueOwnerCoolDownInDays { get; }

	public abstract float GetIssueDifficultyMultiplier();

	public abstract void GetIssueEffectsOfSettlement(IssueEffect issueEffect, Settlement settlement, ref ExplainedNumber explainedNumber);

	public abstract void GetIssueEffectOfHero(IssueEffect issueEffect, Hero hero, ref ExplainedNumber explainedNumber);

	public abstract void GetIssueEffectOfClan(IssueEffect issueEffect, Clan clan, ref ExplainedNumber explainedNumber);

	public abstract (int, int) GetCausalityForHero(Hero alternativeSolutionHero, IssueBase issue);

	public abstract float GetFailureRiskForHero(Hero alternativeSolutionHero, IssueBase issue);

	public abstract CampaignTime GetDurationOfResolutionForHero(Hero alternativeSolutionHero, IssueBase issue);

	public abstract int GetTroopsRequiredForHero(Hero alternativeSolutionHero, IssueBase issue);

	public abstract bool CanTroopsReturnFromAlternativeSolution();

	public abstract (SkillObject, int) GetIssueAlternativeSolutionSkill(Hero hero, IssueBase issue);
}
