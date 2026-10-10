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
    internal static WorldDiplomacyTimelineDocumentsResult Timeline(WorldDiplomacyStorage storage, int maxCount)
    {
        // Preserve take-before-visibility filtering from the former cloned query.
        return WorldDiplomacyTimelineDocumentsResult.Available(
            WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency((storage.Documents ?? new List<WorldDiplomacyDocument>()).Where(x => x != null))
                .Take(Math.Max(1, Math.Min(420, maxCount)))
                .Where(document => document.IsPlayerAuthored || document.IsReadyForPublication)
                .Select(document => new WorldDiplomacyTimelineDocument(
                    document.DocumentId, document.AuthorKingdomId, document.AuthorKingdomName,
                    document.TargetKingdomId, document.TargetKingdomName, document.Title, document.Body,
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
            sb.AppendLine("【外交结果】");
            sb.AppendLine(document.MechanicalResult.Trim());
            sb.AppendLine();
        }
        sb.AppendLine("【国际声誉】");
        sb.AppendLine("变化：" + WorldDiplomacyReputationRules.BuildInternationalReputationImpactDeltaText(document, international));
        sb.AppendLine("原因：" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(international?.Reason,
            document.InternationalReputationEvaluationReason,
            "本篇没有形成明确的国际声誉变化。"));
        sb.AppendLine();
        sb.AppendLine("【国家威望】");
        sb.AppendLine("变化：" + WorldDiplomacyReputationRules.FormatSignedStandingDelta(prestigeDelta));
        sb.AppendLine("原因：" + (prestigeChanges.Count == 0 || string.IsNullOrWhiteSpace(prestigeReason)
            ? "本篇没有触发国家威望结算。"
            : prestigeReason));
        bool hasOtherKingdomImpact = false;
        foreach (WorldDiplomacyStandingChange other in changes.Where(x => x != null
            && !string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)))
        {
            sb.AppendLine();
            if (!hasOtherKingdomImpact)
            {
                sb.AppendLine("【其他国家影响】");
                hasOtherKingdomImpact = true;
            }
            sb.AppendLine(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(other.KingdomName, other.KingdomId, "未知国家") + "："
                + (string.Equals(other.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase) ? "国家威望" : "国际声誉"));
            sb.AppendLine("变化：" + WorldDiplomacyReputationRules.FormatSignedStandingDelta(other.Delta));
            sb.AppendLine("原因：" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(other.Reason, "无说明"));
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
        string subtitle = document.AuthorKingdomName + " · " + document.AuthorRulerName + " · "
            + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDate(document.Day)) + " · "
            + WorldDiplomacyTextRules.DocumentTypeLabel(document);
        return new WorldDiplomacyDocumentDetail(document.DocumentId, document.RoundId, generation,
            WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(document), subtitle,
            string.IsNullOrWhiteSpace(document.Body) ? "（该旧公文正文已压缩至年度摘要。）" : WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay(document.Body),
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
            string date = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.GameDate, formatDate(document.Day));
            string typeLabel = WorldDiplomacyTextRules.DocumentTypeLabel(document);
            string eventMeta = WorldDiplomacyTextRules.BuildDocumentEventMeta(document, resolveRound, resolveDocument);
            string targetSummary = document.Actions?.Count > 1
                ? string.Join("、", document.Actions.Where(x => x != null).Select(x => WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId)))
                : document.TargetKingdomName;
            records.Add(new WorldDiplomacyArchiveRecord(kingdomId, kingdomName,
                EventId: document.DocumentId,
                KindLabel: typeLabel,
                HeaderRightText: WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(targetSummary, "世界公告"),
                DateText: date,
                TitleText: WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(document),
                IndexTitleText: WorldDiplomacyTextRules.BuildArchiveIndexDocumentTitle(document),
                MetaText: date + "  ·  " + typeLabel + "  ·  " + document.AuthorKingdomName + (string.IsNullOrWhiteSpace(targetSummary) ? "" : " → " + targetSummary) + eventMeta,
                PolicyNameText: "",
                BodyText: string.IsNullOrWhiteSpace(document.Body) ? "该旧公文正文已经压缩，可查看对应年度外交摘要。" : WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay(document.Body),
                BodySectionTitleText: "公告正文",
                ImpactSectionTitleText: "外交结果与外交影响",
                ImpactText: BuildImpactText(document),
                IndexMetaText: "外交宣言：" + typeLabel,
                UnreadMarkerText: document.IsRead ? "" : "新",
                IsUnread: !document.IsRead,
                HasPolicyName: false,
                HasImpact: true,
                CanRetryAnalysis: WorldDiplomacyPlayerApplication.CanRetryAnalysis(document, player)));
        }
        foreach (WorldDiplomacyAnnualSummary summary in storage.AnnualSummaries.OrderByDescending(x => x.Year))
        {
            string kingdomId = "diplomacy_archive";
            string kingdomName = "外交编年档案";
            records.Add(new WorldDiplomacyArchiveRecord(kingdomId, kingdomName,
                EventId: "diplomacy_summary:" + summary.Year.ToString(CultureInfo.InvariantCulture),
                KindLabel: "年度外交摘要",
                HeaderRightText: "世界共享记忆",
                DateText: "第" + (summary.Year + 1).ToString(CultureInfo.InvariantCulture) + "年",
                TitleText: "第" + (summary.Year + 1).ToString(CultureInfo.InvariantCulture) + "年外交纪要",
                MetaText: "年度压缩档案",
                BodyText: summary.Summary,
                BodySectionTitleText: "年度摘要",
                ImpactSectionTitleText: summary.MajorEvents.Count > 0 ? "重大事件索引" : "",
                ImpactText: string.Join("\n", summary.MajorEvents ?? new List<string>()),
                IndexMetaText: "年度外交摘要",
                HasImpact: summary.MajorEvents.Count > 0));
        }
        foreach (WorldDiplomacyCompressionSummary summary in (storage.CompressionSummaries ?? new List<WorldDiplomacyCompressionSummary>()).OrderByDescending(x => x.CreatedDay))
        {
            string kingdomId = "diplomacy_archive";
            string kingdomName = "外交编年档案";
            records.Add(new WorldDiplomacyArchiveRecord(kingdomId, kingdomName,
                EventId: "diplomacy_summary:" + summary.BatchId,
                KindLabel: "外交历史整理",
                HeaderRightText: "长期外交记忆",
                DateText: formatDate(summary.CreatedDay),
                TitleText: "外交历史整理档案",
                MetaText: "累计 " + summary.TokenCount.ToString("N0", CultureInfo.InvariantCulture) + " Tokens 后整理",
                BodyText: summary.Summary,
                BodySectionTitleText: "外交纪要",
                ImpactSectionTitleText: summary.ConfirmedResults.Count > 0 ? "游戏确认结果" : "",
                ImpactText: string.Join("\n", summary.ConfirmedResults),
                IndexMetaText: "外交历史整理",
                HasImpact: summary.ConfirmedResults.Count > 0));
        }
        return records.AsReadOnly();
    }
    internal static string Standing(WorldDiplomacyStorage storage, string kingdomId)
    {
        if (kingdomId == null) return "";
            int prestige = WorldDiplomacyReputationRules.GetNationalPrestige(storage?.NationalPrestigeByKingdom, kingdomId);
            int reputation = WorldDiplomacyReputationRules.GetInternationalReputation(storage?.InternationalReputationByKingdom, kingdomId);
            return "【国家威望与国际声誉】\n"
                + "国家威望：" + prestige.ToString(CultureInfo.InvariantCulture)
                + "/100（该国的外交信用与威慑；过低会损害国内贵族关系）\n"
                + "国际声誉：" + reputation.ToString(CultureInfo.InvariantCulture)
                + "/100（他国对该国的评价；影响合作意愿与施压倾向）";
    }
    internal static string ArchiveSubtitle(WorldDiplomacyStorage storage,
        Func<string, WorldDiplomacyDocument> resolveDocument, Func<string, string> representativeName,
        Func<string, string> kingdomName)
    {
        WorldDiplomacyRound round = storage.ActiveRound;
        if (round == null || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)
            || string.IsNullOrWhiteSpace(round.RootDocumentId))
        {
            return "统一查看自定义政策、政策衍生事件与各国公开发布的外交宣言。";
        }
        string topic = WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round.RoundTopic, resolveDocument(round.RootDocumentId)?.Title, "外交交涉"));
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
        return "当前外交事件：" + WorldDiplomacyTextRules.Limit(topic, 60)
            + "  ·  进行中"
            + (participantNames.Count == 0 ? "" : "  ·  参与国：" + string.Join("、", participantNames));
    }
}
