using System;
using RichExecutions.Core;

namespace RichExecutions.Scene;

internal static class SpeechEvidencePolicy
{
    internal static bool Matches(string recordVictim, string recordCharge, EvidenceStrength strength,
        string settlementId, string victimId, string chargeId) =>
        string.Equals(recordVictim, victimId, StringComparison.Ordinal) &&
        string.Equals(recordCharge, chargeId, StringComparison.OrdinalIgnoreCase) &&
        strength == EvidenceStrength.Strong && !string.IsNullOrWhiteSpace(settlementId);
}
