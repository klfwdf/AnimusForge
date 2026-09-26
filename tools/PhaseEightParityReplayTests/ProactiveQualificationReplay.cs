using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

internal static class ProactiveQualificationReplay
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly, string repo)
    {
        string ownerPath = Path.Combine(repo, "src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs");
        string hostPath = Path.Combine(repo, "ProactiveNpcRequestBehavior.cs");
        if (!File.Exists(ownerPath)) throw new InvalidOperationException("Proactive qualification: Social owner source missing");
        string ownerSource = File.ReadAllText(ownerPath);
        string hostSource = File.ReadAllText(hostPath);
        foreach (string symbol in new[] { "private ProactiveCandidate FindBestRequestCandidate(",
            "private bool TryEvaluateCandidateTrigger(", "private List<LetterNeedSnapshot> BuildLetterNeedSnapshots(",
            "private bool TryBuildBaseCandidate(", "private ProactiveCandidate TryBuildNeedCandidate(",
            "private ProactiveCandidate BuildCombinedNeedCandidate(", "private static List<string> FilterPlayerEligibleNeedTypes(",
            "private static bool IsPlayerEligibleForProactiveNeed(", "private bool TryBuildFoodShortageCandidate(",
            "private bool TryBuildDiplomacyCandidate(", "private bool TryBuildSettlementSaleSnapshot(",
            "private static bool IsKingdomVassalInviteNeedMet(" })
            if (!ownerSource.Contains(symbol, StringComparison.Ordinal) || hostSource.Contains(symbol, StringComparison.Ordinal))
                throw new InvalidOperationException("Proactive qualification: real decision is not uniquely in Social: " + symbol);

        Type hostType = assembly.GetType("AnimusForge.ProactiveNpcRequestBehavior", true);
        Type candidateType = hostType.GetNestedType("ProactiveCandidate", BindingFlags.NonPublic);
        Type statsType = hostType.GetNestedType("CandidateScanStats", BindingFlags.NonPublic);
        object host = Activator.CreateInstance(hostType);
        object candidate = Activator.CreateInstance(candidateType, true);
        object stats = Activator.CreateInstance(statsType, true);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Proactive qualification: " + label); }
        object[] baseArgs = { null, null, null, null, null };
        Check(!(bool)hostType.GetMethod("TryBuildBaseCandidate", Members).Invoke(host, baseArgs)
            && (string)baseArgs[4] == "not_active_visible_lord_party", "missing party rejects before game reads");
        object[] invalidArgs = { null, "FoodShortage", null };
        Check(!(bool)hostType.GetMethod("IsPlayerEligibleForProactiveNeed", Members).Invoke(null, invalidArgs)
            && (string)invalidArgs[2] == "candidate_or_need_invalid", "missing candidate is ineligible");
        object[] foodArgs = { candidate, "FoodShortage", null };
        Check((bool)hostType.GetMethod("IsPlayerEligibleForProactiveNeed", Members).Invoke(null, foodArgs), "ordinary food need retains eligibility");
        var mixed = new List<string> { "FoodShortage", "foodshortage", "UnknownNeed" };
        object filtered = hostType.GetMethod("FilterPlayerEligibleNeedTypes", Members).Invoke(null, new object[] { candidate, mixed, "UnknownNeed" });
        Check(filtered is List<string> needs && needs.Count == 1 && needs[0] == "FoodShortage", "normalize, dedupe and reject unknown need");
        Check(!(bool)hostType.GetMethod("TryEvaluateCandidateTrigger", Members).Invoke(host, new[] { candidate, null, stats }),
            "zero-urgency candidate is below minimum before RNG or notoriety reads");
        Check((int)statsType.GetProperty("BelowMinUrgency", Members).GetValue(stats) == 1, "minimum-urgency skip counted");
        Console.WriteLine("PASS proactiveQualificationReplay ownerSource=1 missingParty=1 invalid=1 ordinary=1 normalize=1 minUrgency=1; live eligibility/RNG=NOT_RUN");
    }
}
