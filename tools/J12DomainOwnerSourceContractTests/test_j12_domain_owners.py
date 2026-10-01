from __future__ import annotations

import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8-sig")


class J12DomainOwnerSourceContracts(unittest.TestCase):
    def test_direct_diplomacy_actions_have_one_real_owner(self) -> None:
        host = read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs")
        actions = read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs")
        reward = read("RewardSystemBehavior.cs")
        cross_domain = read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyCrossDomainActionOwner.cs")

        for method in (
            "TryExecuteDeclareWar",
            "TryExecuteMakePeace",
            "TryExecuteFormAlliance",
            "TryExecuteBreakAlliance",
            "TryExecuteMakeTrade",
            "TryExecuteCancelTrade",
        ):
            self.assertNotIn("private string " + method + "(", host)
            self.assertEqual(actions.count("private string " + method + "("), 1)
        self.assertNotIn("private static bool ApplyVassalageRewardTags(", reward)
        self.assertNotIn("private static bool ApplyKingdomAnnexationRewardTags(", reward)
        self.assertEqual(cross_domain.count("internal static bool ApplyVassalageRewardTags("), 1)
        self.assertEqual(cross_domain.count("internal static bool ApplyKingdomAnnexationRewardTags("), 1)
        self.assertIn("DiplomacyConversationBridge.ApplyVassalageRewardTags(", reward)
        self.assertIn("DiplomacyConversationBridge.ApplyKingdomAnnexationRewardTags(", reward)

    def test_world_diplomacy_runtime_is_frozen_and_routed_once(self) -> None:
        host = read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs")
        runtime = read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs")
        coordinator = read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyJobRuntimeCoordinator.cs")

        self.assertNotIn("private void TryStartNextLlmJob()", host)
        self.assertNotIn("private void ProcessCompletedJobs()", host)
        self.assertNotIn("void TryStartNextLlmJob()", runtime)
        orchestration = read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs")
        self.assertEqual(orchestration.count("public void TryStartNextLlmJob()"), 1)
        self.assertNotIn("void ProcessCompletedJobs()", runtime)
        self.assertEqual(orchestration.count("public void ProcessCompletedJobs()"), 1)
        lifecycle = read("Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs")
        dispatch = read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyLlmDispatchApplication.cs")
        self.assertIn("WorldDiplomacyJob job = SelectAndPrepareLlmJob(", dispatch)
        self.assertIn("WorldDiplomacyJobSelectionView.For(storage)", dispatch)
        self.assertIn("string.IsNullOrWhiteSpace(candidate.JobId)", dispatch)
        self.assertIn('(lastCacheAffinityKey ?? "").Trim()', dispatch)
        completion = read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyCompletionApplication.cs")
        self.assertIn("WorldDiplomacyCompletionApplication.Run(ref source, this)", orchestration)
        self.assertNotIn("WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion(", runtime)
        self.assertNotIn("WorldDiplomacyRoundLifecycleRules.CommitCompletedLlmJobResult(", runtime)
        self.assertIn("WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion(", completion)
        self.assertIn("orchestration.CommitGeneratedDocument(job, resultContent)", completion)
        self.assertNotIn("CommitCompletedLlmJobResult(", lifecycle)
        self.assertNotIn("commitGeneratedDocument?.Invoke(job, resultContent)", lifecycle)
        self.assertIn("WorldDiplomacyLlmDispatchApplication.Run(ref source, this)", orchestration)
        self.assertNotIn("WorldDiplomacyRoundLifecycleRules.SelectAndPrepareLlmJob(", runtime)
        transport = read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.LlmDispatchSource.cs")
        task = transport[transport.index("_ = Task.Run(async delegate") : transport.index("completedJobs.Enqueue(result);")]
        self.assertNotIn("job.", task)
        self.assertIn("request.JobId", task)
        application = read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyLlmApplication.cs")
        self.assertIn("WorldDiplomacyLlmApplication.ExecuteAsync(", task)
        self.assertIn("detachedRequest, gateway, CancellationToken.None", task)
        self.assertLess(transport.index("WorldDiplomacyLlmApplication.PrepareRequest("), transport.index("_ = Task.Run"))
        self.assertNotIn("requestMessages", task)
        self.assertIn("RuntimeGeneration = request.Trace.RuntimeGeneration", application)
        self.assertIn("gateway.GenerateAsync(", application)
        self.assertNotIn("TaleWorlds", application)
        # DPL-110 retired the unused selector; the live state machine owns selection.
        self.assertNotIn("SelectNextJobId(", coordinator)
        self.assertNotIn("WorldDiplomacyJobQueueItem", coordinator)
        self.assertEqual(dispatch.count("internal static WorldDiplomacyJob SelectAndPrepareLlmJob("), 1)
        self.assertNotIn("public static WorldDiplomacyJob SelectAndPrepareLlmJob(", lifecycle)

    def test_worldmap_protocol_admission_lifecycle_and_delay_are_split(self) -> None:
        host = read("src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.cs")
        admission = read("src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.Admission.cs")
        protocol = read("src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.Protocol.cs")
        queue = read("src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.QueueRuntime.cs")
        events = read("src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.EventLifecycle.cs")
        delayed = read("src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.DelayedRequests.cs")

        self.assertNotIn("public static bool TryApplyWorldMapOrderTagsForExternal(", host)
        self.assertIn("public static bool TryApplyWorldMapOrderTagsForExternal(", admission)
        self.assertIn("WorldMapOrderCoordinator.SelectAdmissionRoute(", admission)
        self.assertNotIn("private static bool TryParseTag(", host)
        self.assertIn("private static bool TryParseTag(", protocol)
        self.assertIn("WorldMapOrderCoordinator.TryParseToken(", protocol)
        self.assertNotIn("private void ProcessQueueTick(", host)
        self.assertIn("private void ProcessQueueTick(", queue)
        self.assertIn("switch (WorldMapOrderCoordinator.ClassifyCommand(command.Kind))", queue)
        self.assertIn("private void OnHourlyTickParty(", events)
        self.assertIn("private void OnCampaignTick(", events)
        self.assertIn("private bool TryStartGovernorExpeditionRequest(", delayed)
        self.assertIn("_governorExpeditionRequestTickets.TryClaim(", delayed)
        self.assertIn("_createCompanionPartyScreenRequests.TryClaimCompletion(", delayed)
        for source in (admission, protocol, queue, events, delayed):
            self.assertNotIn("SyncData(", source)
        for key in (
            "_af_worldmap_party_command_queues_v1",
            "_af_worldmap_player_detachments_v1",
            "_af_worldmap_governor_expeditions_v1",
            "_af_worldmap_foreign_clan_guests_v1",
        ):
            self.assertEqual(host.count(key), 1)


if __name__ == "__main__":
    unittest.main()
