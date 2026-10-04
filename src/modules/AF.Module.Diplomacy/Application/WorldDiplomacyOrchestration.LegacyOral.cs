using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    internal string SubmitLegacyOralCommitment(string rulerId, string actorId, string action, string payload,
        DialogueInteractionOrigin origin)
    {
        string[] parts = (payload ?? "").Split(':');
        if (parts.Length < 2) return "外交约定未提交：缺少当事国。";
        if (!DialogueRulerIsCurrent(actorId, rulerId)) return "";
        string target = parts[0] == actorId ? parts[1] : parts[1] == actorId ? parts[0] : "";
        if (string.IsNullOrWhiteSpace(target)) return "";
        string request = "action=" + action + ";move=NewMatter;target=" + target;
        if (action == "Peace")
        {
            int tribute = 0, days = 0;
            if (parts.Length > 2 && !int.TryParse(parts[2], out tribute)) return "外交约定未提交：请先明确贡金金额。";
            if (parts.Length > 3 && int.TryParse(parts[3], out int duration)) days = duration;
            request += ";payer=" + parts[0] + ";receiver=" + parts[1] + ";tribute=" + tribute + ";days=" + days;
        }
        else if (action == "Trade" && parts.Length > 2)
        {
            if (!int.TryParse(parts[2], out int days)) return "外交约定未提交：请先明确贸易期限。";
            request += ";days=" + days;
        }
        return SubmitOralDiplomaticCommitment(rulerId, actorId, request, origin);
    }
}
