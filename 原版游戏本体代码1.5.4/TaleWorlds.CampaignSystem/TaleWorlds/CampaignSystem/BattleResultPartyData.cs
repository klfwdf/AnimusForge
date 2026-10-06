using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem;

public struct BattleResultPartyData
{
	public readonly PartyBase Party;

	public readonly List<CharacterObject> Characters;

	public BattleResultPartyData(PartyBase party)
	{
		Party = party;
		Characters = new List<CharacterObject>();
	}
}
