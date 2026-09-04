using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AFWarStatsTerminal.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AFWarStatsTerminal.Behaviors;

public sealed class AfWarStatsBehavior : CampaignBehaviorBase
{
    public enum WarAdvantage
    {
        Stalemate,
        Attacker,
        Defender
    }

    public sealed class HeroDeathEntry
    {
        public string HeroId { get; set; } = string.Empty;

        public string HeroName { get; set; } = string.Empty;

        public string KillerName { get; set; } = string.Empty;

        public KillCharacterAction.KillCharacterActionDetail Cause { get; set; }

        public int Day { get; set; }

        public string DateText { get; set; } = string.Empty;

        public string BattleName { get; set; } = string.Empty;
    }

    public sealed class WarEntry
    {
        public string PairKey { get; set; } = string.Empty;

        public string NameA { get; set; } = string.Empty;

        public string NameB { get; set; } = string.Empty;

        public Banner BannerA { get; set; }

        public Banner BannerB { get; set; }

        public Kingdom KingdomA { get; set; }

        public Kingdom KingdomB { get; set; }

        public int DurationDays { get; set; }

        public int StartDay { get; set; }

        public int TerritoryA { get; set; }

        public int TerritoryB { get; set; }

        public int KillsA { get; set; }

        public int KillsB { get; set; }

        public int CasualtiesA { get; set; }

        public int CasualtiesB { get; set; }

        public int WinsA { get; set; }

        public int WinsB { get; set; }

        public int LossesA { get; set; }

        public int LossesB { get; set; }

        public int WearinessA { get; set; }

        public int WearinessB { get; set; }

        public bool InvolvesPlayer { get; set; }
    }

    public sealed class HistoricalWarEntry
    {
        public string PairKey { get; set; } = string.Empty;

        public string NameA { get; set; } = string.Empty;

        public string NameB { get; set; } = string.Empty;

        public Banner BannerA { get; set; }

        public Banner BannerB { get; set; }

        public Kingdom KingdomA { get; set; }

        public Kingdom KingdomB { get; set; }

        public int DurationDays { get; set; }

        public int TerritoryA { get; set; }

        public int TerritoryB { get; set; }

        public int KillsA { get; set; }

        public int KillsB { get; set; }

        public int CasualtiesA { get; set; }

        public int CasualtiesB { get; set; }

        public int WinsA { get; set; }

        public int WinsB { get; set; }

        public int LossesA { get; set; }

        public int LossesB { get; set; }

        public int WearinessA { get; set; }

        public int WearinessB { get; set; }

        public int EndDay { get; set; }

        public int StartDay { get; set; }

        public string DateRangeText { get; set; } = string.Empty;

        public string EndDateText { get; set; } = string.Empty;

        public bool InvolvesPlayer { get; set; }

        public WarAdvantage Advantage { get; set; }

        public List<HeroDeathEntry> DeathsA { get; set; } = new();

        public List<HeroDeathEntry> DeathsB { get; set; } = new();

        public int TotalKills => KillsA + KillsB;

        public int TotalCasualties => CasualtiesA + CasualtiesB;
    }

    private class WarStatsRecord
    {
        public string NameA = string.Empty;

        public string NameB = string.Empty;

        public int KillsA;

        public int KillsB;

        public int CasualtiesA;

        public int CasualtiesB;

        public int WinsA;

        public int WinsB;

        public int LossesA;

        public int LossesB;

        public int InitialTerritoryA = -1;

        public int InitialTerritoryB = -1;

        public int LastDurationDays;

        public int LastTerritoryA;

        public int LastTerritoryB;

        public bool InvolvesPlayer;

        public int StartDay = -1;

        public int AttackerSide;

        public List<HeroDeathRecord> HeroDeaths = new();

        public Dictionary<string, RecentHeroBattleRecord> RecentHeroBattles = new(StringComparer.Ordinal);
    }

    private sealed class RecentHeroBattleRecord
    {
        public string HeroId = string.Empty;

        public int Day;

        public int Sequence;

        public string OwnKingdomId = string.Empty;
    }

    private sealed class HeroDeathRecord
    {
        public string HeroId = string.Empty;

        public string HeroName = string.Empty;

        public string KillerName = string.Empty;

        public int Cause;

        public int Day;

        public string BattleName = string.Empty;

        public int Side;
    }

    private sealed class HistoricalWarRecord : WarStatsRecord
    {
        public string PairKey = string.Empty;

        public int EndDay;
    }

    private sealed class LegacyPairRecord
    {
        public int InflictedByA;

        public int InflictedByB;
    }

    private const int CurrentDataVersion = 5;

    private readonly Dictionary<string, WarStatsRecord> _activeWars = new(StringComparer.Ordinal);

    private readonly List<HistoricalWarRecord> _historicalWars = new();

    private readonly Dictionary<string, LegacyPairRecord> _legacyRecords = new(StringComparer.Ordinal);

    private int _dataVersion;

    private bool _legacyMigrationPending;

    private int _recentBattleSequence;

    private List<string> _savedPairKeys = new();

    private List<int> _savedCasualtiesA = new();

    private List<int> _savedCasualtiesB = new();

    private List<string> _savedActiveKeysV2 = new();

    private List<string> _savedActiveNamesAV2 = new();

    private List<string> _savedActiveNamesBV2 = new();

    private List<int> _savedActiveKillsAV2 = new();

    private List<int> _savedActiveKillsBV2 = new();

    private List<int> _savedActiveCasualtiesAV2 = new();

    private List<int> _savedActiveCasualtiesBV2 = new();

    private List<int> _savedActiveDurationV2 = new();

    private List<int> _savedActiveTerritoryAV2 = new();

    private List<int> _savedActiveTerritoryBV2 = new();

    private List<int> _savedActivePlayerV2 = new();

    private List<string> _savedHistoryKeysV2 = new();

    private List<string> _savedHistoryNamesAV2 = new();

    private List<string> _savedHistoryNamesBV2 = new();

    private List<int> _savedHistoryKillsAV2 = new();

    private List<int> _savedHistoryKillsBV2 = new();

    private List<int> _savedHistoryCasualtiesAV2 = new();

    private List<int> _savedHistoryCasualtiesBV2 = new();

    private List<int> _savedHistoryDurationV2 = new();

    private List<int> _savedHistoryTerritoryAV2 = new();

    private List<int> _savedHistoryTerritoryBV2 = new();

    private List<int> _savedHistoryEndDayV2 = new();

    private List<int> _savedHistoryPlayerV2 = new();

    private List<int> _savedActiveWinsAV3 = new();

    private List<int> _savedActiveWinsBV3 = new();

    private List<int> _savedActiveLossesAV3 = new();

    private List<int> _savedActiveLossesBV3 = new();

    private List<int> _savedActiveInitialTerritoryAV3 = new();

    private List<int> _savedActiveInitialTerritoryBV3 = new();

    private List<int> _savedHistoryWinsAV3 = new();

    private List<int> _savedHistoryWinsBV3 = new();

    private List<int> _savedHistoryLossesAV3 = new();

    private List<int> _savedHistoryLossesBV3 = new();

    private List<int> _savedHistoryInitialTerritoryAV3 = new();

    private List<int> _savedHistoryInitialTerritoryBV3 = new();

    private List<int> _savedActiveStartDayV4 = new();

    private List<int> _savedActiveAttackerSideV4 = new();

    private List<string> _savedActiveHeroDeathsV4 = new();

    private List<int> _savedHistoryStartDayV4 = new();

    private List<int> _savedHistoryAttackerSideV4 = new();

    private List<string> _savedHistoryHeroDeathsV4 = new();

    private List<string> _savedActiveRecentHeroBattlesV5 = new();

    public static AfWarStatsBehavior Instance { get; private set; }

    public AfWarStatsBehavior()
    {
        Instance = this;
    }

    public override void RegisterEvents()
    {
        Instance = this;
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, OnBeforeHeroKilled);
    }

    public override void SyncData(IDataStore dataStore)
    {
        EnsureSaveLists();
        if (!dataStore.IsLoading)
        {
            ReconcileCurrentWars();
            PrepareSaveData();
            _dataVersion = CurrentDataVersion;
        }

        dataStore.SyncData("_af_war_stats_data_version_v2", ref _dataVersion);

        dataStore.SyncData("_af_war_stats_pair_keys_v1", ref _savedPairKeys);
        dataStore.SyncData("_af_war_stats_casualties_a_v1", ref _savedCasualtiesA);
        dataStore.SyncData("_af_war_stats_casualties_b_v1", ref _savedCasualtiesB);

        dataStore.SyncData("_af_war_stats_active_keys_v2", ref _savedActiveKeysV2);
        dataStore.SyncData("_af_war_stats_active_names_a_v2", ref _savedActiveNamesAV2);
        dataStore.SyncData("_af_war_stats_active_names_b_v2", ref _savedActiveNamesBV2);
        dataStore.SyncData("_af_war_stats_active_kills_a_v2", ref _savedActiveKillsAV2);
        dataStore.SyncData("_af_war_stats_active_kills_b_v2", ref _savedActiveKillsBV2);
        dataStore.SyncData("_af_war_stats_active_casualties_a_v2", ref _savedActiveCasualtiesAV2);
        dataStore.SyncData("_af_war_stats_active_casualties_b_v2", ref _savedActiveCasualtiesBV2);
        dataStore.SyncData("_af_war_stats_active_duration_v2", ref _savedActiveDurationV2);
        dataStore.SyncData("_af_war_stats_active_territory_a_v2", ref _savedActiveTerritoryAV2);
        dataStore.SyncData("_af_war_stats_active_territory_b_v2", ref _savedActiveTerritoryBV2);
        dataStore.SyncData("_af_war_stats_active_player_v2", ref _savedActivePlayerV2);

        dataStore.SyncData("_af_war_stats_history_keys_v2", ref _savedHistoryKeysV2);
        dataStore.SyncData("_af_war_stats_history_names_a_v2", ref _savedHistoryNamesAV2);
        dataStore.SyncData("_af_war_stats_history_names_b_v2", ref _savedHistoryNamesBV2);
        dataStore.SyncData("_af_war_stats_history_kills_a_v2", ref _savedHistoryKillsAV2);
        dataStore.SyncData("_af_war_stats_history_kills_b_v2", ref _savedHistoryKillsBV2);
        dataStore.SyncData("_af_war_stats_history_casualties_a_v2", ref _savedHistoryCasualtiesAV2);
        dataStore.SyncData("_af_war_stats_history_casualties_b_v2", ref _savedHistoryCasualtiesBV2);
        dataStore.SyncData("_af_war_stats_history_duration_v2", ref _savedHistoryDurationV2);
        dataStore.SyncData("_af_war_stats_history_territory_a_v2", ref _savedHistoryTerritoryAV2);
        dataStore.SyncData("_af_war_stats_history_territory_b_v2", ref _savedHistoryTerritoryBV2);
        dataStore.SyncData("_af_war_stats_history_end_day_v2", ref _savedHistoryEndDayV2);
        dataStore.SyncData("_af_war_stats_history_player_v2", ref _savedHistoryPlayerV2);

        dataStore.SyncData("_af_war_stats_active_wins_a_v3", ref _savedActiveWinsAV3);
        dataStore.SyncData("_af_war_stats_active_wins_b_v3", ref _savedActiveWinsBV3);
        dataStore.SyncData("_af_war_stats_active_losses_a_v3", ref _savedActiveLossesAV3);
        dataStore.SyncData("_af_war_stats_active_losses_b_v3", ref _savedActiveLossesBV3);
        dataStore.SyncData("_af_war_stats_active_initial_territory_a_v3", ref _savedActiveInitialTerritoryAV3);
        dataStore.SyncData("_af_war_stats_active_initial_territory_b_v3", ref _savedActiveInitialTerritoryBV3);
        dataStore.SyncData("_af_war_stats_history_wins_a_v3", ref _savedHistoryWinsAV3);
        dataStore.SyncData("_af_war_stats_history_wins_b_v3", ref _savedHistoryWinsBV3);
        dataStore.SyncData("_af_war_stats_history_losses_a_v3", ref _savedHistoryLossesAV3);
        dataStore.SyncData("_af_war_stats_history_losses_b_v3", ref _savedHistoryLossesBV3);
        dataStore.SyncData("_af_war_stats_history_initial_territory_a_v3", ref _savedHistoryInitialTerritoryAV3);
        dataStore.SyncData("_af_war_stats_history_initial_territory_b_v3", ref _savedHistoryInitialTerritoryBV3);

        dataStore.SyncData("_af_war_stats_active_start_day_v4", ref _savedActiveStartDayV4);
        dataStore.SyncData("_af_war_stats_active_attacker_side_v4", ref _savedActiveAttackerSideV4);
        dataStore.SyncData("_af_war_stats_active_hero_deaths_v4", ref _savedActiveHeroDeathsV4);
        dataStore.SyncData("_af_war_stats_history_start_day_v4", ref _savedHistoryStartDayV4);
        dataStore.SyncData("_af_war_stats_history_attacker_side_v4", ref _savedHistoryAttackerSideV4);
        dataStore.SyncData("_af_war_stats_history_hero_deaths_v4", ref _savedHistoryHeroDeathsV4);

        dataStore.SyncData("_af_war_stats_recent_battle_sequence_v5", ref _recentBattleSequence);
        dataStore.SyncData("_af_war_stats_active_recent_hero_battles_v5", ref _savedActiveRecentHeroBattlesV5);

        if (dataStore.IsLoading)
        {
            _recentBattleSequence = Math.Max(0, _recentBattleSequence);
            LoadSavedData();
        }
    }

    public List<WarEntry> BuildCurrentWars()
    {
        ReconcileCurrentWars();
        Dictionary<string, (Kingdom A, Kingdom B)> currentPairs = GatherCurrentWarPairs();
        string playerKingdomId = GetPlayerKingdomId();
        List<WarEntry> entries = new(currentPairs.Count);

        foreach (KeyValuePair<string, (Kingdom A, Kingdom B)> pair in currentPairs)
        {
            Kingdom kingdomA = pair.Value.A;
            Kingdom kingdomB = pair.Value.B;
            WarStatsRecord record = GetOrCreateActiveRecord(pair.Key, kingdomA, kingdomB);
            UpdateRecordMetadata(record, kingdomA, kingdomB);
            int durationDays = GetWarDurationDays(kingdomA, kingdomB);
            int pairTerritoryA = CountTerritory(kingdomA);
            int pairTerritoryB = CountTerritory(kingdomB);
            int pairWearinessA = CalculateWeariness(durationDays, record.CasualtiesA, record.WinsA, record.LossesA, record.InitialTerritoryA, pairTerritoryA);
            int pairWearinessB = CalculateWeariness(durationDays, record.CasualtiesB, record.WinsB, record.LossesB, record.InitialTerritoryB, pairTerritoryB);
            bool swapSides = record.AttackerSide == 1;
            Kingdom attacker = swapSides ? kingdomB : kingdomA;
            Kingdom defender = swapSides ? kingdomA : kingdomB;
            entries.Add(new WarEntry
            {
                PairKey = pair.Key,
                NameA = GetKingdomName(attacker),
                NameB = GetKingdomName(defender),
                BannerA = attacker.Banner,
                BannerB = defender.Banner,
                KingdomA = attacker,
                KingdomB = defender,
                DurationDays = durationDays,
                StartDay = record.StartDay >= 0 ? record.StartDay : Math.Max(0, GetCurrentDay() - durationDays),
                TerritoryA = swapSides ? pairTerritoryB : pairTerritoryA,
                TerritoryB = swapSides ? pairTerritoryA : pairTerritoryB,
                KillsA = swapSides ? record.KillsB : record.KillsA,
                KillsB = swapSides ? record.KillsA : record.KillsB,
                CasualtiesA = swapSides ? record.CasualtiesB : record.CasualtiesA,
                CasualtiesB = swapSides ? record.CasualtiesA : record.CasualtiesB,
                WinsA = swapSides ? record.WinsB : record.WinsA,
                WinsB = swapSides ? record.WinsA : record.WinsB,
                LossesA = swapSides ? record.LossesB : record.LossesA,
                LossesB = swapSides ? record.LossesA : record.LossesB,
                WearinessA = swapSides ? pairWearinessB : pairWearinessA,
                WearinessB = swapSides ? pairWearinessA : pairWearinessB,
                InvolvesPlayer = IsPlayerKingdom(attacker, playerKingdomId) || IsPlayerKingdom(defender, playerKingdomId)
            });
        }

        entries.Sort(static (left, right) =>
        {
            int duration = right.DurationDays.CompareTo(left.DurationDays);
            if (duration != 0)
            {
                return duration;
            }

            int casualties = (right.CasualtiesA + right.CasualtiesB).CompareTo(left.CasualtiesA + left.CasualtiesB);
            return casualties != 0
                ? casualties
                : string.Compare(left.NameA + left.NameB, right.NameA + right.NameB, StringComparison.Ordinal);
        });

        return entries;
    }

    public List<HistoricalWarEntry> BuildHistoricalWars()
    {
        ReconcileCurrentWars();
        List<HistoricalWarEntry> entries = new(_historicalWars.Count);
        foreach (HistoricalWarRecord record in _historicalWars)
        {
            TryResolvePair(record.PairKey, out Kingdom kingdomA, out Kingdom kingdomB);
            bool swapSides = record.AttackerSide == 1;
            Kingdom attacker = swapSides ? kingdomB : kingdomA;
            Kingdom defender = swapSides ? kingdomA : kingdomB;
            int pairWearinessA = CalculateWeariness(record.LastDurationDays, record.CasualtiesA, record.WinsA, record.LossesA, record.InitialTerritoryA, record.LastTerritoryA);
            int pairWearinessB = CalculateWeariness(record.LastDurationDays, record.CasualtiesB, record.WinsB, record.LossesB, record.InitialTerritoryB, record.LastTerritoryB);
            int startDay = ResolveHistoryStartDay(record);
            int pairAdvantageSide = CalculateAdvantageSide(record);
            entries.Add(new HistoricalWarEntry
            {
                PairKey = record.PairKey,
                NameA = ResolveHistoricalName(swapSides ? record.NameB : record.NameA, attacker),
                NameB = ResolveHistoricalName(swapSides ? record.NameA : record.NameB, defender),
                BannerA = attacker?.Banner,
                BannerB = defender?.Banner,
                KingdomA = attacker,
                KingdomB = defender,
                DurationDays = record.LastDurationDays,
                TerritoryA = swapSides ? record.LastTerritoryB : record.LastTerritoryA,
                TerritoryB = swapSides ? record.LastTerritoryA : record.LastTerritoryB,
                KillsA = swapSides ? record.KillsB : record.KillsA,
                KillsB = swapSides ? record.KillsA : record.KillsB,
                CasualtiesA = swapSides ? record.CasualtiesB : record.CasualtiesA,
                CasualtiesB = swapSides ? record.CasualtiesA : record.CasualtiesB,
                WinsA = swapSides ? record.WinsB : record.WinsA,
                WinsB = swapSides ? record.WinsA : record.WinsB,
                LossesA = swapSides ? record.LossesB : record.LossesA,
                LossesB = swapSides ? record.LossesA : record.LossesB,
                WearinessA = swapSides ? pairWearinessB : pairWearinessA,
                WearinessB = swapSides ? pairWearinessA : pairWearinessB,
                StartDay = startDay,
                EndDay = record.EndDay,
                EndDateText = record.EndDay > 0 ? CampaignTime.Days(record.EndDay).ToString() : AfWarStatsTexts.LegacyRecord,
                DateRangeText = FormatDateRange(startDay, record.EndDay),
                InvolvesPlayer = record.InvolvesPlayer,
                Advantage = ResolveOrientedAdvantage(pairAdvantageSide, swapSides),
                DeathsA = BuildHeroDeathEntries(record, record.AttackerSide),
                DeathsB = BuildHeroDeathEntries(record, record.AttackerSide == 0 ? 1 : 0)
            });
        }

        entries.Sort(static (left, right) =>
        {
            int ended = right.EndDay.CompareTo(left.EndDay);
            if (ended != 0)
            {
                return ended;
            }

            int casualties = right.TotalCasualties.CompareTo(left.TotalCasualties);
            return casualties != 0
                ? casualties
                : string.Compare(left.NameA + left.NameB, right.NameA + right.NameB, StringComparison.Ordinal);
        });
        return entries;
    }

    public void ClearAllRecords()
    {
        ReconcileCurrentWars();
        foreach (KeyValuePair<string, WarStatsRecord> item in _activeWars)
        {
            WarStatsRecord record = item.Value;
            record.KillsA = 0;
            record.KillsB = 0;
            record.CasualtiesA = 0;
            record.CasualtiesB = 0;
            record.WinsA = 0;
            record.WinsB = 0;
            record.LossesA = 0;
            record.LossesB = 0;
            record.HeroDeaths.Clear();
            record.RecentHeroBattles?.Clear();
            if (TryResolvePair(item.Key, out Kingdom kingdomA, out Kingdom kingdomB))
            {
                record.InitialTerritoryA = CountTerritory(kingdomA);
                record.InitialTerritoryB = CountTerritory(kingdomB);
                UpdateRecordMetadata(record, kingdomA, kingdomB);
            }
        }

        _historicalWars.Clear();
        _legacyRecords.Clear();
        _legacyMigrationPending = false;
        _recentBattleSequence = 0;
        _dataVersion = CurrentDataVersion;
    }

    public int DeleteHistoricalWars(IEnumerable<HistoricalWarEntry> entries)
    {
        if (entries == null)
        {
            return 0;
        }

        HashSet<string> identities = new(
            entries
                .Where(static entry => entry != null)
                .Select(static entry => MakeHistoricalIdentity(entry.PairKey, entry.StartDay, entry.EndDay)),
            StringComparer.Ordinal);
        if (identities.Count == 0)
        {
            return 0;
        }

        int removed = _historicalWars.RemoveAll(record => identities.Contains(
            MakeHistoricalIdentity(record.PairKey, ResolveHistoryStartDay(record), record.EndDay)));
        if (removed > 0)
        {
            _dataVersion = CurrentDataVersion;
        }

        return removed;
    }

    private void OnDailyTick()
    {
        ReconcileCurrentWars();
    }

    private void OnWarDeclared(
        IFaction factionOne,
        IFaction factionTwo,
        DeclareWarAction.DeclareWarDetail detail)
    {
        if (factionOne is not Kingdom attacker || factionTwo is not Kingdom defender || attacker == defender)
        {
            return;
        }

        string pairKey = MakePairKey(attacker, defender);
        if (string.IsNullOrEmpty(pairKey) || !TryResolvePair(pairKey, out Kingdom kingdomA, out Kingdom kingdomB))
        {
            return;
        }

        WarStatsRecord record = GetOrCreateActiveRecord(pairKey, kingdomA, kingdomB);
        record.RecentHeroBattles?.Clear();
        record.AttackerSide = string.Equals(attacker.StringId, kingdomA.StringId, StringComparison.Ordinal) ? 0 : 1;
        record.StartDay = GetCurrentDay();
        UpdateRecordMetadata(record, kingdomA, kingdomB);
    }

    private void OnBeforeHeroKilled(
        Hero victim,
        Hero killer,
        KillCharacterAction.KillCharacterActionDetail detail,
        bool showNotification)
    {
        if (victim == null || !victim.IsLord || !IsRecordableDeath(detail))
        {
            return;
        }

        Kingdom victimKingdom = GetKingdom(victim);
        if (victimKingdom == null)
        {
            return;
        }

        int deathDay = GetCurrentDay();
        if (HasRecordedHeroDeath(victim))
        {
            // MapEventEnded runs before the engine's final kill action callbacks.
            // Preserve the more accurate opponent and killer captured from the map event.
            return;
        }

        // The engine can replace a battlefield killer with the opposing party leader. The map-event pass
        // is the authoritative source; if it had no usable death row, use only the lord's latest actual
        // participation to decide the archive. This internal choice is never shown as the place of death.
        ReconcileCurrentWars();
        if (!TryResolveMostRecentHeroBattle(
                victim,
                victimKingdom,
                out WarStatsRecord recentRecord,
                out Kingdom recentKingdomA,
                out _))
        {
            return;
        }

        int recentVictimSide = string.Equals(victimKingdom.StringId, recentKingdomA.StringId, StringComparison.Ordinal) ? 0 : 1;
        UpsertHeroDeath(
            recentRecord,
            victim,
            IsBattleDeath(detail) ? null : killer,
            detail,
            deathDay,
            string.Empty,
            recentVictimSide);
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        if (mapEvent == null)
        {
            return;
        }

        TrackRecentHeroBattles(mapEvent);
        CaptureHeroDeaths(mapEvent);
        AccumulateBattleStats(mapEvent);
        ReconcileCurrentWars();
    }

    private void CaptureHeroDeaths(MapEvent mapEvent)
    {
        string battleName = ResolveBattleName(mapEvent);
        CaptureHeroDeathsOnSide(mapEvent.AttackerSide, mapEvent.DefenderSide, battleName);
        CaptureHeroDeathsOnSide(mapEvent.DefenderSide, mapEvent.AttackerSide, battleName);
    }

    private void CaptureHeroDeathsOnSide(MapEventSide victimSide, MapEventSide opposingSide, string battleName)
    {
        if (victimSide?.Parties == null || opposingSide == null)
        {
            return;
        }

        HashSet<Kingdom> opposingKingdoms = ResolveKingdomsOnSide(opposingSide);
        Dictionary<Kingdom, (int Contribution, int Participants)> opposingPriorities = BuildOpponentPriorities(opposingSide);
        foreach (MapEventParty eventParty in victimSide.Parties)
        {
            Kingdom victimKingdom = GetKingdom(eventParty?.Party);
            TroopRoster deadRoster = eventParty?.DiedInBattle;
            if (victimKingdom == null || deadRoster == null)
            {
                continue;
            }

            foreach (TroopRosterElement element in deadRoster.GetTroopRoster())
            {
                Hero victim = element.Character?.HeroObject;
                if (victim == null || !victim.IsLord || HasRecordedHeroDeath(victim))
                {
                    continue;
                }

                Hero killer = victim.DeathMarkKillerHero;
                Kingdom killerKingdom = GetKingdom(killer);
                Kingdom opponent = killerKingdom != null
                    && opposingKingdoms.Contains(killerKingdom)
                    && IsTrackedOpponent(victimKingdom, killerKingdom)
                    ? killerKingdom
                    : ResolvePrimaryOpponent(victimKingdom, opposingKingdoms, opposingPriorities);
                if (opponent == null || opponent == victimKingdom)
                {
                    continue;
                }

                if (!TryResolveActiveWar(
                        victimKingdom,
                        opponent,
                        out WarStatsRecord record,
                        out Kingdom kingdomA,
                        out _))
                {
                    continue;
                }

                int pairSide = string.Equals(victimKingdom.StringId, kingdomA.StringId, StringComparison.Ordinal) ? 0 : 1;
                KillCharacterAction.KillCharacterActionDetail cause = victim.DeathMark;
                if (!IsRecordableDeath(cause))
                {
                    cause = KillCharacterAction.KillCharacterActionDetail.DiedInBattle;
                }

                UpsertHeroDeath(record, victim, killer, cause, GetCurrentDay(), battleName, pairSide);
            }
        }
    }

    private Kingdom ResolvePrimaryOpponent(
        Kingdom ownKingdom,
        HashSet<Kingdom> opposingKingdoms,
        Dictionary<Kingdom, (int Contribution, int Participants)> opposingPriorities)
    {
        if (ownKingdom == null || opposingKingdoms == null || opposingKingdoms.Count == 0)
        {
            return null;
        }

        return opposingKingdoms
            .Where(kingdom => IsTrackedOpponent(ownKingdom, kingdom))
            .OrderByDescending(kingdom => opposingPriorities.TryGetValue(kingdom, out var priority) ? priority.Contribution : 0)
            .ThenByDescending(kingdom => opposingPriorities.TryGetValue(kingdom, out var priority) ? priority.Participants : 0)
            .ThenBy(kingdom => kingdom.StringId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private bool HasRecordedHeroDeath(Hero victim)
    {
        if (victim == null)
        {
            return false;
        }

        string heroId = victim.StringId ?? string.Empty;
        string heroName = victim.Name?.ToString() ?? heroId;
        return _activeWars.Values
            .Concat<WarStatsRecord>(_historicalWars)
            .Any(record => record?.HeroDeaths != null
                && record.HeroDeaths.Any(item => item != null && IsSameHero(item, heroId, heroName)));
    }

    private static bool IsSameHero(HeroDeathRecord item, string heroId, string heroName)
    {
        return item != null
            && ((!string.IsNullOrEmpty(heroId) && string.Equals(item.HeroId, heroId, StringComparison.Ordinal))
                || (string.IsNullOrEmpty(heroId) && string.Equals(item.HeroName, heroName, StringComparison.Ordinal)));
    }

    private static void UpsertHeroDeath(
        WarStatsRecord record,
        Hero victim,
        Hero killer,
        KillCharacterAction.KillCharacterActionDetail cause,
        int day,
        string battleName,
        int pairSide)
    {
        if (record == null || victim == null)
        {
            return;
        }

        record.HeroDeaths ??= new List<HeroDeathRecord>();
        string heroId = victim.StringId ?? string.Empty;
        string heroName = victim.Name?.ToString() ?? heroId;
        HeroDeathRecord existing = record.HeroDeaths.FirstOrDefault(item => IsSameHero(item, heroId, heroName));

        if (existing == null)
        {
            existing = new HeroDeathRecord
            {
                HeroId = heroId,
                HeroName = heroName,
                Day = Math.Max(0, day),
                Side = pairSide == 1 ? 1 : 0
            };
            record.HeroDeaths.Add(existing);
        }

        existing.HeroName = string.IsNullOrWhiteSpace(heroName) ? existing.HeroName : heroName;
        existing.KillerName = killer?.Name?.ToString() ?? existing.KillerName ?? string.Empty;
        existing.Cause = (int)cause;
        existing.Side = pairSide == 1 ? 1 : 0;
        if (!string.IsNullOrWhiteSpace(battleName))
        {
            existing.BattleName = battleName;
        }
    }

    private static bool IsRecordableDeath(KillCharacterAction.KillCharacterActionDetail detail)
    {
        return detail == KillCharacterAction.KillCharacterActionDetail.Murdered
            || detail == KillCharacterAction.KillCharacterActionDetail.DiedInLabor
            || detail == KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge
            || detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle
            || detail == KillCharacterAction.KillCharacterActionDetail.WoundedInBattle
            || detail == KillCharacterAction.KillCharacterActionDetail.Executed
            || detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent
            || detail == KillCharacterAction.KillCharacterActionDetail.Lost;
    }

    private static bool IsBattleDeath(KillCharacterAction.KillCharacterActionDetail detail)
    {
        return detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle;
    }

    private bool IsTrackedOpponent(Kingdom ownKingdom, Kingdom opponent)
    {
        if (ownKingdom == null || opponent == null || ownKingdom == opponent)
        {
            return false;
        }

        string pairKey = MakePairKey(ownKingdom, opponent);
        return !string.IsNullOrEmpty(pairKey)
            && (_activeWars.ContainsKey(pairKey) || ownKingdom.IsAtWarWith(opponent));
    }

    private bool TryResolveActiveWar(
        Kingdom first,
        Kingdom second,
        out WarStatsRecord record,
        out Kingdom kingdomA,
        out Kingdom kingdomB)
    {
        record = null;
        kingdomA = null;
        kingdomB = null;
        if (!IsTrackedOpponent(first, second))
        {
            return false;
        }

        string pairKey = MakePairKey(first, second);
        if (!TryResolvePair(pairKey, out kingdomA, out kingdomB))
        {
            return false;
        }

        record = GetOrCreateActiveRecord(pairKey, kingdomA, kingdomB);
        return record != null;
    }

    private bool TryResolveMostRecentHeroBattle(
        Hero victim,
        Kingdom victimKingdom,
        out WarStatsRecord selectedRecord,
        out Kingdom selectedKingdomA,
        out RecentHeroBattleRecord selectedBattle)
    {
        selectedRecord = null;
        selectedKingdomA = null;
        selectedBattle = null;
        string heroId = victim?.StringId ?? string.Empty;
        if (victimKingdom == null || string.IsNullOrWhiteSpace(heroId))
        {
            return false;
        }

        int bestSequence = -1;
        int bestDay = -1;
        string bestPairKey = string.Empty;
        foreach (KeyValuePair<string, WarStatsRecord> pair in _activeWars)
        {
            WarStatsRecord candidateRecord = pair.Value;
            if (candidateRecord?.RecentHeroBattles == null
                || !candidateRecord.RecentHeroBattles.TryGetValue(heroId, out RecentHeroBattleRecord candidateBattle)
                || candidateBattle == null
                || !TryResolvePair(pair.Key, out Kingdom kingdomA, out Kingdom kingdomB))
            {
                continue;
            }

            if (!string.Equals(candidateBattle.OwnKingdomId, victimKingdom.StringId, StringComparison.Ordinal))
            {
                continue;
            }

            bool victimIsA = string.Equals(victimKingdom.StringId, kingdomA.StringId, StringComparison.Ordinal);
            bool victimIsB = string.Equals(victimKingdom.StringId, kingdomB.StringId, StringComparison.Ordinal);
            if (!victimIsA && !victimIsB)
            {
                continue;
            }

            Kingdom opponent = victimIsA ? kingdomB : kingdomA;
            if (!victimKingdom.IsAtWarWith(opponent)
                || candidateBattle.Day < Math.Max(0, candidateRecord.StartDay))
            {
                continue;
            }

            bool isNewer = candidateBattle.Sequence > bestSequence
                || (candidateBattle.Sequence == bestSequence && candidateBattle.Day > bestDay)
                || (candidateBattle.Sequence == bestSequence
                    && candidateBattle.Day == bestDay
                    && string.CompareOrdinal(pair.Key, bestPairKey) < 0);
            if (!isNewer)
            {
                continue;
            }

            selectedRecord = candidateRecord;
            selectedKingdomA = kingdomA;
            selectedBattle = candidateBattle;
            bestSequence = candidateBattle.Sequence;
            bestDay = candidateBattle.Day;
            bestPairKey = pair.Key;
        }

        return selectedRecord != null && selectedKingdomA != null && selectedBattle != null;
    }

    private void TrackRecentHeroBattles(MapEvent mapEvent)
    {
        HashSet<Kingdom> attackers = ResolveKingdomsOnSide(mapEvent?.AttackerSide);
        HashSet<Kingdom> defenders = ResolveKingdomsOnSide(mapEvent?.DefenderSide);
        if (attackers.Count == 0 || defenders.Count == 0)
        {
            return;
        }

        if (_recentBattleSequence < int.MaxValue)
        {
            _recentBattleSequence++;
        }

        int battleSequence = _recentBattleSequence;
        int battleDay = GetCurrentDay();
        Dictionary<Kingdom, (int Contribution, int Participants)> attackerPriorities = BuildOpponentPriorities(mapEvent.AttackerSide);
        Dictionary<Kingdom, (int Contribution, int Participants)> defenderPriorities = BuildOpponentPriorities(mapEvent.DefenderSide);
        TrackRecentHeroBattlesOnSide(mapEvent.AttackerSide, defenders, defenderPriorities, battleDay, battleSequence);
        TrackRecentHeroBattlesOnSide(mapEvent.DefenderSide, attackers, attackerPriorities, battleDay, battleSequence);
    }

    private void TrackRecentHeroBattlesOnSide(
        MapEventSide ownSide,
        HashSet<Kingdom> opposingKingdoms,
        Dictionary<Kingdom, (int Contribution, int Participants)> opposingPriorities,
        int battleDay,
        int battleSequence)
    {
        if (ownSide?.Parties == null)
        {
            return;
        }

        foreach (MapEventParty eventParty in ownSide.Parties)
        {
            Kingdom ownKingdom = GetKingdom(eventParty?.Party);
            Kingdom opponent = ResolvePrimaryOpponent(ownKingdom, opposingKingdoms, opposingPriorities);
            if (ownKingdom == null
                || opponent == null
                || !TryResolveActiveWar(ownKingdom, opponent, out WarStatsRecord record, out _, out _))
            {
                continue;
            }

            record.RecentHeroBattles ??= new Dictionary<string, RecentHeroBattleRecord>(StringComparer.Ordinal);
            foreach (Hero hero in ResolveParticipatingLords(eventParty))
            {
                if (GetKingdom(hero) != ownKingdom)
                {
                    continue;
                }

                string heroId = hero.StringId ?? string.Empty;
                if (string.IsNullOrWhiteSpace(heroId))
                {
                    continue;
                }

                record.RecentHeroBattles[heroId] = new RecentHeroBattleRecord
                {
                    HeroId = heroId,
                    Day = Math.Max(0, battleDay),
                    Sequence = Math.Max(0, battleSequence),
                    OwnKingdomId = ownKingdom.StringId ?? string.Empty
                };
            }
        }
    }

    private static HashSet<Hero> ResolveParticipatingLords(MapEventParty eventParty)
    {
        HashSet<Hero> result = new();
        if (eventParty?.Troops != null)
        {
            foreach (FlattenedTroopRosterElement element in eventParty.Troops)
            {
                if (element.State != RosterTroopState.Wounded)
                {
                    AddParticipatingLord(result, element.Troop?.HeroObject);
                }
            }
        }

        AddParticipatingLordsFromRoster(result, eventParty?.DiedInBattle);
        AddParticipatingLordsFromRoster(result, eventParty?.WoundedInBattle);
        return result;
    }

    private static void AddParticipatingLordsFromRoster(HashSet<Hero> result, TroopRoster roster)
    {
        if (result == null || roster == null)
        {
            return;
        }

        foreach (TroopRosterElement element in roster.GetTroopRoster())
        {
            AddParticipatingLord(result, element.Character?.HeroObject);
        }
    }

    private static void AddParticipatingLord(HashSet<Hero> result, Hero hero)
    {
        if (result != null && hero?.IsLord == true && !string.IsNullOrWhiteSpace(hero.StringId))
        {
            result.Add(hero);
        }
    }

    private static Dictionary<Kingdom, (int Contribution, int Participants)> BuildOpponentPriorities(MapEventSide side)
    {
        Dictionary<Kingdom, (int Contribution, int Participants)> result = new();
        if (side?.Parties == null)
        {
            return result;
        }

        foreach (MapEventParty eventParty in side.Parties)
        {
            Kingdom kingdom = GetKingdom(eventParty?.Party);
            if (kingdom == null || kingdom.IsEliminated)
            {
                continue;
            }

            int contribution = Math.Max(0, eventParty.ContributionToBattle);
#if !BANNERLORD_1_4_OR_GREATER
            int participants = Math.Max(0, eventParty.HealthyManCountAtStart);
#else
            int participants = eventParty.ParticipatingTroopCount >= 0
                ? eventParty.ParticipatingTroopCount
                : Math.Max(0, eventParty.HealthyManCountAtStart);
#endif
            (int Contribution, int Participants) current = result.TryGetValue(kingdom, out var existing)
                ? existing
                : (0, 0);
            result[kingdom] = (
                current.Contribution + contribution,
                current.Participants + Math.Max(0, participants));
        }

        return result;
    }

    private static string ResolveBattleName(MapEvent mapEvent)
    {
        if (mapEvent == null)
        {
            return string.Empty;
        }

        try
        {
            string localizedName = mapEvent.GetName()?.ToString();
            if (!string.IsNullOrWhiteSpace(localizedName))
            {
                return localizedName;
            }
        }
        catch
        {
        }

        try
        {
            return mapEvent.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void ReconcileCurrentWars()
    {
        if (Campaign.Current == null)
        {
            return;
        }

        Dictionary<string, (Kingdom A, Kingdom B)> currentPairs = GatherCurrentWarPairs();
        if (_legacyMigrationPending)
        {
            MigrateLegacyRecords(currentPairs);
        }

        foreach (KeyValuePair<string, (Kingdom A, Kingdom B)> pair in currentPairs)
        {
            WarStatsRecord record = GetOrCreateActiveRecord(pair.Key, pair.Value.A, pair.Value.B);
            UpdateRecordMetadata(record, pair.Value.A, pair.Value.B);
        }

        List<string> endedPairs = _activeWars.Keys.Where(key => !currentPairs.ContainsKey(key)).ToList();
        foreach (string pairKey in endedPairs)
        {
            WarStatsRecord record = _activeWars[pairKey];
            if (TryResolvePair(pairKey, out Kingdom kingdomA, out Kingdom kingdomB))
            {
                record.NameA = GetKingdomName(kingdomA);
                record.NameB = GetKingdomName(kingdomB);
                record.LastTerritoryA = CountTerritory(kingdomA);
                record.LastTerritoryB = CountTerritory(kingdomB);
                string playerKingdomId = GetPlayerKingdomId();
                record.InvolvesPlayer |= IsPlayerKingdom(kingdomA, playerKingdomId) || IsPlayerKingdom(kingdomB, playerKingdomId);
            }

            ArchiveEndedWar(pairKey, record);
            _activeWars.Remove(pairKey);
        }
    }

    private void MigrateLegacyRecords(Dictionary<string, (Kingdom A, Kingdom B)> currentPairs)
    {
        foreach (KeyValuePair<string, LegacyPairRecord> legacy in _legacyRecords)
        {
            TryResolvePair(legacy.Key, out Kingdom kingdomA, out Kingdom kingdomB);
            WarStatsRecord migrated = new()
            {
                NameA = GetKingdomName(kingdomA),
                NameB = GetKingdomName(kingdomB),
                KillsA = 0,
                KillsB = 0,
                CasualtiesA = Math.Max(0, legacy.Value.InflictedByB),
                CasualtiesB = Math.Max(0, legacy.Value.InflictedByA),
                InitialTerritoryA = CountTerritory(kingdomA),
                InitialTerritoryB = CountTerritory(kingdomB),
                LastDurationDays = kingdomA != null && kingdomB != null && kingdomA.IsAtWarWith(kingdomB) ? GetWarDurationDays(kingdomA, kingdomB) : 0,
                LastTerritoryA = CountTerritory(kingdomA),
                LastTerritoryB = CountTerritory(kingdomB),
                InvolvesPlayer = IsPlayerKingdom(kingdomA, GetPlayerKingdomId()) || IsPlayerKingdom(kingdomB, GetPlayerKingdomId()),
                StartDay = ResolveCurrentWarStartDay(kingdomA, kingdomB),
                AttackerSide = 0
            };

            if (currentPairs.ContainsKey(legacy.Key))
            {
                _activeWars[legacy.Key] = migrated;
            }
            else
            {
                _historicalWars.Add(ToHistoricalRecord(legacy.Key, migrated, 0));
            }
        }

        _legacyRecords.Clear();
        _legacyMigrationPending = false;
        _dataVersion = CurrentDataVersion;
    }

    private void ArchiveEndedWar(string pairKey, WarStatsRecord record)
    {
        int endDay = Campaign.Current == null ? 0 : Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
        _historicalWars.Add(ToHistoricalRecord(pairKey, record, endDay));
    }

    private static HistoricalWarRecord ToHistoricalRecord(string pairKey, WarStatsRecord record, int endDay)
    {
        return new HistoricalWarRecord
        {
            PairKey = pairKey,
            NameA = record.NameA,
            NameB = record.NameB,
            KillsA = record.KillsA,
            KillsB = record.KillsB,
            CasualtiesA = record.CasualtiesA,
            CasualtiesB = record.CasualtiesB,
            WinsA = record.WinsA,
            WinsB = record.WinsB,
            LossesA = record.LossesA,
            LossesB = record.LossesB,
            InitialTerritoryA = record.InitialTerritoryA,
            InitialTerritoryB = record.InitialTerritoryB,
            LastDurationDays = record.LastDurationDays,
            LastTerritoryA = record.LastTerritoryA,
            LastTerritoryB = record.LastTerritoryB,
            InvolvesPlayer = record.InvolvesPlayer,
            EndDay = endDay,
            StartDay = record.StartDay >= 0 ? record.StartDay : Math.Max(0, endDay - record.LastDurationDays),
            AttackerSide = record.AttackerSide == 1 ? 1 : 0,
            HeroDeaths = CloneHeroDeaths(record.HeroDeaths)
        };
    }

    private void AccumulateBattleStats(MapEvent mapEvent)
    {
        HashSet<Kingdom> attackers = ResolveKingdomsOnSide(mapEvent.AttackerSide);
        HashSet<Kingdom> defenders = ResolveKingdomsOnSide(mapEvent.DefenderSide);
        if (attackers.Count == 0 || defenders.Count == 0)
        {
            return;
        }

        Dictionary<Kingdom, int> attackWeights = BuildContributionWeights(mapEvent.AttackerSide);
        Dictionary<Kingdom, int> defenseWeights = BuildContributionWeights(mapEvent.DefenderSide);
        bool hasWinner = mapEvent.HasWinner;
        bool attackerWon = hasWinner && mapEvent.WinningSide == BattleSideEnum.Attacker;

        Dictionary<Kingdom, Dictionary<Kingdom, int>> attackerDeathsByDefender = attackers.ToDictionary(
            kingdom => kingdom,
            kingdom => AllocateByWeights(SumDeathsForKingdom(mapEvent.AttackerSide, kingdom), defenseWeights));
        Dictionary<Kingdom, Dictionary<Kingdom, int>> attackerCasualtiesByDefender = attackers.ToDictionary(
            kingdom => kingdom,
            kingdom => AllocateByWeights(SumCasualtiesForKingdom(mapEvent.AttackerSide, kingdom), defenseWeights));
        Dictionary<Kingdom, Dictionary<Kingdom, int>> defenderDeathsByAttacker = defenders.ToDictionary(
            kingdom => kingdom,
            kingdom => AllocateByWeights(SumDeathsForKingdom(mapEvent.DefenderSide, kingdom), attackWeights));
        Dictionary<Kingdom, Dictionary<Kingdom, int>> defenderCasualtiesByAttacker = defenders.ToDictionary(
            kingdom => kingdom,
            kingdom => AllocateByWeights(SumCasualtiesForKingdom(mapEvent.DefenderSide, kingdom), attackWeights));

        foreach (Kingdom attacker in attackers)
        {
            foreach (Kingdom defender in defenders)
            {
                string pairKey = MakePairKey(attacker, defender);
                bool isTrackedWar = !string.IsNullOrEmpty(pairKey)
                    && (_activeWars.ContainsKey(pairKey) || attacker.IsAtWarWith(defender));
                if (attacker == defender || !isTrackedWar)
                {
                    continue;
                }

                int killsByAttacker = ReadAllocated(defenderDeathsByAttacker, defender, attacker);
                int casualtiesOfAttacker = ReadAllocated(attackerCasualtiesByDefender, attacker, defender);
                int killsByDefender = ReadAllocated(attackerDeathsByDefender, attacker, defender);
                int casualtiesOfDefender = ReadAllocated(defenderCasualtiesByAttacker, defender, attacker);
                if (!hasWinner && killsByAttacker + killsByDefender + casualtiesOfAttacker + casualtiesOfDefender <= 0)
                {
                    continue;
                }

                AddPairStats(
                    attacker,
                    defender,
                    killsByAttacker,
                    casualtiesOfAttacker,
                    killsByDefender,
                    casualtiesOfDefender,
                    hasWinner,
                    attackerWon);
            }
        }
    }

    private void AddPairStats(
        Kingdom kingdomOne,
        Kingdom kingdomTwo,
        int killsByOne,
        int casualtiesOfOne,
        int killsByTwo,
        int casualtiesOfTwo,
        bool hasWinner,
        bool kingdomOneWon)
    {
        string pairKey = MakePairKey(kingdomOne, kingdomTwo);
        if (string.IsNullOrEmpty(pairKey) || !TryResolvePair(pairKey, out Kingdom kingdomA, out Kingdom kingdomB))
        {
            return;
        }

        WarStatsRecord record = GetOrCreateActiveRecord(pairKey, kingdomA, kingdomB);
        bool directOrder = string.Equals(kingdomOne.StringId, kingdomA.StringId, StringComparison.Ordinal);
        if (directOrder)
        {
            record.KillsA += Math.Max(0, killsByOne);
            record.CasualtiesA += Math.Max(0, casualtiesOfOne);
            record.KillsB += Math.Max(0, killsByTwo);
            record.CasualtiesB += Math.Max(0, casualtiesOfTwo);
        }
        else
        {
            record.KillsA += Math.Max(0, killsByTwo);
            record.CasualtiesA += Math.Max(0, casualtiesOfTwo);
            record.KillsB += Math.Max(0, killsByOne);
            record.CasualtiesB += Math.Max(0, casualtiesOfOne);
        }

        if (hasWinner)
        {
            bool aWon = directOrder ? kingdomOneWon : !kingdomOneWon;
            if (aWon)
            {
                record.WinsA++;
                record.LossesB++;
            }
            else
            {
                record.LossesA++;
                record.WinsB++;
            }
        }

        UpdateRecordMetadata(record, kingdomA, kingdomB);
    }

    private WarStatsRecord GetOrCreateActiveRecord(string pairKey, Kingdom kingdomA, Kingdom kingdomB)
    {
        if (!_activeWars.TryGetValue(pairKey, out WarStatsRecord record))
        {
            record = new WarStatsRecord();
            _activeWars[pairKey] = record;
        }

        UpdateRecordMetadata(record, kingdomA, kingdomB);
        return record;
    }

    private static void UpdateRecordMetadata(WarStatsRecord record, Kingdom kingdomA, Kingdom kingdomB)
    {
        if (record == null)
        {
            return;
        }

        record.NameA = GetKingdomName(kingdomA);
        record.NameB = GetKingdomName(kingdomB);
        record.LastDurationDays = kingdomA != null && kingdomB != null ? GetWarDurationDays(kingdomA, kingdomB) : record.LastDurationDays;
        if (record.StartDay < 0)
        {
            record.StartDay = ResolveCurrentWarStartDay(kingdomA, kingdomB);
        }

        if (record.InitialTerritoryA < 0)
        {
            record.InitialTerritoryA = CountTerritory(kingdomA);
        }

        if (record.InitialTerritoryB < 0)
        {
            record.InitialTerritoryB = CountTerritory(kingdomB);
        }

        record.LastTerritoryA = CountTerritory(kingdomA);
        record.LastTerritoryB = CountTerritory(kingdomB);
        string playerKingdomId = GetPlayerKingdomId();
        record.InvolvesPlayer |= IsPlayerKingdom(kingdomA, playerKingdomId) || IsPlayerKingdom(kingdomB, playerKingdomId);
    }

    private static Dictionary<string, (Kingdom A, Kingdom B)> GatherCurrentWarPairs()
    {
        Dictionary<string, (Kingdom A, Kingdom B)> result = new(StringComparer.Ordinal);
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null || kingdom.IsEliminated || kingdom.FactionsAtWarWith == null)
            {
                continue;
            }

            foreach (IFaction faction in kingdom.FactionsAtWarWith)
            {
                if (faction is not Kingdom otherKingdom || otherKingdom.IsEliminated || !kingdom.IsAtWarWith(otherKingdom))
                {
                    continue;
                }

                string pairKey = MakePairKey(kingdom, otherKingdom);
                if (string.IsNullOrEmpty(pairKey) || result.ContainsKey(pairKey) || !TryResolvePair(pairKey, out Kingdom kingdomA, out Kingdom kingdomB))
                {
                    continue;
                }

                result[pairKey] = (kingdomA, kingdomB);
            }
        }

        return result;
    }

    private static HashSet<Kingdom> ResolveKingdomsOnSide(MapEventSide side)
    {
        HashSet<Kingdom> kingdoms = new();
        if (side?.Parties == null)
        {
            return kingdoms;
        }

        foreach (MapEventParty eventParty in side.Parties)
        {
            Kingdom kingdom = GetKingdom(eventParty?.Party);
            if (kingdom != null && !kingdom.IsEliminated)
            {
                kingdoms.Add(kingdom);
            }
        }

        return kingdoms;
    }

    private static Dictionary<Kingdom, int> BuildContributionWeights(MapEventSide side)
    {
        Dictionary<Kingdom, int> weights = new();
        if (side?.Parties == null)
        {
            return weights;
        }

        foreach (MapEventParty eventParty in side.Parties)
        {
            PartyBase party = eventParty?.Party;
            Kingdom kingdom = GetKingdom(party);
            if (kingdom == null || kingdom.IsEliminated)
            {
                continue;
            }

            int weight = ResolvePartyWeight(party);
            weights[kingdom] = weights.TryGetValue(kingdom, out int current) ? current + weight : weight;
        }

        return weights;
    }

    private static int ResolvePartyWeight(PartyBase party)
    {
        int memberCount = party?.MobileParty?.MemberRoster?.TotalManCount ?? 0;
        return Math.Max(1, memberCount);
    }

    private static int SumDeathsForKingdom(MapEventSide side, Kingdom kingdom)
    {
        return SumRosterForKingdom(side, kingdom, includeWounded: false);
    }

    private static int SumCasualtiesForKingdom(MapEventSide side, Kingdom kingdom)
    {
        return SumRosterForKingdom(side, kingdom, includeWounded: true);
    }

    private static int SumRosterForKingdom(MapEventSide side, Kingdom kingdom, bool includeWounded)
    {
        if (side?.Parties == null)
        {
            return 0;
        }

        int total = 0;
        foreach (MapEventParty eventParty in side.Parties)
        {
            if (GetKingdom(eventParty?.Party) != kingdom)
            {
                continue;
            }

            total += GetRosterCount(eventParty.DiedInBattle);
            if (includeWounded)
            {
                total += GetRosterCount(eventParty.WoundedInBattle);
            }
        }

        return total;
    }

    private static int GetRosterCount(TroopRoster roster)
    {
        return roster?.TotalManCount ?? 0;
    }

    private static Dictionary<Kingdom, int> AllocateByWeights(int total, Dictionary<Kingdom, int> weights)
    {
        Dictionary<Kingdom, int> result = new();
        if (total <= 0 || weights.Count == 0)
        {
            return result;
        }

        int weightSum = weights.Values.Where(static value => value > 0).Sum();
        if (weightSum <= 0)
        {
            return result;
        }

        List<(Kingdom Kingdom, double Remainder)> remainders = new();
        int assigned = 0;
        foreach (KeyValuePair<Kingdom, int> weight in weights)
        {
            int clampedWeight = Math.Max(0, weight.Value);
            double exactShare = total * clampedWeight / (double)weightSum;
            int floorShare = (int)Math.Floor(exactShare);
            result[weight.Key] = floorShare;
            assigned += floorShare;
            remainders.Add((weight.Key, exactShare - floorShare));
        }

        remainders.Sort(static (left, right) =>
        {
            int compare = right.Remainder.CompareTo(left.Remainder);
            return compare != 0 ? compare : string.CompareOrdinal(left.Kingdom.StringId, right.Kingdom.StringId);
        });

        int remaining = Math.Max(0, total - assigned);
        for (int i = 0; i < remaining && i < remainders.Count; i++)
        {
            Kingdom kingdom = remainders[i].Kingdom;
            result[kingdom] = result.TryGetValue(kingdom, out int current) ? current + 1 : 1;
        }

        return result;
    }

    private static int ReadAllocated(
        Dictionary<Kingdom, Dictionary<Kingdom, int>> allocations,
        Kingdom subject,
        Kingdom attributedTo)
    {
        return allocations.TryGetValue(subject, out Dictionary<Kingdom, int> byKingdom)
            && byKingdom.TryGetValue(attributedTo, out int value)
            ? value
            : 0;
    }

    private static Kingdom GetKingdom(PartyBase party)
    {
        if (party == null)
        {
            return null;
        }

        Kingdom heroKingdom = party.MobileParty?.LeaderHero?.Clan?.Kingdom;
        if (heroKingdom != null)
        {
            return heroKingdom;
        }

        if (party.MapFaction is Kingdom kingdom)
        {
            return kingdom;
        }

        return party.MapFaction is Clan clan ? clan.Kingdom : null;
    }

    private static Kingdom GetKingdom(Hero hero)
    {
        if (hero?.Clan?.Kingdom != null)
        {
            return hero.Clan.Kingdom;
        }

        return hero?.MapFaction as Kingdom;
    }

    private static string MakePairKey(Kingdom kingdomA, Kingdom kingdomB)
    {
        string idA = kingdomA?.StringId ?? string.Empty;
        string idB = kingdomB?.StringId ?? string.Empty;
        if (string.IsNullOrEmpty(idA) || string.IsNullOrEmpty(idB))
        {
            return string.Empty;
        }

        return string.CompareOrdinal(idA, idB) <= 0 ? idA + "|" + idB : idB + "|" + idA;
    }

    private static bool TryResolvePair(string pairKey, out Kingdom kingdomA, out Kingdom kingdomB)
    {
        kingdomA = null;
        kingdomB = null;
        string[] split = (pairKey ?? string.Empty).Split('|');
        if (split.Length != 2)
        {
            return false;
        }

        kingdomA = ResolveKingdomById(split[0]);
        kingdomB = ResolveKingdomById(split[1]);
        return kingdomA != null && kingdomB != null;
    }

    private static Kingdom ResolveKingdomById(string kingdomId)
    {
        if (string.IsNullOrWhiteSpace(kingdomId))
        {
            return null;
        }

        return Kingdom.All.FirstOrDefault(kingdom => kingdom != null && string.Equals(kingdom.StringId, kingdomId, StringComparison.Ordinal));
    }

    private static int GetWarDurationDays(Kingdom kingdomA, Kingdom kingdomB)
    {
        StanceLink stance = kingdomA?.GetStanceWith(kingdomB);
        return stance == null ? 0 : Math.Max(0, (int)stance.WarStartDate.ElapsedDaysUntilNow);
    }

    private static int ResolveCurrentWarStartDay(Kingdom kingdomA, Kingdom kingdomB)
    {
        StanceLink stance = kingdomA?.GetStanceWith(kingdomB);
        if (stance != null)
        {
            return Math.Max(0, (int)Math.Floor(stance.WarStartDate.ToDays));
        }

        return Math.Max(0, GetCurrentDay() - GetWarDurationDays(kingdomA, kingdomB));
    }

    private static int GetCurrentDay()
    {
        return Campaign.Current == null ? 0 : Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
    }

    private static int CountTerritory(Kingdom kingdom)
    {
        return kingdom?.Fiefs?.Count ?? 0;
    }

    private static int CalculateWeariness(
        int durationDays,
        int casualties,
        int wins,
        int losses,
        int initialTerritory,
        int currentTerritory)
    {
        double durationScore = Math.Min(30d, Math.Max(0, durationDays) / 12d);
        double casualtyScore = Math.Min(40d, Math.Max(0, casualties) / 125d);
        double battleScore = Math.Max(-10d, Math.Min(20d, (Math.Max(0, losses) - Math.Max(0, wins)) * 4d));
        int territoryBaseline = initialTerritory < 0 ? Math.Max(0, currentTerritory) : initialTerritory;
        int lostTerritory = Math.Max(0, territoryBaseline - Math.Max(0, currentTerritory));
        double territoryScore = Math.Min(20d, lostTerritory * 5d);
        int result = (int)Math.Round(durationScore + casualtyScore + battleScore + territoryScore, MidpointRounding.AwayFromZero);
        return Math.Max(0, Math.Min(100, result));
    }

    private static int CalculateAdvantageSide(WarStatsRecord record)
    {
        if (record == null)
        {
            return -1;
        }

        int initialA = record.InitialTerritoryA < 0 ? record.LastTerritoryA : record.InitialTerritoryA;
        int initialB = record.InitialTerritoryB < 0 ? record.LastTerritoryB : record.InitialTerritoryB;
        int territoryDifference = (record.LastTerritoryA - initialA) - (record.LastTerritoryB - initialB);
        int battleDifference = record.WinsA - record.WinsB;
        int score = territoryDifference * 6 + battleDifference * 2;

        int totalCasualties = Math.Max(0, record.CasualtiesA) + Math.Max(0, record.CasualtiesB);
        int meaningfulCasualtyMargin = Math.Max(100, totalCasualties / 10);
        int casualtyDifference = record.CasualtiesB - record.CasualtiesA;
        if (casualtyDifference >= meaningfulCasualtyMargin)
        {
            score++;
        }
        else if (casualtyDifference <= -meaningfulCasualtyMargin)
        {
            score--;
        }

        return score >= 2 ? 0 : score <= -2 ? 1 : -1;
    }

    private static WarAdvantage ResolveOrientedAdvantage(int pairAdvantageSide, bool swapSides)
    {
        if (pairAdvantageSide < 0)
        {
            return WarAdvantage.Stalemate;
        }

        int orientedSide = swapSides ? 1 - pairAdvantageSide : pairAdvantageSide;
        return orientedSide == 0 ? WarAdvantage.Attacker : WarAdvantage.Defender;
    }

    private static int ResolveHistoryStartDay(HistoricalWarRecord record)
    {
        if (record == null)
        {
            return 0;
        }

        return record.StartDay >= 0
            ? record.StartDay
            : Math.Max(0, record.EndDay - Math.Max(0, record.LastDurationDays));
    }

    public static string MakeHistoricalIdentity(string pairKey, int startDay, int endDay)
    {
        return (pairKey ?? string.Empty) + "\u001F" + startDay + "\u001F" + endDay;
    }

    private static string FormatDateRange(int startDay, int endDay)
    {
        if (endDay <= 0)
        {
            return AfWarStatsTexts.LegacyRecord;
        }

        string startDate = CampaignTime.Days(Math.Max(0, startDay)).ToString();
        string endDate = CampaignTime.Days(Math.Max(0, endDay)).ToString();
        return AfWarStatsTexts.DateRange(startDate, endDate);
    }

    private static string ResolveHistoricalName(string savedName, Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(savedName) ? GetKingdomName(kingdom) : savedName;
    }

    private static List<HeroDeathEntry> BuildHeroDeathEntries(WarStatsRecord record, int pairSide)
    {
        if (record?.HeroDeaths == null)
        {
            return new List<HeroDeathEntry>();
        }

        return record.HeroDeaths
            .Where(item => item != null && item.Side == pairSide)
            .OrderBy(item => item.Day)
            .ThenBy(item => item.HeroName, StringComparer.Ordinal)
            .Select(item => new HeroDeathEntry
            {
                HeroId = item.HeroId ?? string.Empty,
                HeroName = item.HeroName ?? string.Empty,
                KillerName = item.KillerName ?? string.Empty,
                Cause = Enum.IsDefined(typeof(KillCharacterAction.KillCharacterActionDetail), item.Cause)
                    ? (KillCharacterAction.KillCharacterActionDetail)item.Cause
                    : KillCharacterAction.KillCharacterActionDetail.None,
                Day = Math.Max(0, item.Day),
                DateText = item.Day > 0 ? CampaignTime.Days(item.Day).ToString() : AfWarStatsTexts.LegacyRecord,
                BattleName = item.BattleName ?? string.Empty
            })
            .ToList();
    }

    private static List<HeroDeathRecord> CloneHeroDeaths(List<HeroDeathRecord> records)
    {
        if (records == null)
        {
            return new List<HeroDeathRecord>();
        }

        return records
            .Where(static item => item != null)
            .Select(static item => new HeroDeathRecord
            {
                HeroId = item.HeroId ?? string.Empty,
                HeroName = item.HeroName ?? string.Empty,
                KillerName = item.KillerName ?? string.Empty,
                Cause = item.Cause,
                Day = item.Day,
                BattleName = item.BattleName ?? string.Empty,
                Side = item.Side == 1 ? 1 : 0
            })
            .ToList();
    }

    private static string GetKingdomName(Kingdom kingdom)
    {
        return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? AfWarStatsTexts.UnknownKingdom;
    }

    private static string GetPlayerKingdomId()
    {
        return Hero.MainHero?.Clan?.Kingdom?.StringId ?? string.Empty;
    }

    private static bool IsPlayerKingdom(Kingdom kingdom, string playerKingdomId)
    {
        return kingdom != null
            && !string.IsNullOrEmpty(playerKingdomId)
            && string.Equals(kingdom.StringId, playerKingdomId, StringComparison.Ordinal);
    }

    private void EnsureSaveLists()
    {
        _savedPairKeys ??= new List<string>();
        _savedCasualtiesA ??= new List<int>();
        _savedCasualtiesB ??= new List<int>();
        _savedActiveKeysV2 ??= new List<string>();
        _savedActiveNamesAV2 ??= new List<string>();
        _savedActiveNamesBV2 ??= new List<string>();
        _savedActiveKillsAV2 ??= new List<int>();
        _savedActiveKillsBV2 ??= new List<int>();
        _savedActiveCasualtiesAV2 ??= new List<int>();
        _savedActiveCasualtiesBV2 ??= new List<int>();
        _savedActiveDurationV2 ??= new List<int>();
        _savedActiveTerritoryAV2 ??= new List<int>();
        _savedActiveTerritoryBV2 ??= new List<int>();
        _savedActivePlayerV2 ??= new List<int>();
        _savedHistoryKeysV2 ??= new List<string>();
        _savedHistoryNamesAV2 ??= new List<string>();
        _savedHistoryNamesBV2 ??= new List<string>();
        _savedHistoryKillsAV2 ??= new List<int>();
        _savedHistoryKillsBV2 ??= new List<int>();
        _savedHistoryCasualtiesAV2 ??= new List<int>();
        _savedHistoryCasualtiesBV2 ??= new List<int>();
        _savedHistoryDurationV2 ??= new List<int>();
        _savedHistoryTerritoryAV2 ??= new List<int>();
        _savedHistoryTerritoryBV2 ??= new List<int>();
        _savedHistoryEndDayV2 ??= new List<int>();
        _savedHistoryPlayerV2 ??= new List<int>();
        _savedActiveWinsAV3 ??= new List<int>();
        _savedActiveWinsBV3 ??= new List<int>();
        _savedActiveLossesAV3 ??= new List<int>();
        _savedActiveLossesBV3 ??= new List<int>();
        _savedActiveInitialTerritoryAV3 ??= new List<int>();
        _savedActiveInitialTerritoryBV3 ??= new List<int>();
        _savedHistoryWinsAV3 ??= new List<int>();
        _savedHistoryWinsBV3 ??= new List<int>();
        _savedHistoryLossesAV3 ??= new List<int>();
        _savedHistoryLossesBV3 ??= new List<int>();
        _savedHistoryInitialTerritoryAV3 ??= new List<int>();
        _savedHistoryInitialTerritoryBV3 ??= new List<int>();
        _savedActiveStartDayV4 ??= new List<int>();
        _savedActiveAttackerSideV4 ??= new List<int>();
        _savedActiveHeroDeathsV4 ??= new List<string>();
        _savedHistoryStartDayV4 ??= new List<int>();
        _savedHistoryAttackerSideV4 ??= new List<int>();
        _savedHistoryHeroDeathsV4 ??= new List<string>();
        _savedActiveRecentHeroBattlesV5 ??= new List<string>();
    }

    private void PrepareSaveData()
    {
        ClearAllSaveLists();
        foreach (KeyValuePair<string, WarStatsRecord> item in _activeWars.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            _savedActiveKeysV2.Add(item.Key);
            _savedActiveNamesAV2.Add(item.Value.NameA ?? string.Empty);
            _savedActiveNamesBV2.Add(item.Value.NameB ?? string.Empty);
            _savedActiveKillsAV2.Add(item.Value.KillsA);
            _savedActiveKillsBV2.Add(item.Value.KillsB);
            _savedActiveCasualtiesAV2.Add(item.Value.CasualtiesA);
            _savedActiveCasualtiesBV2.Add(item.Value.CasualtiesB);
            _savedActiveDurationV2.Add(item.Value.LastDurationDays);
            _savedActiveTerritoryAV2.Add(item.Value.LastTerritoryA);
            _savedActiveTerritoryBV2.Add(item.Value.LastTerritoryB);
            _savedActivePlayerV2.Add(item.Value.InvolvesPlayer ? 1 : 0);
            _savedActiveWinsAV3.Add(item.Value.WinsA);
            _savedActiveWinsBV3.Add(item.Value.WinsB);
            _savedActiveLossesAV3.Add(item.Value.LossesA);
            _savedActiveLossesBV3.Add(item.Value.LossesB);
            _savedActiveInitialTerritoryAV3.Add(item.Value.InitialTerritoryA < 0 ? item.Value.LastTerritoryA : item.Value.InitialTerritoryA);
            _savedActiveInitialTerritoryBV3.Add(item.Value.InitialTerritoryB < 0 ? item.Value.LastTerritoryB : item.Value.InitialTerritoryB);
            _savedActiveStartDayV4.Add(Math.Max(0, item.Value.StartDay));
            _savedActiveAttackerSideV4.Add(item.Value.AttackerSide == 1 ? 1 : 0);
            _savedActiveHeroDeathsV4.Add(SerializeHeroDeaths(item.Value.HeroDeaths));
            _savedActiveRecentHeroBattlesV5.Add(SerializeRecentHeroBattles(item.Value.RecentHeroBattles));
        }

        foreach (HistoricalWarRecord record in _historicalWars)
        {
            _savedHistoryKeysV2.Add(record.PairKey ?? string.Empty);
            _savedHistoryNamesAV2.Add(record.NameA ?? string.Empty);
            _savedHistoryNamesBV2.Add(record.NameB ?? string.Empty);
            _savedHistoryKillsAV2.Add(record.KillsA);
            _savedHistoryKillsBV2.Add(record.KillsB);
            _savedHistoryCasualtiesAV2.Add(record.CasualtiesA);
            _savedHistoryCasualtiesBV2.Add(record.CasualtiesB);
            _savedHistoryDurationV2.Add(record.LastDurationDays);
            _savedHistoryTerritoryAV2.Add(record.LastTerritoryA);
            _savedHistoryTerritoryBV2.Add(record.LastTerritoryB);
            _savedHistoryEndDayV2.Add(record.EndDay);
            _savedHistoryPlayerV2.Add(record.InvolvesPlayer ? 1 : 0);
            _savedHistoryWinsAV3.Add(record.WinsA);
            _savedHistoryWinsBV3.Add(record.WinsB);
            _savedHistoryLossesAV3.Add(record.LossesA);
            _savedHistoryLossesBV3.Add(record.LossesB);
            _savedHistoryInitialTerritoryAV3.Add(record.InitialTerritoryA < 0 ? record.LastTerritoryA : record.InitialTerritoryA);
            _savedHistoryInitialTerritoryBV3.Add(record.InitialTerritoryB < 0 ? record.LastTerritoryB : record.InitialTerritoryB);
            _savedHistoryStartDayV4.Add(Math.Max(0, ResolveHistoryStartDay(record)));
            _savedHistoryAttackerSideV4.Add(record.AttackerSide == 1 ? 1 : 0);
            _savedHistoryHeroDeathsV4.Add(SerializeHeroDeaths(record.HeroDeaths));
        }
    }

    private void LoadSavedData()
    {
        EnsureSaveLists();
        _activeWars.Clear();
        _historicalWars.Clear();
        _legacyRecords.Clear();

        if (_dataVersion >= CurrentDataVersion || _savedActiveKeysV2.Count > 0 || _savedHistoryKeysV2.Count > 0)
        {
            for (int i = 0; i < _savedActiveKeysV2.Count; i++)
            {
                string pairKey = ReadString(_savedActiveKeysV2, i);
                if (string.IsNullOrWhiteSpace(pairKey))
                {
                    continue;
                }

                _activeWars[pairKey] = new WarStatsRecord
                {
                    NameA = ReadString(_savedActiveNamesAV2, i),
                    NameB = ReadString(_savedActiveNamesBV2, i),
                    KillsA = ReadInt(_savedActiveKillsAV2, i),
                    KillsB = ReadInt(_savedActiveKillsBV2, i),
                    CasualtiesA = ReadInt(_savedActiveCasualtiesAV2, i),
                    CasualtiesB = ReadInt(_savedActiveCasualtiesBV2, i),
                    WinsA = ReadInt(_savedActiveWinsAV3, i),
                    WinsB = ReadInt(_savedActiveWinsBV3, i),
                    LossesA = ReadInt(_savedActiveLossesAV3, i),
                    LossesB = ReadInt(_savedActiveLossesBV3, i),
                    LastDurationDays = ReadInt(_savedActiveDurationV2, i),
                    LastTerritoryA = ReadInt(_savedActiveTerritoryAV2, i),
                    LastTerritoryB = ReadInt(_savedActiveTerritoryBV2, i),
                    InitialTerritoryA = ReadIntOrDefault(_savedActiveInitialTerritoryAV3, i, ReadInt(_savedActiveTerritoryAV2, i)),
                    InitialTerritoryB = ReadIntOrDefault(_savedActiveInitialTerritoryBV3, i, ReadInt(_savedActiveTerritoryBV2, i)),
                    InvolvesPlayer = ReadInt(_savedActivePlayerV2, i) != 0,
                    StartDay = ReadIntOrDefault(
                        _savedActiveStartDayV4,
                        i,
                        Math.Max(0, GetCurrentDay() - ReadInt(_savedActiveDurationV2, i))),
                    AttackerSide = ReadSide(_savedActiveAttackerSideV4, i),
                    HeroDeaths = DeserializeHeroDeaths(ReadString(_savedActiveHeroDeathsV4, i)),
                    RecentHeroBattles = DeserializeRecentHeroBattles(ReadString(_savedActiveRecentHeroBattlesV5, i))
                };
            }

            for (int i = 0; i < _savedHistoryKeysV2.Count; i++)
            {
                string pairKey = ReadString(_savedHistoryKeysV2, i);
                if (string.IsNullOrWhiteSpace(pairKey))
                {
                    continue;
                }

                _historicalWars.Add(new HistoricalWarRecord
                {
                    PairKey = pairKey,
                    NameA = ReadString(_savedHistoryNamesAV2, i),
                    NameB = ReadString(_savedHistoryNamesBV2, i),
                    KillsA = ReadInt(_savedHistoryKillsAV2, i),
                    KillsB = ReadInt(_savedHistoryKillsBV2, i),
                    CasualtiesA = ReadInt(_savedHistoryCasualtiesAV2, i),
                    CasualtiesB = ReadInt(_savedHistoryCasualtiesBV2, i),
                    WinsA = ReadInt(_savedHistoryWinsAV3, i),
                    WinsB = ReadInt(_savedHistoryWinsBV3, i),
                    LossesA = ReadInt(_savedHistoryLossesAV3, i),
                    LossesB = ReadInt(_savedHistoryLossesBV3, i),
                    LastDurationDays = ReadInt(_savedHistoryDurationV2, i),
                    LastTerritoryA = ReadInt(_savedHistoryTerritoryAV2, i),
                    LastTerritoryB = ReadInt(_savedHistoryTerritoryBV2, i),
                    InitialTerritoryA = ReadIntOrDefault(_savedHistoryInitialTerritoryAV3, i, ReadInt(_savedHistoryTerritoryAV2, i)),
                    InitialTerritoryB = ReadIntOrDefault(_savedHistoryInitialTerritoryBV3, i, ReadInt(_savedHistoryTerritoryBV2, i)),
                    EndDay = ReadInt(_savedHistoryEndDayV2, i),
                    InvolvesPlayer = ReadInt(_savedHistoryPlayerV2, i) != 0,
                    StartDay = ReadIntOrDefault(
                        _savedHistoryStartDayV4,
                        i,
                        Math.Max(0, ReadInt(_savedHistoryEndDayV2, i) - ReadInt(_savedHistoryDurationV2, i))),
                    AttackerSide = ReadSide(_savedHistoryAttackerSideV4, i),
                    HeroDeaths = DeserializeHeroDeaths(ReadString(_savedHistoryHeroDeathsV4, i))
                });
            }

            _legacyMigrationPending = false;
            _recentBattleSequence = Math.Max(_recentBattleSequence, GetMaxRecentBattleSequence());
            return;
        }

        int legacyCount = Math.Min(_savedPairKeys.Count, Math.Min(_savedCasualtiesA.Count, _savedCasualtiesB.Count));
        for (int i = 0; i < legacyCount; i++)
        {
            string pairKey = ReadString(_savedPairKeys, i);
            if (!string.IsNullOrWhiteSpace(pairKey))
            {
                _legacyRecords[pairKey] = new LegacyPairRecord
                {
                    InflictedByA = Math.Max(0, ReadInt(_savedCasualtiesA, i)),
                    InflictedByB = Math.Max(0, ReadInt(_savedCasualtiesB, i))
                };
            }
        }

        _legacyMigrationPending = _legacyRecords.Count > 0;
    }

    private void ClearAllSaveLists()
    {
        _savedPairKeys.Clear();
        _savedCasualtiesA.Clear();
        _savedCasualtiesB.Clear();
        _savedActiveKeysV2.Clear();
        _savedActiveNamesAV2.Clear();
        _savedActiveNamesBV2.Clear();
        _savedActiveKillsAV2.Clear();
        _savedActiveKillsBV2.Clear();
        _savedActiveCasualtiesAV2.Clear();
        _savedActiveCasualtiesBV2.Clear();
        _savedActiveDurationV2.Clear();
        _savedActiveTerritoryAV2.Clear();
        _savedActiveTerritoryBV2.Clear();
        _savedActivePlayerV2.Clear();
        _savedHistoryKeysV2.Clear();
        _savedHistoryNamesAV2.Clear();
        _savedHistoryNamesBV2.Clear();
        _savedHistoryKillsAV2.Clear();
        _savedHistoryKillsBV2.Clear();
        _savedHistoryCasualtiesAV2.Clear();
        _savedHistoryCasualtiesBV2.Clear();
        _savedHistoryDurationV2.Clear();
        _savedHistoryTerritoryAV2.Clear();
        _savedHistoryTerritoryBV2.Clear();
        _savedHistoryEndDayV2.Clear();
        _savedHistoryPlayerV2.Clear();
        _savedActiveWinsAV3.Clear();
        _savedActiveWinsBV3.Clear();
        _savedActiveLossesAV3.Clear();
        _savedActiveLossesBV3.Clear();
        _savedActiveInitialTerritoryAV3.Clear();
        _savedActiveInitialTerritoryBV3.Clear();
        _savedHistoryWinsAV3.Clear();
        _savedHistoryWinsBV3.Clear();
        _savedHistoryLossesAV3.Clear();
        _savedHistoryLossesBV3.Clear();
        _savedHistoryInitialTerritoryAV3.Clear();
        _savedHistoryInitialTerritoryBV3.Clear();
        _savedActiveStartDayV4.Clear();
        _savedActiveAttackerSideV4.Clear();
        _savedActiveHeroDeathsV4.Clear();
        _savedHistoryStartDayV4.Clear();
        _savedHistoryAttackerSideV4.Clear();
        _savedHistoryHeroDeathsV4.Clear();
        _savedActiveRecentHeroBattlesV5.Clear();
    }

    private static string SerializeHeroDeaths(List<HeroDeathRecord> records)
    {
        if (records == null || records.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ";",
            records
                .Where(static item => item != null)
                .Select(static item => string.Join(
                    ",",
                    item.Side == 1 ? "1" : "0",
                    Math.Max(0, item.Day).ToString(),
                    item.Cause.ToString(),
                    EncodeSaveField(item.HeroId),
                    EncodeSaveField(item.HeroName),
                    EncodeSaveField(item.KillerName),
                    EncodeSaveField(item.BattleName))));
    }

    private static List<HeroDeathRecord> DeserializeHeroDeaths(string serialized)
    {
        List<HeroDeathRecord> result = new();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return result;
        }

        foreach (string recordText in serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = recordText.Split(new[] { ',' }, StringSplitOptions.None);
            if (fields.Length != 7
                || !int.TryParse(fields[0], out int side)
                || !int.TryParse(fields[1], out int day)
                || !int.TryParse(fields[2], out int cause))
            {
                continue;
            }

            result.Add(new HeroDeathRecord
            {
                Side = side == 1 ? 1 : 0,
                Day = Math.Max(0, day),
                Cause = cause,
                HeroId = DecodeSaveField(fields[3]),
                HeroName = DecodeSaveField(fields[4]),
                KillerName = DecodeSaveField(fields[5]),
                BattleName = DecodeSaveField(fields[6])
            });
        }

        return result;
    }

    private static string SerializeRecentHeroBattles(Dictionary<string, RecentHeroBattleRecord> records)
    {
        if (records == null || records.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ";",
            records.Values
                .Where(static item => item != null
                    && !string.IsNullOrWhiteSpace(item.HeroId)
                    && !string.IsNullOrWhiteSpace(item.OwnKingdomId))
                .OrderBy(static item => item.HeroId, StringComparer.Ordinal)
                .Select(static item => string.Join(
                    ",",
                    Math.Max(0, item.Sequence).ToString(),
                    Math.Max(0, item.Day).ToString(),
                    EncodeSaveField(item.HeroId),
                    EncodeSaveField(item.OwnKingdomId))));
    }

    private static Dictionary<string, RecentHeroBattleRecord> DeserializeRecentHeroBattles(string serialized)
    {
        Dictionary<string, RecentHeroBattleRecord> result = new(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return result;
        }

        foreach (string recordText in serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = recordText.Split(new[] { ',' }, StringSplitOptions.None);
            if (fields.Length != 4
                || !int.TryParse(fields[0], out int sequence)
                || !int.TryParse(fields[1], out int day))
            {
                continue;
            }

            string heroId = DecodeSaveField(fields[2]);
            string ownKingdomId = DecodeSaveField(fields[3]);
            if (string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(ownKingdomId))
            {
                continue;
            }

            RecentHeroBattleRecord candidate = new()
            {
                HeroId = heroId,
                Day = Math.Max(0, day),
                Sequence = Math.Max(0, sequence),
                OwnKingdomId = ownKingdomId
            };
            if (result.TryGetValue(heroId, out RecentHeroBattleRecord existing)
                && (existing.Sequence > candidate.Sequence
                    || (existing.Sequence == candidate.Sequence && existing.Day >= candidate.Day)))
            {
                continue;
            }

            result[heroId] = candidate;
        }

        return result;
    }

    private int GetMaxRecentBattleSequence()
    {
        int maximum = 0;
        foreach (WarStatsRecord record in _activeWars.Values)
        {
            if (record?.RecentHeroBattles == null)
            {
                continue;
            }

            foreach (RecentHeroBattleRecord recentBattle in record.RecentHeroBattles.Values)
            {
                if (recentBattle != null)
                {
                    maximum = Math.Max(maximum, recentBattle.Sequence);
                }
            }
        }

        return Math.Max(0, maximum);
    }

    private static string EncodeSaveField(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }

    private static string DecodeSaveField(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    private static int ReadSide(List<int> values, int index)
    {
        return values != null && index >= 0 && index < values.Count && values[index] == 1 ? 1 : 0;
    }

    private static string ReadString(List<string> values, int index)
    {
        return values != null && index >= 0 && index < values.Count ? values[index] ?? string.Empty : string.Empty;
    }

    private static int ReadInt(List<int> values, int index)
    {
        return values != null && index >= 0 && index < values.Count ? Math.Max(0, values[index]) : 0;
    }

    private static int ReadIntOrDefault(List<int> values, int index, int defaultValue)
    {
        return values != null && index >= 0 && index < values.Count
            ? Math.Max(0, values[index])
            : Math.Max(0, defaultValue);
    }
}
