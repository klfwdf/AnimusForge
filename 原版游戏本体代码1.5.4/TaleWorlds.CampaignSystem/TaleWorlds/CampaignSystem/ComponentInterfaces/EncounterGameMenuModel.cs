using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class EncounterGameMenuModel : MBGameModel<EncounterGameMenuModel>
{
	public abstract string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle);

	public abstract string GetRaidCompleteMenu();

	public abstract string GetNewPartyJoinMenu(MobileParty newParty);

	public abstract string GetGenericStateMenu();

	public abstract bool IsPlunderMenu(string menuId);
}
