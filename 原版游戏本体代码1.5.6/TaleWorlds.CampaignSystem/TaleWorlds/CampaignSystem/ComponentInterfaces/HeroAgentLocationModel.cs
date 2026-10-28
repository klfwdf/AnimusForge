using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class HeroAgentLocationModel : MBGameModel<HeroAgentLocationModel>
{
	public enum HeroLocationDetail
	{
		None,
		SettlementKingQueen,
		NobleBelongingToNoParty,
		Prisoner,
		PlayerClanMember,
		MainPartyCompanion,
		Notable,
		Wanderer,
		PartyLeader,
		PartylessHeroInsideVillage
	}

	public abstract bool WillBeListedInOverlay(LocationCharacter locationCharacter);

	public abstract Location GetLocationForHero(Hero hero, Settlement settlement, out HeroLocationDetail heroSpawnDetail);
}
