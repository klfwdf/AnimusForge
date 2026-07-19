using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Parses only explicit, non-overlapping multi-group orders such as
/// "50名强壮战俘修缮城堡，其余卖掉". Vague combinations remain fail-closed.
/// </summary>
public static class SiegeCastleCompoundDispositionPlanProfile
{
    public const int MaximumSteps = 4;

    private static readonly Regex RemainderBoundaryRegex = new Regex(
        @"(?<![，,；;\s])(?=(?:剩下|剩余|其余|余下))",
        RegexOptions.Compiled);

    private static readonly Regex ClauseSeparatorRegex = new Regex(
        @"[，,；;。！!\r\n]+|然后|随后|再把|再将|再让",
        RegexOptions.Compiled);

    public static bool TryBuild(string playerText, out SiegeCastleCompoundDispositionPlan plan)
    {
        string text = (playerText ?? string.Empty).Trim();
        if (text.Length == 0
            || SiegeCastlePlayerAuthorizationPolicy.IsDiscussionText(text)
            || SiegeCastlePrisonerAllocationProfile.IsPlanResetRequested(text))
        {
            plan = SiegeCastleCompoundDispositionPlan.Invalid("compound_text_not_direct");
            return false;
        }

        string expanded = RemainderBoundaryRegex.Replace(text, "；");
        string[] clauses = ClauseSeparatorRegex.Split(expanded);
        var steps = new List<SiegeCastleCompoundDispositionStep>();
        var seen = new HashSet<SiegeCastlePrisonerDispositionKind>();
        int remainderSteps = 0;
        foreach (string rawClause in clauses)
        {
            string clause = (rawClause ?? string.Empty).Trim();
            if (clause.Length == 0)
            {
                continue;
            }

            IReadOnlyList<SiegeCastlePrisonerDispositionKind> intents =
                SiegeCastlePlayerAuthorizationPolicy.DetectPositiveIntents(clause);
            if (intents.Count == 0)
            {
                continue;
            }
            if (intents.Count != 1 || !seen.Add(intents[0]))
            {
                plan = SiegeCastleCompoundDispositionPlan.Invalid("compound_clause_ambiguous_or_duplicate");
                return false;
            }

            bool hasExplicitCount = SiegeCastlePrisonerAllocationProfile.TryParseExplicitCount(clause, out _);
            bool usesAllAvailable = !hasExplicitCount
                && SiegeCastlePrisonerAllocationProfile.RequestsAllAvailable(clause);
            if (!hasExplicitCount && !usesAllAvailable)
            {
                plan = SiegeCastleCompoundDispositionPlan.Invalid("compound_clause_quantity_required");
                return false;
            }
            if (usesAllAvailable)
            {
                remainderSteps++;
            }
            steps.Add(new SiegeCastleCompoundDispositionStep(
                intents[0],
                clause,
                hasExplicitCount,
                usesAllAvailable));
        }

        int distinctIntentCount = SiegeCastlePlayerAuthorizationPolicy.DetectPositiveIntents(text).Count;
        if (steps.Count < 2
            || steps.Count > MaximumSteps
            || steps.Count != distinctIntentCount
            || remainderSteps > 1)
        {
            plan = SiegeCastleCompoundDispositionPlan.Invalid("compound_partition_incomplete");
            return false;
        }

        plan = new SiegeCastleCompoundDispositionPlan(steps, true, "explicit_compound_partition");
        return true;
    }
}

public sealed class SiegeCastleCompoundDispositionPlan
{
    internal SiegeCastleCompoundDispositionPlan(
        IReadOnlyList<SiegeCastleCompoundDispositionStep> steps,
        bool isValid,
        string reasonCode)
    {
        Steps = steps ?? Array.Empty<SiegeCastleCompoundDispositionStep>();
        IsValid = isValid;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public IReadOnlyList<SiegeCastleCompoundDispositionStep> Steps { get; }

    public bool IsValid { get; }

    public string ReasonCode { get; }

    internal static SiegeCastleCompoundDispositionPlan Invalid(string reasonCode)
        => new SiegeCastleCompoundDispositionPlan(
            Array.Empty<SiegeCastleCompoundDispositionStep>(),
            false,
            reasonCode);
}

public sealed class SiegeCastleCompoundDispositionStep
{
    internal SiegeCastleCompoundDispositionStep(
        SiegeCastlePrisonerDispositionKind disposition,
        string allocationText,
        bool hasExplicitCount,
        bool usesAllAvailable)
    {
        Disposition = disposition;
        AllocationText = allocationText ?? string.Empty;
        HasExplicitCount = hasExplicitCount;
        UsesAllAvailable = usesAllAvailable;
    }

    public SiegeCastlePrisonerDispositionKind Disposition { get; }

    public string AllocationText { get; }

    public bool HasExplicitCount { get; }

    public bool UsesAllAvailable { get; }
}
