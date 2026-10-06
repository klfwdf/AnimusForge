using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ClanPoliticsModel : MBGameModel<ClanPoliticsModel>
{
	public abstract ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false);

	public abstract float CalculateSupportForPolicyInClan(Clan clan, PolicyObject policy);

	public abstract float CalculateRelationshipChangeWithSponsor(Clan clan, Clan sponsorClan);

	public abstract int GetInfluenceRequiredToOverrideKingdomDecision(DecisionOutcome popularOption, DecisionOutcome overridingOption, KingdomDecision decision);

	public abstract bool CanHeroBeGovernor(Hero hero);
}
