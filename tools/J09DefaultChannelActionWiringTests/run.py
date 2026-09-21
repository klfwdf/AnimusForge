"""Audit the three default channel action tails against the J09 shared boundary."""
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location(
    "extractor", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)


def require(value, message):
    if not value:
        raise AssertionError(message)


def ordered(text, *markers):
    cursor = -1
    for marker in markers:
        next_cursor = text.find(marker, cursor + 1)
        require(next_cursor > cursor, "missing/out-of-order marker: " + marker)
        cursor = next_cursor


def main():
    shout = (ROOT / "ShoutBehavior.cs").read_text(encoding="utf-8-sig")
    native = (ROOT / "ShoutBehavior.NativeActionCommit.cs").read_text(encoding="utf-8-sig")
    scene = (ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs").read_text(encoding="utf-8-sig")
    courier = extractor.courier_source(None)
    courier_dispatch = (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs").read_text(
        encoding="utf-8-sig")

    native_queue = extractor.declaration(
        shout, "private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync(")
    native_core = extractor.declaration(
        native, "private NativeConversationGameActionResult ApplyNativeConversationGameActionsCore(")
    native_legacy = extractor.declaration(
        shout, "private NativeConversationGameActionResult ApplyNativeConversationGameActionsLegacyCore(")
    native_factory = extractor.declaration(
        shout, "public static LegacyNativeActionPlanExecutor CreateNativeConversationActionPlanExecutorForExternal(")
    require(native_queue.count("ApplyNativeConversationGameActionsCore(") == 1,
            "Native default queue must enter the shared wrapper exactly once")
    require(native_factory.count("ApplyNativeConversationGameActionsLegacyCore(") == 1
            and "ApplyNativeConversationGameActionsCore(" not in native_factory,
            "Detached Native factory must not nest the default compatibility boundary")
    ordered(native_core,
            "actionCommitter.Prepare(raw)",
            "CaptureActionCommit(",
            "new LegacyChannelActionPlanExecutor(",
            "ApplyNativeConversationGameActionsLegacyCore(",
            "actionCommitter.Commit(")
    require("actionCommitter.Prepare(ownerResult.Content)" in native_core,
            "Native wrapper no longer detects unresolved action tags")
    require("ApplyNativeConversationActionTags(" in native_legacy,
            "Native legacy domain owner was detached from the compatibility wrapper")
    require("InteractionResultCommitter" not in native_core,
            "Native default action boundary must not duplicate visible history/AFEF commit")

    scene_queue = extractor.declaration(
        scene, "private Task<int> QueueDeferredScenePostprocessActions(")
    scene_commit = extractor.declaration(
        scene, "private bool CommitDeferredSceneActionPlan(")
    require(scene_queue.count("CommitDeferredSceneActionPlan(") == 1,
            "Scene queue must enter one shared action boundary")
    for direct in ("TryApplyDeferredSceneMoodTag(",
                   "TeamModuleServices.Siege.TryProcessActionTags(",
                   "TryApplyDeferredScenePostprocessActionTagsDirectly(",
                   "TryExecuteDeferredSceneFollowTagsDirectly("):
        require(direct not in scene_queue,
                "Scene queue still executes an old direct path beside the shared boundary: " + direct)
    ordered(scene_queue,
            "NormalizeAutoGroupRelayPostprocessTagsForScene(",
            "CommitDeferredSceneActionPlan(",
            "before_relay_publish")
    ordered(scene_commit,
            "actionCommitter.Prepare(rawTags)",
            "CaptureActionCommit(",
            "new LegacyChannelActionPlanExecutor(",
            "TryApplyDeferredSceneMoodTag(",
            "TeamModuleServices.Siege.TryProcessActionTags(",
            "TryApplyDeferredScenePostprocessActionTagsDirectly(",
            "TryExecuteDeferredSceneFollowTagsDirectly(",
            "EnqueueSpeechLineWithOptions(",
            "actionCommitter.Commit(")
    require("InteractionStatus.NonRetryableFailure" in scene_commit,
            "Queued Scene speech must remain terminal/unknown rather than look confirmed")
    require("InteractionResultCommitter" not in scene_commit,
            "Scene default action boundary must not duplicate history/AFEF")

    courier_wrapper = extractor.declaration(
        courier_dispatch, "private void CommitGeneratedReplyAtRecipient(")
    courier_core = extractor.declaration(
        courier, "private void CommitGeneratedReplyActionsAtRecipientCore(")
    courier_detached = extractor.declaration(
        courier, "private InteractionStatus ExecuteCourierActionPlanForExternal(")
    require(courier.count("CommitGeneratedReplyAtRecipient(session, recipient);") == 2,
            "Courier delivery/return state machine lost a default commit call site")
    require("CommitGeneratedReplyAtRecipient(" not in courier_detached
            and "CommitGeneratedReplyActionsAtRecipientCore(" in courier_detached,
            "Detached Courier path must call the domain core, not nest the default wrapper")
    ordered(courier_wrapper,
            "!session.DeliveryApplied",
            "actionCommitter.Prepare(raw)",
            "CaptureActionCommit(",
            "new LegacyChannelActionPlanExecutor(",
            "CommitGeneratedReplyActionsAtRecipientCore(",
            "actionCommitter.Commit(")
    require("actionCommitter.Prepare(" in courier_wrapper
            and "session.ReplyPostprocessedText" in courier_wrapper,
            "Courier wrapper no longer detects unresolved action tags")
    require("PersistCourierReplyToHistories(" in courier_core,
            "Courier domain core lost its original arrival-time history owner")
    require("InteractionResultCommitter" not in courier_wrapper,
            "Courier default action boundary must not duplicate detached history commit")

    shared_files = [
        ROOT / "src/modules/AF.Module.Actions/Execute/LegacyChannelActionCommitter.cs",
        ROOT / "src/modules/AF.Module.Actions/Execute/LegacyChannelActionPlanExecutor.cs",
        ROOT / "src/modules/AF.Module.Actions/Receipts/ActionExecutionCommitter.cs",
    ]
    require(all("TaleWorlds." not in path.read_text(encoding="utf-8-sig")
                for path in shared_files),
            "Shared Actions owner acquired a Bannerlord live-object dependency")

    print("PASS J09DefaultChannelActionWiring checks=25 channels=3 "
          "singleExecution=1 noMemoryDoubleWrite=1 courierArrival=1 sceneOrder=1 "
          "nativeCompletionOwner=preserved live=NOT_RUN")


if __name__ == "__main__":
    main()
