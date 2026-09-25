using System;
using System.IO;

internal static class J13D4DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("J13D4 owner contract: " + label);
        }

        string composition = Read("src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs");
        string host = Read("WarStats/AfWarStatsBehavior.cs");
        string owner = Read("src/modules/AF.Module.WarStats/WarStatsLedgerOwner.cs");
        string terminal = Read("WarStats/AfWarStatsPopupVM.cs");

        Require(composition.Contains("campaignGameStarter.AddBehavior(new AfWarStatsBehavior())", StringComparison.Ordinal)
            && host.Contains("public sealed partial class AfWarStatsBehavior : CampaignBehaviorBase", StringComparison.Ordinal)
            && host.Contains("CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded)", StringComparison.Ordinal)
            && host.Contains("CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared)", StringComparison.Ordinal)
            && host.Contains("CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace)", StringComparison.Ordinal)
            && host.Contains("CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, OnBeforeHeroKilled)", StringComparison.Ordinal),
            "original behavior and campaign event entrypoints remain registered");
        Require(host.Contains("private readonly WarStatsLedgerOwner _ledger = new()", StringComparison.Ordinal)
            && host.Contains("_activeWars => _ledger.ActiveWars", StringComparison.Ordinal)
            && host.Contains("_historicalWars => _ledger.HistoricalWars", StringComparison.Ordinal)
            && owner.Contains("internal Dictionary<string, WarStatsRecord> ActiveWars", StringComparison.Ordinal)
            && owner.Contains("internal List<HistoricalWarRecord> HistoricalWars", StringComparison.Ordinal)
            && !host.Contains("private readonly Dictionary<string, WarStatsRecord> _activeWars", StringComparison.Ordinal),
            "single owner holds active and historical state");
        Require(host.Contains("_ledger.ArchiveAndRemove(pairKey, record, endDay)", StringComparison.Ordinal)
            && host.Contains("_ledger.ApplyBattleStats(record, directOrder", StringComparison.Ordinal)
            && host.Contains("_ledger.UpsertHeroDeath(record", StringComparison.Ordinal)
            && host.Contains("_ledger.RecordRecentHeroBattle(record", StringComparison.Ordinal)
            && host.Contains("_legacyMigrationPending = _ledger.RestoreSavedData(this)", StringComparison.Ordinal)
            && host.Contains("_ledger.PrepareSaveData(this)", StringComparison.Ordinal),
            "campaign facts feed ledger decisions and save facade");
        Require(host.Contains("_af_war_stats_pair_keys_v1", StringComparison.Ordinal)
            && host.Contains("_af_war_stats_data_version_v2", StringComparison.Ordinal)
            && host.Contains("_af_war_stats_active_hero_deaths_v4", StringComparison.Ordinal)
            && host.Contains("_af_war_stats_recent_battle_sequence_v5", StringComparison.Ordinal)
            && host.Contains("_af_war_stats_active_recent_hero_battles_v5", StringComparison.Ordinal),
            "original v1-v5 IDataStore identities remain on behavior");
        Require(terminal.Contains("behavior.BuildCurrentWars()", StringComparison.Ordinal)
            && terminal.Contains("behavior.BuildHistoricalWars()", StringComparison.Ordinal)
            && terminal.Contains("behavior.DeleteHistoricalWars(selectedEntries)", StringComparison.Ordinal)
            && terminal.Contains("behavior.ClearAllRecords()", StringComparison.Ordinal)
            && terminal.Contains("AfWarStatsBehavior.MakeHistoricalIdentity(entry.PairKey, entry.StartDay, entry.EndDay)", StringComparison.Ordinal),
            "terminal projection, identity-scoped delete and clear still consume original behavior");

        Console.WriteLine("PASS J13D4DomainOwnerContractReplay campaign=1 ledger=1 save=v1-v5 terminal=projection/delete/clear; source-wiring-only live=NOT_RUN");
    }
}
