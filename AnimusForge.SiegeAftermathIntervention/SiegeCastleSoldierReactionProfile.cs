using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only policy for one allied witness reaction after a meaningful prisoner action.
/// Ordinary actions create unrest only when the witness actually voices discontent;
/// prisoner recruitment always carries the explicit stacked recruitment penalty.
/// </summary>
public static class SiegeCastleSoldierReactionProfile
{
    public const int ExpressedDiscontentPenalty = 30;
    public const int VoluntaryRecruitmentPenalty = 30;
    public const int ForcedRecruitmentPenalty = 60;
    public const int VoluntaryRecruitmentTotalPenalty = ExpressedDiscontentPenalty + VoluntaryRecruitmentPenalty;
    public const int ForcedRecruitmentTotalPenalty = ExpressedDiscontentPenalty + ForcedRecruitmentPenalty;

    public const uint NeedMessageColor = 0xFFFFD27Fu;
    public const uint AppeasedMessageColor = 0xFFB6F7A8u;

    public const string NeedMemoryTitle = "Castle prisoner disposition morale discontent";
    public const string NeedMemoryTitleEn = "Castle prisoner disposition morale concern";
    public const string AppeasementMemoryTitle = "Castle soldier appeasement";
    public const string AppeasementMemoryTitleEn = "Castle soldier appeasement";
    public const string PenaltyMemoryTitle = "Castle prisoner disposition morale penalty";
    public const string PenaltyMemoryTitleEn = "Castle prisoner disposition morale penalty";

    public static string GetNeedMemoryTitle(bool isEnglish) => isEnglish ? NeedMemoryTitleEn : NeedMemoryTitle;
    public static string GetAppeasementMemoryTitle(bool isEnglish) => isEnglish ? AppeasementMemoryTitleEn : AppeasementMemoryTitle;
    public static string GetPenaltyMemoryTitle(bool isEnglish) => isEnglish ? PenaltyMemoryTitleEn : PenaltyMemoryTitle;

    public static bool CanReactTo(SiegeCastleActionKind action)
    {
        return SiegeCastleActionKindProfile.IsProcess(action)
            || SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action)
            || SiegeCastleActionKindProfile.IsLordTerminal(action);
    }

    public static bool AlwaysCreatesConcern(SiegeCastleActionKind action)
        => SiegeCastleActionKindProfile.IsRecruitment(action);

    public static int ResolvePendingMoralePenalty(
        SiegeCastleActionKind action,
        bool witnessExpressedDiscontent)
    {
        if (action == SiegeCastleActionKind.RecruitPrisonersVoluntary)
        {
            return VoluntaryRecruitmentTotalPenalty;
        }
        if (action == SiegeCastleActionKind.RecruitPrisonersForced)
        {
            return ForcedRecruitmentTotalPenalty;
        }
        return witnessExpressedDiscontent && CanReactTo(action)
            ? ExpressedDiscontentPenalty
            : 0;
    }

    /// <summary>
    /// Final morale penalty at castle exit. Recruitment unrest is scaled by the prisoners that
    /// actually joined the party (capacity limits can leave most of a staged group as prisoners),
    /// so a group where nobody joined costs nothing. Unrest from other actions is not scaled.
    /// </summary>
    public static int ResolveExitMoralePenalty(
        int otherConcernPenalty,
        int recruitmentConcernPenalty,
        int stagedRecruitCount,
        int actualRecruitCount)
    {
        int other = Clamp(otherConcernPenalty);
        int recruitment = Clamp(recruitmentConcernPenalty);
        int staged = Clamp(stagedRecruitCount);
        int actual = Math.Min(Clamp(actualRecruitCount), staged);
        int scaledRecruitment = staged == 0 || actual == 0
            ? 0
            : (int)Math.Ceiling((double)recruitment * actual / staged);
        return Math.Max(other, scaledRecruitment);
    }

    /// <summary>
    /// A new concern only cancels an appeasement the player already gave when it raises the penalty.
    /// </summary>
    public static bool ShouldReopenAppeasement(bool appeasementApplied, int currentPendingPenalty, int newPenalty)
        => !appeasementApplied || newPenalty > currentPendingPenalty;

    public static string BuildWitnessFact(
        string playerName,
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        bool sameCulture)
        => BuildWitnessFact(playerName, action, affectedRegularPrisoners, sameCulture, false);

    public static string BuildWitnessFact(
        string playerName,
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        bool sameCulture,
        bool isEnglish)
    {
        string commander = string.IsNullOrWhiteSpace(playerName) ? (isEnglish ? "The player" : CastleAftermathCompatibilityTexts.Raw("player_fallback")) : playerName.Trim();
        if (isEnglish)
        {
            return "[CASTLE DISPOSITION WITNESS REACTION] " + commander + " just decided to execute '"
                + DescribeConcernAction(action, true) + "' on " + Clamp(affectedRegularPrisoners)
                + " prisoners. Comment freely according only to your personality, discipline view, shared culture, and firsthand observations; you may agree, stay neutral, question, or show discontent, but cannot disobey orders. "
                + (sameCulture ? "You share culture with at least some affected captives; realistically consider this identity conflict. " : string.Empty)
                + (SiegeCastleActionKindProfile.IsRecruitment(action)
                    ? "Enlisting defeated enemy troops into the party creates real soldier unrest; your line should clearly show at least one concern or dissatisfaction. "
                    : "Do not force discontent merely to trigger mechanics; if you approve, you may agree directly. ")
                + "This is a free reaction; do not propose or execute new prisoner dispositions.";
        }
        return CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_title") + commander + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_decided")
            + Clamp(affectedRegularPrisoners) + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_count") + DescribeConcernAction(action)
            + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_comment")
            + (sameCulture ? CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_culture") : string.Empty)
            + (SiegeCastleActionKindProfile.IsRecruitment(action)
                ? CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_recruit")
                : CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_no_force"))
            + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_witness_free_speech");
    }

    public static string BuildNeedMessage(
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        int pendingPenalty)
        => BuildNeedMessage(action, affectedRegularPrisoners, pendingPenalty, false);

    public static string BuildNeedMessage(
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        int pendingPenalty,
        bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Castle aftermath] Escort soldiers voiced concern regarding '" + DescribeConcernAction(action, true) + "' for "
                + Clamp(affectedRegularPrisoners) + " prisoners; appease directly before leaving, otherwise party morale -"
                + Clamp(pendingPenalty)
                + (SiegeCastleActionKindProfile.IsRecruitment(action) ? " (recruitment scaled by actual recruits at departure)" : string.Empty)
                + ".";
        }
        return CastleAftermathCompatibilityTexts.Raw("soldier_reaction_msg_prefix") + DescribeConcernAction(action) + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_msg_quote")
            + Clamp(affectedRegularPrisoners) + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_msg_middle")
            + Clamp(pendingPenalty)
            + (SiegeCastleActionKindProfile.IsRecruitment(action) ? CastleAftermathCompatibilityTexts.Raw("soldier_reaction_msg_recruit_note") : string.Empty)
            + "。";
    }

    public static string BuildNeedMemoryText(
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        int pendingPenalty)
        => BuildNeedMemoryText(action, affectedRegularPrisoners, pendingPenalty, false);

    public static string BuildNeedMemoryText(
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        int pendingPenalty,
        bool isEnglish)
    {
        if (isEnglish)
        {
            return "The player decided to handle " + Clamp(affectedRegularPrisoners) + " defeated garrison troops with '"
                + DescribeConcernAction(action, true) + "'; an escort soldier clearly voiced discontent, pending morale penalty is "
                + Clamp(pendingPenalty) + ".";
        }
        return CastleAftermathCompatibilityTexts.Raw("soldier_reaction_memory_prefix") + DescribeConcernAction(action) + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_memory_action")
            + Clamp(affectedRegularPrisoners) + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_memory_penalty")
            + Clamp(pendingPenalty) + "。";
    }

    public static string BuildAppeasementMessage(int avoidedPenalty)
        => BuildAppeasementMessage(avoidedPenalty, false);

    public static string BuildAppeasementMessage(int avoidedPenalty, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Castle aftermath] Allied soldiers accepted the player\u0027s explanation, compensation, and orders; avoided -"
                + Clamp(avoidedPenalty) + " morale.";
        }
        return CastleAftermathCompatibilityTexts.Raw("soldier_reaction_avoid_prefix")
            + Clamp(avoidedPenalty) + CastleAftermathCompatibilityTexts.Raw("soldier_reaction_avoid_suffix");
    }

    public static string BuildAppeasementMemoryText(int avoidedPenalty)
        => BuildAppeasementMemoryText(avoidedPenalty, false);

    public static string BuildAppeasementMemoryText(int avoidedPenalty, bool isEnglish)
    {
        if (isEnglish)
        {
            return "The player directly appeased discontented escort soldiers on-site, preventing party morale from dropping "
                + Clamp(avoidedPenalty) + ".";
        }
        return CastleAftermathCompatibilityTexts.Raw("soldier_reaction_avoid_memory")
            + Clamp(avoidedPenalty) + "。";
    }

    public static string BuildPenaltyMemoryText(int penalty)
        => BuildPenaltyMemoryText(penalty, false);

    public static string BuildPenaltyMemoryText(int penalty, bool isEnglish)
    {
        if (isEnglish)
        {
            return "The player did not appease escort soldiers who expressed concern over prisoner disposition before leaving; party morale decreased by "
                + Clamp(penalty) + ".";
        }
        return CastleAftermathCompatibilityTexts.Raw("soldier_reaction_penalty_memory_text")
            + Clamp(penalty) + "。";
    }

    public static string DescribeConcernAction(SiegeCastleActionKind action)
        => DescribeConcernAction(action, false);

    public static string DescribeConcernAction(SiegeCastleActionKind action, bool isEnglish)
    {
        if (isEnglish)
        {
            return action switch
            {
                SiegeCastleActionKind.TreatPrisoners => "treating prisoners",
                SiegeCastleActionKind.ReceiveArmaments => "confiscating prisoner arms",
                SiegeCastleActionKind.ReleasePrisoners => "releasing prisoners",
                SiegeCastleActionKind.SlaughterPrisoners => "slaughtering prisoners",
                SiegeCastleActionKind.SellPrisoners => "selling prisoners",
                SiegeCastleActionKind.LaborPrisonersVoluntary => "voluntary penal labor",
                SiegeCastleActionKind.LaborPrisonersForced => "forced penal labor",
                SiegeCastleActionKind.RepairCastleLaborVoluntary => "voluntary castle repair",
                SiegeCastleActionKind.RepairCastleLaborForced => "forced castle repair",
                SiegeCastleActionKind.InstructorPrisonersVoluntary => "voluntary instructors",
                SiegeCastleActionKind.InstructorPrisonersForced => "forced instructors",
                SiegeCastleActionKind.RecruitPrisonersVoluntary => "voluntary recruitment",
                SiegeCastleActionKind.RecruitPrisonersForced => "forced recruitment",
                SiegeCastleActionKind.RecruitLord => "recruiting captive lord",
                SiegeCastleActionKind.SellLord => "selling captive lord",
                SiegeCastleActionKind.ExecuteLord => "executing captive lord",
                _ => "prisoner disposition"
            };
        }
        return action switch
        {
            SiegeCastleActionKind.TreatPrisoners => CastleAftermathCompatibilityTexts.Raw("effect_treat"),
            SiegeCastleActionKind.ReceiveArmaments => CastleAftermathCompatibilityTexts.Raw("effect_receive_armaments"),
            SiegeCastleActionKind.ReleasePrisoners => CastleAftermathCompatibilityTexts.Raw("effect_release"),
            SiegeCastleActionKind.SlaughterPrisoners => CastleAftermathCompatibilityTexts.Raw("effect_slaughter"),
            SiegeCastleActionKind.SellPrisoners => CastleAftermathCompatibilityTexts.Raw("effect_sell"),
            SiegeCastleActionKind.LaborPrisonersVoluntary => CastleAftermathCompatibilityTexts.Raw("staging_labor_voluntary"),
            SiegeCastleActionKind.LaborPrisonersForced => CastleAftermathCompatibilityTexts.Raw("staging_labor_forced"),
            SiegeCastleActionKind.RepairCastleLaborVoluntary => CastleAftermathCompatibilityTexts.Raw("staging_repair_voluntary"),
            SiegeCastleActionKind.RepairCastleLaborForced => CastleAftermathCompatibilityTexts.Raw("staging_repair_forced"),
            SiegeCastleActionKind.InstructorPrisonersVoluntary => CastleAftermathCompatibilityTexts.Raw("staging_instructor_voluntary"),
            SiegeCastleActionKind.InstructorPrisonersForced => CastleAftermathCompatibilityTexts.Raw("staging_instructor_forced"),
            SiegeCastleActionKind.RecruitPrisonersVoluntary => CastleAftermathCompatibilityTexts.Raw("effect_recruit_voluntary"),
            SiegeCastleActionKind.RecruitPrisonersForced => CastleAftermathCompatibilityTexts.Raw("effect_recruit_forced"),
            SiegeCastleActionKind.RecruitLord => CastleAftermathCompatibilityTexts.Raw("concern_recruit_lord"),
            SiegeCastleActionKind.SellLord => CastleAftermathCompatibilityTexts.Raw("concern_sell_lord"),
            SiegeCastleActionKind.ExecuteLord => CastleAftermathCompatibilityTexts.Raw("concern_execute_lord"),
            _ => CastleAftermathCompatibilityTexts.Raw("concern_default")
        };
    }

    private static int Clamp(int value) => value < 0 ? 0 : value;
}
