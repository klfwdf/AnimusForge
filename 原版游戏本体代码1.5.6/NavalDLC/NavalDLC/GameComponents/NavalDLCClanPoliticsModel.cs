using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;

namespace NavalDLC.GameComponents;

public class NavalDLCClanPoliticsModel : ClanPoliticsModel
{
	public override ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false)
	{
		ExplainedNumber result = base.BaseModel.CalculateInfluenceChange(clan, includeDescriptions);
		if (clan.Kingdom != null && !clan.IsUnderMercenaryService && clan.Kingdom.HasPolicy(NavalPolicies.NavalConjoiningStatute))
		{
			List<Ship> source = clan.AliveLords.Where((Hero x) => x.PartyBelongedTo != null).SelectMany((Hero x) => x.PartyBelongedTo.Ships).ToList();
			if (source.Any((Ship x) => x.ShipHull.Type == ShipHull.ShipType.Heavy))
			{
				result.Add(1f, NavalPolicies.NavalConjoiningStatute.Name);
			}
			else if (source.All((Ship x) => x.ShipHull.Type == ShipHull.ShipType.Light))
			{
				result.Add(-1f, NavalPolicies.NavalConjoiningStatute.Name);
			}
		}
		return result;
	}

	public override float CalculateSupportForPolicyInClan(Clan clan, PolicyObject policy)
	{
		return base.BaseModel.CalculateSupportForPolicyInClan(clan, policy);
	}

	public override float CalculateRelationshipChangeWithSponsor(Clan clan, Clan sponsorClan)
	{
		return base.BaseModel.CalculateRelationshipChangeWithSponsor(clan, sponsorClan);
	}

	public override int GetInfluenceRequiredToOverrideKingdomDecision(DecisionOutcome popularOption, DecisionOutcome overridingOption, KingdomDecision decision)
	{
		return base.BaseModel.GetInfluenceRequiredToOverrideKingdomDecision(popularOption, overridingOption, decision);
	}

	public override bool CanHeroBeGovernor(Hero hero)
	{
		return base.BaseModel.CanHeroBeGovernor(hero);
	}
}
