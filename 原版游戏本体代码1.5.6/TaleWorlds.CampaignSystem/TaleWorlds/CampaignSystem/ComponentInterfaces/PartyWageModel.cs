using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyWageModel : MBGameModel<PartyWageModel>
{
	public abstract int MaxWagePaymentLimit { get; }

	public abstract int GetCharacterWage(CharacterObject character);

	public abstract ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false);

	public abstract ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false);
}
