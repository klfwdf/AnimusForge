using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// One concise castle completion ledger; it replaces separate native, GCCZ and morale notices.
/// </summary>
public static class SiegeCastleCompletedInterventionSummaryBuilder
{
    private static readonly Regex TemplateSlotRegex = new Regex(@"\{[a-z_]+\}", RegexOptions.Compiled);

    public static string Build(SiegeCastleCompletedInterventionSummaryFacts facts)
    {
        return Build(facts, isEnglish: false);
    }

    public static string Build(SiegeCastleCompletedInterventionSummaryFacts facts, bool isEnglish)
    {
        if (facts == null)
        {
            return isEnglish
                ? "[Castle aftermath settlement] Native mercy and castle disposition have been completed."
                : CastleAftermathCompatibilityTexts.Raw("summary_empty");
        }

        string settlement = Normalize(facts.SettlementName, isEnglish ? "this castle" : CastleAftermathCompatibilityTexts.Raw("summary_settlement_fallback"));
        string player = Normalize(facts.PlayerName, isEnglish ? "The player" : CastleAftermathCompatibilityTexts.Raw("summary_player_fallback"));
        var sb = new StringBuilder();
        if (isEnglish)
        {
            sb.Append('[').Append(settlement).Append(" - Castle Aftermath Settlement]").AppendLine();
            sb.Append("Fief: Loyalty ").Append(Signed(facts.LoyaltyDelta))
                .Append(" | Security ").Append(Signed(facts.SecurityDelta))
                .Append(" | Prosperity ").Append(Signed(facts.ProsperityDelta));
            if (facts.ConstructionSpeedBonusPercent > 0.005f)
            {
                sb.Append(" | Project Construction Speed +")
                    .Append(facts.ConstructionSpeedBonusPercent.ToString("0.##"))
                    .Append("% (1 year)");
            }
            sb.AppendLine();
            sb.Append("Prisoners: ").Append(BuildPrisonerLine(facts, true)).AppendLine();
            sb.Append("Morale: ").Append(BuildMoraleLine(facts, player, true)).AppendLine();
            sb.Append("Public Opinion: ").Append(SiegeCastlePublicOpinionProfile.Build(
                player,
                facts.SettlementPublicTrustDelta,
                facts.BoundVillagePublicTrustDelta,
                facts.NotableRelationDelta,
                facts.NotableTrustDelta,
                true));

            if (!string.IsNullOrWhiteSpace(facts.LordOutcomeSummary) || facts.RetainedLordPrisoners > 0)
            {
                sb.AppendLine();
                sb.Append("Lords: ");
                if (!string.IsNullOrWhiteSpace(facts.LordOutcomeSummary))
                {
                    sb.Append(SiegeSceneMemoryPromptProjection.ProjectLordOutcomes(facts.LordOutcomeSummary.Trim(), true));
                    if (facts.RetainedLordPrisoners > 0)
                    {
                        sb.Append("; ");
                    }
                }
                if (facts.RetainedLordPrisoners > 0)
                {
                    sb.Append(facts.RetainedLordPrisoners).Append(" remain captives");
                }
                sb.Append('.');
            }
            return sb.ToString();
        }

        sb.Append(FormatTemplate(CastleAftermathCompatibilityTexts.Raw("summary_title"), ("{settlement}", settlement))).AppendLine();
        sb.Append(CastleAftermathCompatibilityTexts.Raw("summary_fief_prefix")).Append(Signed(facts.LoyaltyDelta))
            .Append(CastleAftermathCompatibilityTexts.Raw("summary_fief_security")).Append(Signed(facts.SecurityDelta))
            .Append(CastleAftermathCompatibilityTexts.Raw("summary_fief_prosperity")).Append(Signed(facts.ProsperityDelta));
        if (facts.ConstructionSpeedBonusPercent > 0.005f)
        {
            sb.Append(CastleAftermathCompatibilityTexts.Raw("summary_project_speed"))
                .Append(facts.ConstructionSpeedBonusPercent.ToString("0.##"))
                .Append(CastleAftermathCompatibilityTexts.Raw("summary_project_year"));
        }
        sb.AppendLine();
        sb.Append(CastleAftermathCompatibilityTexts.Raw("summary_prisoners_label")).Append(BuildPrisonerLine(facts, false)).AppendLine();
        sb.Append(CastleAftermathCompatibilityTexts.Raw("summary_morale_label")).Append(BuildMoraleLine(facts, player, false)).AppendLine();
        sb.Append(CastleAftermathCompatibilityTexts.Raw("summary_opinion_label")).Append(SiegeCastlePublicOpinionProfile.Build(
            player,
            facts.SettlementPublicTrustDelta,
            facts.BoundVillagePublicTrustDelta,
            facts.NotableRelationDelta,
            facts.NotableTrustDelta));

        if (!string.IsNullOrWhiteSpace(facts.LordOutcomeSummary) || facts.RetainedLordPrisoners > 0)
        {
            sb.AppendLine();
            sb.Append(CastleAftermathCompatibilityTexts.Raw("summary_lord_label"));
            if (!string.IsNullOrWhiteSpace(facts.LordOutcomeSummary))
            {
                sb.Append(facts.LordOutcomeSummary.Trim());
                if (facts.RetainedLordPrisoners > 0)
                {
                    sb.Append("；");
                }
            }
            if (facts.RetainedLordPrisoners > 0)
            {
                sb.Append(facts.RetainedLordPrisoners).Append(CastleAftermathCompatibilityTexts.Raw("summary_lord_retained"));
            }
            sb.Append('。');
        }
        return sb.ToString();
    }

    private static string BuildPrisonerLine(SiegeCastleCompletedInterventionSummaryFacts facts)
        => BuildPrisonerLine(facts, false);

    private static string BuildPrisonerLine(SiegeCastleCompletedInterventionSummaryFacts facts, bool isEnglish)
    {
        List<string> parts = facts.RegularPrisonerOutcomes
            .Where(entry => entry != null && entry.AffectedCount > 0)
            .Select(e => DescribeOutcome(e, isEnglish))
            .ToList();
        if (facts.TreatedRegularPrisoners)
        {
            parts.Add(isEnglish ? "supplies distributed and mistreatment forbidden" : CastleAftermathCompatibilityTexts.Raw("summary_care_done"));
        }
        if (facts.ReceivedRegularArmaments)
        {
            parts.Add(isEnglish ? "confiscated arms sent to inventory" : CastleAftermathCompatibilityTexts.Raw("summary_arms_done"));
        }
        if (facts.RemainingRegularPrisoners > 0)
        {
            parts.Add(isEnglish ? facts.RemainingRegularPrisoners + " remain captives" : facts.RemainingRegularPrisoners + CastleAftermathCompatibilityTexts.Raw("summary_retained_suffix"));
        }
        if (parts.Count == 0)
        {
            parts.Add(isEnglish ? "no further disposition, brought troops remain captives" : CastleAftermathCompatibilityTexts.Raw("summary_no_disposition"));
        }
        return string.Join(isEnglish ? "; " : "；", parts) + (isEnglish ? "." : "。");
    }

    private static string DescribeOutcome(SiegeCastleDispositionSummaryEntry entry)
        => DescribeOutcome(entry, false);

    private static string DescribeOutcome(SiegeCastleDispositionSummaryEntry entry, bool isEnglish)
    {
        string count = entry.AffectedCount + (isEnglish ? " troops " : CastleAftermathCompatibilityTexts.Raw("summary_count_unit"));
        if (isEnglish)
        {
            return entry.Action switch
            {
                SiegeCastleActionKind.ReleasePrisoners => count + "released",
                SiegeCastleActionKind.SellPrisoners => count + "sold" + (entry.Gold > 0 ? " (" + entry.Gold + " denars)" : string.Empty),
                SiegeCastleActionKind.RecruitPrisonersVoluntary => count + "voluntarily joined party",
                SiegeCastleActionKind.RecruitPrisonersForced => count + "forcibly recruited",
                SiegeCastleActionKind.LaborPrisonersVoluntary => count + "voluntary penal labor",
                SiegeCastleActionKind.LaborPrisonersForced => count + "forced penal labor",
                SiegeCastleActionKind.RepairCastleLaborVoluntary => count + "voluntary castle repair",
                SiegeCastleActionKind.RepairCastleLaborForced => count + "forced castle repair",
                SiegeCastleActionKind.InstructorPrisonersVoluntary => count + "voluntary instructors",
                SiegeCastleActionKind.InstructorPrisonersForced => count + "forced instructors",
                SiegeCastleActionKind.SlaughterPrisoners => count + "slaughtered on-site",
                _ => count + "disposition completed"
            };
        }
        return entry.Action switch
        {
            SiegeCastleActionKind.ReleasePrisoners => count + CastleAftermathCompatibilityTexts.Raw("summary_action_release"),
            SiegeCastleActionKind.SellPrisoners => count + CastleAftermathCompatibilityTexts.Raw("summary_action_sell") + (entry.Gold > 0 ? FormatTemplate(CastleAftermathCompatibilityTexts.Raw("summary_action_sell_gold_legacy"), ("{gold}", entry.Gold.ToString())) : string.Empty),
            SiegeCastleActionKind.RecruitPrisonersVoluntary => count + CastleAftermathCompatibilityTexts.Raw("summary_action_recruit_voluntary"),
            SiegeCastleActionKind.RecruitPrisonersForced => count + CastleAftermathCompatibilityTexts.Raw("summary_action_recruit_forced"),
            SiegeCastleActionKind.LaborPrisonersVoluntary => count + CastleAftermathCompatibilityTexts.Raw("summary_action_labor_voluntary"),
            SiegeCastleActionKind.LaborPrisonersForced => count + CastleAftermathCompatibilityTexts.Raw("summary_action_labor_forced"),
            SiegeCastleActionKind.RepairCastleLaborVoluntary => count + CastleAftermathCompatibilityTexts.Raw("summary_action_repair_voluntary"),
            SiegeCastleActionKind.RepairCastleLaborForced => count + CastleAftermathCompatibilityTexts.Raw("summary_action_repair_forced"),
            SiegeCastleActionKind.InstructorPrisonersVoluntary => count + CastleAftermathCompatibilityTexts.Raw("summary_action_instructor_voluntary"),
            SiegeCastleActionKind.InstructorPrisonersForced => count + CastleAftermathCompatibilityTexts.Raw("summary_action_instructor_forced_legacy"),
            SiegeCastleActionKind.SlaughterPrisoners => count + CastleAftermathCompatibilityTexts.Raw("summary_action_slaughter"),
            _ => count + CastleAftermathCompatibilityTexts.Raw("summary_action_default")
        };
    }

    private static string BuildMoraleLine(SiegeCastleCompletedInterventionSummaryFacts facts, string player)
        => BuildMoraleLine(facts, player, false);

    private static string BuildMoraleLine(SiegeCastleCompletedInterventionSummaryFacts facts, string player, bool isEnglish)
    {
        string action = SiegeCastleSoldierReactionProfile.DescribeConcernAction(facts.SoldierConcernAction, isEnglish);
        if (isEnglish)
        {
            if (facts.SoldierMoralePenaltyApplied > 0)
            {
                return "Because " + player + " executed \u0027" + action
                    + "\u0027 on prisoners, escort soldiers expressed discontent and were not appeased; morale -" + facts.SoldierMoralePenaltyApplied
                    + BuildRecruitmentMoraleBreakdown(facts.SoldierConcernAction, facts.SoldierMoralePenaltyApplied, true)
                    + ".";
            }
            if (facts.SoldierAppeasementRequired && facts.SoldierAppeasementApplied)
            {
                return "Escort soldiers voiced concern regarding \u0027" + action + "\u0027, but accepted " + player + "\u0027s appeasement; no morale was deducted.";
            }
            return "Escort soldiers developed no public discontent requiring appeasement; no additional morale was deducted.";
        }
        if (facts.SoldierMoralePenaltyApplied > 0)
        {
            return FormatTemplate(CastleAftermathCompatibilityTexts.Raw("summary_penalty_prefix_legacy"),
                ("{player}", player), ("{action}", action), ("{penalty}", facts.SoldierMoralePenaltyApplied.ToString()))
                + BuildRecruitmentMoraleBreakdown(facts.SoldierConcernAction, facts.SoldierMoralePenaltyApplied, false)
                + "\u3002";
        }
        if (facts.SoldierAppeasementRequired && facts.SoldierAppeasementApplied)
        {
            return FormatTemplate(CastleAftermathCompatibilityTexts.Raw("summary_appeased_text"), ("{action}", action), ("{player}", player));
        }
        return CastleAftermathCompatibilityTexts.Raw("summary_no_discontent_text");
    }

    private static string BuildRecruitmentMoraleBreakdown(SiegeCastleActionKind action, int penalty)
        => BuildRecruitmentMoraleBreakdown(action, penalty, false);

    private static string BuildRecruitmentMoraleBreakdown(SiegeCastleActionKind action, int penalty, bool isEnglish)
    {
        if (action == SiegeCastleActionKind.RecruitPrisonersVoluntary
            && penalty == SiegeCastleSoldierReactionProfile.VoluntaryRecruitmentTotalPenalty)
        {
            return isEnglish
                ? " (Recruit discontent -30 + voluntary recruit penalty -30)"
                : CastleAftermathCompatibilityTexts.Raw("summary_voluntary_recruit_penalty_detail");
        }
        if (action == SiegeCastleActionKind.RecruitPrisonersForced
            && penalty == SiegeCastleSoldierReactionProfile.ForcedRecruitmentTotalPenalty)
        {
            return isEnglish
                ? " (Recruit discontent -30 + forced recruit penalty -60)"
                : CastleAftermathCompatibilityTexts.Raw("summary_forced_recruit_penalty_detail");
        }
        return string.Empty;
    }

    private static string Signed(float value)
    {
        float normalized = Math.Abs(value) < 0.005f ? 0f : value;
        return normalized > 0f ? "+" + normalized.ToString("0.##") : normalized.ToString("0.##");
    }

    private static string FormatTemplate(string template, params (string Token, string Value)[] replacements)
    {
        return TemplateSlotRegex.Replace(template ?? string.Empty, match =>
        {
            foreach (var replacement in replacements)
            {
                if (match.Value == replacement.Token) return replacement.Value ?? string.Empty;
            }
            return match.Value;
        });
    }

    private static string Normalize(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
