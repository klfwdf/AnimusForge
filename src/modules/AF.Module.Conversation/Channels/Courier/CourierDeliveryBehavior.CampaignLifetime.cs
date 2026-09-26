using System;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class CourierDeliveryBehavior
{
    private readonly PendingOperationRegistry _pendingOwnerPhases = new PendingOperationRegistry(
        error => Log("operation retirement failed: " + error.Message));

    private void ResetPendingOwnerPhases()
    {
        _pendingOwnerPhases.ResetAndClear(() =>
        {
            while (MainThreadActions.TryDequeue(out _)) { }
        });
    }

    internal void RetireCampaignRuntime(string reason)
    {
        _pendingOwnerPhases.Seal();
        try
        {
            while (MainThreadActions.TryDequeue(out _)) { }
            ResetTransientRuntimeForLoadedSave(reason);
        }
        finally
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }
}
