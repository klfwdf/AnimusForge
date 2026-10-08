using System;
using System.Collections.Generic;

namespace AnimusForge;

internal static class NativeMeetingElapsedHistoryProjection
{
    internal const int DraftBudget = 32, LineBudget = 512, BlockBudget = 32;
    private sealed class Candidate { internal DailyMemoryLine Line; internal int Order; }
    // Already loaded targeted lists; bounded request-boundary reads, no Tick or JSON work.
    internal static NativeMeetingElapsedSnapshot Capture(IReadOnlyList<DailyMemoryDraft> drafts,
        IReadOnlyList<CompressedMemoryBlock> blocks, double nowHours, Func<string, bool> isPlayer)
    {
        var snapshot = new NativeMeetingElapsedSnapshot { NowHours = nowHours };
        var candidates = new List<Candidate>();
        var users = new HashSet<string>(StringComparer.Ordinal);
        int inspected = 0, bestOrder = int.MaxValue;
        bool truncated = drafts?.Count > DraftBudget || blocks?.Count > BlockBudget;
        if (drafts != null)
        for (int d = drafts.Count - 1, visited = 0; d >= 0 && visited < DraftBudget; d--, visited++)
        {
            var draft = drafts[d]; if (draft?.Lines == null) continue;
            Candidate legacyReply = null;
            for (int i = draft.Lines.Count - 1; i >= 0; i--)
            {
                if (inspected >= LineBudget) { truncated = true; break; }
                inspected++;
                var line = draft.Lines[i];
                if (line == null || line.IsAfef || !line.IsLlmDialogue || string.IsNullOrWhiteSpace(line.Text)) continue;
                if (!string.IsNullOrEmpty(line.MemoryCommitId))
                {
                    if (line.MemoryCommitPart == "user") users.Add(line.MemoryCommitId);
                    else if (line.MemoryCommitPart == "assistant") candidates.Add(new Candidate { Line = line, Order = inspected });
                    continue;
                }
                // Legacy observer/other-NPC lines cannot establish a direct player-target exchange.
                if (!isPlayer(line.Speaker) && string.Equals((line.Speaker ?? "").Trim(), (draft.HeroName ?? "").Trim(), StringComparison.Ordinal))
                { if (legacyReply == null) legacyReply = new Candidate { Line = line, Order = inspected }; }
                else if (isPlayer(line.Speaker) && legacyReply != null)
                {
                    Consider(snapshot, legacyReply, legacyReply.Line.GameHour > 0 && legacyReply.Line.GameHour <= 23, ref bestOrder);
                    legacyReply = null;
                }
            }
        }
        foreach (var candidate in candidates)
        {
            var line = candidate.Line;
            if (users.Contains(line.MemoryCommitId))
                Consider(snapshot, candidate, !string.IsNullOrEmpty(line.MemoryCommitHash) && line.MemoryCommitOriginGameDay >= 0 && line.GameHour >= 0 && line.GameHour <= 23, ref bestOrder);
        }
        // Old sealed summaries may contain only AFEF/imported notes. Date is a memory-record boundary,
        // never proof of a completed player-NPC dialogue or its last exact hour.
        if (blocks != null)
        for (int i = blocks.Count - 1, visited = 0; i >= 0 && visited < BlockBudget; i--, visited++)
        {
            var block = blocks[i];
            if (block == null || block.GameDayIndex < 0 || string.IsNullOrWhiteSpace(block.Summary)) continue;
            if (block.GameDayIndex >= snapshot.Day)
            { snapshot.HasMemoryRecord = true; snapshot.Day = block.GameDayIndex; snapshot.Hour = -1; }
        }
        if (truncated)
        {
            // A bounded tail is not a certificate of the last exchange. Never substitute an older pair.
            snapshot.Day = -1; snapshot.Hour = -1;
            snapshot.TimeUnknown = true;
        }
        return snapshot;
    }
    private static void Consider(NativeMeetingElapsedSnapshot snapshot, Candidate candidate, bool hourKnown, ref int bestOrder)
    {
        var line = candidate.Line;
        int day = !string.IsNullOrEmpty(line.MemoryCommitId) && line.MemoryCommitOriginGameDay >= 0 ? line.MemoryCommitOriginGameDay : line.GameDayIndex;
        snapshot.HasPriorDialogue = true;
        if (day < 0) { snapshot.TimeUnknown = true; return; }
        if (day > snapshot.Day || day == snapshot.Day && candidate.Order < bestOrder)
        { snapshot.Day = day; snapshot.Hour = hourKnown ? line.GameHour : -1; bestOrder = candidate.Order; }
    }
}
