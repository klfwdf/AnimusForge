using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal interface IDiplomacySilenceEffectHost
{
    IReadOnlyList<string> KingdomTownIds(string kingdomId);
}

internal interface IDiplomacyEventCultureHost
{
    string EventCulture(string kingdomId);
}

internal interface IDiplomacyEventCalendarHost
{
    string FormatEventDate(int day);
}

internal interface IDiplomacyEventHost
{
    long EventGeneration { get; }
    string CaptureEventWorld(IReadOnlyCollection<string> participants);
    bool TryReadEventEffect(DiplomacyEventEffectState effect, out float value, out float minimum, out float maximum);
    void ApplyEventEffectDelta(DiplomacyEventEffectState effect, float delta);
    bool ShowEvent(DiplomacyEventPresentation data, out string imageId, Action acknowledged, IReadOnlyList<string> recentImages);
}

internal static class DiplomacyEventEffects
{
    internal static bool Validate(DiplomacyEventEffectState effect, ISet<string> participants, int day, out string reason)
    {
        reason = "invalid_effect";
        if (effect == null || string.IsNullOrWhiteSpace(effect.Id) || string.IsNullOrWhiteSpace(effect.EventId)
            || string.IsNullOrWhiteSpace(effect.Reason) || effect.Reason.Length > 600 || participants?.Contains(effect.KingdomId) != true
            || float.IsNaN(effect.Delta) || float.IsInfinity(effect.Delta) || effect.Delta == 0
            || effect.StartedDay != day || effect.ExpiresDay < day || effect.ExpiresDay > day + 21) return false;
        float limit;
        switch (effect.Kind)
        {
            case "ruler_relation":
                limit = 5;
                if (!participants.Contains(effect.OtherKingdomId) || effect.OtherKingdomId == effect.KingdomId
                    || string.IsNullOrEmpty(effect.RulerId) || string.IsNullOrEmpty(effect.OtherRulerId)
                    || effect.Delta != Math.Truncate(effect.Delta) || effect.ExpiresDay != day) return false;
                break;
            case "town_loyalty": case "town_security": limit = 1; break;
            case "town_prosperity": limit = 10; break;
            case "town_food": limit = 5; break;
            default: return false;
        }
        if (Math.Abs(effect.Delta) > limit || (effect.Kind != "ruler_relation" && (string.IsNullOrEmpty(effect.SettlementId) || effect.ExpiresDay <= day))) return false;
        reason = ""; return true;
    }

    // Main-thread state transition. A receipt is durable before another request
    // may publish. Unknown interrupted applications are never automatically replayed.
    internal static bool Pulse(DiplomacyEventEffectState effect, int day, IDiplomacyEventHost host)
    {
        if (effect == null || host == null || effect.Status is "removed" or "expired" or "unavailable" or "uncertain" or "complete"
            || effect.LastAppliedDay >= day) return false;
        if (effect.Status == "applying") { effect.Status = "uncertain"; return false; }
        if (day > effect.ExpiresDay || (day == effect.ExpiresDay && effect.Kind != "ruler_relation"))
        { effect.Status = "expired"; return false; }
        if (!host.TryReadEventEffect(effect, out float before, out float minimum, out float maximum))
        { effect.Status = "unavailable"; effect.Failure = "effect_target_changed"; return false; }
        float delta = Math.Max(minimum, Math.Min(maximum, before + effect.Delta)) - before;
        effect.LastAppliedDay = day;
        if (Math.Abs(delta) < 0.0001f) { effect.Status = effect.Kind == "ruler_relation" ? "complete" : "active"; return false; }
        effect.Status = "applying";
        try { host.ApplyEventEffectDelta(effect, delta); }
        catch (Exception ex) { effect.Failure = ex.GetType().Name; }
        if (!host.TryReadEventEffect(effect, out float after, out _, out _))
        { effect.Status = "uncertain"; return false; }
        float applied = after - before;
        effect.AppliedTotal += applied;
        effect.Status = effect.Kind == "ruler_relation" ? "complete" : "active";
        return Math.Abs(applied) > 0.0001f;
    }
}
