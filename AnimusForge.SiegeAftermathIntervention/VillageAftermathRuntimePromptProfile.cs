using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class VillageAftermathRuntimePromptProfile
{
    public const string RuleId = "gccz_village_administration";

    public const string InjectedRuleBlockMarker = "【GCCZ村庄处置规则】";

    public static string BuildPrompt(string villageName, string boundSettlementName, VillageAftermathAuthorityKind authorityKind)
    {
        string authority = authorityKind == VillageAftermathAuthorityKind.DirectOwner
            ? "玩家家族直属村庄"
            : "玩家作为统治者辖下封臣的村庄";
        var text = new StringBuilder();
        text.AppendLine("当前是 GCCZ 村庄处置：" + (villageName ?? "该村庄") + "，上级封地为 " + (boundSettlementName ?? "未知封地") + "；权限=" + authority + "。");
        text.AppendLine("这不是敌对劫掠入口，也不是城镇/城堡攻城处置。普通 AF、原版对话和场景功能必须继续可用。");
        text.AppendLine("只有玩家明确命令且你正在直接回复玩家时，才可输出一个对应的 [VILLAGE_ACTION:...] 标签；不要因环境闲聊触发处置。");
        text.AppendLine("可用标签：召集长老、约束军纪、平息、赈济、罚赎、征粮、征收物产、征收牲畜、征丁、惩办首恶、查抄村产、毁坏生计、屠村、文化改造。");
        text.Append("文化改造标签只打开玩家确认界面，不得自行决定文化与手段。");
        return text.ToString();
    }
}
