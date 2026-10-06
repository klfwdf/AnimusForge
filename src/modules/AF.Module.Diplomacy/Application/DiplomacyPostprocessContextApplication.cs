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
            string royal = BuildRoyal(ref source, kingdoms, allowFullDiplomacy);
            if (source is IDiplomacyOralPostprocessSource oral)
                royal += "\n" + oral.OralArrangementContext();
            return royal;
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

        sb.AppendLine("仅 NPC 明确最终答应时使用 [ACTION:DIPLOMACY:COMMIT:action=Peace;move=NewMatter;target=王国ID]；action 可为 Peace/Alliance/Trade/DeclareWar/BreakAlliance/CancelTrade/Annexation/Tributary/Garrison/Vassal。考虑、报价、假设不输出。move=AcceptProposal 或 RejectProposal 时附 source_document=原宣言ID;source_action=原动作ID（单动作公文无动作ID可留空）。原案歧义先让玩家选择。贡金必须明确金额与期限；未明确先澄清，无贡金的和平期限为0。和平可附 payer/receiver/tribute/days/cession_from/cession_to/settlement，必须复制明确同意的原始条款；不改接受原案条款。吞并或臣属必须附 receiving=接收国/宗主国ID;joining=并入国/臣属国ID。接受原案可以省略条款表示完全接受原案，不能省略来源。新条件必须 NewMatter，不能假装接受原案。玩家不是立约方也可劝说你回应第三国提案。不得代玩家发布宣言。NPC 口头答应宣战使用 COMMIT，由 NPC 正式发文再开战。");
        sb.AppendLine("NPC 明确决定延期、取消或恢复自己的发文约定时，用 [ACTION:DIPLOMACY:COMMITMENT:arrangement=约定ID;state=deferred|cancelled|accepted;reason=明确原因]。ID从约定上下文复制；已发布且尚未被接受的自己提案只能 cancelled，系统另发正式撤回宣言。已生效的行动不能撤销；改变条款另用 COMMIT:NewMatter。");
        if (value.PlayerIsRuler && value.PlayerKingdomExists && value.KingdomsDiffer)
            sb.AppendLine("玩家以国王身份亲自明确向你的王国宣战，使用 [ACTION:DIPLOMACY:DECLARE_WAR:" + value.PlayerId + ":" + value.NpcId + "]，此项立即生效；不得把劝说、威胁或假设当宣战。");
        sb.AppendLine(AnimusForge.DiplomacyDialogue.DialoguePeaceClarificationRules.PostprocessInstruction);
        return sb.ToString().TrimEnd();
    }
}

internal interface IDiplomacyOralPostprocessSource
{
    string OralArrangementContext();
}
