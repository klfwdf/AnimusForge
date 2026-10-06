using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class EncounterModel : MBGameModel<EncounterModel>
{
	public abstract float NeededMaximumLandDistanceForEncounteringMobileParty { get; }

	public abstract float NeededMaximumNavalDistanceForEncounteringMobileParty { get; }

	public abstract float MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy { get; }

	public abstract float MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy { get; }

	public abstract float NeededMaximumDistanceForEncounteringTown { get; }

	public abstract float NeededMaximumDistanceForEncounteringBlockade { get; }

	public abstract float NeededMaximumDistanceForEncounteringVillage { get; }

	public abstract float GetEncounterJoiningRadius { get; }

	public abstract float GetSettlementBeingNearFieldBattleRadius { get; }

	public abstract float PlayerParleyDistance { get; }

	public abstract int MinimumNumberOfMenForAttackingVillageViaScene { get; }

	public abstract bool IsEncounterExemptFromHostileActions(PartyBase side1, PartyBase side2);

	public abstract bool CanMainHeroDoParleyWithParty(PartyBase partyBase, out TextObject explanation);

	public abstract Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side);

	public abstract Hero GetLeaderOfMapEvent(MapEvent mapEvent, BattleSideEnum side);

	public abstract int GetCharacterSergeantScore(Hero hero);

	public abstract IEnumerable<PartyBase> GetDefenderPartiesOfSettlement(Settlement settlement, MapEvent.BattleTypes mapEventType);

	public abstract PartyBase GetNextDefenderPartyOfSettlement(Settlement settlement, ref int partyIndex, MapEvent.BattleTypes mapEventType);

	public abstract MapEventComponent CreateMapEventComponentForEncounter(PartyBase attackerParty, PartyBase defenderParty, MapEvent.BattleTypes battleType);

	public abstract ExplainedNumber GetBribeChance(MobileParty defenderParty, MobileParty attackerParty);

	public abstract float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty);

	public abstract float GetMapEventSideRunAwayChance(MapEventSide mapEventside);

	public abstract void FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide);

	public abstract bool CanPlayerForceBanditsToJoin(out TextObject explanation);

	public abstract bool IsPartyUnderPlayerCommand(PartyBase party);

	public abstract MBReadOnlyList<MobileParty> GetPartiesToTeleportOnMapEventFinalize(MapEvent mapEvent);
}
