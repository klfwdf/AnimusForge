using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace AnimusForge;

internal readonly struct DiplomacyPromptSnapshot
{
    internal DiplomacyPromptSnapshot(bool hasPlayerClan, bool hasPlayerHero, bool playerClanHasKingdom, bool playerMercenary, bool playerLeadsClan, bool hasPlayerFief, bool npcMercenary, string playerName, string playerClanName, string playerFiefs, string peaceKingdomName, int trustLevel)
    { HasPlayerClan = hasPlayerClan; HasPlayerHero = hasPlayerHero; PlayerClanHasKingdom = playerClanHasKingdom; PlayerMercenary = playerMercenary; PlayerLeadsClan = playerLeadsClan; HasPlayerFief = hasPlayerFief; NpcMercenary = npcMercenary; PlayerName = playerName; PlayerClanName = playerClanName; PlayerFiefs = playerFiefs; PeaceKingdomName = peaceKingdomName; TrustLevel = trustLevel; }
    internal bool HasPlayerClan { get; }
    internal bool HasPlayerHero { get; }
    internal bool PlayerClanHasKingdom { get; }
    internal bool PlayerMercenary { get; }
    internal bool PlayerLeadsClan { get; }
    internal bool HasPlayerFief { get; }
    internal bool NpcMercenary { get; }
    internal string PlayerName { get; }
    internal string PlayerClanName { get; }
    internal string PlayerFiefs { get; }
    internal string PeaceKingdomName { get; }
    internal int TrustLevel { get; }
}
internal readonly struct DiplomacyPromptWar
{
    internal DiplomacyPromptWar(string id, string name, int days, float ownProgress, float enemyProgress, int ownKills, int enemyKills, int towns, int castles, float ownStrength, float enemyStrength, float enemyProsperity, bool listed)
    { Id = id; Name = name; Days = days; OwnProgress = ownProgress; EnemyProgress = enemyProgress; OwnKills = ownKills; EnemyKills = enemyKills; Towns = towns; Castles = castles; OwnStrength = ownStrength; EnemyStrength = enemyStrength; EnemyProsperity = enemyProsperity; Listed = listed; }
    internal string Id { get; }
    internal string Name { get; }
    internal int Days { get; }
    internal float OwnProgress { get; }
    internal float EnemyProgress { get; }
    internal int OwnKills { get; }
    internal int EnemyKills { get; }
    internal int Towns { get; }
    internal int Castles { get; }
    internal float OwnStrength { get; }
    internal float EnemyStrength { get; }
    internal float EnemyProsperity { get; }
    internal bool Listed { get; }
}

internal interface IDiplomacyPromptSource
{
    DiplomacyConversationEligibilitySnapshot CaptureEligibility();
    DiplomacyPromptSnapshot Capture();
    IReadOnlyList<DiplomacyPromptWar> CaptureWars();
    bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot);
    string Template(string key, Dictionary<string, string> tokens);
    string AnnexationInstruction();
    void Log(string message);
}
internal static class DiplomacyPromptApplication
{
    internal static string Build(IDiplomacyPromptSource source, string extras)
    {
        try
        {
            bool topic = PromptRuleBlockText.Has(extras, "diplomacy");
            string independent = IndependentInstruction(source);
            if (!topic && string.IsNullOrWhiteSpace(independent)) return "";
            StringBuilder result = new StringBuilder();
            if (topic && source is IDiplomacyOralPromptSource oral)
                Append(result, oral.OralArrangementContext());
            var eligibility = source.CaptureEligibility();
            if (topic && DiplomacyConversationEligibilityApplication.CanInject(eligibility))
            {
                if (!(source is IDiplomacyOralPromptSource mode) || mode.UseFormalCommitments)
                    Append(result, AnimusForge.DiplomacyDialogue.DialoguePeaceClarificationRules.MainReplyInstruction);
                var snapshot = source.Capture();
                Append(result, BuildInstruction(source, eligibility, snapshot));
                Append(result, RuntimeInstruction(source, eligibility, snapshot));
            }
            Append(result, independent);
            return result.ToString();
        }
        catch (Exception ex) { source.Log("[PatchContext Error] " + ex.Message); return ""; }
    }
    private static void Append(StringBuilder result, string text)
    { if (!string.IsNullOrWhiteSpace(text)) result.Append("\n").Append(text); }
    internal static bool IndependentSettlementClan(DiplomacyPromptSnapshot s) =>
        s.HasPlayerClan && s.HasPlayerHero && !s.PlayerClanHasKingdom && !s.PlayerMercenary && s.PlayerLeadsClan && s.HasPlayerFief;
    private static string IndependentInstruction(IDiplomacyPromptSource source)
    {
        try
        {
            if (!source.TryCaptureIndependentPeace(out var value)) return "";
            return source.Template("independent_clan_peace", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["playerClanName"] = value.PlayerClanName, ["playerSettlementCount"] = value.SettlementCount.ToString(),
                ["targetKingdomName"] = value.TargetName });
        }
        catch (Exception ex) { source.Log("[IndependentClanPeace] Build instruction failed: " + ex.Message); return ""; }
    }
    private static string BuildInstruction(IDiplomacyPromptSource source, DiplomacyConversationEligibilitySnapshot e, DiplomacyPromptSnapshot s)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(); sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_ConversationRulesHeader", "[Royal diplomacy rules]"));
            sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_ConversationCalendar", "Important: in this game, 84 days = one year and 21 days = one quarter. There are no months or weeks. Discuss time in quarters or years."));
            if (e.PlayerIsRuler) sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_ConversationBothRulers", "You and the player are both monarchs and may discuss declarations of war, peace, alliances, trade, and other diplomatic matters."));
            else if (IndependentSettlementClan(s))
            {
                sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_ConversationIndependentClan", "The player has not founded a kingdom, but their independent clan holds towns/castles and may discuss political recognition, truces, non-aggression, tribute, or pre-foundation conditions with monarchs. Formal kingdom alliances, trade agreements, and kingdom peace treaties require a founded kingdom."));
                sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_ConversationIdentityPrefix", "[Player political identity] Independent landholding clan: ") + s.PlayerClanName + DiplomacyPromptTexts.Get("AF_DIP_ConversationFiefs", "; Bases: ") + s.PlayerFiefs + DiplomacyPromptTexts.Get("AF_DIP_ConversationFullStop", "."));
            }
            else sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_ConversationNotRuler", "The player is not a monarch and cannot conclude formal kingdom diplomacy; political negotiations, audiences, recognition, truces, or pre-foundation conditions may be discussed."));
            var wars = source.CaptureWars();
            foreach (var war in wars) AppendWar(sb, war, wars);
            if (!string.IsNullOrEmpty(s.PeaceKingdomName))
            { sb.AppendLine(); sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_PeaceStatus", "[Peace status with {0}] Both sides are currently at peace."), s.PeaceKingdomName)); }
            string annexation = source is IDiplomacyOralPromptSource mode && !mode.UseFormalCommitments
                ? "" : source.AnnexationInstruction();
            if (!string.IsNullOrWhiteSpace(annexation)) { sb.AppendLine(); sb.AppendLine(annexation); }
            return sb.ToString().TrimEnd();
        }
        catch (Exception ex) { source.Log("[BuildInstruction Error] " + ex.Message); return ""; }
    }
    internal static string RuntimeInstruction(IDiplomacyPromptSource source, DiplomacyConversationEligibilitySnapshot e, DiplomacyPromptSnapshot s)
    {
        try
        {
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["playerName"] = string.IsNullOrWhiteSpace(s.PlayerName) ? DiplomacyPromptTexts.Get("AF_DIP_ConversationPlayer", "player") : s.PlayerName };
            string state = !e.NpcKingdomExists ? "no_kingdom" : s.NpcMercenary ? "mercenary" : !e.NpcIsRuler ? "not_king"
                : !e.PlayerIsRuler ? IndependentSettlementClan(s) ? "player_independent_settlement_clan" : "player_not_king" : "";
            if (state.Length > 0)
            {
                string template = source.Template(state, tokens);
                if (!string.IsNullOrWhiteSpace(template)) return template;
                if (state != "player_independent_settlement_clan") return "";
            }
            if (e.NpcKingdomExists && !s.NpcMercenary && e.NpcIsRuler)
                return source.Template("level_" + s.TrustLevel, tokens) ?? "";
            return "";
        }
        catch (Exception ex) { source.Log("[RuntimeInstruction Error] " + ex.Message); return ""; }
    }
    private static void AppendWar(StringBuilder sb, DiplomacyPromptWar war, IReadOnlyList<DiplomacyPromptWar> wars)
    {
        sb.AppendLine(); sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_WarHeader", "[War situation against {0}] (Only for judging the negotiating position; do not recite these details one by one in the reply.)"), war.Name));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_WarDays", "- War duration: {0} days"), war.Days));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_OwnWarProgress", "- Your war progress score: {0:F0} / 750"), war.OwnProgress));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_EnemyWarProgress", "- Enemy war progress score: {0:F0} / 750"), war.EnemyProgress));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_WarKills", "  Your kills: {0}; enemy kills: {1}"), war.OwnKills, war.EnemyKills));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_WarConquests", "  Your conquests: {0} towns, {1} castles"), war.Towns, war.Castles));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_WarStrength", "- Your total strength: {0:F0}; enemy total strength: {1:F0}"), war.OwnStrength, war.EnemyStrength));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_WarTribute", "- Enemy prosperity: {0:F0}; reference tribute range 0 ~ {1} denars/day (a negotiating reference, not a system limit; both parties may agree to any nonnegative integer amount)"), war.EnemyProsperity, (int)(war.EnemyProsperity*0.15f*0.35f)));
        int count = 0; float strength = 0;
        foreach (var other in wars) if (other.Listed && other.Id != war.Id) { count++; strength += other.EnemyStrength; }
        if (count > 0) sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_WarMultipleFronts", "- Multiple fronts: at war with {0} other enemies (combined strength {1:F0})"), count, strength));
        float diff = war.OwnProgress - war.EnemyProgress;
        sb.AppendLine(diff > 100 ? DiplomacyPromptTexts.Get("AF_DIP_ConversationAdvantage", "- [Negotiating position] Your side has a clear advantage") : diff < -100 ? DiplomacyPromptTexts.Get("AF_DIP_ConversationDisadvantage", "- [Negotiating position] Your side has a clear disadvantage") : DiplomacyPromptTexts.Get("AF_DIP_ConversationEven", "- [Negotiating position] Both sides are broadly even"));
    }
}

// Optional request-local read port; arrangement state remains with the world owner.
internal interface IDiplomacyOralPromptSource
{
    bool UseFormalCommitments { get; }
    string OralArrangementContext();
}
