using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.Refactor.Domain;

internal static class DiplomacyConvergenceRules
{
    internal const int OrdinaryRoundLimit = 2;

    // Sources, not time, requests, speaker counts or document totals, define rounds.
    // Several countries answering the same claim occupy the same round.
    internal static void Record(DiplomacyEventConvergence state, WorldDiplomacyDocument doc,
        string rootId, IEnumerable<string> answerSources)
    {
        if (doc?.IsReadyForPublication != true || state.Ending || state.CompletedRounds >= OrdinaryRoundLimit) return;
        state.ReceiptIndex ??= state.Receipts.ToDictionary(x => x.DocumentId, StringComparer.Ordinal);
        if (state.ReceiptIndex.ContainsKey(doc.DocumentId)) return;
        DiplomacyNegotiationReceipt parent = null;
        foreach (string id in answerSources.Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal))
            if (state.ReceiptIndex.TryGetValue(id, out var candidate) && candidate.Author != doc.AuthorKingdomId
                && (parent == null || candidate.Round > parent.Round
                    || candidate.Round == parent.Round && candidate.Answer && !parent.Answer)) parent = candidate;
        if (parent == null && doc.DocumentId != rootId) return;
        var receipt = new DiplomacyNegotiationReceipt {
            DocumentId = doc.DocumentId, Author = doc.AuthorKingdomId, SourceId = parent?.DocumentId ?? "",
            Round = parent == null ? 1 : Math.Min(OrdinaryRoundLimit, parent.Round + (parent.Answer ? 1 : 0)),
            Answer = parent != null && !parent.Answer
        };
        state.Receipts.Add(receipt); state.ReceiptIndex.Add(receipt.DocumentId, receipt);
        if (receipt.Answer) state.CompletedRounds = Math.Max(state.CompletedRounds, receipt.Round);
    }

    // Implementation parameters: relation contributes at most 6, strength 4,
    // other wars 4, common enemy 3 and existing treaties 3; combined transfer
    // capped at 12 points. Base tiers remain dominant. No prestige input.
    internal static (int threat, int agreement, int deadlock) Weights(int tendency,
        float realmRelation, int rulerRelation, float strength, float opposingStrength,
        int otherWars, bool commonEnemy, bool allied, bool trading,
        bool threatLegal, bool agreementLegal)
    {
        int threat = tendency < 50 ? 50 : tendency == 50 ? 60 : 70;
        int agreement = 90 - threat;
        double relation = Math.Max(-100, Math.Min(100, realmRelation * 0.8 + rulerRelation * 0.2));
        double power = strength > 0 && opposingStrength > 0 ? (strength - opposingStrength) / (strength + opposingStrength) : 0;
        int transfer = (int)Math.Round(-relation * 0.06 + Math.Max(-4, Math.Min(4, power * 8))
            - Math.Min(4, Math.Max(0, otherWars) * 2) - (commonEnemy ? 3 : 0) - (allied || trading ? 3 : 0));
        transfer = Math.Max(-12, Math.Min(12, transfer));
        return (threatLegal ? threat + transfer : 0, agreementLegal ? agreement - transfer : 0, 10);
    }

    internal static string Select((int threat, int agreement, int deadlock) weights, int draw) =>
        draw < weights.threat ? "threat" : draw < weights.threat + weights.agreement ? "agreement" : "deadlock";
}
