using System;
using System.Text;

namespace AnimusForge.CoupSystem;

// Final confirmed facts only. One pass over the session records, once per outcome receipt.
internal static class CoupOutcomeReport
{
    internal static bool CanReport(CoupSession session, bool success) => session != null && session.CasualtiesCommitted
        && (success ? session.KingSubdued && session.RulingClanCommitted && session.TownCommitted && session.CustodyCommitted
            : session.DefectionCommitted && session.WithdrawalCommitted);

    internal static string Build(CoupSession session, string player, string king, string town, string kingdom,
        bool success, bool captured, bool atWarWithOriginalKingdom)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        int allies = 0, hallAllies = 0, alliedKilled = 0, alliedWounded = 0, defenderKilled = 0, defenderWounded = 0;
        foreach (CoupTroopRecord troop in session.Troops)
        {
            if (troop.Role == CoupTroopRole.Ally)
            {
                allies++;
                if (troop.HallSelected) hallAllies++;
                if (troop.Killed) alliedKilled++;
                if (troop.Wounded) alliedWounded++;
            }
            else
            {
                if (troop.Killed) defenderKilled++;
                if (troop.Wounded) defenderWounded++;
            }
        }
        var text = new StringBuilder();
        text.Append(player).Append("在").Append(kingdom).Append("的").Append(town)
            .Append("针对国王").Append(king).Append("发动武装政变，突击队共").Append(allies).Append("人。");
        bool hallReached = session.HallEntered || session.KingSubdued;
        bool gateReached = session.GateBreached || hallReached;
        text.Append("过程：");
        if (!session.SceneEntered && !gateReached) text.Append("已集结突击队，未记录到实际进场。");
        else
        {
            text.Append("进入城镇街道");
            if (gateReached) text.Append("，突破大厅入口");
            if (hallReached) text.Append("，率").Append(hallAllies).Append("名突击队员攻入领主大厅");
            if (session.KingSubdued) text.Append("，制服国王");
            text.Append("。");
        }
        text.Append("结果：");
        if (success)
            text.Append("政变成功，").Append(player).Append("取得").Append(kingdom).Append("王位并接管").Append(town)
                .Append(captured ? "，旧王被扣押。" : "，选择不扣押旧王。");
        else
        {
            text.Append("政变失败，未夺取王位，家族带原有领地脱离原王国");
            if (atWarWithOriginalKingdom) text.Append("并与之开战");
            text.Append("。");
        }
        text.Append("已记录伤亡：突击队阵亡").Append(alliedKilled).Append("、负伤").Append(alliedWounded)
            .Append("；守军阵亡").Append(defenderKilled).Append("、负伤").Append(defenderWounded).Append("。");
        return text.ToString();
    }
}
