using System;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class ShoutBehavior
{


    private void RetireChannelRequestLifetimes()
    {
        _nativeAdmissionOwner.Current?.Lifetime?.Retire();
        _sceneRequestLifetime.Retire();
    }

    private readonly PendingOperationRegistry _pendingMainThreadFunctions = new PendingOperationRegistry(
        error => Logger.Log("ShoutBehavior", "[WARN] operation retirement failed: " + error.Message));

    private void ResetPendingMainThreadFunctions()
    {
        RetireChannelRequestLifetimes();
        _sceneRequestLifetime = new AnimusForge.Refactor.Runtime.ConversationRequestLifetime();
        _pendingMainThreadFunctions.ResetAndClear(() =>
        {
MainThreadActionDrain.ResetQueue();
        });
    }

    internal void RetireCampaignRuntime(string reason)
    {
        _pendingMainThreadFunctions.Seal();
        RetireChannelRequestLifetimes();
        PublicExecutionOrderRuntime.Reset();
        // This captured owner can be retired even after Campaign.Current has been cleared.
        ResetInstanceTransientRuntimeForLoadedSave(reason);
        CloseNativeConversationInput(clearSessionHistory: true);
        RetireChannelRequestLifetimes();
    }
}
