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
