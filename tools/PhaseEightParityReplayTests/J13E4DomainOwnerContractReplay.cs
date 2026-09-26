using System;
using System.IO;

internal static class J13E4DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("J13E4 owner contract: " + label);
        }
        int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
        string Slice(string source, string first, string next)
        {
            int start = source.IndexOf(first, StringComparison.Ordinal);
            int end = start < 0 ? -1 : source.IndexOf(next, start + first.Length, StringComparison.Ordinal);
            Require(start >= 0 && end > start, "method boundary missing: " + first);
            return source.Substring(start, end - start);
        }

        string campaign = Read("src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs");
        string patches = Read("src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs");
        string terminal = Read("AnimusForgeTerminalBehavior.cs");
        string sets = Read("SettlementEntryTroopSelectionBehavior.cs");
        string inspection = Read("TroopInspectionBehavior.cs");
        string entryOwner = Read("src/modules/AF.Module.Settlement/SettlementMissionEntryOwner.cs");
        string followerOwner = Read("src/modules/AF.Module.Settlement/SettlementFollowerMissionOwner.cs");
        string inspectionOwner = Read("src/modules/AF.Module.Settlement/TroopInspectionSessionOwner.cs");
        string siege = Read("SiegeAiInterventionBehavior.cs");
        string castle = Read("CastleAftermathSiegeSceneBridge.cs");

        Require(Count(campaign, "campaignGameStarter.AddBehavior(new SettlementEntryTroopSelectionBehavior())") == 1
            && Count(patches, "SettlementEntryTroopSelectionBehavior.RegisterHarmonyPatches(harmony)") == 1
            && Count(patches, "TroopInspectionBehavior.RegisterHarmonyPatches(harmony)") == 1
            && sets.Contains("CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted)", StringComparison.Ordinal)
            && sets.Contains("CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnSetsMissionEnded)", StringComparison.Ordinal)
            && sets.Contains("CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded)", StringComparison.Ordinal)
            && sets.Contains("_setsOwnSettlementEntryProfile_v1", StringComparison.Ordinal)
            && sets.Contains("_setsOtherSettlementEntryProfile_v1", StringComparison.Ordinal),
            "original Campaign/Harmony/save registration drifted");
        Require(sets.Contains("PatchEncounterEntry(harmony, typeof(TownEncounter)", StringComparison.Ordinal)
            && sets.Contains("PatchEncounterEntry(harmony, typeof(CastleEncounter)", StringComparison.Ordinal)
            && sets.Contains("PatchEncounterEntry(harmony, typeof(VillageEncounter)", StringComparison.Ordinal)
            && sets.Contains("PatchSetsEntryDamage(harmony)", StringComparison.Ordinal)
            && sets.Contains("PatchNativeAlleyCompatibility(harmony)", StringComparison.Ordinal)
            && sets.Contains("if (settlement.IsUnderSiege || settlement.Party?.MapEvent != null || MobileParty.MainParty?.MapEvent != null)", StringComparison.Ordinal)
            && sets.Contains("SiegeAiInterventionBehavior.IsInterventionMissionOpenOrPendingForExternal()", StringComparison.Ordinal),
            "original scene entry patches or GCCZ exclusion drifted");
        Require(sets.Contains("private static readonly SettlementMissionEntryOwner<PendingMissionEntry> _missionEntryOwner", StringComparison.Ordinal)
            && sets.Contains("_missionEntryOwner.CancelVillageAftermath(settlementId, out PendingMissionEntry pending)", StringComparison.Ordinal)
            && sets.Contains("_missionEntryOwner.TryConsumeForMission(current?.StringId,", StringComparison.Ordinal)
            && sets.Contains("_missionEntryOwner.TryExpire(DateTime.UtcNow, Mission.Current != null,", StringComparison.Ordinal)
            && entryOwner.Contains("Pending = null;", StringComparison.Ordinal)
            && entryOwner.Contains("StringComparison.OrdinalIgnoreCase", StringComparison.Ordinal)
            && entryOwner.Contains("StringComparison.Ordinal", StringComparison.Ordinal)
            && siege.Contains("SettlementEntryTroopSelectionBehavior.CancelPendingVillageAftermathMissionEntryForExternal(", StringComparison.Ordinal),
            "one-shot entry ticket, village cancellation or mismatch/expiry consumer disconnected");
        string removed = Slice(sets, "public override void OnAgentRemoved(Agent affectedAgent,", "protected override void OnEndMission()");
        Require(sets.Contains("private static readonly SettlementFollowerMissionOwner<Mission, Agent> _followerOwner", StringComparison.Ordinal)
            && sets.Contains("_followerOwner.IsTracked(Mission.Current, agent.Index, agent)", StringComparison.Ordinal)
            && sets.Contains("_followerOwner.Register(agent.Index, agent)", StringComparison.Ordinal)
            && sets.Contains("_followerOwner.SetActive(mission, active)", StringComparison.Ordinal)
            && removed.Contains("_followerOwner.Remove(affectedAgent.Index, affectedAgent)", StringComparison.Ordinal)
            && sets.Contains("_followerOwner.Clear()", StringComparison.Ordinal)
            && followerOwner.Contains("ReferenceEquals(tracked, agent)", StringComparison.Ordinal)
            && followerOwner.Contains("ReferenceEquals(Mission, currentMission)", StringComparison.Ordinal),
            "selected Agent identity, death, Mission transition or end cleanup disconnected");
        string cleanup = Slice(inspection, "internal static void CleanupRuntime(string reason)",
            "private static void TryCleanupStaleInspectionStateBeforeOpen(string reason)");
        Require(terminal.Contains("TroopInspectionBehavior.OpenInspectionFromTerminal()", StringComparison.Ordinal)
            && terminal.Contains("TroopInspectionBehavior.NeedsEngineTick()", StringComparison.Ordinal)
            && inspection.Contains("_sessionOwner.IsQueuedOpenReady((float)Environment.TickCount / 1000f,", StringComparison.Ordinal)
            && inspection.Contains("_sessionOwner.BeginQueuedOpen()", StringComparison.Ordinal)
            && inspection.Contains("_sessionOwner.Queue((float)Environment.TickCount / 1000f, 0.35f)", StringComparison.Ordinal)
            && inspection.Contains("_sessionOwner.ResetPendingSelection()", StringComparison.Ordinal)
            && cleanup.Contains("_sessionOwner.BeginCleanup()", StringComparison.Ordinal)
            && cleanup.Contains("RestoreAndDestroyHoldingDummyParty(holdingParty", StringComparison.Ordinal)
            && cleanup.Contains("RestoreMainPartyRolesFromSnapshot(runtime, reason)", StringComparison.Ordinal)
            && cleanup.Contains("_sessionOwner.ReleaseTransient()", StringComparison.Ordinal)
            && cleanup.Contains("CleanupMapEventAndPlayerEncounter(mapEvent, reason)", StringComparison.Ordinal)
            && cleanup.Contains("DestroyInspectionDummyParty(dummyParty, dummyId", StringComparison.Ordinal)
            && cleanup.Contains("RestoreExternalCampaignEncounter(runtime, reason)", StringComparison.Ordinal)
            && cleanup.Contains("RestoreMainHeroAfterInspection(reason)", StringComparison.Ordinal),
            "inspection UI queue or temporary party/role/encounter teardown disconnected");
        Require(inspection.Contains("TroopInspectionBehavior.IsCurrentInspectionRuntime(_dummyPartyStringId)", StringComparison.Ordinal)
            && inspection.Contains("RequestCleanup(\"OnRemoveBehavior\")", StringComparison.Ordinal)
            && inspection.Contains("RequestCleanup(\"OnEndMission\")", StringComparison.Ordinal)
            && inspection.Contains("_sessionOwner.ResetForNewCampaign()", StringComparison.Ordinal)
            && Count(sets, "TroopInspectionBehavior.ResetForCampaignTransition()") == 2
            && inspectionOwner.Contains("internal void ResetForNewCampaign()", StringComparison.Ordinal)
            && siege.Contains("TroopInspectionBehavior.TryPrepareExternalInspectionRuntime(", StringComparison.Ordinal)
            && siege.Contains("TroopInspectionBehavior.CancelPreparedExternalInspectionRuntime(", StringComparison.Ordinal)
            && castle.Contains("TroopInspectionBehavior.TryOpenPreparedExternalInspectionMission(", StringComparison.Ordinal),
            "mission removal/load or existing GCCZ thin seam disconnected");
        Require(inspection.Contains("Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>() == null", StringComparison.Ordinal)
            && inspection.Contains("__instance?.GetMissionBehavior<TroopInspectionMissionLogic>() == null", StringComparison.Ordinal)
            && inspection.Contains("[HarmonyPatch(typeof(Mission), \"CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase\")]", StringComparison.Ordinal)
            && inspection.Contains("return true;", StringComparison.Ordinal),
            "inspection damage/death patch no longer guarded to its Mission");

        Console.WriteLine("PASS J13E4DomainOwnerContractReplay Campaign/patch/save/terminal/GCCZ=retained entry/follower/inspection=owned cleanup=connected; source-wiring-only live=NOT_RUN");
    }
}
