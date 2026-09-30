using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class CourierDeliveryBehavior
{
    private readonly Dictionary<CourierSession, ConversationRequestLifetime> _courierRequestLifetimes =
        new Dictionary<CourierSession, ConversationRequestLifetime>();

    private ConversationRequestLifetime BeginCourierRequestLifetime(CourierSession session)
    {
        RetireCourierRequestLifetime(session);
        var lifetime = new ConversationRequestLifetime();
        _courierRequestLifetimes.Add(session, lifetime);
        return lifetime;
    }

    private void RetireCourierRequestLifetime(CourierSession session)
    {
        if (session == null || !_courierRequestLifetimes.TryGetValue(session, out var lifetime)) return;
        _courierRequestLifetimes.Remove(session);
        lifetime.Retire();
    }

    private void RetireCourierRequestLifetimes()
    {
        var lifetimes = new List<ConversationRequestLifetime>(_courierRequestLifetimes.Values);
        _courierRequestLifetimes.Clear();
        foreach (var lifetime in lifetimes) lifetime.Retire();
    }
    private readonly PendingOperationRegistry _pendingOwnerPhases = new PendingOperationRegistry(
        error => Log("operation retirement failed: " + error.Message));

    private void ResetPendingOwnerPhases()
    {
        RetireCourierRequestLifetimes();
        _pendingOwnerPhases.ResetAndClear(() =>
        {
            while (MainThreadActions.TryDequeue(out _)) { }
        });
    }

    internal void RetireCampaignRuntime(string reason)
    {
        _pendingOwnerPhases.Seal();
        RetireCourierRequestLifetimes();
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
