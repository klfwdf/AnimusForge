using System;



namespace AnimusForge.SiegeAftermathIntervention;



/// <summary>

/// Dependency-free count preservation and player-facing wording for castle prisoner disposition.

/// </summary>

public static class SiegeCastlePrisonerDispositionProfile

{

    public const string RosterUnavailableReason = "castle_roster_unavailable";



    public const string PartyCapacityFullReason = "party_capacity_full";



    public const string NoMatchingRegularPrisonersReason = "no_matching_regular_prisoners";



    public const string NoSellableRegularPrisonersReason = "no_sellable_regular_prisoners";



    public const string UnsupportedDeferredTerminalActionReason = "unsupported_deferred_terminal_action";



    public const string RemovedForReasonPrefix = "removed_for_";



    public const string RecruitedReason = "recruited";



    public const string SlaughteredReason = "slaughtered";



    public const string ExceptionReasonPrefix = "exception:";



    public const uint SuccessMessageColor = 0xFFB6F7A8u;



    public const uint WarningMessageColor = 0xFFFFD27Fu;



    public static int ResolveRecruitCount(int availablePrisoners, int freePartySlots)

    {

        return Math.Min(Math.Max(0, availablePrisoners), Math.Max(0, freePartySlots));

    }



    /// <summary>

    /// Caps a new recruitment group to the party slots not already promised to earlier recruit groups,

    /// so the staged plan (and the soldiers' unrest) never exceeds what can actually join at exit.

    /// </summary>

    public static int ResolveStageableRecruitCount(int requestedCount, int currentFreePartySlots, int previouslyStagedRecruitCount)

    {

        return Math.Min(

            Math.Max(0, requestedCount),

            ResolveRemainingStagedRecruitCapacity(currentFreePartySlots, previouslyStagedRecruitCount));

    }



    public static string BuildRecruitCapacityTrimMessage(int requestedCount, int stagedCount, int currentFreePartySlots)

        => BuildRecruitCapacityTrimMessage( requestedCount, stagedCount, currentFreePartySlots, false);



    public static string BuildRecruitCapacityTrimMessage(int requestedCount, int stagedCount, int currentFreePartySlots, bool isEnglish)

    {

        int requested = Math.Max(0, requestedCount);

        int staged = Math.Max(0, stagedCount);

        if (isEnglish)

        {

            return staged <= 0

                ? "[Castle aftermath] No party slots available for recruitment (" + Math.Max(0, currentFreePartySlots)

                    + " free, reserved by earlier groups). This recruitment was not staged; free slots or choose another disposition."

                : "[Castle aftermath] Insufficient party slots; recruitment reduced from " + requested + " to " + staged

                    + "; other prisoners remain unassigned and can be handled separately.";

        }

        return staged <= 0

            ? CastleAftermathCompatibilityTexts.Raw("disp_cap_none_prefix") + Math.Max(0, currentFreePartySlots)

                + CastleAftermathCompatibilityTexts.Raw("disp_cap_none_suffix")

            : CastleAftermathCompatibilityTexts.Raw("disp_cap_trim_prefix") + requested + CastleAftermathCompatibilityTexts.Raw("disp_cap_trim_middle") + staged

                + CastleAftermathCompatibilityTexts.Raw("disp_cap_trim_suffix");

    }



    public static int ResolveRemainingStagedRecruitCapacity(int currentFreePartySlots, int previouslyStagedRecruitCount)

    {

        return Math.Max(0, Math.Max(0, currentFreePartySlots) - Math.Max(0, previouslyStagedRecruitCount));

    }



    // Public legacy helper retained for existing external source/ABI consumers.

    public static string BuildStagedRecruitCapacityWarning(

        int currentFreePartySlots,

        int previouslyStagedRecruitCount,

        int currentGroupCount)

    {

        int freeSlots = Math.Max(0, currentFreePartySlots);

        int previousCount = Math.Max(0, previouslyStagedRecruitCount);

        int groupCount = Math.Max(0, currentGroupCount);

        int remainingCapacity = ResolveRemainingStagedRecruitCapacity(freeSlots, previousCount);

        if (groupCount <= remainingCapacity)

        {

            return string.Empty;

        }



        string previousPlan = previousCount > 0 ? CastleAftermathCompatibilityTexts.Raw("disp_prev_plan_prefix") + previousCount + CastleAftermathCompatibilityTexts.Raw("disp_prev_plan_suffix") : string.Empty;

        return CastleAftermathCompatibilityTexts.Raw("disp_staged_warning_prefix") + freeSlots + CastleAftermathCompatibilityTexts.Raw("disp_staged_warning_slots")

            + previousPlan + CastleAftermathCompatibilityTexts.Raw("disp_staged_warning_est")

            + remainingCapacity + CastleAftermathCompatibilityTexts.Raw("disp_staged_warning_suffix");

    }



    public static string DescribeDeferredFailureReason(string reasonCode)
        => DescribeDeferredFailureReason( reasonCode, false);

    public static string DescribeDeferredFailureReason(string reasonCode, bool isEnglish)
    {
        string normalized = (reasonCode ?? string.Empty).Trim();
        if (isEnglish)
        {
            switch (normalized)
            {
                case PartyCapacityFullReason:
                    return "The main party is full";
                case RosterUnavailableReason:
                    return "Prisoner or party roster unavailable";
                case NoMatchingRegularPrisonersReason:
                    return "No remaining regular prisoners in roster for this group";
                case NoSellableRegularPrisonersReason:
                    return "No remaining sellable regular prisoners in roster for this group";
                case UnsupportedDeferredTerminalActionReason:
                    return "This staged disposition is not supported on departure";
            }
            if (normalized.StartsWith(RemovedForReasonPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return "No remaining regular prisoners in roster for this group";
            }
            if (IsExceptionReason(normalized))
            {
                return "Exception occurred during settlement, details logged";
            }
            return "Settlement had no effect, details logged";
        }

        switch (normalized)
        {
            case PartyCapacityFullReason:
                return CastleAftermathCompatibilityTexts.Raw("disp_reason_party_full");
            case RosterUnavailableReason:
                return CastleAftermathCompatibilityTexts.Raw("disp_reason_roster_unavailable");
            case NoMatchingRegularPrisonersReason:
                return CastleAftermathCompatibilityTexts.Raw("disp_reason_no_matching_prisoners");
            case NoSellableRegularPrisonersReason:
                return CastleAftermathCompatibilityTexts.Raw("disp_reason_no_sellable_prisoners");
            case UnsupportedDeferredTerminalActionReason:
                return CastleAftermathCompatibilityTexts.Raw("disp_reason_unsupported_staged");
        }

        if (normalized.StartsWith(RemovedForReasonPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return CastleAftermathCompatibilityTexts.Raw("disp_reason_no_matching_prisoners");
        }
        if (IsExceptionReason(normalized))
        {
            return CastleAftermathCompatibilityTexts.Raw("disp_reason_exception");
        }
        return CastleAftermathCompatibilityTexts.Raw("disp_reason_other");
    }

    public static int ResolveTransferredWounded(int stackCount, int stackWounded, int transferredCount)

    {

        int count = Math.Max(0, stackCount);

        int wounded = Math.Min(count, Math.Max(0, stackWounded));

        int transferred = Math.Min(count, Math.Max(0, transferredCount));

        return count == 0 ? 0 : Math.Min(transferred, (int)Math.Floor((double)wounded * transferred / count));

    }



    public static int ResolveTransferredXp(int stackCount, int stackXp, int transferredCount)

    {

        int count = Math.Max(0, stackCount);

        int xp = Math.Max(0, stackXp);

        int transferred = Math.Min(count, Math.Max(0, transferredCount));

        return count == 0 ? 0 : Math.Min(xp, (int)Math.Floor((double)xp * transferred / count));

    }



    public static string BuildRecruitMessage(int recruited, int remaining, string reasonCode)

    {

        int recruitedCount = Math.Max(0, recruited);

        int remainingCount = Math.Max(0, remaining);

        if (recruitedCount > 0)

        {

            string message = CastleAftermathCompatibilityTexts.Raw("disp_recruit_msg_prefix") + recruitedCount

                + CastleAftermathCompatibilityTexts.Raw("disp_recruit_msg_middle") + remainingCount + CastleAftermathCompatibilityTexts.Raw("disp_recruit_msg_suffix");

            return IsExceptionReason(reasonCode)

                ? message + CastleAftermathCompatibilityTexts.Raw("disp_partial_suffix")

                : message;

        }



        switch ((reasonCode ?? string.Empty).Trim())

        {

            case PartyCapacityFullReason:

                return CastleAftermathCompatibilityTexts.Raw("disp_recruit_no_slots");

            case NoMatchingRegularPrisonersReason:

                return CastleAftermathCompatibilityTexts.Raw("disp_recruit_no_matching");

            case RosterUnavailableReason:

                return CastleAftermathCompatibilityTexts.Raw("disp_recruit_roster_unavailable");

            default:

                return CastleAftermathCompatibilityTexts.Raw("disp_recruit_failed");

        }

    }



    public static string BuildSlaughterMessage(int slaughtered, int remaining, string reasonCode)

    {

        int slaughteredCount = Math.Max(0, slaughtered);

        int remainingCount = Math.Max(0, remaining);

        if (slaughteredCount > 0)

        {

            string message = CastleAftermathCompatibilityTexts.Raw("disp_slaughter_msg_prefix") + slaughteredCount

                + CastleAftermathCompatibilityTexts.Raw("disp_slaughter_msg_middle") + remainingCount

                + CastleAftermathCompatibilityTexts.Raw("disp_slaughter_msg_suffix");

            return IsExceptionReason(reasonCode)

                ? message + CastleAftermathCompatibilityTexts.Raw("disp_partial_suffix")

                : message;

        }



        switch ((reasonCode ?? string.Empty).Trim())

        {

            case NoMatchingRegularPrisonersReason:

                return CastleAftermathCompatibilityTexts.Raw("disp_slaughter_no_matching");

            case RosterUnavailableReason:

                return CastleAftermathCompatibilityTexts.Raw("disp_slaughter_roster_unavailable");

            default:

                return CastleAftermathCompatibilityTexts.Raw("disp_slaughter_failed");

        }

    }



    public static string BuildRecruitMemoryText(int recruited, int remaining)

    {

        return CastleAftermathCompatibilityTexts.Raw("disp_memory_recruit_prefix") + Math.Max(0, recruited)

            + CastleAftermathCompatibilityTexts.Raw("disp_memory_recruit_middle") + Math.Max(0, remaining) + CastleAftermathCompatibilityTexts.Raw("disp_memory_recruit_suffix");

    }



    public static string BuildSlaughterMemoryText(int slaughtered, int remaining)

    {

        return CastleAftermathCompatibilityTexts.Raw("disp_memory_slaughter_prefix") + Math.Max(0, slaughtered)

            + CastleAftermathCompatibilityTexts.Raw("disp_memory_slaughter_middle") + Math.Max(0, remaining)

            + CastleAftermathCompatibilityTexts.Raw("disp_memory_slaughter_suffix");

    }



    private static bool IsExceptionReason(string reasonCode)

    {

        return (reasonCode ?? string.Empty).StartsWith(ExceptionReasonPrefix, StringComparison.OrdinalIgnoreCase);

    }

}

