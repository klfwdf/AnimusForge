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
            bool topic = (extras ?? "").IndexOf("【附加规则:diplomacy】", StringComparison.OrdinalIgnoreCase) >= 0;
            string independent = IndependentInstruction(source);
            if (!topic && string.IsNullOrWhiteSpace(independent)) return "";
            StringBuilder result = new StringBuilder();
            var eligibility = source.CaptureEligibility();
            if (topic && DiplomacyConversationEligibilityApplication.CanInject(eligibility))
            {
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
            sb.AppendLine(); sb.AppendLine("【国王外交规则】");
            sb.AppendLine("重要：游戏内84天=一年，21天=一季度，没有月和周的概念。谈论时间请用季度或年。");
            if (e.PlayerIsRuler) sb.AppendLine("你和玩家都是国王，可以讨论宣战、议和、结盟、贸易等外交事务。");
            else if (IndependentSettlementClan(s))
            {
                sb.AppendLine("玩家尚未建国，但其独立家族占有城镇/城堡，可与国王谈政治承认、停战、互不侵犯、贡金或建国前条件。正式王国同盟、贸易协议和王国和约需建国后。");
                sb.AppendLine("【玩家政治身份】独立有城家族：" + s.PlayerClanName + "；据点：" + s.PlayerFiefs + "。");
            }
            else sb.AppendLine("玩家不是国王，不能签正式王国外交；可谈政治交涉、觐见、承认、停战或建国前条件。");
            var wars = source.CaptureWars();
            foreach (var war in wars) AppendWar(sb, war, wars);
            if (!string.IsNullOrEmpty(s.PeaceKingdomName))
            { sb.AppendLine(); sb.AppendLine($"【与{s.PeaceKingdomName}的和平状态】双方目前处于和平状态。"); }
            string annexation = source.AnnexationInstruction();
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
                ["playerName"] = string.IsNullOrWhiteSpace(s.PlayerName) ? "玩家" : s.PlayerName };
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
        sb.AppendLine(); sb.AppendLine($"【与{war.Name}的战争局势】（仅供判断谈判立场，勿在正文逐条朗读）");
        sb.AppendLine($"- 战争已持续：{war.Days} 天");
        sb.AppendLine($"- 你方战争进展分：{war.OwnProgress:F0} / 750");
        sb.AppendLine($"- 敌方战争进展分：{war.EnemyProgress:F0} / 750");
        sb.AppendLine($"  你方击杀：{war.OwnKills}，敌方击杀：{war.EnemyKills}");
        sb.AppendLine($"  你方占城：{war.Towns}城{war.Castles}堡");
        sb.AppendLine($"- 你方总战力：{war.OwnStrength:F0}，敌方总战力：{war.EnemyStrength:F0}");
        sb.AppendLine($"- 敌方繁荣度：{war.EnemyProsperity:F0}，参考贡金范围 0 ~ {(int)(war.EnemyProsperity*0.15f*0.35f)} 第纳尔/天（仅供谈判参考，不是系统上限；双方可商定任意非负整数金额）");
        int count = 0; float strength = 0;
        foreach (var other in wars) if (other.Listed && other.Id != war.Id) { count++; strength += other.EnemyStrength; }
        if (count > 0) sb.AppendLine($"- 多线作战：同时与 {count} 个敌人交战（总战力 {strength:F0}）");
        float diff = war.OwnProgress - war.EnemyProgress;
        sb.AppendLine(diff > 100 ? "- 【谈判立场】你方明显占优" : diff < -100 ? "- 【谈判立场】你方明显劣势" : "- 【谈判立场】双方大体持平");
    }
}
