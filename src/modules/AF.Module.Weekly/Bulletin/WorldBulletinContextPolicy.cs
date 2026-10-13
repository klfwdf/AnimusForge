using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Request-only projections. Never persist these or treat reviewed dialogue as executed game facts.
internal sealed class WorldBulletinHistoryCandidate
{
    internal string Id, Date, NpcName, Material, PublicityReason;
    internal int Day;
}

internal static class WorldBulletinContextPolicy
{
    internal const int MaxHistoryHeroes = 4;
    internal const int MaxHistoryCandidates = 6;
    internal const int MaxApprovedHistory = 3;

    internal static WorldBulletinHistoryCandidate CreateHistoryCandidate(string id, int day, string date,
        string npcName, string publicity, string material, string reason, int currentDay)
    {
        // leaked_public may mean a low-trust inference, not an observed public disclosure.
        if (!string.Equals((publicity ?? "").Trim(), "public", StringComparison.OrdinalIgnoreCase)
            || day < 0 || day > currentDay || string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(material) || string.IsNullOrWhiteSpace(reason)
            || material.Length > 2000 || reason.Length > 1000) return null;
        return new WorldBulletinHistoryCandidate
        {
            Id = id, Day = day, Date = date ?? "", NpcName = npcName ?? "",
            Material = material.Trim(), PublicityReason = reason.Trim()
        };
    }

    internal static string BuildReviewSystemPrompt()
        => WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_PrivacyReview");

    internal static string BuildReviewUserPrompt(string eventFacts, IReadOnlyList<WorldBulletinHistoryCandidate> candidates)
        => JsonConvert.SerializeObject(new
        {
            event_facts = eventFacts ?? "",
            candidates = (candidates ?? Array.Empty<WorldBulletinHistoryCandidate>()).Take(MaxHistoryCandidates)
                .Select(c => new { id = c.Id, date = c.Date, npc = c.NpcName, public_excerpt = c.Material, prior_publicity_reason = c.PublicityReason })
        });

    internal static string BuildApprovedHistory(string response, IReadOnlyList<WorldBulletinHistoryCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(response) || response.Length > 4096 || candidates == null) return "";
        try
        {
            JObject root = JObject.Parse(response, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (root.Count != 1 || !(root["public_ids"] is JArray ids) || ids.Count > MaxApprovedHistory) return "";
            var allowed = candidates.Take(MaxHistoryCandidates).ToDictionary(c => c.Id, StringComparer.Ordinal);
            var selected = new HashSet<string>(StringComparer.Ordinal);
            var text = new StringBuilder();
            foreach (var token in ids)
            {
                if (token.Type != JTokenType.String || !allowed.TryGetValue((string)token, out var candidate)
                    || !selected.Add(candidate.Id)) return "";
                text.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_PublicHistoryEntry", "",
                    ("DATE", candidate.Date), ("NPC", candidate.NpcName), ("TEXT", candidate.Material)));
            }
            return text.ToString().TrimEnd();
        }
        catch (JsonException) { return ""; }
        catch (ArgumentException) { return ""; }
    }

    internal static string BuildWriterContext(string lore, string publicHistory)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(lore))
            text.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_LoreContextHeader")).AppendLine(lore);
        text.AppendLine(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_PublicHistoryHeader"));
        text.AppendLine(string.IsNullOrWhiteSpace(publicHistory)
            ? WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_NoPublicHistory") : publicHistory);
        return text.ToString().TrimEnd();
    }
}
