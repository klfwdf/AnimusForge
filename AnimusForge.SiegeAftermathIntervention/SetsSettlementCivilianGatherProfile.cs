using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for gathering civilians during an ordinary SETS town or village visit.
/// Live mission-agent selection and formation assignment remain in the fused AF adapter.
/// </summary>
public static class SetsSettlementCivilianGatherProfile
{
    // FormationClass.Cavalry, displayed as command group 3.
    public const int NativeCommandFormationClassIndex = 2;

    public const float FormationAssignmentInitialDelaySeconds = 0.8f;

    public const float FormationAssignmentBatchIntervalSeconds = 0.12f;

    public const float FormationOrderFinalizeDelaySeconds = 0.25f;

    public const int FormationAssignmentBatchSize = 8;

    public const uint MessageColor = 0xFFB6F7A8u;

    public const string PlayerCommandSource = "sets_settlement_civilian_gather_player_command";

    public const string AiActionTagSource = "sets_settlement_civilian_gather_action_tag";

    public const string NoEligibleCivilianMessage = "[SETS] There are no eligible adult civilians to gather.";
    public const string NoEligibleCivilianMessageEn = "[SETS] There are no eligible adult civilians to gather.";


    public static string GetNoEligibleCivilianMessage(bool isEnglish)
        => isEnglish ? NoEligibleCivilianMessageEn : NoEligibleCivilianMessage;

    public static string BuildQueuedMessage(int civilianCount)
    {
        return BuildQueuedMessage(civilianCount, isEnglish: false);
    }

    public static string BuildQueuedMessage(int civilianCount, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[SETS] Gathering " + Math.Max(0, civilianCount) + " adult civilians; they will join command group 3 when ready.";
        }
        return isEnglish ? "[SETS] Gathering " + Math.Max(0, civilianCount) + " adult civilians; they will join command group 3 when ready." : SiegeAftermathCompatibilityTexts.Raw("sets_gather_queued_prefix") + Math.Max(0, civilianCount) + SiegeAftermathCompatibilityTexts.Raw("sets_gather_queued_suffix");
    }

    public static bool ShouldHandleExplicitPlayerCommand(string playerText)
    {
        string normalized = Normalize(playerText);
        string lower = (playerText ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)
            || ContainsAny(normalized, LoadGatherStopTerms())
            || lower.Contains("don't gather")
            || lower.Contains("do not gather")
            || lower.Contains("no need to gather")
            || lower.Contains("stop gathering"))
        {
            return false;
        }

        bool namesCivilians = ContainsAny(normalized, LoadGatherNouns())
            || lower.Contains("civilian")
            || lower.Contains("civilians")
            || lower.Contains("villager")
            || lower.Contains("villagers")
            || lower.Contains("resident")
            || lower.Contains("residents")
            || lower.Contains("people")
            || lower.Contains("townsfolk")
            || lower.Contains("everyone");

        bool givesGatherOrder = ContainsAny(normalized, LoadGatherVerbs())
            || lower.Contains("gather")
            || lower.Contains("assemble")
            || lower.Contains("rally")
            || lower.Contains("call over")
            || lower.Contains("bring over")
            || lower.Contains("summon");

        return namesCivilians && givesGatherOrder;
    }

    public static string BuildGatheredMessage(int civilianCount)
    {
        return BuildGatheredMessage(civilianCount, isEnglish: false);
    }

    public static string BuildGatheredMessage(int civilianCount, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[SETS] Gathered " + Math.Max(0, civilianCount) + " adult civilians and assigned them to command group 3.";
        }
        return isEnglish ? "[SETS] Gathered " + Math.Max(0, civilianCount) + " adult civilians and assigned them to command group 3." : SiegeAftermathCompatibilityTexts.Raw("sets_gathered_prefix") + Math.Max(0, civilianCount) + SiegeAftermathCompatibilityTexts.Raw("sets_gathered_suffix");
    }

    private static string Normalize(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Replace(" ", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("，", string.Empty)
                .Replace("。", string.Empty)
                .Replace("！", string.Empty)
                .Replace("？", string.Empty)
                .Trim();
    }

    private static string[] LoadGatherStopTerms() => SiegeAftermathCompatibilityTexts.Raw("sets_gather_stop_terms").Split(',');
    private static string[] LoadGatherNouns() => SiegeAftermathCompatibilityTexts.Raw("sets_gather_nouns").Split(',');
    private static string[] LoadGatherVerbs() => SiegeAftermathCompatibilityTexts.Raw("sets_gather_verbs").Split(',');
    private static bool ContainsAny(string text, string[] terms)
    {
        if (string.IsNullOrEmpty(text) || terms == null) return false;
        foreach (var t in terms) if (!string.IsNullOrEmpty(t) && text.Contains(t)) return true;
        return false;
    }

    private static bool Contains(string text, string value)
    {
        return text?.IndexOf(value, StringComparison.Ordinal) >= 0;
    }
}
