using System;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private readonly PendingOperationRegistry _pendingMainThreadFunctions = new PendingOperationRegistry(
        error => Logger.Log("ShoutBehavior", "[WARN] operation retirement failed: " + error.Message));

    private void ResetPendingMainThreadFunctions()
    {
        _pendingMainThreadFunctions.ResetAndClear(() =>
        {
            while (_mainThreadActions.TryDequeue(out _)) { }
        });
    }

    internal void RetireCampaignRuntime(string reason)
    {
        _pendingMainThreadFunctions.Seal();
        // This captured owner can be retired even after Campaign.Current has been cleared.
        ResetInstanceTransientRuntimeForLoadedSave(reason);
        CloseNativeConversationInput(clearSessionHistory: true);
    }
}
