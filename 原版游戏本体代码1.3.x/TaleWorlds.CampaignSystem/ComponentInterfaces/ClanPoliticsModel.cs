using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D1 RID: 465
	public abstract class ClanPoliticsModel : MBGameModel<ClanPoliticsModel>
	{
		// Token: 0x06001E20 RID: 7712
		public abstract ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false);

		// Token: 0x06001E21 RID: 7713
		public abstract float CalculateSupportForPolicyInClan(Clan clan, PolicyObject policy);

		// Token: 0x06001E22 RID: 7714
		public abstract float CalculateRelationshipChangeWithSponsor(Clan clan, Clan sponsorClan);

		// Token: 0x06001E23 RID: 7715
		public abstract int GetInfluenceRequiredToOverrideKingdomDecision(DecisionOutcome popularOption, DecisionOutcome overridingOption, KingdomDecision decision);

		// Token: 0x06001E24 RID: 7716
		public abstract bool CanHeroBeGovernor(Hero hero);
	}
}
