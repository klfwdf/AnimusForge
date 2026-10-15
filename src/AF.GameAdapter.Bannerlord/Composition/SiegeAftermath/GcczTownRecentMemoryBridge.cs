using System;
using System.Globalization;
using System.Reflection;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

// All entry points run on the campaign/UI thread. No tick scan, provider request or game-object background access.
internal static class GcczTownRecentMemoryBridge
{
    internal const string SaveKey = "_gcczTownRecentMemory_v1";
    private static TownRecentMemoryStore _store = new TownRecentMemoryStore();
    private static CampaignSaveChunkHelper.StrictStringRead _failedRead;
    // This private field has no public actor accessor on either supported native API.
    private static readonly FieldInfo DecisionMaker = typeof(SiegeAftermathLogEntry).GetField("_decisionMaker", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static void Register(object owner)
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(owner, OnSessionLaunched);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(owner, OnDailyTick);
        CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(owner, OnRaidCompleted);
        CampaignEvents.ForceSuppliesCompletedEvent.AddNonSerializedListener(owner, OnForceSuppliesCompleted);
        CampaignEvents.ForceVolunteersCompletedEvent.AddNonSerializedListener(owner, OnForceVolunteersCompleted);
        CampaignEvents.SettlementEntered.AddNonSerializedListener(owner, OnSettlementEntered);
    }

    internal static void Reset()
    {
        _store = new TownRecentMemoryStore();
        _failedRead = null;
    }

    internal static void SyncData(IDataStore dataStore)
    {
        if (dataStore.IsLoading)
        {
            Reset();
            var read = CampaignSaveChunkHelper.LoadChunkedStringStrict(dataStore, SaveKey);
            if (read.Status == CampaignSaveChunkHelper.StrictReadStatus.Absent) return;
            try
            {
                if (read.Status != CampaignSaveChunkHelper.StrictReadStatus.Complete) throw new InvalidOperationException(read.Error ?? read.Status.ToString());
                var json = JObject.Parse(read.Text);
                if (json["Version"]?.Value<int>() != 1 || json["TrackingStarted"] == null
                    || json["NativeHistoryImported"] == null || json["Towns"] == null
                    || !_store.Restore(json.ToObject<TownRecentMemorySnapshot>()))
                    throw new InvalidOperationException("invalid_or_future_recent_memory_schema");
            }
            catch (Exception ex)
            {
                _store = new TownRecentMemoryStore();
                _failedRead = read;
                Logger.Log("TownRecentMemory", "Recent memory disabled; retaining original namespace: " + ex.Message);
            }
        }
        else if (dataStore.IsSaving)
        {
            if (_failedRead != null)
            {
                if (!_failedRead.EvidenceComplete) throw new InvalidOperationException("recent_memory_incomplete_read_refuses_save");
                foreach (var pair in _failedRead.Records)
                    if (CampaignSaveChunkHelper.GetUtf8ByteCountForDiagnostics(pair.Key) > 12000
                        || (pair.Value is string s && CampaignSaveChunkHelper.GetUtf8ByteCountForDiagnostics(s) > (pair.Key == SaveKey ? 240 : 12000)))
                        throw new InvalidOperationException("recent_memory_unsafe_evidence_refuses_save");
                CampaignSaveChunkHelper.ReplaySafeRawRecords(dataStore, _failedRead.Records, SaveKey);
                return;
            }
            _store.PruneAll(CampaignTime.Now.ToDays);
            CampaignSaveChunkHelper.SaveChunkedStringStrict(dataStore, SaveKey, JsonConvert.SerializeObject(_store.State));
        }
    }

    private static void OnSessionLaunched(CampaignGameStarter starter)
    {
        if (_failedRead != null) return;
        _store.Start(CampaignTime.Now.ToDays);
        if (_store.State.NativeHistoryImported) return;
        try
        {
            var logs = Campaign.Current?.LogEntryHistory?.GameActionLogs;
            if (logs == null) return;
            // One pass on first load of a legacy save. Never reconstruct undocumented GCCZ actions or arrivals.
            foreach (var log in logs)
            {
                if (!TownRecentMemoryStore.InWindow((int)Math.Floor(log.GameTime.ToDays), CampaignTime.Now.ToDays)) continue;
                if (log is VillageStateChangedLogEntry raid && raid.NewState == Village.VillageStates.Looted)
                    Record(raid.Village?.Settlement, raid.RaidLeader, "native.raid", log.GameTime, "log:" + log.Id);
                else if (log is SiegeAftermathLogEntry siege)
                {
                    Hero actor = DecisionMaker?.GetValue(siege) as Hero;
                    // Old player aftermath logs cannot distinguish native choices from GCCZ's internal native settlement.
                    if (actor == null || actor == Hero.MainHero) continue;
                    Record(siege.CapturedSettlement, actor, "native.capture", log.GameTime, "log:" + log.Id + ":capture");
                    Record(siege.CapturedSettlement, actor, "native." + siege.SiegeAftermath, log.GameTime, "log:" + log.Id);
                }
            }
            _store.State.NativeHistoryImported = true;
        }
        catch (Exception ex) { Logger.Log("TownRecentMemory", "Legacy native history import incomplete: " + ex.Message); }
    }

    private static void OnDailyTick()
    {
        if (_failedRead == null) _store.PruneAll(CampaignTime.Now.ToDays);
    }

    private static void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raid)
    {
        if (winnerSide != BattleSideEnum.Attacker || raid?.MapEventSettlement?.Village?.VillageState != Village.VillageStates.Looted) return;
        Record(raid.MapEventSettlement, raid.AttackerSide?.LeaderParty?.LeaderHero, "native.raid", CampaignTime.Now);
    }

    private static void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
    {
        if (_failedRead != null || settlement?.IsTown != true || party == null || (!party.IsCaravan && !party.IsVillager)) return;
        // Identity is retained only in the dedup receipt; public output contains aggregate counts alone.
        _store.Arrival(settlement.StringId, party.StringId + ":" + CampaignTime.Now.ToSeconds.ToString("R", CultureInfo.InvariantCulture),
            party.IsCaravan, CampaignTime.Now.ToDays);
    }

    private static void OnForceSuppliesCompleted(BattleSideEnum winner, ForceSuppliesEventComponent completed)
    {
        if (winner == BattleSideEnum.Attacker)
            Record(completed?.MapEvent?.MapEventSettlement, completed?.MapEvent?.AttackerSide?.LeaderParty?.LeaderHero,
                "native.force_supplies", CampaignTime.Now);
    }

    private static void OnForceVolunteersCompleted(BattleSideEnum winner, ForceVolunteersEventComponent completed)
    {
        if (winner == BattleSideEnum.Attacker)
            Record(completed?.MapEvent?.MapEventSettlement, completed?.MapEvent?.AttackerSide?.LeaderParty?.LeaderHero,
                "native.force_volunteers", CampaignTime.Now);
    }

    internal static void OwnerChanged(Settlement settlement, Hero capturer, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            Record(settlement, capturer, "native.capture", CampaignTime.Now);
    }

    internal static void NativeAftermath(MobileParty attacker, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermath)
        => Record(settlement, attacker?.LeaderHero, "native." + aftermath, CampaignTime.Now);

    internal static void VillageAction(Settlement village, VillageDispositionActionKind kind, string receipt)
    {
        if (TownRecentMemoryStore.IsRecordedVillageAction(kind))
            Record(village, Hero.MainHero, "village." + kind, CampaignTime.Now, "gccz-village:" + receipt);
    }

    internal static void TownAction(Settlement town, string key)
        // Campaign time is paused in a mission: repeated acknowledgements of the same
        // applied disposition have one factual line. Later visits/actions remain distinct.
        => Record(town, Hero.MainHero, "town." + key, CampaignTime.Now);

    private static void Record(Settlement target, Hero actor, string action, CampaignTime when, string receipt = null)
    {
        if (_failedRead != null || target == null || actor == null) return;
        bool villageAction = action.StartsWith("village.", StringComparison.Ordinal) || action == "native.raid"
            || action == "native.force_supplies" || action == "native.force_volunteers";
        if (villageAction ? !target.IsVillage : !target.IsTown) return;
        Settlement town = target.IsVillage ? target.Village?.Bound : target;
        if (town?.IsTown != true) return;
        try
        {
            _store.Add(new TownRecentMemoryFact {
                Id = receipt ?? (target.StringId + ":" + actor.StringId + ":" + action + ":" + when.ToSeconds.ToString("R", CultureInfo.InvariantCulture)),
                TownId = town.StringId, TownName = town.Name?.ToString(),
                VillageName = target.IsVillage ? target.Name?.ToString() : "", ActorName = actor.Name?.ToString(), Action = action,
                Day = (int)Math.Floor(when.ToDays), Year = when.GetYear, Season = (int)when.GetSeasonOfYear, DayOfSeason = when.GetDayOfSeason + 1
            }, CampaignTime.Now.ToDays);
        }
        catch (Exception ex) { Logger.Log("TownRecentMemory", "Confirmed event capture failed: " + ex.Message); }
    }

    internal static string Build(Settlement town, TownPromptTextCatalog catalog)
    {
        if (town?.IsTown != true) return "";
        if (_failedRead != null) return TownRecentMemoryText.Unavailable(catalog);
        double now = CampaignTime.Now.ToDays;
        return TownRecentMemoryText.Build(_store.Read(town.StringId, now), _store.HasCompleteArrivalWindow(now), catalog);
    }
}
