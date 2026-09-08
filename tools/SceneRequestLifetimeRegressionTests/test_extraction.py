"""Check that the fixture runs production boundaries, not rewritten sample algorithms."""
import unittest
import run


class ExtractionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scene = run.ex.source("ShoutBehavior.cs", None)
        cls.compat = run.ex.source("extensions/AnimusForge.XihaiAction/src/Runtime/AfCompatV130.cs", None)
        cls.generated = run.generate()

    def test_request_methods_are_verbatim(self):
        for signature in ("private sealed class ScenePlayerShoutRequest", "internal object CaptureScenePlayerShoutRequestForReplay(",
                          "private ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(", "private bool IsScenePlayerShoutRequestCurrent(",
                          "internal bool TryReplayCapturedScenePlayerShout(", "private async Task ProcessShoutConfirmedInternal(",
                          "private async Task ProcessCapturedScenePlayerShoutAsync("):
            with self.subTest(signature=signature):
                self.assertIn(run.ex.declaration(self.scene, signature), self.generated)

    def test_gate_and_ui_methods_are_verbatim(self):
        for signature in ("private void RegisterScenePostprocessGateTask(", "private Task GetScenePostprocessGateTask(",
                          "private void ForceClearScenePostprocessGate(", "private async Task WaitForScenePostprocessGateAsync(",
                          "private void BeginShoutProcessing(", "private void EndShoutProcessing(", "private void ResumeGame("):
            with self.subTest(signature=signature):
                self.assertIn(run.ex.declaration(self.scene, signature), self.generated)

    def test_compat_observers_and_replay_are_verbatim(self):
        for signature in ("private static bool ObserveAcceptedPlayerShout(", "private static void ObserveRecordedPlayerMessage(",
                          "internal static bool TryReplayOriginalPlayerShout(", "private static void ResumeAfShoutUi("):
            with self.subTest(signature=signature):
                self.assertIn(run.ex.declaration(self.compat, signature), self.generated)

    def test_mutation_splice_is_after_real_target_acceptance(self):
        method = run.ex.declaration(self.scene, "private void ProcessCurrentScenePlayerShout(")
        marker = "\t\tif (!TryBuildSceneShoutConversationScope("
        self.assertEqual(1, method.count(marker))
        self.assertIn(method.split(marker)[0], self.generated)
        self.assertIn("int conversationEpoch = BeginNewPlayerDrivenSceneConversationEpoch();", method.split(marker)[0])
        # Downstream game owners are intentionally NOT executed by this input-lifetime test.
        self.assertNotIn("ActivateMultiSceneMovementSuppression(new int[1]", self.generated)

    def test_capture_precedes_first_await(self):
        method = run.ex.declaration(self.scene, "private async Task ProcessShoutConfirmedInternal(")
        self.assertLess(method.index("CaptureScenePlayerShoutRequest("), method.index("await "))
        resumed = run.ex.declaration(self.scene, "private async Task ProcessCapturedScenePlayerShoutAsync(")
        self.assertLess(resumed.index("RunNativeConversationMainThreadFuncAsync("), resumed.index("IsScenePlayerShoutRequestCurrent(request)"))
        self.assertNotIn("_activeShoutTargetingContext =", resumed)

    def test_frozen_bridge_is_forwarded_through_clone(self):
        host = run.ex.source("extensions/AnimusForge.XihaiAction/src/Runtime/BattleSpeechRuntimeHost.cs", None)
        clone = run.ex.source("extensions/AnimusForge.XihaiAction/src/Runtime/BattleSpeechMissionBehavior.V2.cs", None)
        self.assertIn("OriginalScenePlayerShoutRequest = capturedScenePlayerShoutRequest", host)
        self.assertIn("OriginalScenePlayerShoutRequest = input.OriginalScenePlayerShoutRequest", clone)
        replay = run.ex.declaration(self.compat, "internal static bool TryReplayOriginalPlayerShout(")
        self.assertIn("input.OriginalScenePlayerShoutRequest", replay)
        self.assertNotIn("_patchedMethod.Invoke", replay)
        self.assertNotIn("?? _behaviorInstance", replay)

    def test_own_resume_does_not_cancel_accepted_request(self):
        for signature in ("private void ResumeGame(", "private void EndShoutProcessing("):
            self.assertNotIn("_scenePlayerInputSequence", run.ex.declaration(self.scene, signature))
        self.assertIn("_sceneShoutProcessingSequence", run.ex.declaration(self.scene, "private void EndShoutProcessing("))


if __name__ == "__main__":
    unittest.main()
