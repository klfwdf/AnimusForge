using System;
using System.IO;

internal static class J13E2DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("J13E2 owner contract: " + label);
        }
        string Slice(string source, string first, string next)
        {
            int start = source.IndexOf(first, StringComparison.Ordinal);
            int end = start < 0 ? -1 : source.IndexOf(next, start + first.Length, StringComparison.Ordinal);
            Require(start >= 0 && end > start, "method boundary missing: " + first);
            return source.Substring(start, end - start);
        }
        int Count(string source, string value) =>
            source.Split(value, StringSplitOptions.None).Length - 1;

        string host = Read("SceneTauntBehavior.cs");
        string campaign = Read("src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs");
        string patches = Read("src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs");
        string context = Read("src/modules/AF.Module.Taunt/ScenePeaceConflictContextOwner.cs");
        string penalty = Read("src/modules/AF.Module.Taunt/SceneTauntPenaltyLedgerOwner.cs");
        string lifecycle = Read("src/modules/AF.Module.Taunt/SceneTauntConflictLifecycleOwner.cs");

        Require(Count(campaign, "campaignGameStarter.AddBehavior(new SceneTauntBehavior())") == 1
            && host.Contains("CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted)", StringComparison.Ordinal)
            && host.Contains("mission2.GetMissionBehavior<SceneTauntMissionBehavior>() == null", StringComparison.Ordinal)
            && host.Contains("mission2.AddMissionBehavior(new SceneTauntMissionBehavior())", StringComparison.Ordinal)
            && host.Contains("DuelBehavior.IsAnimusForgeIndependentDuelMission(mission2)", StringComparison.Ordinal),
            "original Campaign/Mission adapters or duel exclusion drifted");

        foreach (string patch in new[] { "SceneTauntWieldBlockPatch", "SceneTauntMissionDifficultyPatch",
            "SceneTauntNativeConversationBlockPatch", "SceneTauntLeaveMissionBlockPatch", "SceneTauntFightAutoEndDelayPatch" })
            Require(Count(patches, patch + ".EnsurePatched()") == 1
                && Count(host, "public static class " + patch) == 1,
                "original Harmony registration drifted: " + patch);
        Require(host.Contains("TryToWieldWeaponInSlot", StringComparison.Ordinal)
            && host.Contains("TryToWieldWeaponInHand", StringComparison.Ordinal)
            && host.Contains("WieldInitialWeapons", StringComparison.Ordinal)
            && host.Contains("GetDamageMultiplierOfCombatDifficulty", StringComparison.Ordinal)
            && host.Contains("AccessTools.Method(typeof(MissionFightHandler), \"OnMissionTick\")", StringComparison.Ordinal)
            && host.Contains("StartConversation", StringComparison.Ordinal)
            && host.Contains("CheckAndTriggerConversationWithRivalThug", StringComparison.Ordinal)
            && host.Contains("StartCommonAreaBattle", StringComparison.Ordinal)
            && host.Contains("AccessTools.Method(type, \"OnEndMissionRequest\")", StringComparison.Ordinal)
            && host.Contains("AccessTools.Method(typeof(MissionFightHandler), \"OnEndMissionRequest\")", StringComparison.Ordinal)
            && !host.Contains("PatchAll(", StringComparison.Ordinal),
            "Harmony target or bounded registration contract drifted");

        string physical = Slice(host, "private bool TryStartConflictFromPhysicalAttack(", "private bool ShouldSuppressDuplicateNativeCriminalConflict(");
        string verbal = Slice(host, "private static bool TryStartSceneTauntFight(", "private static bool HasSceneTauntWarning(");
        string end = Slice(host, "private void ClearRuntimeState(", "private void RememberSceneNotableHitLethality(");
        Require(host.Contains("sameSettlement: encounter.Settlement == settlement", StringComparison.Ordinal)
            && host.Contains("hasBattle: PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null", StringComparison.Ordinal)
            && host.Contains("hasSiegeHandler: mission.GetMissionBehavior<CampaignSiegeStateHandler>() != null", StringComparison.Ordinal)
            && host.Contains("ScenePeaceConflictContextOwner.CanInitialize(in facts)", StringComparison.Ordinal)
            && context.Contains("CanInitializePhysical(bool enabled, in ScenePeaceConflictContext facts)", StringComparison.Ordinal)
            && physical.Contains("CanInitializePeaceSceneConflict(Settlement.CurrentSettlement, physicalAttack: true)", StringComparison.Ordinal)
            && verbal.Contains("fromVerbalTaunt: true", StringComparison.Ordinal)
            && !verbal.Contains("IsPeaceSceneConflictEnabled()", StringComparison.Ordinal),
            "peace scene facts, physical MCM off or verbal exception are disconnected");

        Require(host.Contains("private readonly SceneTauntPenaltyLedgerOwner _penaltyLedger", StringComparison.Ordinal)
            && host.Contains("_penaltyLedger.CaptureDeferredCrime()", StringComparison.Ordinal)
            && host.Contains("_penaltyLedger.RestoreDeferredCrime(_pendingDeferredCrimeByFactionStorage)", StringComparison.Ordinal)
            && host.Contains("_penaltyLedger.ReserveNativeCommit(text, num2, num3)", StringComparison.Ordinal)
            && host.Contains("_penaltyLedger.RestoreFailedNativeCommit(text, num)", StringComparison.Ordinal)
            && host.Contains("_penaltyLedger.AwardCriminalKnockdownTrust(text, out int num3)", StringComparison.Ordinal)
            && host.Contains("_sceneTauntDeferredCrimeByFaction_v1", StringComparison.Ordinal)
            && host.Contains("_sceneTauntCriminalTrustRewardTenthBySettlement_v1", StringComparison.Ordinal)
            && penalty.Contains("internal sealed class SceneTauntPenaltyLedgerOwner", StringComparison.Ordinal),
            "production crime/trust consumers or existing save identities are disconnected");

        Require(host.Contains("private readonly SceneTauntConflictLifecycleOwner _conflictLifecycle", StringComparison.Ordinal)
            && host.Contains("_conflictLifecycle.TryBeginUnarmed()", StringComparison.Ordinal)
            && host.Contains("_conflictLifecycle.TryBeginArmedCarryover()", StringComparison.Ordinal)
            && host.Contains("_conflictLifecycle.TryEscalate()", StringComparison.Ordinal)
            && end.Contains("_conflictLifecycle.End(preserveArmedDefeatState)", StringComparison.Ordinal)
            && end.Contains("ClearOwnedSettlementPassiveAttackState(\"clear_runtime_state\")", StringComparison.Ordinal)
            && host.Contains("RestoreOwnedSettlementPassiveAttackTeams()", StringComparison.Ordinal)
            && host.Contains("SetIsEnemyOf(_ownedSettlementPassiveEnemyTeam, isEnemyOf: false)", StringComparison.Ordinal)
            && host.Contains("agent.SetTeam(item.Value, sync: true)", StringComparison.Ordinal)
            && lifecycle.Contains("internal bool TryEscalate()", StringComparison.Ordinal),
            "Mission transition or owned-settlement team restoration is disconnected");

        Console.WriteLine("PASS J13E2DomainOwnerContractReplay campaign=1 mission=guarded patches=5 context/MCM=owner penalty/save=owner lifecycle/recovery=owner; source-wiring-only live=NOT_RUN");
    }
}
