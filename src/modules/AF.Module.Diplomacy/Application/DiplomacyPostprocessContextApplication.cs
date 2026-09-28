using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

internal readonly struct DiplomacyIndependentPeaceContextSnapshot
{
    internal DiplomacyIndependentPeaceContextSnapshot(string playerClanName, int settlementCount, string targetName)
    {
        PlayerClanName = playerClanName;
        SettlementCount = settlementCount;
        TargetName = targetName;
    }
    internal string PlayerClanName { get; }
    internal int SettlementCount { get; }
    internal string TargetName { get; }
}

internal readonly struct DiplomacyKingdomSummary
{
    internal DiplomacyKingdomSummary(string id, string name, bool eliminated)
    { Id = id; Name = name; Eliminated = eliminated; }
    internal string Id { get; }
    internal string Name { get; }
    internal bool Eliminated { get; }
}

internal readonly struct DiplomacyPostprocessKingdomSnapshot
{
    internal DiplomacyPostprocessKingdomSnapshot(bool npcKingdomExists, string npcId, string npcName,
        bool playerKingdomExists, bool playerKingdomEliminated, string playerId, string playerName,
        bool playerIsRuler, bool kingdomsDiffer, IReadOnlyList<DiplomacyKingdomSummary> kingdoms)
    {
        NpcKingdomExists = npcKingdomExists;
        NpcId = npcId;
        NpcName = npcName;
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomEliminated = playerKingdomEliminated;
        PlayerId = playerId;
        PlayerName = playerName;
        PlayerIsRuler = playerIsRuler;
        KingdomsDiffer = kingdomsDiffer;
        Kingdoms = kingdoms;
    }
    internal bool NpcKingdomExists { get; }
    internal string NpcId { get; }
    internal string NpcName { get; }
    internal bool PlayerKingdomExists { get; }
    internal bool PlayerKingdomEliminated { get; }
    internal string PlayerId { get; }
    internal string PlayerName { get; }
    internal bool PlayerIsRuler { get; }
    internal bool KingdomsDiffer { get; }
    internal IReadOnlyList<DiplomacyKingdomSummary> Kingdoms { get; }
}

internal interface IDiplomacyPostprocessContextSource
{
    bool HasSpeaker { get; }
    bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot);
    DiplomacyConversationEligibilitySnapshot CaptureEligibility();
    DiplomacyPostprocessKingdomSnapshot CaptureKingdoms();
    string GetAnnexationHint();
    bool ArePlayerAndNpcAtWar();
    int CalculateDailyTribute(bool npcPays);
    void LogFailure(string message);
}

internal static class DiplomacyPostprocessContextApplication
{
    internal static string Build<TSource>(ref TSource source) where TSource : struct, IDiplomacyPostprocessContextSource
    {
        try
        {
            if (!source.HasSpeaker) return "";
            if (source.TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot independent))
                return BuildIndependent(independent);
            DiplomacyConversationEligibilitySnapshot eligibility = source.CaptureEligibility();
            bool allowFullDiplomacy = DiplomacyConversationEligibilityApplication.CanUseFull(eligibility);
            bool allowNpcDeclareWar = DiplomacyConversationEligibilityApplication.CanUseNpcDeclareWar(eligibility);
            if (!allowFullDiplomacy && !allowNpcDeclareWar) return "";
            DiplomacyPostprocessKingdomSnapshot kingdoms = source.CaptureKingdoms();
            if (!kingdoms.NpcKingdomExists) return "";
            return BuildRoyal(ref source, kingdoms, allowFullDiplomacy);
        }
        catch (Exception ex)
        {
            source.LogFailure(ex.Message);
            return "";
        }
    }

    private static string BuildIndependent(DiplomacyIndependentPeaceContextSnapshot value)
    {
        var independent = new StringBuilder();
        independent.AppendLine("【独立家族议和运行时事实】");
        independent.AppendLine("玩家家族“" + value.PlayerClanName + "”是独立家族；定居点数：" + value.SettlementCount);
        independent.AppendLine("你是敌对王国“" + value.TargetName + "”的当前国王。");
        independent.AppendLine("双方当前敌对：是");
        return independent.ToString().TrimEnd();
    }

    private static string BuildRoyal<TSource>(ref TSource source, DiplomacyPostprocessKingdomSnapshot value,
        bool allowFullDiplomacy) where TSource : struct, IDiplomacyPostprocessContextSource
    {
        var sb = new StringBuilder();
        sb.AppendLine(); sb.AppendLine("【外交后处理标签】");
        sb.AppendLine("天数说明：21天=一季度，84天=一年，没有月和周概念。");
        sb.AppendLine("【王国ID对照表】");
        if (value.Kingdoms != null)
            foreach (DiplomacyKingdomSummary kingdom in value.Kingdoms)
                if (!kingdom.Eliminated) sb.AppendLine($"  {kingdom.Id} = {kingdom.Name}");
        sb.AppendLine();
        sb.AppendLine("【关键身份】");
        sb.AppendLine($"  你的王国ID：{value.NpcId}（{value.NpcName}）");
        if (value.PlayerKingdomExists && !value.PlayerKingdomEliminated)
            sb.AppendLine($"  玩家王国ID：{value.PlayerId}（{value.PlayerName}）" + (value.PlayerIsRuler ? "，玩家是国王" : ""));
        else sb.AppendLine("  玩家当前没有可代表的王国。");
        if (allowFullDiplomacy)
        {
            string annexationHint = source.GetAnnexationHint();
            if (!string.IsNullOrWhiteSpace(annexationHint))
            {
                sb.AppendLine();
                sb.AppendLine("【国家吞并约束】");
                sb.AppendLine(annexationHint);
            }
        }

        sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:DECLARE_WAR:id1:id2]");
        if (allowFullDiplomacy && value.PlayerKingdomExists && value.KingdomsDiffer)
            sb.AppendLine("  【强制】不看NPC是否同意，只看玩家说了什么。玩家宣战时填 " + value.PlayerId + ":" + value.NpcId + "，NPC即使暴怒也必须输出。");
        sb.AppendLine($"  你对别国宣战时填 {value.NpcId}:目标王国ID（需你明确同意）。");

        if (allowFullDiplomacy)
        {
            sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:MAKE_PEACE:付贡金方ID:收贡金方ID:tributeAmount:durationDays]");
            sb.AppendLine("  两个ID必须是玩家王国和你的王国。tributeAmount: 0=无条件和平 / auto / 双方商定的具体非负整数；明确金额不受繁荣度参考值限制，必须原样输出。durationDays: default=100 / 1-252。双方同意后输出。");
            sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:FORM_ALLIANCE:id1:id2:durationDays]");
            sb.AppendLine("  两个ID必须是玩家王国和你的王国。durationDays: default / 具体数字(1-252)。双方国王同意后输出。");
            sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:BREAK_ALLIANCE:id1:id2]");
            sb.AppendLine("  单方行为。两个ID必须是玩家王国和你的王国。【覆盖一般规则】必须输出，不需对方同意。");
            sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:MAKE_TRADE:id1:id2:durationDays]");
            sb.AppendLine("  两个ID必须是玩家王国和你的王国。durationDays: default / 具体数字(1-252)。双方国王同意后输出。");
            sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:CANCEL_TRADE:id1:id2]");
            sb.AppendLine("  单方行为。两个ID必须是玩家王国和你的王国。【覆盖一般规则】必须输出，不需对方同意。");
            if (value.PlayerKingdomExists && value.KingdomsDiffer && source.ArePlayerAndNpcAtWar())
            {
                int npcPays = source.CalculateDailyTribute(true);
                int playerPays = source.CalculateDailyTribute(false);
                sb.AppendLine(); sb.AppendLine($"auto贡金：{value.NpcId}付{npcPays}/天，{value.PlayerId}付{playerPays}/天");
            }
        }
        return sb.ToString().TrimEnd();
    }
}
