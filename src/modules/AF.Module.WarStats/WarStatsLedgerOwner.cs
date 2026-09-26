using System;
using System.Collections.Generic;
using System.Linq;

namespace AFWarStatsTerminal.Behaviors;

public sealed partial class AfWarStatsBehavior
{
    private sealed class WarStatsLedgerOwner
    {
        internal Dictionary<string, WarStatsRecord> ActiveWars { get; } = new(StringComparer.Ordinal);

        internal List<HistoricalWarRecord> HistoricalWars { get; } = new();

        internal Dictionary<string, LegacyPairRecord> LegacyRecords { get; } = new(StringComparer.Ordinal);

        internal int RecentBattleSequence { get; private set; }

        internal void SetRecentBattleSequence(int sequence)
        {
            RecentBattleSequence = Math.Max(0, sequence);
        }

        internal int NextBattleSequence()
        {
            if (RecentBattleSequence < int.MaxValue)
            {
                RecentBattleSequence++;
            }

            return RecentBattleSequence;
        }

        internal void StartWar(WarStatsRecord record, int attackerSide, int startDay)
        {
            if (record == null)
            {
                return;
            }

            record.RecentHeroBattles?.Clear();
            record.AttackerSide = attackerSide == 1 ? 1 : 0;
            record.StartDay = Math.Max(0, startDay);
        }

        internal void ResetActiveCountsAndIncidents(WarStatsRecord record)
        {
            if (record == null)
            {
                return;
            }

            record.KillsA = 0;
            record.KillsB = 0;
            record.CasualtiesA = 0;
            record.CasualtiesB = 0;
            record.WinsA = 0;
            record.WinsB = 0;
            record.LossesA = 0;
            record.LossesB = 0;
            record.HeroDeaths?.Clear();
            record.RecentHeroBattles?.Clear();
        }

        internal void RecordRecentHeroBattle(
            WarStatsRecord record,
            string heroId,
            string ownKingdomId,
            int battleDay,
            int battleSequence)
        {
            if (record == null || string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(ownKingdomId))
            {
                return;
            }

            record.RecentHeroBattles ??= new Dictionary<string, RecentHeroBattleRecord>(StringComparer.Ordinal);
            record.RecentHeroBattles[heroId] = new RecentHeroBattleRecord
            {
                HeroId = heroId,
                Day = Math.Max(0, battleDay),
                Sequence = Math.Max(0, battleSequence),
                OwnKingdomId = ownKingdomId
            };
        }

        internal bool HasRecordedHeroDeath(string heroId, string heroName)
        {
            foreach (WarStatsRecord record in ActiveWars.Values)
            {
                if (ContainsHeroDeath(record, heroId, heroName))
                {
                    return true;
                }
            }

            foreach (HistoricalWarRecord record in HistoricalWars)
            {
                if (ContainsHeroDeath(record, heroId, heroName))
                {
                    return true;
                }
            }

            return false;
        }

        internal void UpsertHeroDeath(
            WarStatsRecord record,
            string heroId,
            string heroName,
            string killerName,
            int cause,
            int day,
            string battleName,
            int pairSide)
        {
            if (record == null)
            {
                return;
            }

            record.HeroDeaths ??= new List<HeroDeathRecord>();
            HeroDeathRecord existing = null;
            foreach (HeroDeathRecord item in record.HeroDeaths)
            {
                if (IsSameHero(item, heroId, heroName))
                {
                    existing = item;
                    break;
                }
            }
            if (existing == null)
            {
                existing = new HeroDeathRecord
                {
                    HeroId = heroId ?? string.Empty,
                    HeroName = heroName ?? string.Empty,
                    Day = Math.Max(0, day),
                    Side = pairSide == 1 ? 1 : 0
                };
                record.HeroDeaths.Add(existing);
            }

            existing.HeroName = string.IsNullOrWhiteSpace(heroName) ? existing.HeroName : heroName;
            existing.KillerName = killerName ?? existing.KillerName ?? string.Empty;
            existing.Cause = cause;
            existing.Side = pairSide == 1 ? 1 : 0;
            if (!string.IsNullOrWhiteSpace(battleName))
            {
                existing.BattleName = battleName;
            }
        }

        private static bool ContainsHeroDeath(WarStatsRecord record, string heroId, string heroName)
        {
            if (record?.HeroDeaths == null)
            {
                return false;
            }

            foreach (HeroDeathRecord item in record.HeroDeaths)
            {
                if (IsSameHero(item, heroId, heroName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameHero(HeroDeathRecord item, string heroId, string heroName)
        {
            return item != null
                && ((!string.IsNullOrEmpty(heroId) && string.Equals(item.HeroId, heroId, StringComparison.Ordinal))
                    || (string.IsNullOrEmpty(heroId) && string.Equals(item.HeroName, heroName, StringComparison.Ordinal)));
        }

        internal WarStatsRecord GetOrCreateActive(string pairKey)
        {
            if (!ActiveWars.TryGetValue(pairKey, out WarStatsRecord record))
            {
                record = new WarStatsRecord();
                ActiveWars.Add(pairKey, record);
            }

            return record;
        }

        internal void ArchiveAndRemove(string pairKey, WarStatsRecord record, int endDay)
        {
            if (!ActiveWars.TryGetValue(pairKey, out WarStatsRecord current) || !ReferenceEquals(current, record))
            {
                return;
            }

            HistoricalWars.Add(ToHistoricalRecord(pairKey, record, endDay));
            ActiveWars.Remove(pairKey);
        }

        internal void AddHistorical(string pairKey, WarStatsRecord record, int endDay)
        {
            HistoricalWars.Add(ToHistoricalRecord(pairKey, record, endDay));
        }

        internal void ApplyMigratedLegacy(string pairKey, WarStatsRecord record, bool isCurrentWar)
        {
            if (isCurrentWar)
            {
                ActiveWars[pairKey] = record;
            }
            else
            {
                AddHistorical(pairKey, record, 0);
            }
        }

        internal void ClearLegacyRecords()
        {
            LegacyRecords.Clear();
        }

        internal void ApplyBattleStats(
            WarStatsRecord record,
            bool directOrder,
            int killsByOne,
            int casualtiesOfOne,
            int killsByTwo,
            int casualtiesOfTwo,
            bool hasWinner,
            bool kingdomOneWon)
        {
            if (record == null)
            {
                return;
            }

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
        }

        internal void PrepareSaveData(AfWarStatsBehavior host)
        {
            host.ClearAllSaveLists();
            foreach (KeyValuePair<string, WarStatsRecord> item in ActiveWars.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                host._savedActiveKeysV2.Add(item.Key);
                host._savedActiveNamesAV2.Add(item.Value.NameA ?? string.Empty);
                host._savedActiveNamesBV2.Add(item.Value.NameB ?? string.Empty);
                host._savedActiveKillsAV2.Add(item.Value.KillsA);
                host._savedActiveKillsBV2.Add(item.Value.KillsB);
                host._savedActiveCasualtiesAV2.Add(item.Value.CasualtiesA);
                host._savedActiveCasualtiesBV2.Add(item.Value.CasualtiesB);
                host._savedActiveDurationV2.Add(item.Value.LastDurationDays);
                host._savedActiveTerritoryAV2.Add(item.Value.LastTerritoryA);
                host._savedActiveTerritoryBV2.Add(item.Value.LastTerritoryB);
                host._savedActivePlayerV2.Add(item.Value.InvolvesPlayer ? 1 : 0);
                host._savedActiveWinsAV3.Add(item.Value.WinsA);
                host._savedActiveWinsBV3.Add(item.Value.WinsB);
                host._savedActiveLossesAV3.Add(item.Value.LossesA);
                host._savedActiveLossesBV3.Add(item.Value.LossesB);
                host._savedActiveInitialTerritoryAV3.Add(item.Value.InitialTerritoryA < 0 ? item.Value.LastTerritoryA : item.Value.InitialTerritoryA);
                host._savedActiveInitialTerritoryBV3.Add(item.Value.InitialTerritoryB < 0 ? item.Value.LastTerritoryB : item.Value.InitialTerritoryB);
                host._savedActiveStartDayV4.Add(Math.Max(0, item.Value.StartDay));
                host._savedActiveAttackerSideV4.Add(item.Value.AttackerSide == 1 ? 1 : 0);
                host._savedActiveHeroDeathsV4.Add(SerializeHeroDeaths(item.Value.HeroDeaths));
                host._savedActiveRecentHeroBattlesV5.Add(SerializeRecentHeroBattles(item.Value.RecentHeroBattles));
            }

            foreach (HistoricalWarRecord record in HistoricalWars)
            {
                host._savedHistoryKeysV2.Add(record.PairKey ?? string.Empty);
                host._savedHistoryNamesAV2.Add(record.NameA ?? string.Empty);
                host._savedHistoryNamesBV2.Add(record.NameB ?? string.Empty);
                host._savedHistoryKillsAV2.Add(record.KillsA);
                host._savedHistoryKillsBV2.Add(record.KillsB);
                host._savedHistoryCasualtiesAV2.Add(record.CasualtiesA);
                host._savedHistoryCasualtiesBV2.Add(record.CasualtiesB);
                host._savedHistoryDurationV2.Add(record.LastDurationDays);
                host._savedHistoryTerritoryAV2.Add(record.LastTerritoryA);
                host._savedHistoryTerritoryBV2.Add(record.LastTerritoryB);
                host._savedHistoryEndDayV2.Add(record.EndDay);
                host._savedHistoryPlayerV2.Add(record.InvolvesPlayer ? 1 : 0);
                host._savedHistoryWinsAV3.Add(record.WinsA);
                host._savedHistoryWinsBV3.Add(record.WinsB);
                host._savedHistoryLossesAV3.Add(record.LossesA);
                host._savedHistoryLossesBV3.Add(record.LossesB);
                host._savedHistoryInitialTerritoryAV3.Add(record.InitialTerritoryA < 0 ? record.LastTerritoryA : record.InitialTerritoryA);
                host._savedHistoryInitialTerritoryBV3.Add(record.InitialTerritoryB < 0 ? record.LastTerritoryB : record.InitialTerritoryB);
                host._savedHistoryStartDayV4.Add(Math.Max(0, ResolveHistoryStartDay(record)));
                host._savedHistoryAttackerSideV4.Add(record.AttackerSide == 1 ? 1 : 0);
                host._savedHistoryHeroDeathsV4.Add(SerializeHeroDeaths(record.HeroDeaths));
            }
        }

        internal bool RestoreSavedData(AfWarStatsBehavior host)
        {
            host.EnsureSaveLists();
            ActiveWars.Clear();
            HistoricalWars.Clear();
            LegacyRecords.Clear();

            if (host._dataVersion >= CurrentDataVersion || host._savedActiveKeysV2.Count > 0 || host._savedHistoryKeysV2.Count > 0)
            {
                for (int i = 0; i < host._savedActiveKeysV2.Count; i++)
                {
                    string pairKey = ReadString(host._savedActiveKeysV2, i);
                    if (string.IsNullOrWhiteSpace(pairKey))
                    {
                        continue;
                    }

                    ActiveWars[pairKey] = new WarStatsRecord
                    {
                        NameA = ReadString(host._savedActiveNamesAV2, i),
                        NameB = ReadString(host._savedActiveNamesBV2, i),
                        KillsA = ReadInt(host._savedActiveKillsAV2, i),
                        KillsB = ReadInt(host._savedActiveKillsBV2, i),
                        CasualtiesA = ReadInt(host._savedActiveCasualtiesAV2, i),
                        CasualtiesB = ReadInt(host._savedActiveCasualtiesBV2, i),
                        WinsA = ReadInt(host._savedActiveWinsAV3, i),
                        WinsB = ReadInt(host._savedActiveWinsBV3, i),
                        LossesA = ReadInt(host._savedActiveLossesAV3, i),
                        LossesB = ReadInt(host._savedActiveLossesBV3, i),
                        LastDurationDays = ReadInt(host._savedActiveDurationV2, i),
                        LastTerritoryA = ReadInt(host._savedActiveTerritoryAV2, i),
                        LastTerritoryB = ReadInt(host._savedActiveTerritoryBV2, i),
                        InitialTerritoryA = ReadIntOrDefault(host._savedActiveInitialTerritoryAV3, i, ReadInt(host._savedActiveTerritoryAV2, i)),
                        InitialTerritoryB = ReadIntOrDefault(host._savedActiveInitialTerritoryBV3, i, ReadInt(host._savedActiveTerritoryBV2, i)),
                        InvolvesPlayer = ReadInt(host._savedActivePlayerV2, i) != 0,
                        StartDay = ReadIntOrDefault(
                            host._savedActiveStartDayV4,
                            i,
                            Math.Max(0, GetCurrentDay() - ReadInt(host._savedActiveDurationV2, i))),
                        AttackerSide = ReadSide(host._savedActiveAttackerSideV4, i),
                        HeroDeaths = DeserializeHeroDeaths(ReadString(host._savedActiveHeroDeathsV4, i)),
                        RecentHeroBattles = DeserializeRecentHeroBattles(ReadString(host._savedActiveRecentHeroBattlesV5, i))
                    };
                }

                for (int i = 0; i < host._savedHistoryKeysV2.Count; i++)
                {
                    string pairKey = ReadString(host._savedHistoryKeysV2, i);
                    if (string.IsNullOrWhiteSpace(pairKey))
                    {
                        continue;
                    }

                    HistoricalWars.Add(new HistoricalWarRecord
                    {
                        PairKey = pairKey,
                        NameA = ReadString(host._savedHistoryNamesAV2, i),
                        NameB = ReadString(host._savedHistoryNamesBV2, i),
                        KillsA = ReadInt(host._savedHistoryKillsAV2, i),
                        KillsB = ReadInt(host._savedHistoryKillsBV2, i),
                        CasualtiesA = ReadInt(host._savedHistoryCasualtiesAV2, i),
                        CasualtiesB = ReadInt(host._savedHistoryCasualtiesBV2, i),
                        WinsA = ReadInt(host._savedHistoryWinsAV3, i),
                        WinsB = ReadInt(host._savedHistoryWinsBV3, i),
                        LossesA = ReadInt(host._savedHistoryLossesAV3, i),
                        LossesB = ReadInt(host._savedHistoryLossesBV3, i),
                        LastDurationDays = ReadInt(host._savedHistoryDurationV2, i),
                        LastTerritoryA = ReadInt(host._savedHistoryTerritoryAV2, i),
                        LastTerritoryB = ReadInt(host._savedHistoryTerritoryBV2, i),
                        InitialTerritoryA = ReadIntOrDefault(host._savedHistoryInitialTerritoryAV3, i, ReadInt(host._savedHistoryTerritoryAV2, i)),
                        InitialTerritoryB = ReadIntOrDefault(host._savedHistoryInitialTerritoryBV3, i, ReadInt(host._savedHistoryTerritoryBV2, i)),
                        EndDay = ReadInt(host._savedHistoryEndDayV2, i),
                        InvolvesPlayer = ReadInt(host._savedHistoryPlayerV2, i) != 0,
                        StartDay = ReadIntOrDefault(
                            host._savedHistoryStartDayV4,
                            i,
                            Math.Max(0, ReadInt(host._savedHistoryEndDayV2, i) - ReadInt(host._savedHistoryDurationV2, i))),
                        AttackerSide = ReadSide(host._savedHistoryAttackerSideV4, i),
                        HeroDeaths = DeserializeHeroDeaths(ReadString(host._savedHistoryHeroDeathsV4, i))
                    });
                }

                RecentBattleSequence = Math.Max(RecentBattleSequence, host.GetMaxRecentBattleSequence());
                return false;
            }

            int legacyCount = Math.Min(host._savedPairKeys.Count, Math.Min(host._savedCasualtiesA.Count, host._savedCasualtiesB.Count));
            for (int i = 0; i < legacyCount; i++)
            {
                string pairKey = ReadString(host._savedPairKeys, i);
                if (!string.IsNullOrWhiteSpace(pairKey))
                {
                    LegacyRecords[pairKey] = new LegacyPairRecord
                    {
                        InflictedByA = Math.Max(0, ReadInt(host._savedCasualtiesA, i)),
                        InflictedByB = Math.Max(0, ReadInt(host._savedCasualtiesB, i))
                    };
                }
            }

            return LegacyRecords.Count > 0;
        }

        internal int DeleteHistoricalWars(IEnumerable<HistoricalWarEntry> entries)
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

            return HistoricalWars.RemoveAll(record => identities.Contains(
                MakeHistoricalIdentity(record.PairKey, ResolveHistoryStartDay(record), record.EndDay)));
        }

        internal void ClearHistoryAndLegacy()
        {
            HistoricalWars.Clear();
            LegacyRecords.Clear();
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
    }
}
