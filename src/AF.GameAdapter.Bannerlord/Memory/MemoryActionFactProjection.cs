using System;

namespace AnimusForge;

internal static class MemoryActionFactProjection
{
    // Returns a fully rendered actor line, or null for unknown/free-form records.
    internal static string Project(string kind, string raw, string actor)
    {
        if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(actor)) return null;
        if(kind == "hero_killed" || kind == "hero_executed_victim")
            return CampaignSystemFactProjection.ProjectNpcKill(kind,raw,actor);
        if (kind == "civil_war")
        {
            string projected = CampaignSystemFactProjection.Project(kind, raw);
            return projected == raw ? null : actor + ": " + projected;
        }
        string key = null, start = null, end = null;
        switch (kind)
        {
            case "companion_join":
                if (raw != AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_b6cd7a5cb23e")) return null;
                return Render("ActionFactCompanionJoin", actor);
            case "companion_leave": key = "ActionFactCompanionLeave"; start = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_6ccb95384c33"); end = "。"; break;
            case "governor_removed": key = "ActionFactGovernorRemoved"; start = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_a44422197df4"); end = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_1986b37416dd"); break;
            case "governor_appointed": key = "ActionFactGovernorAppointed"; start = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_6ba0e3f9c973"); end = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_1986b37416dd"); break;
            case "duel_result":
                if (raw.StartsWith(AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_4a3c35db7d53"), StringComparison.Ordinal))
                { key = "ActionFactDuelWin"; start = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_4a3c35db7d53"); }
                else { key = "ActionFactDuelLoss"; start = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_907bee73114f"); }
                end = "。"; break;
            case "quest_success": case "quest_timeout": case "quest_betrayal": case "quest_fail": case "quest_cancel": case "quest_result":
                start = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_210f188d0588"); end = "。";
                if (!Extract(raw, start, end, out string quest)) return null;
                string boundary = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_bf48b64fbd33");
                int at = quest.IndexOf(boundary, StringComparison.Ordinal);
                if (at <= 0 || quest.IndexOf(boundary, at + boundary.Length, StringComparison.Ordinal) >= 0) return null;
                string title = quest.Substring(0, at), result = quest.Substring(at + boundary.Length);
                if (result.Length == 0) return null;
                return Render("ActionFactQuest", actor, title, ProjectQuestResult(kind, result));
            case "vote_deal_fulfilled":
                start = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_d4afcc3b9a36"); end = "。";
                if (!Extract(raw, start, end, out string vote)) return null;
                string voteBoundary = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_d5c10fdb63cc");
                int split = vote.IndexOf(voteBoundary, StringComparison.Ordinal);
                if (split <= 0 || vote.IndexOf(voteBoundary, split + voteBoundary.Length, StringComparison.Ordinal) >= 0) return null;
                string option = vote.Substring(split + voteBoundary.Length);
                if (option.Length == 0) return Render("ActionFactVote", actor, vote.Substring(0, split));
                if (!Extract(option, AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_78434ad59afa"), "”", out string optionName)) return null;
                return Render("ActionFactVoteOption", actor, vote.Substring(0, split), optionName);
            case "prison_break_rescue":
                if (Extract(raw, AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_57b64a7ff0b1"), AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_3f8b1d37a64e"), out string rescuer))
                    return Render("ActionFactPrisonRescue", actor, rescuer);
                if (!Extract(raw, AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_1ee3b47f70e9"), AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_3f8b1d37a64e"), out string rescue)) return null;
                string prisonBoundary = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_0d814c82e469");
                int prison = rescue.IndexOf(prisonBoundary, StringComparison.Ordinal);
                if (prison <= 0 || rescue.IndexOf(prisonBoundary, prison + prisonBoundary.Length, StringComparison.Ordinal) >= 0) return null;
                return Render("ActionFactPrisonRescueLocation", actor, rescue.Substring(0, prison), rescue.Substring(prison + prisonBoundary.Length));
        }
        if (key == null || !Extract(raw, start, end, out string value)) return null;
        if (kind == "companion_leave" && value == AdapterRecordTexts.Raw("AF_REC_Character_0006"))
            value = AdapterRecordTexts.Prompt("ActionFactCompanionFired");
        return Render(key, actor, value);
    }

    private static string ProjectQuestResult(string kind, string value)
    {
        string id = kind == "quest_success" ? "0001" : kind == "quest_fail" ? "0002"
            : kind == "quest_betrayal" ? "0003" : kind == "quest_timeout" ? "0004" : kind == "quest_cancel" ? "0005" : null;
        return id != null && value == AdapterRecordTexts.Raw("AF_REC_Character_" + id)
            ? AdapterRecordTexts.Prompt("ActionFactQuestResult_" + id) : value;
    }

    private static bool Extract(string raw, string start, string end, out string value)
    {
        value = null;
        if (!raw.StartsWith(start, StringComparison.Ordinal) || !raw.EndsWith(end, StringComparison.Ordinal)
            || raw.Length <= start.Length + end.Length) return false;
        value = raw.Substring(start.Length, raw.Length - start.Length - end.Length);
        return value.IndexOf(start, StringComparison.Ordinal) < 0 && value.IndexOf(end, StringComparison.Ordinal) < 0;
    }

    private static string Render(string key, string actor, string first = null, string second = null)
        => AdapterRecordTexts.PromptFormat(key, ("ACTOR", actor), ("FIRST", first ?? ""), ("SECOND", second ?? ""));
}
