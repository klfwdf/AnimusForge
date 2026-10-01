using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

internal sealed class CastleAftermathAlliedRosterSelectionContext
{
	private readonly TroopRoster _playerPartyMembers;

	private readonly Dictionary<string, Hero> _guestLeaders;

	internal CastleAftermathAlliedRosterSelectionContext(
		TroopRoster playerPartyMembers,
		Dictionary<string, Hero> guestLeaders,
		string source)
	{
		_playerPartyMembers = CastleAftermathArmyRosterRuntimeBridge.CloneRoster(playerPartyMembers);
		_guestLeaders = guestLeaders != null
			? new Dictionary<string, Hero>(guestLeaders, StringComparer.OrdinalIgnoreCase)
			: new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);
		Source = source ?? "unknown";
	}

	internal string Source { get; }

	internal int PlayerPartyMemberCount => _playerPartyMembers?.TotalManCount ?? 0;

	internal int GuestLeaderCount => _guestLeaders.Count;

	internal TroopRoster BuildAvailableMembers()
	{
		TroopRoster result = CastleAftermathArmyRosterRuntimeBridge.CloneRoster(_playerPartyMembers);
		foreach (Hero hero in _guestLeaders.Values)
		{
			CharacterObject character = hero?.CharacterObject;
			if (character != null && result.FindIndexOfTroop(character) < 0)
			{
				result.AddToCounts(character, 1, false, 0, 0, true, -1);
			}
		}
		return result;
	}

	internal TroopRoster BuildDefaultPlayerPartyMembers()
	{
		return CastleAftermathArmyRosterRuntimeBridge.CloneRoster(_playerPartyMembers);
	}

	internal TroopRoster BuildSelectedPlayerPartyMembers(TroopRoster selectedMembers)
	{
		return CastleAftermathArmyRosterRuntimeBridge.IntersectRoster(selectedMembers, _playerPartyMembers);
	}

	internal TroopRoster BuildSelectedGuestLeaders(TroopRoster selectedMembers)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (selectedMembers == null)
		{
			return result;
		}

		foreach (TroopRosterElement element in selectedMembers.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			string key = CastleAftermathArmyRosterRuntimeBridge.GetCharacterKey(character);
			if (element.Number <= 0
				|| string.IsNullOrWhiteSpace(key)
				|| !_guestLeaders.TryGetValue(key, out Hero hero)
				|| !CastleAftermathArmyRosterRuntimeBridge.IsEligibleGuestLeader(hero))
			{
				continue;
			}
			result.AddToCounts(hero.CharacterObject, 1, false, 0, 0, true, -1);
		}
		return result;
	}

	internal TroopRoster BuildRuntimeNotSelectedMainPartyMembers(TroopRoster selectedPlayerPartyMembers)
	{
		return CastleAftermathArmyRosterRuntimeBridge.BuildRuntimeNotSelectedMainPartyMembers(selectedPlayerPartyMembers);
	}
}

/// <summary>
/// Bannerlord-only bridge for the castle aftermath allied selector. It snapshots the
/// player's own MapEventParty before the winning army is collapsed, while exposing
/// other participating parties only through their noble leaders.
/// </summary>
internal static class CastleAftermathArmyRosterRuntimeBridge
{
	private static MapEvent _trackedMapEvent;

	private static MapEventParty _trackedPlayerBattleParty;

	private static readonly List<MapEventParty> TrackedAlliedBattleParties = new List<MapEventParty>();

	private static TroopRoster _battlePlayerPartyMembers;

	private static readonly Dictionary<string, Hero> BattleGuestLeaders = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Hero> SelectedGuestLeaders = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);

	private static string _battleSettlementId = "";

	private static int _battleDay = -1;

	private static bool _battleVictoryConfirmed;

	internal static void TryCapturePlayerCastleBattleStart()
	{
		try
		{
			MapEvent mapEvent = MapEvent.PlayerMapEvent ?? MobileParty.MainParty?.MapEvent;
			if (!IsPlayerCastleAssault(mapEvent, requireVictory: false))
			{
				return;
			}
			TrackBattle(mapEvent, "mission_start");
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Capture player castle battle roster at mission start failed: " + ex.Message);
		}
	}

	internal static void RefreshTrackedPlayerCastleBattle(string source)
	{
		try
		{
			if (_trackedMapEvent == null || _trackedPlayerBattleParty == null)
			{
				return;
			}
			RefreshTrackedSnapshot(source ?? "mission_end");
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Refresh tracked player castle battle roster failed: " + ex.Message);
		}
	}

	internal static void FinalizePlayerCastleVictory(MapEvent mapEvent, Settlement settlement)
	{
		try
		{
			if (!IsPlayerCastleAssault(mapEvent, requireVictory: true) || settlement?.IsCastle != true)
			{
				ClearBattleSnapshot("battle_end_not_player_castle_victory");
				return;
			}

			if (!ReferenceEquals(_trackedMapEvent, mapEvent) || _trackedPlayerBattleParty == null)
			{
				TrackBattle(mapEvent, "battle_end_fallback");
			}
			else
			{
				RefreshTrackedSnapshot("battle_end_finalize");
			}

			_battleSettlementId = settlement.StringId ?? mapEvent.MapEventSettlement?.StringId ?? "";
			_battleDay = GetCurrentCampaignDay();
			_battleVictoryConfirmed = true;
			Logger.Log("CastleAftermath", "Confirmed player-party castle roster snapshot. Settlement="
				+ (_battleSettlementId ?? "N/A")
				+ ", OwnHealthy=" + (_battlePlayerPartyMembers?.TotalManCount ?? 0)
				+ ", AlliedCaptains=" + BattleGuestLeaders.Count
				+ ", PlayerSideParties=" + (TrackedAlliedBattleParties.Count + 1));
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Finalize player castle victory roster failed: " + ex.Message);
			ClearBattleSnapshot("battle_end_finalize_exception");
		}
	}

	internal static CastleAftermathAlliedRosterSelectionContext BuildSelectionContext(
		Settlement settlement,
		IEnumerable<MobileParty> contributingParties)
	{
		TryFinalizeLiveWinningMapEvent(settlement);

		bool useBattleSnapshot = IsBattleSnapshotCurrent(settlement);
		TroopRoster playerPartyMembers = useBattleSnapshot
			? ClampSnapshotToLiveMainParty(_battlePlayerPartyMembers)
			: BuildHealthyPlayerPartyRoster(MobileParty.MainParty?.MemberRoster);
		Dictionary<string, Hero> guestLeaders = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);
		if (useBattleSnapshot)
		{
			foreach (KeyValuePair<string, Hero> pair in BattleGuestLeaders)
			{
				TryAddGuestLeader(guestLeaders, pair.Value, isFriendly: true);
			}
		}

		TryAddArmyGuestLeaders(guestLeaders, MobileParty.MainParty?.Army);
		if (contributingParties != null)
		{
			foreach (MobileParty party in contributingParties)
			{
				if (party == null || party == MobileParty.MainParty)
				{
					continue;
				}
				TryAddGuestLeader(guestLeaders, party.LeaderHero, IsFriendlyToPlayer(party));
			}
		}

		string source = useBattleSnapshot ? "player_map_event_party_snapshot" : "live_main_party_fallback";
		CastleAftermathAlliedRosterSelectionContext context = new CastleAftermathAlliedRosterSelectionContext(
			playerPartyMembers,
			guestLeaders,
			source);
		Logger.Log("CastleAftermath", "Built castle allied selection context. Source=" + source
			+ ", OwnCandidates=" + context.PlayerPartyMemberCount
			+ ", AlliedCaptainCandidates=" + context.GuestLeaderCount
			+ ", LiveMainHealthy=" + CountHealthySelectableMainPartyMembers());
		return context;
	}

	internal static void StoreSelectedGuestLeaders(TroopRoster selectedGuests)
	{
		SelectedGuestLeaders.Clear();
		if (selectedGuests != null)
		{
			foreach (TroopRosterElement element in selectedGuests.GetTroopRoster())
			{
				Hero hero = element.Character?.HeroObject;
				string key = GetCharacterKey(element.Character);
				if (element.Number > 0 && !string.IsNullOrWhiteSpace(key) && IsEligibleGuestLeader(hero))
				{
					SelectedGuestLeaders[key] = hero;
				}
			}
		}
		Logger.Log("CastleAftermath", "Stored selected allied army captains. Count=" + SelectedGuestLeaders.Count);
	}

	internal static void ClearSelectedGuestLeaders(string source)
	{
		int count = SelectedGuestLeaders.Count;
		SelectedGuestLeaders.Clear();
		if (count > 0)
		{
			Logger.Log("CastleAftermath", "Cleared selected allied army captains. Count=" + count + ", Source=" + (source ?? "N/A"));
		}
	}

	internal static bool IsSelectedGuestLeader(CharacterObject character)
	{
		string key = GetCharacterKey(character);
		return !string.IsNullOrWhiteSpace(key) && SelectedGuestLeaders.ContainsKey(key);
	}

	internal static PartyBase ResolveAgentOriginParty(CharacterObject character, PartyBase fallback)
	{
		try
		{
			string key = GetCharacterKey(character);
			if (!string.IsNullOrWhiteSpace(key)
				&& SelectedGuestLeaders.TryGetValue(key, out Hero hero)
				&& hero?.PartyBelongedTo?.Party != null
				&& hero.PartyBelongedTo.IsActive)
			{
				return hero.PartyBelongedTo.Party;
			}
		}
		catch
		{
		}
		return fallback;
	}

	internal static void ClearBattleSnapshot(string source)
	{
		_trackedMapEvent = null;
		_trackedPlayerBattleParty = null;
		TrackedAlliedBattleParties.Clear();
		_battlePlayerPartyMembers = null;
		BattleGuestLeaders.Clear();
		_battleSettlementId = "";
		_battleDay = -1;
		_battleVictoryConfirmed = false;
		Logger.Log("CastleAftermath", "Cleared player-party castle roster snapshot. Source=" + (source ?? "N/A"));
	}

	internal static TroopRoster BuildRuntimeNotSelectedMainPartyMembers(TroopRoster selectedPlayerPartyMembers)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		TroopRoster source = MobileParty.MainParty?.MemberRoster;
		if (source == null)
		{
			return result;
		}

		Dictionary<string, int> selectedCounts = BuildRosterCounts(selectedPlayerPartyMembers);
		foreach (TroopRosterElement element in source.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			if (character == null || character.IsPlayerCharacter || element.Number <= 0)
			{
				continue;
			}

			Hero hero = character.HeroObject;
			if (hero != null)
			{
				if (hero.PartyBelongedTo != MobileParty.MainParty || !IsEligiblePlayerPartyMember(character))
				{
					continue;
				}
			}

			int available = hero != null ? 1 : Math.Max(0, element.Number - element.WoundedNumber);
			selectedCounts.TryGetValue(GetCharacterKey(character), out int selected);
			int notSelected = Math.Max(0, available - selected);
			if (notSelected > 0)
			{
				result.AddToCounts(character, notSelected, false, 0, 0, true, -1);
			}
		}
		return result;
	}

	internal static TroopRoster IntersectRoster(TroopRoster requested, TroopRoster allowed)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (requested == null || allowed == null)
		{
			return result;
		}

		foreach (TroopRosterElement requestedElement in requested.GetTroopRoster())
		{
			CharacterObject character = requestedElement.Character;
			int allowedIndex = character == null ? -1 : allowed.FindIndexOfTroop(character);
			if (allowedIndex < 0 || requestedElement.Number <= 0)
			{
				continue;
			}
			TroopRosterElement allowedElement = allowed.GetElementCopyAtIndex(allowedIndex);
			int number = Math.Min(requestedElement.Number, Math.Max(0, allowedElement.Number - allowedElement.WoundedNumber));
			if (character.IsHero)
			{
				number = Math.Min(1, number);
			}
			if (number > 0)
			{
				result.AddToCounts(character, number, false, 0, 0, true, -1);
			}
		}
		return result;
	}

	internal static TroopRoster CloneRoster(TroopRoster source)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (source == null)
		{
			return result;
		}
		foreach (TroopRosterElement element in source.GetTroopRoster())
		{
			if (element.Character != null && element.Number > 0)
			{
				result.AddToCounts(
					element.Character,
					element.Number,
					false,
					Math.Min(element.Number, Math.Max(0, element.WoundedNumber)),
					Math.Max(0, element.Xp),
					true,
					-1);
			}
		}
		return result;
	}

	internal static string GetCharacterKey(CharacterObject character)
	{
		return character?.HeroObject?.StringId ?? character?.StringId ?? "";
	}

	internal static bool IsEligibleGuestLeader(Hero hero)
	{
		try
		{
			MobileParty party = hero?.PartyBelongedTo;
			return SiegeCastleRosterSelectionProfile.ShouldIncludeArmyPartyLeader(
				hero == Hero.MainHero,
				hero?.CharacterObject?.IsHero == true,
				hero?.IsLord == true,
				party?.LeaderHero == hero,
				IsFriendlyToPlayer(party),
				hero?.IsAlive == true,
				hero?.IsPrisoner == true,
				hero?.IsWounded == true);
		}
		catch
		{
			return false;
		}
	}

	private static void TryFinalizeLiveWinningMapEvent(Settlement settlement)
	{
		try
		{
			MapEvent mapEvent = MapEvent.PlayerMapEvent ?? MobileParty.MainParty?.MapEvent;
			if (mapEvent != null
				&& mapEvent.HasWinner
				&& mapEvent.WinningSide == mapEvent.PlayerSide
				&& settlement?.IsCastle == true
				&& string.Equals(mapEvent.MapEventSettlement?.StringId ?? "", settlement.StringId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				FinalizePlayerCastleVictory(mapEvent, settlement);
			}
		}
		catch
		{
		}
	}

	private static void TrackBattle(MapEvent mapEvent, string source)
	{
		MapEventSide playerSide = mapEvent?.GetMapEventSide(mapEvent.PlayerSide);
		MapEventParty playerBattleParty = playerSide?.Parties?.FirstOrDefault(x => x?.Party == PartyBase.MainParty);
		if (playerBattleParty == null)
		{
			return;
		}

		_trackedMapEvent = mapEvent;
		_trackedPlayerBattleParty = playerBattleParty;
		TrackedAlliedBattleParties.Clear();
		foreach (MapEventParty battleParty in playerSide.Parties)
		{
			if (battleParty?.Party != null && battleParty != playerBattleParty)
			{
				TrackedAlliedBattleParties.Add(battleParty);
			}
		}
		_battleVictoryConfirmed = false;
		RefreshTrackedSnapshot(source);
	}

	private static void RefreshTrackedSnapshot(string source)
	{
		_battlePlayerPartyMembers = BuildHealthyPlayerBattleRoster(_trackedPlayerBattleParty);
		BattleGuestLeaders.Clear();
		foreach (MapEventParty battleParty in TrackedAlliedBattleParties)
		{
			TryAddGuestLeader(BattleGuestLeaders, battleParty?.Party?.LeaderHero, isFriendly: true);
		}
		_battleSettlementId = _trackedMapEvent?.MapEventSettlement?.StringId ?? "";
		_battleDay = GetCurrentCampaignDay();
		Logger.Log("CastleAftermath", "Refreshed player-party castle battle roster. Source=" + (source ?? "N/A")
			+ ", Settlement=" + (_battleSettlementId ?? "N/A")
			+ ", OwnHealthy=" + (_battlePlayerPartyMembers?.TotalManCount ?? 0)
			+ ", AlliedCaptains=" + BattleGuestLeaders.Count
			+ ", SideParties=" + (TrackedAlliedBattleParties.Count + 1));
	}

	private static TroopRoster BuildHealthyPlayerBattleRoster(MapEventParty battleParty)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (battleParty?.Troops == null)
		{
			return BuildHealthyPlayerPartyRoster(battleParty?.Party?.MemberRoster);
		}

		foreach (FlattenedTroopRosterElement element in battleParty.Troops)
		{
			CharacterObject character = element.Troop;
			if (element.State == RosterTroopState.Active && IsEligiblePlayerPartyMember(character))
			{
				result.AddToCounts(character, 1, false, 0, 0, true, -1);
			}
		}
		return result;
	}

	private static TroopRoster BuildHealthyPlayerPartyRoster(TroopRoster source)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (source == null)
		{
			return result;
		}
		foreach (TroopRosterElement element in source.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			if (!IsEligiblePlayerPartyMember(character) || element.Number <= 0)
			{
				continue;
			}
			int available = character.IsHero ? 1 : Math.Max(0, element.Number - element.WoundedNumber);
			if (available > 0)
			{
				result.AddToCounts(character, available, false, 0, 0, true, -1);
			}
		}
		return result;
	}

	private static TroopRoster ClampSnapshotToLiveMainParty(TroopRoster snapshot)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		TroopRoster live = MobileParty.MainParty?.MemberRoster;
		if (snapshot == null || live == null)
		{
			return result;
		}

		foreach (TroopRosterElement element in snapshot.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			int liveIndex = character == null ? -1 : live.FindIndexOfTroop(character);
			if (liveIndex < 0 || !IsEligiblePlayerPartyMember(character))
			{
				continue;
			}
			TroopRosterElement liveElement = live.GetElementCopyAtIndex(liveIndex);
			int liveAvailable = character.IsHero ? 1 : Math.Max(0, liveElement.Number - liveElement.WoundedNumber);
			int available = Math.Min(element.Number, liveAvailable);
			if (available > 0)
			{
				result.AddToCounts(character, available, false, 0, 0, true, -1);
			}
		}
		return result;
	}

	private static bool IsEligiblePlayerPartyMember(CharacterObject character)
	{
		try
		{
			Hero hero = character?.HeroObject;
			if (hero != null && hero.PartyBelongedTo != MobileParty.MainParty)
			{
				return false;
			}
			return SiegeCastleRosterSelectionProfile.ShouldIncludePlayerPartyMember(
				character == CharacterObject.PlayerCharacter || hero == Hero.MainHero,
				character?.IsHero == true,
				hero == null || hero.IsAlive,
				hero?.IsPrisoner == true,
				hero?.IsWounded == true);
		}
		catch
		{
			return false;
		}
	}

	private static void TryAddArmyGuestLeaders(Dictionary<string, Hero> result, Army army)
	{
		try
		{
			if (army?.Parties == null)
			{
				return;
			}
			foreach (MobileParty party in army.Parties)
			{
				if (party != null && party != MobileParty.MainParty)
				{
					TryAddGuestLeader(result, party.LeaderHero, isFriendly: true);
				}
			}
		}
		catch
		{
		}
	}

	private static void TryAddGuestLeader(Dictionary<string, Hero> result, Hero hero, bool isFriendly)
	{
		if (result == null || hero == null)
		{
			return;
		}
		try
		{
			MobileParty party = hero.PartyBelongedTo;
			bool eligible = SiegeCastleRosterSelectionProfile.ShouldIncludeArmyPartyLeader(
				hero == Hero.MainHero,
				hero.CharacterObject?.IsHero == true,
				hero.IsLord,
				party?.LeaderHero == hero,
				isFriendly,
				hero.IsAlive,
				hero.IsPrisoner,
				hero.IsWounded);
			string key = GetCharacterKey(hero.CharacterObject);
			if (eligible && !string.IsNullOrWhiteSpace(key))
			{
				result[key] = hero;
			}
		}
		catch
		{
		}
	}

	private static bool IsFriendlyToPlayer(MobileParty party)
	{
		try
		{
			if (party == null || party == MobileParty.MainParty)
			{
				return false;
			}
			IFaction playerFaction = MobileParty.MainParty?.MapFaction ?? Hero.MainHero?.MapFaction;
			IFaction otherFaction = party.MapFaction;
			return playerFaction != null
				&& otherFaction != null
				&& (playerFaction == otherFaction || !playerFaction.IsAtWarWith(otherFaction));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerCastleAssault(MapEvent mapEvent, bool requireVictory)
	{
		if (mapEvent == null
			|| !mapEvent.IsPlayerMapEvent
			|| mapEvent.PlayerSide != BattleSideEnum.Attacker
			|| mapEvent.MapEventSettlement?.IsCastle != true
			|| (!mapEvent.IsSiegeAssault && !mapEvent.IsSiegeOutside && !mapEvent.IsSallyOut))
		{
			return false;
		}
		return !requireVictory || (mapEvent.HasWinner && mapEvent.WinningSide == mapEvent.PlayerSide);
	}

	private static bool IsBattleSnapshotCurrent(Settlement settlement)
	{
		return _battleVictoryConfirmed
			&& settlement?.IsCastle == true
			&& _battleDay >= 0
			&& GetCurrentCampaignDay() - _battleDay >= 0
			&& GetCurrentCampaignDay() - _battleDay <= 1
			&& string.Equals(_battleSettlementId ?? "", settlement.StringId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private static Dictionary<string, int> BuildRosterCounts(TroopRoster roster)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (roster == null)
		{
			return result;
		}
		foreach (TroopRosterElement element in roster.GetTroopRoster())
		{
			string key = GetCharacterKey(element.Character);
			if (!string.IsNullOrWhiteSpace(key) && element.Number > 0)
			{
				result.TryGetValue(key, out int current);
				result[key] = current + element.Number;
			}
		}
		return result;
	}

	private static int CountHealthySelectableMainPartyMembers()
	{
		return BuildHealthyPlayerPartyRoster(MobileParty.MainParty?.MemberRoster).TotalManCount;
	}

	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}
}
