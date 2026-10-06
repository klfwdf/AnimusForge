using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyDesertionModel : MBGameModel<PartyDesertionModel>
{
	public abstract TroopRoster GetTroopsToDesert(MobileParty mobileParty);

	public abstract float GetDesertionChanceForTroop(MobileParty mobileParty, in TroopRosterElement troopRosterElement);

	public abstract int GetMoraleThresholdForTroopDesertion();
}
