using System.Collections.Generic;
using System.Linq;
using StoryMode.Quests.SecondPhase.ConspiracyQuests;
using StoryMode.Quests.ThirdPhase;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace StoryMode.GameComponents;

public class StoryModePartySizeLimitModel : PartySizeLimitModel
{
	private DefeatTheConspiracyQuestBehavior _defeatTheConspiracyQuestBehavior;

	public override int MinimumNumberOfVillagersAtVillagerParty => base.BaseModel.MinimumNumberOfVillagersAtVillagerParty;

	private DefeatTheConspiracyQuestBehavior DefeatTheConspiracyQuestBehavior
	{
		get
		{
			if (_defeatTheConspiracyQuestBehavior != null)
			{
				return _defeatTheConspiracyQuestBehavior;
			}
			_defeatTheConspiracyQuestBehavior = Campaign.Current.GetCampaignBehavior<DefeatTheConspiracyQuestBehavior>();
			return _defeatTheConspiracyQuestBehavior;
		}
	}

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
		if (party.IsMobile)
		{
			QuestBase questBase = Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase q) => !q.IsFinalized && q.GetType() == typeof(DisruptSupplyLinesConspiracyQuest));
			if (questBase != null && ((DisruptSupplyLinesConspiracyQuest)questBase).ConspiracyCaravan?.Party == party)
			{
				return new ExplainedNumber(((DisruptSupplyLinesConspiracyQuest)questBase).CaravanPartySize);
			}
			if (DefeatTheConspiracyQuestBehavior != null && DefeatTheConspiracyQuestBehavior.IsMobilePartyCreatedForQuest(party.MobileParty))
			{
				return new ExplainedNumber(600f);
			}
		}
		return base.BaseModel.GetPartyMemberSizeLimit(party, includeDescriptions);
	}

	public override ExplainedNumber GetPartyPrisonerSizeLimit(PartyBase party, bool includeDescriptions = false)
	{
		return base.BaseModel.GetPartyPrisonerSizeLimit(party, includeDescriptions);
	}
}
