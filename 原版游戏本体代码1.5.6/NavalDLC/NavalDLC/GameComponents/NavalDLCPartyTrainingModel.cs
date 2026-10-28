using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace NavalDLC.GameComponents;

public class NavalDLCPartyTrainingModel : PartyTrainingModel
{
	public override int GenerateSharedXp(CharacterObject troop, int xp, MobileParty mobileParty)
	{
		return base.BaseModel.GenerateSharedXp(troop, xp, mobileParty);
	}

	public override ExplainedNumber CalculateXpGainFromBattles(FlattenedTroopRosterElement troopRosterElement, PartyBase party)
	{
		ExplainedNumber stat = base.BaseModel.CalculateXpGainFromBattles(troopRosterElement, party);
		CharacterObject troop = troopRosterElement.Troop;
		if (!troop.IsHero)
		{
			if (troop.IsMariner)
			{
				PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.Arr, party.MobileParty, isPrimaryBonus: false, ref stat);
			}
			if (troop.IsRegular)
			{
				PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.PirateHunter, party.MobileParty, isPrimaryBonus: false, ref stat);
			}
		}
		return stat;
	}

	public override int GetXpReward(CharacterObject character)
	{
		return base.BaseModel.GetXpReward(character);
	}

	public override ExplainedNumber GetEffectiveDailyExperience(MobileParty party, TroopRosterElement troop)
	{
		return base.BaseModel.GetEffectiveDailyExperience(party, troop);
	}
}
