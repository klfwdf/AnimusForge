using System;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E8 RID: 488
	public abstract class IssueModel : MBGameModel<IssueModel>
	{
		// Token: 0x06001EB6 RID: 7862
		public abstract float GetIssueDifficultyMultiplier();

		// Token: 0x170007A7 RID: 1959
		// (get) Token: 0x06001EB7 RID: 7863
		public abstract int IssueOwnerCoolDownInDays { get; }

		// Token: 0x06001EB8 RID: 7864
		public abstract void GetIssueEffectsOfSettlement(IssueEffect issueEffect, Settlement settlement, ref ExplainedNumber explainedNumber);

		// Token: 0x06001EB9 RID: 7865
		public abstract void GetIssueEffectOfHero(IssueEffect issueEffect, Hero hero, ref ExplainedNumber explainedNumber);

		// Token: 0x06001EBA RID: 7866
		public abstract void GetIssueEffectOfClan(IssueEffect issueEffect, Clan clan, ref ExplainedNumber explainedNumber);

		// Token: 0x06001EBB RID: 7867
		public abstract ValueTuple<int, int> GetCausalityForHero(Hero alternativeSolutionHero, IssueBase issue);

		// Token: 0x06001EBC RID: 7868
		public abstract float GetFailureRiskForHero(Hero alternativeSolutionHero, IssueBase issue);

		// Token: 0x06001EBD RID: 7869
		public abstract CampaignTime GetDurationOfResolutionForHero(Hero alternativeSolutionHero, IssueBase issue);

		// Token: 0x06001EBE RID: 7870
		public abstract int GetTroopsRequiredForHero(Hero alternativeSolutionHero, IssueBase issue);

		// Token: 0x06001EBF RID: 7871
		public abstract bool CanTroopsReturnFromAlternativeSolution();

		// Token: 0x06001EC0 RID: 7872
		public abstract ValueTuple<SkillObject, int> GetIssueAlternativeSolutionSkill(Hero hero, IssueBase issue);
	}
}
