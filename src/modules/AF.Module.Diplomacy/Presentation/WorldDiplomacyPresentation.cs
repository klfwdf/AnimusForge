using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

internal static class WorldDiplomacyPresentation
{
    internal static bool OpenComposeFromTerminal(Action onClose = null)
    {
        WorldDiplomacyPlayerContext player = WorldDiplomacyPresentationHost.Player();
        if (player == null)
        {
            InformationManager.DisplayMessage(new InformationMessage("AI 外交功能尚未初始化。"));
            return false;
        }
        if (!player.IsRuler)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "无法发布外交宣言",
                "只有王国统治者才能发布外交宣言。",
                true,
                false,
                "知道了",
                "",
                onClose,
                null),
                pauseGameActiveState: true);
            return false;
        }
        return WorldDiplomacyComposePopup.Show(
            "撰写外交宣言",
            "",
            "",
            body => DisplayResult(WorldDiplomacyPresentationHost.Submit(new WorldDiplomacyPlayerDocumentCommand(body, player.Generation))),
            onClose);
    }
    private static void DisplayResult(string message)
    {
        if (!string.IsNullOrEmpty(message)) InformationManager.DisplayMessage(new InformationMessage(message));
    }

    internal static bool OpenDocumentFromNotification(string documentId)
    {
        // Preserve the original read-before-popup behavior, including a failed popup open.
        if (!WorldDiplomacyPresentationHost.MarkRead(documentId)) return false;
        WorldDiplomacyDocumentDetail detail = WorldDiplomacyPresentationHost.Detail(documentId);
        if (detail == null) return false;
        Action reply = detail.CanRetryAnalysis
            ? (Action)(() => DisplayResult(WorldDiplomacyPresentationHost.RetryAnalysis(detail.DocumentId, detail.Generation)))
            : detail.CanReply ? (Action)(() => OpenPlayerReplyCompose(detail)) : null;
        return CourierLetterReplyPopup.ShowWithReply(detail.Title, detail.Subtitle, detail.Body,
            reply, detail.CanRetryAnalysis ? "重新解析" : "回应", null, "关闭", detail.Impact);
    }

    private static void OpenPlayerReplyCompose(WorldDiplomacyDocumentDetail detail)
    {
        if (!WorldDiplomacyPresentationHost.CanOpenReply(detail.DocumentId, detail.RoundId, detail.Generation)) return;
        WorldDiplomacyComposePopup.Show("回应外交宣言", "", "", body => DisplayResult(
            WorldDiplomacyPresentationHost.Submit(new WorldDiplomacyPlayerDocumentCommand(
                body, detail.Generation, detail.DocumentId, detail.RoundId))), null);
    }
    internal static bool ShowRoyalAnnouncementArchive(Action onClose = null)
    {
        bool available = WorldDiplomacyPresentationHost.IsAvailable;
        if (!available || Campaign.Current == null || !(ScreenManager.TopScreen is MapScreen))
        {
            return false;
        }
        try
        {
            Action returnToArchive = () => ShowRoyalAnnouncementArchive(onClose);
            long generation = WorldDiplomacyPresentationHost.Player()?.Generation ?? -1;
            return AnimusForgeWorldEventInboxPopup.Show(
                BuildRoyalAnnouncementArchiveData(),
                recordId => {
                    const string retryPrefix = "diplomacy_analysis:";
                    if (recordId.StartsWith(retryPrefix, StringComparison.Ordinal)) {
                        DisplayResult(WorldDiplomacyPresentationHost.RetryAnalysis(recordId.Substring(retryPrefix.Length), generation));
                        returnToArchive();
                    } else CustomPolicyBehavior.OpenKingdomPolicyReReviewFromWorldArchive(recordId, returnToArchive);
                },
                onClose,
                key => CustomPolicyBehavior.RequestDeletePolicyHistoryRecord(key, returnToArchive));
        }
        catch (Exception ex)
        {
            Logger.Log("WorldDiplomacy", "[AF-WORLD-DIPLOMACY] archive open failed: " + ex.Message);
            return false;
        }
    }
    private static WorldEventInboxPopupData BuildRoyalAnnouncementArchiveData()
    {
        Dictionary<string, WorldEventCountryData> groups = new Dictionary<string, WorldEventCountryData>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> representedPolicies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (PolicyRecordPresentationData policy in CustomPolicyBehavior.GetPolicyRecordPresentationSnapshot())
        {
            representedPolicies.Add(policy.HistoryKey);
            WorldEventCountryData group = GetOrCreateArchiveGroup(groups, policy.KingdomId, policy.KingdomName);
            group.Records.Add(new WorldEventRecordData
            {
                EventId = "policy_history:" + policy.HistoryKey,
                HistoryKey = policy.HistoryKey,
                CanDelete = policy.CanDelete,
                KindLabel = "自定义政策",
                HeaderRightText = policy.StatusText,
                DateText = policy.DateText,
                TitleText = policy.TitleText,
                MetaText = policy.DateText + "  ·  " + policy.StatusText + "  ·  " + policy.KingdomName,
                BodySectionTitleText = "政策记录",
                BodyText = policy.BodyText,
                ImpactSectionTitleText = "政策影响效果",
                ImpactText = policy.ImpactText,
                HasImpact = !string.IsNullOrWhiteSpace(policy.ImpactText),
                IndexMetaText = policy.DateText + "  ·  " + policy.StatusText,
                PolicyRecordId = policy.RecordId,
                ShowReReview = policy.SourceKind == "player_kingdom",
                CanReReview = policy.CanReReview
            });
        }
        foreach (AnimusForgeWorldEventInboxEntry entry in AnimusForgeWorldEventBehavior.GetInboxSnapshotForExternal(160))
        {
            if (entry == null)
            {
                continue;
            }
            string historyKey = CustomPolicyBehavior.GetPolicyAnnouncementHistoryKey(entry);
            if (!string.IsNullOrWhiteSpace(historyKey) && (representedPolicies.Contains(historyKey)
                || CustomPolicyBehavior.Instance != null && CustomPolicyBehavior.IsPolicyAnnouncementDeleted(entry))) continue;
            string kingdomId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.KingdomId, "policy_unknown");
            WorldEventCountryData group = GetOrCreateArchiveGroup(groups, kingdomId, WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.KingdomName, "未知国家"));
            string date = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.GameDate, entry.Day > 0 ? "第" + entry.Day.ToString(CultureInfo.InvariantCulture) + "天" : "未知日期");
            bool showReReview = TryResolveWorldEventPolicyReReview(
                entry,
                out string policyRecordId,
                out bool canReReview,
                out string reReviewDisabledReason);
            group.Records.Add(new WorldEventRecordData
            {
                EventId = entry.EventId ?? "",
                KindLabel = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.KindLabel, "自定义政策"),
                HeaderRightText = entry.HeaderRightText ?? "",
                DateText = date,
                TitleText = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.Title, entry.KindLabel, "自定义政策"),
                MetaText = date + "  ·  " + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.KindLabel, "自定义政策") + "  ·  " + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.KingdomName, entry.KingdomId),
                PolicyNameText = "",
                BodyText = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.DetailText, entry.Summary, "（无详情）"),
                BodySectionTitleText = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.BodySectionTitleText, "公告详情"),
                ImpactSectionTitleText = entry.ImpactSectionTitleText ?? "",
                ImpactText = entry.ImpactText ?? "",
                IndexMetaText = date + "  ·  " + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.KindLabel, "自定义政策"),
                UnreadMarkerText = entry.IsRead ? "" : "新",
                PolicyRecordId = policyRecordId,
                ReReviewText = "重新评议政策",
                ReReviewDisabledReasonText = reReviewDisabledReason,
                IsUnread = !entry.IsRead,
                HasPolicyName = false,
                HasImpact = !string.IsNullOrWhiteSpace(entry.ImpactText),
                ShowReReview = showReReview,
                CanReReview = canReReview
            });
        }
        foreach (WorldDiplomacyArchiveRecord record in WorldDiplomacyPresentationHost.Archive())
        {
            WorldEventCountryData group = GetOrCreateArchiveGroup(groups, record.KingdomId, record.KingdomName);
            group.Records.Add(new WorldEventRecordData
            {
                EventId = record.EventId,
                KindLabel = record.KindLabel,
                HeaderRightText = record.HeaderRightText,
                DateText = record.DateText,
                TitleText = record.TitleText,
                IndexTitleText = record.IndexTitleText,
                MetaText = record.MetaText,
                PolicyNameText = record.PolicyNameText,
                BodyText = record.BodyText,
                BodySectionTitleText = record.BodySectionTitleText,
                ImpactSectionTitleText = record.ImpactSectionTitleText,
                ImpactText = record.ImpactText,
                IndexMetaText = record.IndexMetaText,
                UnreadMarkerText = record.UnreadMarkerText,
                IsUnread = record.IsUnread,
                HasPolicyName = record.HasPolicyName,
                HasImpact = record.HasImpact,
                PolicyRecordId = "diplomacy_analysis:" + record.EventId,
                ReReviewText = "重新解析",
                ShowReReview = record.CanRetryAnalysis,
                CanReReview = record.CanRetryAnalysis
            });
        }
        WorldEventInboxPopupData data = new WorldEventInboxPopupData
        {
            TitleText = "王国公告",
            SubtitleText = WorldDiplomacyPresentationHost.ArchiveSubtitle(),
            EmptyStateText = "目前还没有王国公告。",
            CloseText = "关闭",
            Countries = groups.Values
                .OrderBy(x => string.Equals(x.KingdomId, "diplomacy_archive", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(x => x.KingdomName, StringComparer.CurrentCulture)
                .ToList()
        };
        foreach (WorldEventCountryData group in data.Countries)
        {
            group.Records = group.Records
                .OrderByDescending(x => WorldDiplomacyTextRules.ParseDayForArchive(x.DateText))
                .ThenBy(x => x.TitleText, StringComparer.CurrentCulture)
                .ToList();
            group.UnreadCount = group.Records.Count(x => x.IsUnread);
        }
        data.SelectedCountryIndex = Math.Max(0, data.Countries.FindIndex(x => x.Records.Count > 0));
        return data;
    }
    private static bool TryResolveWorldEventPolicyReReview(
        AnimusForgeWorldEventInboxEntry entry,
        out string recordId,
        out bool canReReview,
        out string disabledReason)
    {
        recordId = string.Empty;
        canReReview = false;
        disabledReason = string.Empty;
        if (entry == null)
        {
            return false;
        }

        string candidateId = (entry.PolicyRecordId ?? string.Empty).Trim();
        if (entry.Version >= 2)
        {
            if (!entry.IsPlayerPolicy || candidateId.Length == 0)
            {
                return false;
            }
        }
        else if (candidateId.Length == 0)
        {
            const string legacyPrefix = "npc_ruler_policy:";
            string eventId = (entry.EventId ?? string.Empty).Trim();
            if (!eventId.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            candidateId = eventId.Substring(legacyPrefix.Length).Trim();
        }

        if (candidateId.Length == 0
            || !CustomPolicyBehavior.TryGetKingdomPolicyReReviewAvailabilityForExternal(
                candidateId,
                out canReReview,
                out disabledReason))
        {
            canReReview = false;
            disabledReason = string.Empty;
            return false;
        }

        recordId = candidateId;
        return true;
    }
    private static WorldEventCountryData GetOrCreateArchiveGroup(Dictionary<string, WorldEventCountryData> groups, string id, string name)
    {
        string key = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(id, "unknown");
        if (!groups.TryGetValue(key, out WorldEventCountryData group))
        {
            group = new WorldEventCountryData
            {
                KingdomId = key,
                KingdomName = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(name, key, "未知国家")
            };
            groups[key] = group;
        }
        return group;
    }
}
