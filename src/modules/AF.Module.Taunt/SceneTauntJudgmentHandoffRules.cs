using System;

namespace AnimusForge;

// Event-time matching of the saved pending incident; no world scan or crime-decay guess.
internal static class SceneTauntJudgmentHandoffRules
{
    internal static bool ShouldHandOff(bool pending, string reason, string pendingFactionId,
        string pendingSettlementId, string judgmentFactionId, string judgmentSettlementId,
        bool isTown, bool samePlayerFaction)
    {
        return pending && isTown && samePlayerFaction
            && string.Equals(reason, "scene_taunt_lord_scene", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(pendingFactionId)
            && !string.IsNullOrWhiteSpace(pendingSettlementId)
            && string.Equals(pendingFactionId, judgmentFactionId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(pendingSettlementId, judgmentSettlementId, StringComparison.OrdinalIgnoreCase);
    }
}
