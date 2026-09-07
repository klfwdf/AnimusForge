"""Narrow parser checks; behavioral red/green evidence comes from run.py."""
from __future__ import annotations

import unittest

from run import SCENE_LIFECYCLE_DISPATCHES, declaration, extract, source


class ExtractionTests(unittest.TestCase):
    def test_strings_and_comments_do_not_end_declaration(self):
        method = '''private void M()
{
    var regular = "quoted brace } and escaped quote \\"{";
    var verbatim = @"brace } and "" quote";
    var character = '}';
    // }
    /* } { */
    if (true) { }
}'''
        self.assertEqual(method, declaration("prefix\n" + method + "\nnext", "private void M("))

    def test_optional_missing_declaration_is_empty(self):
        self.assertEqual("", declaration("class Other {}", "void Absent(", optional=True))

    def test_missing_required_declaration_fails(self):
        with self.assertRaises(ValueError):
            declaration("class Other {}", "void Absent(")

    def test_unterminated_declaration_fails(self):
        with self.assertRaises(ValueError):
            declaration("private void M() {", "private void M(")

    def test_blocks_are_contiguous_unmodified_production_substrings(self):
        blocks = extract(None)
        scene = source("ShoutBehavior.cs", None)
        courier = source("CourierDeliveryBehavior.cs", None)
        self.assertIn(blocks["SCENE_BLOCK"], scene)
        self.assertIn(blocks["COURIER_BLOCK"], courier)
        self.assertTrue(blocks["SCENE_BLOCK"].startswith('string output = "";'))
        self.assertTrue(blocks["COURIER_BLOCK"].startswith("if (IsCourierBridgeEnabled()"))
        self.assertEqual(2, blocks["COURIER_BLOCK"].count("await GenerateNpcReplyAsync(request).ConfigureAwait(false);"))
        self.assertIn(blocks["FAIL_METHOD"], courier)
        self.assertIn(blocks["FINALIZE_METHOD"], courier)
        self.assertIn(blocks["STATUS_ENUM"], source("Refactor/Contracts/InteractionContracts.cs", None))

    def test_prompt_factory_and_anonymous_message_adapter_are_production_declarations(self):
        blocks = extract(None)
        scene = source("ShoutBehavior.cs", None)
        for key in ("PUBLIC_SCENE_FACTORY", "MAIN_REPLY_FACTORY", "MAIN_REPLY_METHOD", "CREATE_MESSAGE"):
            self.assertTrue(blocks[key], key)
            self.assertIn(blocks[key], scene)
        self.assertIn(blocks["BUILD_PROMPT"], source("Refactor/Adapters/LegacyConfiguredChatGateway.cs", None))
        self.assertIn(blocks["PORTS_TYPE"], source("Refactor/Adapters/LegacyInteractionPipelineComposition.cs", None))
        self.assertIn(blocks["MAIN_COMPOSER"], source("Refactor/Adapters/LegacyDetachedPromptComposer.cs", None))
        self.assertIn(blocks["POSTPROCESS_COMPOSER"], source("Refactor/Adapters/LegacyDetachedPostprocessPromptComposer.cs", None))

    def test_scene_main_reply_is_generation_only_and_keeps_public_optin(self):
        blocks = extract(None)
        self.assertIn("GenerateSceneShoutMainReplyAsync(", blocks["SCENE_BLOCK"])
        self.assertNotIn("SubmitSceneShoutRefactorOptInForExternalAsync(", blocks["SCENE_BLOCK"])
        self.assertNotIn("sceneShoutDetachedCommitted", blocks["SCENE_BLOCK"])
        self.assertIn("GenerateAsync(", blocks["MAIN_REPLY_METHOD"])
        self.assertNotIn(".Commit(", blocks["MAIN_REPLY_METHOD"])
        self.assertNotIn("MemoryFacade", blocks["MAIN_REPLY_METHOD"])
        self.assertNotIn("ActionPlanExecutor", blocks["MAIN_REPLY_METHOD"])
        scene = source("ShoutBehavior.cs", None)
        self.assertTrue(declaration(scene, "public static Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInForExternalAsync("))
        self.assertIn("postprocessComposer.Compose", blocks["PUBLIC_SCENE_FACTORY"])

    def test_scene_tail_keeps_one_history_and_one_authoritative_queue(self):
        scene = source("ShoutBehavior.cs", None)
        method = declaration(scene, "private async Task HandleGroupResponsePerHeroIndependent(")
        self.assertEqual(1, method.count("RecordSceneReplyHistoryOnMainThreadAsync("))
        self.assertEqual(1, method.count("QueueDeferredScenePostprocessActions("))
        self.assertNotIn("sceneShoutDetachedCommitted", method)
        self.assertNotIn("PersistNpcSpeechToNamedHeroes(", method)
        self.assertIn("if (!historyRecorded)", method)
        self.assertLess(method.index("if (!historyRecorded)"), method.index("QueueDeferredScenePostprocessActions("))
        self.assertIn("postprocessEntityContext, replyIsDirectPlayerResponse", method)
        self.assertIn("replyIsDirectPlayerResponse = firstTurn;", method)
        self.assertIn("relayCandidates: relayCandidatesForNextTurn", method)
        self.assertIn("preprocessRuleHits: postprocessPreprocessHits", method)
        self.assertIn("expectedRuntimeGeneration: sceneReplyGeneration", method)
        self.assertIn("expectedSceneSessionId: sceneReplySessionId", method)
        self.assertLess(method.index('await WaitForScenePostprocessGateAsync("scene_relay")'),
                        method.index("await postprocessTask"))
        self.assertIn("postprocessTask.IsCompleted ?", method)
        history = extract(None)["SCENE_HISTORY_METHOD"]
        self.assertEqual(1, history.count("RecordResponseForAllNearbySafe("))
        self.assertEqual(1, history.count("PersistNpcSpeechToNamedHeroes("))
        self.assertNotIn("PersistPlayerMessageToNamedHeroes", history)

    def test_tail_fixture_executes_unmodified_production_decisions(self):
        blocks = extract(None)
        scene = source("ShoutBehavior.cs", None)
        self.assertIn(blocks["SCENE_TAIL_DECISIONS"], scene)
        self.assertIn(blocks["SCENE_DIRECT_REPLY_ASSIGNMENT"], scene)
        self.assertIn(blocks["BATTLE_QUEUE_METHOD"], source("extensions/AnimusForge.XihaiAction/src/CoreProject/BattleSpeechFrameworkV2.cs", None))

    def test_main_speech_fixture_extracts_real_sanitization_and_asset_codec(self):
        blocks = extract(None)
        scene = source("ShoutBehavior.cs", None)
        for name in ("SCENE_MAIN_SPEECH_SANITIZER", "SCENE_MAIN_SPEECH_QUEUE", "SCENE_SPEECH_SANITIZATION_BLOCK"):
            self.assertTrue(blocks[name], name)
            self.assertIn(blocks[name], scene)
        for signature in ("private static bool ContainsAutoGroupEndSignal(", "private static string StripAutoGroupStopSignal(",
                          "private static string StripAutoGroupRelaySignal(", "private static string StripActionTagsForSceneSpeech("):
            self.assertIn(declaration(scene, signature), blocks["SCENE_SPEECH_STRIPPERS"])
        codec = source("GiveAssetTagCodec.cs", None)
        self.assertIn(declaration(codec, "internal readonly struct GiveAssetTag"), blocks["GIVE_ASSET_CODEC"])
        self.assertIn(declaration(codec, "internal static class GiveAssetTagCodec"), blocks["GIVE_ASSET_CODEC"])

    def test_main_speech_has_no_unvalidated_direct_enqueue_in_default_tail(self):
        method = declaration(source("ShoutBehavior.cs", None), "private async Task HandleGroupResponsePerHeroIndependent(")
        self.assertEqual(1, method.count("QueueSceneMainReplyOnMainThreadAsync("))
        self.assertNotIn("EnqueueSpeechLineWithOptions(", method)
        self.assertIn("cleaned = PrepareSceneMainReplySpeechText(cleaned, flag9, flag10);", method)
        self.assertLess(method.index("ContainsAutoGroupEndSignal(cleaned)"), method.index("PrepareSceneMainReplySpeechText("))
        self.assertLess(method.index("PrepareSceneMainReplySpeechText("), method.index("RecordSceneReplyHistoryOnMainThreadAsync("))
        self.assertLess(method.index("RecordSceneReplyHistoryOnMainThreadAsync("), method.index("QueueSceneMainReplyOnMainThreadAsync("))
        self.assertLess(method.index("if (!speechQueued)"), method.index("QueueDeferredScenePostprocessActions("))

    def test_main_speech_queue_retains_lifetime_and_no_history_commit(self):
        method = extract(None)["SCENE_MAIN_SPEECH_QUEUE"]
        self.assertIn("RunNativeConversationMainThreadFuncAsync(", method)
        self.assertIn("SaveRuntimeGuard.IsCurrentGeneration(generation)", method)
        self.assertIn("Volatile.Read(ref _sceneHistorySessionId)", method)
        self.assertIn("IsSceneConversationEpochCurrent(conversationEpoch)", method)
        self.assertIn("!IsBannerlordMainThreadForNativeActions()", method)
        self.assertIn("IsNativeConversationResponseTargetAvailableForActionDispatch(", method)
        self.assertIn("canStillPublish: CanStillPublish", method)
        self.assertIn("commitHistory: false", method)

    def test_lifecycle_delegates_are_complete_production_blocks_using_initial_capture(self):
        blocks = extract(None)
        method = declaration(source("ShoutBehavior.cs", None), "private async Task HandleGroupResponsePerHeroIndependent(")
        generation_capture = method.index("long sceneReplyGeneration = SaveRuntimeGuard.CaptureGeneration();")
        session_capture = method.index("int sceneReplySessionId = Volatile.Read(ref _sceneHistorySessionId);")
        for name, operation in SCENE_LIFECYCLE_DISPATCHES.items():
            block = blocks[name]
            self.assertTrue(block.startswith("delegate"))
            self.assertIn(block, method)
            dispatch_index = method.index('RunNativeConversationMainThreadFuncAsync("' + operation + '"')
            self.assertLess(generation_capture, dispatch_index)
            self.assertLess(session_capture, dispatch_index)
            self.assertIn("SaveRuntimeGuard.IsCurrentGeneration(sceneReplyGeneration)", block)
            self.assertIn("sceneReplySessionId != Volatile.Read(ref _sceneHistorySessionId)", block)
            self.assertIn("IsSceneConversationEpochCurrent(conversationEpoch)", block)

    def test_known_prompt_baseline_keeps_real_public_factory_and_no_synthetic_fix(self):
        blocks = extract("92ad625a")
        self.assertEqual("", blocks["PRIVATE_SCENE_FACTORY"])
        self.assertIn(blocks["PUBLIC_SCENE_FACTORY"], source("ShoutBehavior.cs", "92ad625a"))
        self.assertNotIn("preparedMainPrompt", blocks["SCENE_BLOCK"])


if __name__ == "__main__":
    unittest.main()
