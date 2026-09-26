using System;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

public partial class ShoutBehavior
{
    internal static void SubmitModuleNativeDialogue(CoreDialogueOperation operation)
    {
        ShoutBehavior owner = CurrentInstance;
        if (owner == null)
        {
            operation.Finish("native.owner_unavailable");
            return;
        }
        long generation = SaveRuntimeGuard.CaptureGeneration();
        long conversationEpoch = owner._nativeAdmissionOwner.ConversationEpoch;
        long presentationRevision = owner._nativeAdmissionOwner.PresentationRevision;
        _ = owner.RunModuleNativeDialogueAsync(operation, generation, conversationEpoch, presentationRevision);
    }

    private async Task RunModuleNativeDialogueAsync(CoreDialogueOperation operation, long generation, long conversationEpoch, long presentationRevision)
    {
        try
        {
            // Existing retirement-aware queue, not a second API queue. Validation and claim share
            // the main-thread phase; cancellation that wins first prevents admission and all effects.
            Task<string> execution = await RunNativeConversationMainThreadFuncAsync(
                "module_native_admission", "module", -1, () =>
                {
                    if (!ReferenceEquals(CurrentInstance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation)
                        || !_nativeAdmissionOwner.IsConversationEpochCurrent(conversationEpoch)
                        || !_nativeAdmissionOwner.IsPresentationCurrent(presentationRevision)
                        || !_pendingMainThreadFunctions.Accepting || !operation.TryBegin())
                        return null;
                    return SubmitNativeConversationAdmittedAsync(operation.PlayerText, null, null, null,
                        null, false, moduleOperation: operation);
                }, (Task<string>)null).ConfigureAwait(false);
            if (execution != null) await execution.ConfigureAwait(false);
            // The legacy string can be empty or an error message. Never infer success from it.
            operation.Finish(execution == null ? "native.not_started" : "native.owner_completion_missing");
        }
        catch (NativeConversationAdmissionException error)
        {
            operation.Finish(error.ReasonCode == "native.busy" ? "native.busy" : "native.admission_failed");
        }
        catch (Exception)
        {
            // No exception text, Prompt, paths or provider details cross the public boundary.
            // A confirmed owner receipt wins even if later non-authoritative work failed.
            operation.Finish("native.execution_failed");
        }
    }
}
