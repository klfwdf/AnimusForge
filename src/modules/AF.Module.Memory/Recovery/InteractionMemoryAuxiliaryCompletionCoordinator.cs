using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

internal enum MemoryAuxiliaryReceiptOutcome { Accepted, Duplicate, Unavailable }

internal readonly struct MemoryAuxiliaryCompletionReceipt
{
    internal readonly int Accepted, Duplicate, Unavailable;
    internal MemoryAuxiliaryCompletionReceipt(int accepted, int duplicate, int unavailable)
    { Accepted = accepted; Duplicate = duplicate; Unavailable = unavailable; }
    internal bool HasAttempt => Accepted + Duplicate + Unavailable > 0;
}

// Coordinates memory publication proof and the independent Social receipt. It owns no
// Social roll/state and never receives an action executor or replays a game action.
internal static class InteractionMemoryAuxiliaryCompletionCoordinator
{
    internal static MemoryAuxiliaryCompletionReceipt CompleteInitial(
        InteractionMemoryRecoverySeed seed, string recoveryId, string payloadHash,
        Func<string, string, string, string, bool> hasPublishedDailyComponent,
        Func<InteractionMemoryRecoverySeed, string, string, string, MemoryAuxiliaryReceiptOutcome> notify)
    {
        if (seed == null || string.IsNullOrWhiteSpace(recoveryId) || string.IsNullOrWhiteSpace(payloadHash))
            return default;
        int accepted = 0, duplicate = 0, unavailable = 0;
        var attemptedParts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in seed.Components ?? Array.Empty<InteractionMemoryRecoveryComponentSeed>())
        {
            string part = (component?.Part ?? string.Empty).Trim().ToLowerInvariant();
            if (!IsEligible(component) || !attemptedParts.Add(part)
                || !hasPublishedDailyComponent(seed.SubjectId, recoveryId, payloadHash, part)) continue;
            switch (notify(seed, recoveryId, payloadHash, part))
            {
                case MemoryAuxiliaryReceiptOutcome.Accepted: accepted++; break;
                case MemoryAuxiliaryReceiptOutcome.Duplicate: duplicate++; break;
                default: unavailable++; break;
            }
        }
        return new MemoryAuxiliaryCompletionReceipt(accepted, duplicate, unavailable);
    }

    internal static bool IsEligible(InteractionMemoryRecoveryComponentSeed component)
    {
        string part = (component?.Part ?? string.Empty).Trim().ToLowerInvariant();
        return component != null && component.IsLlmDialogue && !component.IsAfef
            && !string.IsNullOrWhiteSpace(component.DailyText) && (part == "user" || part == "assistant");
    }
}
