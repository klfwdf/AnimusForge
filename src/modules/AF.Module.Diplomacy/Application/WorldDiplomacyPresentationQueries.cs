using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Synchronous projections over canonical records. No record or mutable collection escapes to UI.
internal static class WorldDiplomacyPresentationQueries
{
    internal static string ProjectDocumentBody(WorldDiplomacyDocument document) => document.IsPlayerAuthored
        ? document.Body : DiplomacyReplyTextRules.CleanNpcText(document.Body);

    internal static WorldDiplomacyTimelineDocumentsResult Timeline(WorldDiplomacyStorage storage, int maxCount)
    {
        // Preserve take-before-visibility filtering from the former cloned query.
        return WorldDiplomacyTimelineDocumentsResult.Available(
            WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency((storage.Documents ?? new List<WorldDiplomacyDocument>()).Where(x => x != null))
                .Take(Math.Max(1, Math.Min(420, maxCount)))
                .Where(document => document.IsPlayerAuthored || document.IsReadyForPublication)
                .Select(document => new WorldDiplomacyTimelineDocument(
                    document.DocumentId, document.AuthorKingdomId, document.AuthorKingdomName,
                    document.TargetKingdomId, document.TargetKingdomName, document.Title, ProjectDocumentBody(document),
                    document.GameDate, document.Day, document.CreatedUtcTicks, document.IsResponse,
                    document.RequiresResponse, document.ChangedDiplomaticState, document.IsRead,
                    BuildImpactText(document), (document.Actions ?? new List<WorldDiplomacyDocumentAction>())
                        .Where(action => action != null).Select(action =>
                            new WorldDiplomacyTimelineCountryReference(action.TargetKingdomId, action.TargetKingdomName)))));
    }

    internal static string BuildImpactText(WorldDiplomacyDocument document)
    {
        if (document == null) return "";
        List<WorldDiplomacyStandingChange> changes = document.DiplomaticStandingChanges
            ?? new List<WorldDiplomacyStandingChange>();
        List<WorldDiplomacyStandingChange> authorChanges = changes
            .Where(x => x != null && string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        WorldDiplomacyStandingChange international = authorChanges.LastOrDefault(x =>
            string.Equals(x.Kind, "international_reputation", StringComparison.OrdinalIgnoreCase));
        List<WorldDiplomacyStandingChange> prestigeChanges = authorChanges.Where(x =>
            string.Equals(x.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase)).ToList();
        int prestigeDelta = prestigeChanges.Sum(x => x.Delta);
        string prestigeReason = string.Join("；", prestigeChanges.Select(x => x.Reason)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(document.MechanicalResult))
        {
            sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactResultSection", "【Diplomatic result】"));
            sb.AppendLine(WorldDiplomacyAnalysisApplication.ProjectMechanicalResultForDisplay(document.MechanicalResult).Trim());
            sb.AppendLine();
        }
        sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactReputationSection", "【International reputation】"));
        sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactDeltaLabel", "Change: ") + WorldDiplomacyReputationRules.BuildInternationalReputationImpactDeltaText(document, international, DiplomacyUiLocalization.Text));
        sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactReasonLabel", "Reason: ") + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(international?.Reason,
            document.InternationalReputationEvaluationReason,
            DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactNoReputationChange", "This declaration produced no clear change in international reputation.")));
        sb.AppendLine();
        sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactPrestigeSection", "【National prestige】"));
        sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactDeltaLabel", "Change: ") + WorldDiplomacyReputationRules.FormatSignedStandingDelta(prestigeDelta, DiplomacyUiLocalization.Text));
        sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactReasonLabel", "Reason: ") + (prestigeChanges.Count == 0 || string.IsNullOrWhiteSpace(prestigeReason)
            ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactNoPrestigeChange", "This declaration triggered no national prestige settlement.")
            : prestigeReason));
        bool hasOtherKingdomImpact = false;
        foreach (WorldDiplomacyStandingChange other in changes.Where(x => x != null
            && !string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)))
        {
            sb.AppendLine();
            if (!hasOtherKingdomImpact)
            {
                sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactOtherKingdomsSection", "【Impact on other kingdoms】"));
                hasOtherKingdomImpact = true;
            }
            sb.AppendLine(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(other.KingdomName, other.KingdomId, DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_UnknownKingdom", "Unknown kingdom")) + ": "
                + (string.Equals(other.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase)
                    ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactPrestigeSection", "【National prestige】")
                    : DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactReputationSection", "【International reputation】")));
            sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactDeltaLabel", "Change: ") + WorldDiplomacyReputationRules.FormatSignedStandingDelta(other.Delta, DiplomacyUiLocalization.Text));
            sb.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactReasonLabel", "Reason: ") + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(other.Reason, DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactNoReason", "no explanation given")));
        }
        return sb.ToString().TrimEnd();
    }

    internal static WorldDiplomacyDocumentDetail Detail(WorldDiplomacyDocument document, WorldDiplomacyRound round,
        WorldDiplomacyPlayerContext player, Func<int, string> formatDate)
    {
        if (document == null) return null;
        // The notice already selects the relevant public document. Reply is a
        // compose shortcut; only the player's ruler authority gates this entry.
        bool canReply = player?.IsRuler == true;
        return Detail(document, player.Generation, canReply, formatDate,
            WorldDiplomacyPlayerApplication.CanRetryAnalysis(document, player));
    }

    internal static WorldDiplomacyDocumentDetail Detail(WorldDiplomacyDocument document, long generation,
        bool canReply, Func<int, string> formatDate, bool canRetryAnalysis = false)
    {
        string date = DiplomacyDateProjection.ForUi(
            WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDate(document.Day)));
        string subtitle = document.AuthorKingdomName + " · " + document.AuthorRulerName + " · "
            + date + " · "
            + WorldDiplomacyTextRules.DocumentTypeLabel(document, DiplomacyUiLocalization.Text);
        return new WorldDiplomacyDocumentDetail(document.DocumentId, document.RoundId, generation,
            WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(document, DiplomacyUiLocalization.Text), subtitle,
            string.IsNullOrWhiteSpace(document.Body) ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_BodyArchived", "(This older declaration's body has been compressed into an annual summary.)") : WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay(ProjectDocumentBody(document)),
            BuildImpactText(document), canReply, canRetryAnalysis);
    }

    internal static IReadOnlyList<WorldDiplomacyArchiveRecord> Archive(WorldDiplomacyStorage storage,
        Func<int, string> formatDate, Func<string, WorldDiplomacyRound> resolveRound,
        Func<string, WorldDiplomacyDocument> resolveDocument, WorldDiplomacyPlayerContext player = null)
    {
        var records = new List<WorldDiplomacyArchiveRecord>();
        foreach (WorldDiplomacyDocument document in WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(storage.Documents
                .Where(x => x != null && (x.IsPlayerAuthored || x.IsReadyForPublication))).Take(240))
        {
            if (document == null)
            {
                continue;
            }
            string kingdomId = document.AuthorKingdomId;
            string kingdomName = document.AuthorKingdomName;
            string date = DiplomacyDateProjection.ForUi(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDate(document.Day)));
            string typeLabel = WorldDiplomacyTextRules.DocumentTypeLabel(document, DiplomacyUiLocalization.Text);
            string eventMeta = WorldDiplomacyTextRules.BuildDocumentEventMeta(document, resolveRound, resolveDocument);
            string targetSummary = document.Actions?.Count > 1
                ? string.Join("、", document.Actions.Where(x => x != null).Select(x => WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId)))
                : document.TargetKingdomName;
            records.Add(new WorldDiplomacyArchiveRecord(kingdomId, kingdomName,
                EventId: document.DocumentId,
                KindLabel: typeLabel,
                HeaderRightText: WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(targetSummary, DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_WorldBulletin", "World bulletin")),
                DateText: date,
                TitleText: WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(document, DiplomacyUiLocalization.Text),
                IndexTitleText: WorldDiplomacyTextRules.BuildArchiveIndexDocumentTitle(document, DiplomacyUiLocalization.Text),
                MetaText: date + "  ·  " + typeLabel + "  ·  " + document.AuthorKingdomName + (string.IsNullOrWhiteSpace(targetSummary) ? "" : " → " + targetSummary) + eventMeta,
                PolicyNameText: "",
                BodyText: string.IsNullOrWhiteSpace(document.Body) ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_BodyCompressed", "This older declaration's body has been compressed; see the matching annual diplomatic summary.") : WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay(ProjectDocumentBody(document)),
                BodySectionTitleText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_BodySectionTitle", "Declaration body"),
                ImpactSectionTitleText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ImpactSectionTitle", "Diplomatic result and impact"),
                ImpactText: BuildImpactText(document),
                IndexMetaText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_IndexMetaDeclaration", "Diplomatic declaration: ") + typeLabel,
                UnreadMarkerText: document.IsRead ? "" : DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_UnreadMarker", "New"),
                IsUnread: !document.IsRead,
                HasPolicyName: false,
                HasImpact: true,
                CanRetryAnalysis: WorldDiplomacyPlayerApplication.CanRetryAnalysis(document, player)));
        }
        foreach (WorldDiplomacyAnnualSummary summary in storage.AnnualSummaries.OrderByDescending(x => x.Year))
        {
            string kingdomId = "diplomacy_archive";
            string kingdomName = DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ChronicleArchive", "Diplomatic chronicle archive");
            records.Add(new WorldDiplomacyArchiveRecord(kingdomId, kingdomName,
                EventId: "diplomacy_summary:" + summary.Year.ToString(CultureInfo.InvariantCulture),
                KindLabel: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnnualSummaryKind", "Annual diplomatic summary"),
                HeaderRightText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_WorldSharedMemory", "World shared memory"),
                DateText: DiplomacyUiLocalization.Format("AF_DIP_UI_WorldDiplomacy_YearLabel", "Year {YEAR}", ("YEAR", (summary.Year + 1).ToString(CultureInfo.InvariantCulture))),
                TitleText: DiplomacyUiLocalization.Format("AF_DIP_UI_WorldDiplomacy_AnnualRecordTitle", "Year {YEAR} diplomatic record", ("YEAR", (summary.Year + 1).ToString(CultureInfo.InvariantCulture))),
                MetaText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnnualCompressedArchive", "Annual compressed archive"),
                BodyText: summary.Summary,
                BodySectionTitleText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnnualSummarySection", "Annual summary"),
                ImpactSectionTitleText: summary.MajorEvents.Count > 0 ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_MajorEventsSection", "Major event index") : "",
                ImpactText: string.Join("\n", summary.MajorEvents ?? new List<string>()),
                IndexMetaText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_AnnualSummaryKind", "Annual diplomatic summary"),
                HasImpact: summary.MajorEvents.Count > 0));
        }
        foreach (WorldDiplomacyCompressionSummary summary in (storage.CompressionSummaries ?? new List<WorldDiplomacyCompressionSummary>()).OrderByDescending(x => x.CreatedDay))
        {
            string kingdomId = "diplomacy_archive";
            string kingdomName = DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ChronicleArchive", "Diplomatic chronicle archive");
            records.Add(new WorldDiplomacyArchiveRecord(kingdomId, kingdomName,
                EventId: "diplomacy_summary:" + summary.BatchId,
                KindLabel: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_HistoryCompactionKind", "Diplomatic history compaction"),
                HeaderRightText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_LongTermMemory", "Long-term diplomatic memory"),
                DateText: DiplomacyDateProjection.ForUi(formatDate(summary.CreatedDay)),
                TitleText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_HistoryCompactionTitle", "Diplomatic history compaction record"),
                MetaText: DiplomacyUiLocalization.Format("AF_DIP_UI_WorldDiplomacy_CompactionMeta", "Compacted after {TOKENS} accumulated tokens", ("TOKENS", summary.TokenCount.ToString("N0", CultureInfo.InvariantCulture))),
                BodyText: summary.Summary,
                BodySectionTitleText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_DiplomaticRecordSection", "Diplomatic record"),
                ImpactSectionTitleText: summary.ConfirmedResults.Count > 0 ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_GameConfirmedResultsSection", "Game-confirmed results") : "",
                ImpactText: string.Join("\n", summary.ConfirmedResults),
                IndexMetaText: DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_HistoryCompactionKind", "Diplomatic history compaction"),
                HasImpact: summary.ConfirmedResults.Count > 0));
        }
        return records.AsReadOnly();
    }
    internal static string Standing(WorldDiplomacyStorage storage, string kingdomId)
    {
        if (kingdomId == null) return "";
            int prestige = WorldDiplomacyReputationRules.GetNationalPrestige(storage?.NationalPrestigeByKingdom, kingdomId);
            int reputation = WorldDiplomacyReputationRules.GetInternationalReputation(storage?.InternationalReputationByKingdom, kingdomId);
            return DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_StandingSection", "【National prestige and international reputation】") + "\n"
                + DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_StandingPrestigeLabel", "National prestige: ") + prestige.ToString(CultureInfo.InvariantCulture)
                + DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_StandingPrestigeHint", "/100 (this kingdom's diplomatic credit and deterrence; too low a value damages relations with its nobles)") + "\n"
                + DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_StandingReputationLabel", "International reputation: ") + reputation.ToString(CultureInfo.InvariantCulture)
                + DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_StandingReputationHint", "/100 (how other kingdoms judge this one; affects willingness to cooperate and to apply pressure)");
    }
    internal static string ArchiveSubtitle(WorldDiplomacyStorage storage,
        Func<string, WorldDiplomacyDocument> resolveDocument, Func<string, string> representativeName,
        Func<string, string> kingdomName)
    {
        WorldDiplomacyRound round = storage.ActiveRound;
        if (round == null || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)
            || string.IsNullOrWhiteSpace(round.RootDocumentId))
        {
            return DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ArchiveSubtitleIdle", "Review custom policies, policy-derived events, and diplomatic declarations published by every kingdom in one place.");
        }
        string topic = WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round.RoundTopic, resolveDocument(round.RootDocumentId)?.Title, DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_DefaultTopic", "diplomatic negotiation")));
        List<string> participantNames = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
            .Where(x => x != null && string.Equals(x.State, "active", StringComparison.OrdinalIgnoreCase))
            .Select(x => representativeName(x.KingdomId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.CurrentCulture)
            .ToList();
        if (participantNames.Count == 0)
        {
            string initiator = kingdomName(round.InitiatorKingdomId);
            if (initiator != null) participantNames.Add(initiator);
        }
        return DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ActiveEventPrefix", "Current diplomatic event: ") + WorldDiplomacyTextRules.Limit(topic, 60)
            + DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ActiveEventInProgress", "  ·  In progress")
            + (participantNames.Count == 0 ? "" : DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ActiveEventParticipants", "  ·  Participants: ") + string.Join(", ", participantNames));
    }
}
