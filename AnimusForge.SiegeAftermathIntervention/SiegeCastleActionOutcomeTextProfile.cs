using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Player-facing castle-only action result wording.</summary>
public static class SiegeCastleActionOutcomeTextProfile
{
    public const uint SuccessColor = 0xFFB6F7A8u;
    public const uint WarningColor = 0xFFFFD27Fu;
    public const uint DangerColor = 0xFFFF7A7Au;

    public static string BuildCareMessage(int affected, int trustDelta)
        => BuildCareMessage(affected, trustDelta, false);

    public static string BuildCareMessage(int affected, int trustDelta, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Supplies distributed to " + Count(affected)
                + " regular prisoners by troop tier; mistreatment forbidden; prisoner trust " + Signed(trustDelta) + "."
            : CastleAftermathCompatibilityTexts.Raw("outcome_care_prefix") + Count(affected)
                + CastleAftermathCompatibilityTexts.Raw("outcome_care_middle") + Signed(trustDelta) + CastleAftermathCompatibilityTexts.Raw("outcome_care_suffix");

    public static string BuildCareInsufficientMessage()
        => BuildCareInsufficientMessage(false);

    public static string BuildCareInsufficientMessage(bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Insufficient food in party; prisoner care was not settled and no supplies deducted."
            : CastleAftermathCompatibilityTexts.Raw("outcome_care_insufficient");

    public static string BuildArmamentMessage(int affected, int items, int kinds, int gold, bool lord)
        => BuildArmamentMessage(affected, items, kinds, gold, lord, false);

    public static string BuildArmamentMessage(int affected, int items, int kinds, int gold, bool lord, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Confiscated arms and belongings from " + (lord ? "the lord" : Count(affected) + " regular prisoners")
                + " sent directly to inventory: " + Count(items)
                + " items / " + Count(kinds) + " types, gold " + Count(gold) + "; loot screen not opened."
            : CastleAftermathCompatibilityTexts.Raw("outcome_armaments_prefix") + (lord ? CastleAftermathCompatibilityTexts.Raw("outcome_lord_single") : Count(affected) + CastleAftermathCompatibilityTexts.Raw("outcome_regular_count"))
                + CastleAftermathCompatibilityTexts.Raw("outcome_armaments_middle") + Count(items)
                + CastleAftermathCompatibilityTexts.Raw("outcome_items_unit") + Count(kinds)
                + CastleAftermathCompatibilityTexts.Raw("outcome_kinds_unit") + Count(gold) + CastleAftermathCompatibilityTexts.Raw("outcome_armaments_suffix");

    public static string BuildSlaughterKillMessage(int killedTotal, int remaining)
        => BuildSlaughterKillMessage(killedTotal, remaining, false);

    public static string BuildSlaughterKillMessage(int killedTotal, int remaining, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Regular prisoner confirmed deaths: " + Count(killedTotal)
                + "; " + Count(remaining) + " remain to be killed."
            : CastleAftermathCompatibilityTexts.Raw("outcome_slaughter_prefix") + Count(killedTotal) + CastleAftermathCompatibilityTexts.Raw("outcome_slaughter_middle") + Count(remaining) + CastleAftermathCompatibilityTexts.Raw("outcome_slaughter_suffix");

    public static string BuildLordProcessMessage(SiegeCastleActionKind action, string lordName, int items = 0)
        => BuildLordProcessMessage(action, lordName, items, false);

    public static string BuildLordProcessMessage(SiegeCastleActionKind action, string lordName, int items, bool isEnglish)
    {
        if (action == SiegeCastleActionKind.ReceiveArmaments)
        {
            return isEnglish
                ? "[Castle aftermath] Confiscated weapons and armor from " + Name(lordName, true) + ", total " + Count(items) + " items sent to inventory."
                : CastleAftermathCompatibilityTexts.Raw("outcome_lord_armaments_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_lord_armaments_middle") + Count(items) + CastleAftermathCompatibilityTexts.Raw("outcome_lord_armaments_suffix");
        }
        return isEnglish
            ? "[Castle aftermath] Provided supplies to " + Name(lordName, true) + " and mistreatment forbidden; process applies only to this lord."
            : CastleAftermathCompatibilityTexts.Raw("outcome_lord_care_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_lord_care_suffix");
    }

    public static string BuildLordRecruitmentMessage(
        SiegeCastleLordRecruitmentBranch branch,
        string lordName,
        string statusText)
        => BuildLordRecruitmentMessage(branch, lordName, statusText, false);

    public static string BuildLordRecruitmentMessage(
        SiegeCastleLordRecruitmentBranch branch,
        string lordName,
        string statusText,
        bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] " + Name(lordName, true) + ": "
                + SiegeCastleLordRecruitmentBranchProfile.Describe(branch, true)
                + (string.IsNullOrWhiteSpace(statusText) ? "." : "; " + statusText.Trim())
            : CastleAftermathCompatibilityTexts.Raw("outcome_lord_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_lord_middle")
                + SiegeCastleLordRecruitmentBranchProfile.Describe(branch, false)
                + (string.IsNullOrWhiteSpace(statusText) ? "。" : "；" + statusText.Trim());

    public static string BuildLordSaleMessage(string lordName, int gold)
        => BuildLordSaleMessage(lordName, gold, false);

    public static string BuildLordSaleMessage(string lordName, int gold, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Handed " + Name(lordName, true)
                + " to ransom broker, strictly obtaining " + Count(gold)
                + " gold at native tavern ransom value; the lord is released from captivity and left."
            : CastleAftermathCompatibilityTexts.Raw("outcome_sale_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_sale_middle") + Count(gold) + CastleAftermathCompatibilityTexts.Raw("outcome_sale_suffix");

    public static string BuildLordSaleFailedMessage(string lordName, string reasonCode)
        => BuildLordSaleFailedMessage(lordName, reasonCode, false);

    public static string BuildLordSaleFailedMessage(string lordName, string reasonCode, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Could not ransom " + Name(lordName, true)
                + "; gold or final disposition not settled. " + DescribeLordSaleFailure(reasonCode, true)
            : CastleAftermathCompatibilityTexts.Raw("outcome_sale_failed_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_sale_failed_suffix") + DescribeLordSaleFailure(reasonCode, false);

    public static string BuildLordExecutionQueuedMessage(string lordName)
        => BuildLordExecutionQueuedMessage(lordName, false);

    public static string BuildLordExecutionQueuedMessage(string lordName, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Ordered execution of " + Name(lordName, true)
                + "; opening native execution confirmation; cancelling will not alter life or captive state."
            : CastleAftermathCompatibilityTexts.Raw("outcome_exec_queued_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_queued_suffix");

    public static string BuildLordExecutionCancelledMessage(string lordName)
        => BuildLordExecutionCancelledMessage(lordName, false);

    public static string BuildLordExecutionCancelledMessage(string lordName, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Cancelled execution of " + Name(lordName, true)
                + "; the lord remains alive and captive, negotiations may resume."
            : CastleAftermathCompatibilityTexts.Raw("outcome_exec_cancelled_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_cancelled_suffix");

    public static string BuildLordExecutionFailedMessage(string lordName)
        => BuildLordExecutionFailedMessage(lordName, false);

    public static string BuildLordExecutionFailedMessage(string lordName, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Failed to open native execution confirmation for " + Name(lordName, true)
                + "; death not settled; retry later, details logged."
            : CastleAftermathCompatibilityTexts.Raw("outcome_exec_failed_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_failed_suffix");

    public static string BuildLordExecutionPersistenceFailedMessage(string lordName)
        => BuildLordExecutionPersistenceFailedMessage(lordName, false);

    public static string BuildLordExecutionPersistenceFailedMessage(string lordName, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Native execution confirmation for " + Name(lordName, true)
                + " finished, but native game did not record death; disposition had no effect, retry later."
            : CastleAftermathCompatibilityTexts.Raw("outcome_exec_persist_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_persist_suffix");

    public static string BuildLordExecutionFinalizationPendingMessage(string lordName)
        => BuildLordExecutionFinalizationPendingMessage(lordName, false);

    public static string BuildLordExecutionFinalizationPendingMessage(string lordName, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Execution death mark for " + Name(lordName, true)
                + " recorded, but native character finalization pending; cannot continue disposition or leave normally."
            : CastleAftermathCompatibilityTexts.Raw("outcome_exec_final_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_final_suffix");

    public static string BuildLordExecutionConfirmationPendingMessage(string lordName)
        => BuildLordExecutionConfirmationPendingMessage(lordName, false);

    public static string BuildLordExecutionConfirmationPendingMessage(string lordName, bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Native execution confirmation for " + Name(lordName, true)
                + " is still open; please confirm or cancel first."
            : CastleAftermathCompatibilityTexts.Raw("outcome_exec_confirm_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_confirm_suffix");

    public static string BuildLordExecutionCompletedMessage(
        string lordName,
        bool sceneDeathApplied)
        => BuildLordExecutionCompletedMessage(lordName, sceneDeathApplied, false);

    public static string BuildLordExecutionCompletedMessage(
        string lordName,
        bool sceneDeathApplied,
        bool isEnglish)
        => isEnglish
            ? "[Castle aftermath] Execution of " + Name(lordName, true)
                + " confirmed by player"
                + (sceneDeathApplied ? ", fell on-site in castle scene" : "; on-site fall animation failed, death recorded")
                + "; native character death persisted, relationship and clan consequences handled by native campaign system."
            : CastleAftermathCompatibilityTexts.Raw("outcome_exec_completed_prefix") + Name(lordName, false) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_completed_middle") + (sceneDeathApplied ? CastleAftermathCompatibilityTexts.Raw("outcome_exec_scene_true") : CastleAftermathCompatibilityTexts.Raw("outcome_exec_scene_false")) + CastleAftermathCompatibilityTexts.Raw("outcome_exec_completed_suffix");

    private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    private static int Count(int value) => Math.Max(0, value);
    private static string Name(string value) => Name(value, false);
    private static string Name(string value, bool isEnglish) => string.IsNullOrWhiteSpace(value) ? (isEnglish ? "the captive lord" : CastleAftermathCompatibilityTexts.Raw("captured_lord")) : value.Trim();

    private static string DescribeLordSaleFailure(string reasonCode)
        => DescribeLordSaleFailure(reasonCode, false);

    private static string DescribeLordSaleFailure(string reasonCode, bool isEnglish)
    {
        string normalized = (reasonCode ?? string.Empty).Trim();
        if (isEnglish)
        {
            return normalized switch
            {
                "lord_sale_roster_unavailable" => "Main party prisoner roster currently unavailable.",
                "lord_sale_target_invalid" => "Target is dead or lacks ransomable status.",
                "lord_not_selected_main_party_prisoner" => "Target is no longer a selected lord held by main party.",
                "lord_prisoner_locked_from_ransom" => "Lord is locked by native quest or roster; cannot be ransomed.",
                "ransom_value_model_unavailable" => "Native ransom valuation model currently unavailable.",
                "vanilla_lord_sale_did_not_release_target" => "Native ransom action failed to release captive lord.",
                "" => "Target is currently not ransomable.",
                _ => "Native ransom action failed, details written to log."
            };
        }
        return normalized switch
        {
            "lord_sale_roster_unavailable" => CastleAftermathCompatibilityTexts.Raw("sale_reason_roster"),
            "lord_sale_target_invalid" => CastleAftermathCompatibilityTexts.Raw("sale_reason_invalid"),
            "lord_not_selected_main_party_prisoner" => CastleAftermathCompatibilityTexts.Raw("sale_reason_not_selected"),
            "lord_prisoner_locked_from_ransom" => CastleAftermathCompatibilityTexts.Raw("sale_reason_locked"),
            "ransom_value_model_unavailable" => CastleAftermathCompatibilityTexts.Raw("sale_reason_model"),
            "vanilla_lord_sale_did_not_release_target" => CastleAftermathCompatibilityTexts.Raw("sale_reason_not_released"),
            "" => CastleAftermathCompatibilityTexts.Raw("sale_reason_unavailable"),
            _ => CastleAftermathCompatibilityTexts.Raw("sale_reason_other")
        };
    }
}
