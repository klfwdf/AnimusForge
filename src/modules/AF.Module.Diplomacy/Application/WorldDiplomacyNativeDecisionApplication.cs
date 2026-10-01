using System;
using System.Collections.Generic;
using System.Globalization;
namespace AnimusForge;

internal interface IWorldDiplomacyNativeDecisionPort
{
    IEnumerable<IReadOnlyList<int>> Queues();
    WorldDiplomacyNativeDecisionSnapshot Capture(int token);
    string Reason(int token, bool incomingPlayerOffer, string action);
    void Remove(int token);
    string Describe(int token);
    void Log(string message);
}
internal static class WorldDiplomacyNativeDecisionApplication
{
    internal static bool Capture(IWorldDiplomacyNativeDecisionPort port, int token, IWorldDiplomacyOrchestration orchestration)
    {
        var snapshot = port.Capture(token);
        if (!WorldDiplomacyEventRules.TryNativeSignal(snapshot, out string source, out string target)) return false;
        string reason = port.Reason(token, WorldDiplomacyEventRules.IsIncomingPlayerOffer(snapshot), snapshot.Action);
        return orchestration.RecordNativeSignal(source, target, snapshot.Action, reason);
    }
    internal static void Sanitize(IWorldDiplomacyNativeDecisionPort port, IWorldDiplomacyOrchestration orchestration)
    {
        int removed = 0;
        foreach (var queue in port.Queues())
        foreach (int token in queue)
        {
            try
            {
                // A rejected signal still removes the native decision. A failed capture leaves that item queued.
                Capture(port, token, orchestration);
                port.Remove(token);
                removed++;
            }
            catch (Exception ex) { port.Log("remove queued native diplomacy decision failed " + port.Describe(token) + " error=" + ex.Message); }
        }
        if (removed > 0) port.Log("removed queued native diplomacy decisions count=" + removed.ToString(CultureInfo.InvariantCulture));
    }
}
