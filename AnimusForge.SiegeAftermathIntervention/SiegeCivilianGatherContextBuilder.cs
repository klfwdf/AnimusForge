namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeCivilianGatherContextBuilder
{
    public static string Build(SiegeCivilianGatherContextFacts facts)
    {
        if (facts == null)
        {
            return string.Empty;
        }

        if (!facts.SpeechRallyActive
            && !facts.GatherPropagationActive
            && !facts.FormationControlPending
            && !facts.FormationControlComplete
            && facts.FollowerCount <= 0
            && facts.ReadyFormationCount <= 0)
        {
            return string.Empty;
        }

        string stage = DescribeStage(facts);
        return "【民众召集状态】" + stage + "当前已被召集/跟随的民众约 " + facts.FollowerCount
            + " 人，已进入民众队列约 " + facts.ReadyFormationCount
            + " 人，传令者约 " + facts.MessengerCount
            + " 人，场景内可识别普通民众约 " + facts.TotalCivilianCount
            + " 人。后续对话必须承认这件事：不要表现得像从未召集过民众，也不要再次要求全体士兵乱跑。";
    }

    public static string DescribeStage(SiegeCivilianGatherContextFacts facts)
    {
        if (facts == null)
        {
            return string.Empty;
        }

        if (facts.FormationControlComplete)
        {
            return "民众已经完成召集，并已转入玩家可调度的民众队列；他们正在聚拢听候玩家训话或进一步处置。";
        }

        if (facts.FormationControlPending)
        {
            return "民众已经基本聚拢，正在转入玩家可调度的民众队列。";
        }

        if (facts.GatherPropagationActive)
        {
            return "召集民众的传令正在进行，传令者会通知尚未听到命令的平民。";
        }

        return "已有民众听到召集并开始向玩家附近聚拢。";
    }
}
