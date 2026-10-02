using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyProactiveDiscussionSource
{
    bool TryCaptureSpeaker(string heroId, out WorldDiplomacyProactiveSpeakerCandidate candidate, out string playerKingdomId);
    bool TryCaptureDocuments(string heroId, string playerKingdomId,
        out IReadOnlyList<WorldDiplomacyDocument> documents, out HashSet<string> knownIds, out int currentDay);
    string GetPlayerKingdomName(string playerKingdomId);
    string FormatDate(int day);
}

internal static class WorldDiplomacyProactiveDiscussionApplication
{
    internal static bool TryBuild(IWorldDiplomacyProactiveDiscussionSource source, string heroId,
        out string stableKey, out string fact, out float urgency)
    {
        try
        {
            return TryBuildCore(source, heroId, out stableKey, out fact, out urgency);
        }
        catch
        {
            stableKey = "";
            fact = "";
            urgency = 0f;
            return false;
        }
    }

    private static bool TryBuildCore(IWorldDiplomacyProactiveDiscussionSource source, string heroId,
        out string stableKey, out string fact, out float urgency)
    {
        stableKey = "";
        fact = "";
        urgency = 0f;
        if (!source.TryCaptureSpeaker(heroId, out WorldDiplomacyProactiveSpeakerCandidate speaker, out string playerKingdomId)
            || !WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(speaker))
        {
            return false;
        }
        if (!source.TryCaptureDocuments(heroId, playerKingdomId,
            out IReadOnlyList<WorldDiplomacyDocument> documents, out HashSet<string> knownIds, out int currentDay))
        {
            return false;
        }
        int earliestDay = Math.Max(0, currentDay - 7);
        WorldDiplomacyDocument selected = null;
        WorldDiplomacyProactiveDocumentCandidate selectedCandidate = default;
        foreach (WorldDiplomacyDocument document in documents ?? Enumerable.Empty<WorldDiplomacyDocument>())
        {
            if (document == null)
            {
                continue;
            }

            WorldDiplomacyProactiveDocumentCandidate baseCandidate = new WorldDiplomacyProactiveDocumentCandidate(
                isReadyForPublication: document.IsReadyForPublication,
                isCompressed: document.IsCompressed,
                day: document.Day,
                createdUtcTicks: document.CreatedUtcTicks,
                isKnown: knownIds.Contains(document.DocumentId ?? ""),
                isAuthoredByPlayerKingdom: false,
                isTargetingPlayerKingdom: false,
                isAddressedToPlayerKingdom: false,
                mentionsPlayerKingdom: false,
                isMajor: false,
                hasMechanicalResult: false);
            if (!WorldDiplomacyProactiveDocumentSelectionRules.MeetsBaseEligibility(baseCandidate, earliestDay))
            {
                continue;
            }

            bool isAuthoredByPlayerKingdom = string.Equals(
                document.AuthorKingdomId, playerKingdomId, StringComparison.OrdinalIgnoreCase);
            bool isTargetingPlayerKingdom = string.Equals(
                document.TargetKingdomId, playerKingdomId, StringComparison.OrdinalIgnoreCase);
            bool isAddressedToPlayerKingdom = document.AddressedKingdomIds?.Contains(
                playerKingdomId, StringComparer.OrdinalIgnoreCase) == true;
            bool mentionsPlayerKingdom = document.MentionedKingdomIds?.Contains(
                playerKingdomId, StringComparer.OrdinalIgnoreCase) == true;
            bool isMajor = !isAuthoredByPlayerKingdom
                && !isTargetingPlayerKingdom
                && !isAddressedToPlayerKingdom
                && !mentionsPlayerKingdom
                && WorldDiplomacyDocumentFactRules.IsMajorDiplomaticDocument(document);
            WorldDiplomacyProactiveDocumentCandidate candidate = new WorldDiplomacyProactiveDocumentCandidate(
                isReadyForPublication: baseCandidate.IsReadyForPublication,
                isCompressed: baseCandidate.IsCompressed,
                day: baseCandidate.Day,
                createdUtcTicks: baseCandidate.CreatedUtcTicks,
                isKnown: baseCandidate.IsKnown,
                isAuthoredByPlayerKingdom: isAuthoredByPlayerKingdom,
                isTargetingPlayerKingdom: isTargetingPlayerKingdom,
                isAddressedToPlayerKingdom: isAddressedToPlayerKingdom,
                mentionsPlayerKingdom: mentionsPlayerKingdom,
                isMajor: isMajor,
                hasMechanicalResult: !string.IsNullOrWhiteSpace(document.MechanicalResult));
            if (!WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(candidate, earliestDay))
            {
                continue;
            }
            if (selected != null
                && !WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(candidate, selectedCandidate))
            {
                continue;
            }

            selected = document;
            selectedCandidate = candidate;
        }
        if (selected == null)
        {
            return false;
        }

        stableKey = WorldDiplomacyProactiveDocumentSelectionRules.BuildDiscussionStableKey(
            selected.RoundId,
            selected.DocumentId);
        bool selectedIsMajorForUrgency = !selectedCandidate.HasMechanicalResult
            && !selectedCandidate.IsTargetingPlayerKingdom
            && WorldDiplomacyDocumentFactRules.IsMajorDiplomaticDocument(selected);
        urgency = WorldDiplomacyProactiveDocumentSelectionRules.CalculateUrgency(
            selectedCandidate.HasMechanicalResult,
            selectedCandidate.IsTargetingPlayerKingdom,
            selectedIsMajorForUrgency);
        WorldDiplomacyDocument relatedFirst = null;
        WorldDiplomacyDocument relatedSecond = null;
        WorldDiplomacyDocument relatedThird = null;
        WorldDiplomacyProactiveRelatedDocumentCandidate relatedFirstCandidate = default;
        WorldDiplomacyProactiveRelatedDocumentCandidate relatedSecondCandidate = default;
        WorldDiplomacyProactiveRelatedDocumentCandidate relatedThirdCandidate = default;
        int relatedCount = 0;
        foreach (WorldDiplomacyDocument document in documents ?? Enumerable.Empty<WorldDiplomacyDocument>())
        {
            if (document == null)
            {
                continue;
            }
            WorldDiplomacyProactiveRelatedDocumentCandidate candidate = new WorldDiplomacyProactiveRelatedDocumentCandidate(
                isCompressed: document.IsCompressed,
                isKnown: knownIds.Contains(document.DocumentId ?? ""),
                isSameRound: WorldDiplomacyRoundLifecycleRules.IsRecordInRound(document.RoundId, selected.RoundId),
                day: document.Day,
                createdUtcTicks: document.CreatedUtcTicks);
            if (!WorldDiplomacyProactiveDocumentSelectionRules.IsEligibleRelatedDocument(candidate))
            {
                continue;
            }
            int insertionIndex = WorldDiplomacyProactiveDocumentSelectionRules.GetRelatedDocumentInsertionIndex(
                candidate,
                relatedCount,
                relatedFirstCandidate,
                relatedSecondCandidate,
                relatedThirdCandidate);
            if (insertionIndex == 0)
            {
                relatedThird = relatedSecond;
                relatedThirdCandidate = relatedSecondCandidate;
                relatedSecond = relatedFirst;
                relatedSecondCandidate = relatedFirstCandidate;
                relatedFirst = document;
                relatedFirstCandidate = candidate;
            }
            else if (insertionIndex == 1)
            {
                relatedThird = relatedSecond;
                relatedThirdCandidate = relatedSecondCandidate;
                relatedSecond = document;
                relatedSecondCandidate = candidate;
            }
            else if (insertionIndex == 2)
            {
                relatedThird = document;
                relatedThirdCandidate = candidate;
            }
            else
            {
                continue;
            }
            if (relatedCount < 3)
            {
                relatedCount++;
            }
        }
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("【本国领主主动讨论的外交局势】");
        sb.AppendLine("你与玩家同属" + source.GetPlayerKingdomName(playerKingdomId) + "。你是来交换判断、讨论本国应如何看待和应对局势，不是代表王国擅自签订协议。");
        WorldDiplomacyTextRules.AppendProactiveDiscussionDocument(sb, relatedFirst, source.FormatDate);
        WorldDiplomacyTextRules.AppendProactiveDiscussionDocument(sb, relatedSecond, source.FormatDate);
        WorldDiplomacyTextRules.AppendProactiveDiscussionDocument(sb, relatedThird, source.FormatDate);
        fact = sb.ToString().TrimEnd();
        return true;
    }
}
