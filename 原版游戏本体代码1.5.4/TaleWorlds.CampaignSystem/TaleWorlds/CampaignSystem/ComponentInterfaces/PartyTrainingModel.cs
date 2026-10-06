using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyTrainingModel : MBGameModel<PartyTrainingModel>
{
	public abstract int GenerateSharedXp(CharacterObject troop, int xp, MobileParty mobileParty);

	public abstract ExplainedNumber CalculateXpGainFromBattles(FlattenedTroopRosterElement troopRosterElement, PartyBase party);

	public abstract int GetXpReward(CharacterObject character);

	public abstract ExplainedNumber GetEffectiveDailyExperience(MobileParty party, TroopRosterElement troop);
}
