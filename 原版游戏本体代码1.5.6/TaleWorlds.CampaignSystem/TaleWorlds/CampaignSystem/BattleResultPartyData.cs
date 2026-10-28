using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem;

public struct BattleResultPartyData(PartyBase party)
{
	public readonly PartyBase Party = party;

	public readonly List<CharacterObject> Characters = new List<CharacterObject>();
}
