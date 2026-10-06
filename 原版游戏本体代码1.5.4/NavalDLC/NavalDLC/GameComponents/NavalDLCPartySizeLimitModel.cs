using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalDLCPartySizeLimitModel : PartySizeLimitModel
{
	public override int MinimumNumberOfVillagersAtVillagerParty => base.BaseModel.MinimumNumberOfVillagersAtVillagerParty;

	public override ExplainedNumber CalculateGarrisonPartySizeLimit(Settlement settlement, bool includeDescriptions = false)
	{
		return base.BaseModel.CalculateGarrisonPartySizeLimit(settlement, includeDescriptions);
	}

	public override TroopRoster FindAppropriateInitialRosterForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
	{
		return base.BaseModel.FindAppropriateInitialRosterForMobileParty(party, partyTemplate);
	}

	public override List<Ship> FindAppropriateInitialShipsForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
	{
		return base.BaseModel.FindAppropriateInitialShipsForMobileParty(party, partyTemplate);
	}

	public override int GetAssumedPartySizeForLordParty(Hero leaderHero, IFaction partyMapFaction, Clan actualClan)
	{
		return base.BaseModel.GetAssumedPartySizeForLordParty(leaderHero, partyMapFaction, actualClan);
	}

	public override int GetClanTierPartySizeEffectForHero(Hero hero)
	{
		return base.BaseModel.GetClanTierPartySizeEffectForHero(hero);
	}

	public override int GetIdealVillagerPartySize(Village village)
	{
		return base.BaseModel.GetIdealVillagerPartySize(village);
	}

	public override int GetNextClanTierPartySizeEffectChangeForHero(Hero hero)
	{
		return base.BaseModel.GetNextClanTierPartySizeEffectChangeForHero(hero);
	}

	public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
	{
		if (party.IsNavalStorylineQuestParty(out var partyData) && partyData.IsQuestParty)
		{
			return new ExplainedNumber(partyData.PartySize);
		}
		if (party.IsMobile && party.MobileParty.ActualClan != null && party.MobileParty.ActualClan.IsBanditFaction && !party.MobileParty.IsCurrentlyUsedByAQuest && party.MobileParty.HasNavalNavigationCapability)
		{
			return new ExplainedNumber(party.MobileParty.ActualClan.DefaultPartyTemplate.GetUpperTroopLimit());
		}
		if (party.IsMobile && party.MobileParty.IsPatrolParty && party.MobileParty.PatrolPartyComponent.IsNaval)
		{
			return CalculatePatrolPartySizeLimit(party.MobileParty, includeDescriptions);
		}
		return base.BaseModel.GetPartyMemberSizeLimit(party, includeDescriptions);
	}

	private ExplainedNumber CalculatePatrolPartySizeLimit(MobileParty mobileParty, bool includeDescriptions)
	{
		return new ExplainedNumber(mobileParty.HomeSettlement.Culture.SettlementPatrolPartyTemplateNaval.GetUpperTroopLimit(), includeDescriptions);
	}

	public override ExplainedNumber GetPartyPrisonerSizeLimit(PartyBase party, bool includeDescriptions = false)
	{
		return base.BaseModel.GetPartyPrisonerSizeLimit(party, includeDescriptions);
	}
}
