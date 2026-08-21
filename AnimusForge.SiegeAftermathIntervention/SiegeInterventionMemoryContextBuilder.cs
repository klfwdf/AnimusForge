using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free formatter for GCCZ per-scene memory context.
/// TownSceneMemoryStore owns collection, duplicate suppression, and trimming.
/// </summary>
public static class SiegeInterventionMemoryContextBuilder
{
    public const int MaxMemoryEvents = 10;

    public static string Build(IReadOnlyList<string> memoryEvents)
    {
        return Build(memoryEvents, SiegeInterventionMemoryAudience.General);
    }

    public static string Build(IReadOnlyList<string> memoryEvents, SiegeInterventionMemoryAudience audience)
    {
        if (memoryEvents == null || memoryEvents.Count == 0)
        {
            return string.Empty;
        }

        string baseContext = "【攻城处置记忆】" + string.Join("；", memoryEvents)
            + "。这些是本次入城处置内已经发生的事实，后续NPC必须承认大概情况，不能表现得像玩家没有下过这些命令或民众没有被聚集过。";
        string perspective = BuildPerspective(memoryEvents, audience);
        return string.IsNullOrWhiteSpace(perspective) ? baseContext : baseContext + perspective;
    }

    private static string BuildPerspective(IReadOnlyList<string> memoryEvents, SiegeInterventionMemoryAudience audience)
    {
        string joined = string.Join("；", memoryEvents ?? new List<string>());
        if (string.IsNullOrWhiteSpace(joined))
        {
            return string.Empty;
        }

        bool hasMercy = ContainsAny(joined, "宽恕", "救济", "宣抚", "盟誓");
        bool hasSoldierAppeasement = ContainsAny(joined, "安兵", "军心");
        bool hasGather = ContainsAny(joined, "召集", "聚集");
        bool hasRobbery = ContainsAny(joined, "抢钱");
        bool hasPlunder = ContainsAny(joined, "搜掠");
        bool hasMassacre = ContainsAny(joined, "血洗");
        bool hasRepopulation = ContainsAny(joined, "殖民");

        if (audience == SiegeInterventionMemoryAudience.AlliedSoldier)
        {
            var parts = new List<string>();
            if (hasRepopulation)
            {
                parts.Add("你应知道处置已指向杀尽原住民并迁入己方人口，语气不能轻飘，服从中可带紧绷、沉默或同文化不适。");
            }
            else if (hasMassacre)
            {
                parts.Add("你应知道血洗已经开始或被明确下令，这是不可回退的升级；同文化只会让你更压抑，不会让你抗命。");
            }
            else if (hasPlunder)
            {
                parts.Add("你应知道玩家承认了胜利方战利权，士兵可更兴奋、粗鲁或贪婪，但仍听玩家节制。");
            }

            if (hasRobbery)
            {
                parts.Add("你听到过平民侧局部索财苗头时，可以向玩家请示是否扩大处置，但不能自行宣布全城搜掠。");
            }

            if (hasMercy)
            {
                parts.Add("你应承认玩家走过宽恕/救济/宣抚路线，服从之余可压着战利落空的不满，尤其在军心待安抚时更明显。");
            }

            if (hasSoldierAppeasement)
            {
                parts.Add("你应承认玩家已经处理过军心问题，后续士兵口吻应更稳。");
            }

            if (hasGather)
            {
                parts.Add("你应知道民众正在被传来或已经聚集，回应时可把自己当成维持现场秩序的入城士兵。");
            }

            return BuildPerspectiveBlock("士兵视角记忆解读", parts);
        }

        if (audience == SiegeInterventionMemoryAudience.Civilian)
        {
            var parts = new List<string>();
            if (hasRepopulation)
            {
                parts.Add("你应知道玩家一方要清除原住民并迁入己方人口，平民反应应接近绝望、逃散、哀求或少数反抗，不能假装只是普通征税。");
            }
            else if (hasMassacre)
            {
                parts.Add("你应知道血洗已经开始或被明确下令，平民应恐慌、逃避、崩溃或在少数情况下反抗，不能表现得安全从容。");
            }
            else if (hasPlunder || hasRobbery)
            {
                parts.Add("你应知道财物正被索取或搜掠，平民可藏财、求饶、讨价还价或交出钱物换安全。");
            }

            if (hasMercy)
            {
                parts.Add("你应知道玩家给过宽恕、救济或安民承诺，恐惧仍在，但可从崩溃转为试探性顺从和求保障。");
            }

            if (hasGather)
            {
                parts.Add("你应知道民众正在被召集或已经到场听命，回应时应承认自己处在被胜利者集中处置的现场。");
            }

            return BuildPerspectiveBlock("平民视角记忆解读", parts);
        }

        if (hasMassacre || hasRepopulation || hasPlunder || hasMercy || hasGather)
        {
            return "【记忆优先级】宽恕/救济/宣抚/盟誓、搜掠、血洗、殖民这类处置方向优先于传令和走位细节；后续回复必须承认当前大方向。";
        }

        return string.Empty;
    }

    private static string BuildPerspectiveBlock(string title, List<string> parts)
    {
        if (parts == null || parts.Count == 0)
        {
            return string.Empty;
        }

        return "【" + title + "】" + string.Join("", parts);
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(text) || tokens == null)
        {
            return false;
        }

        foreach (string token in tokens)
        {
            if (!string.IsNullOrWhiteSpace(token) && text.Contains(token))
            {
                return true;
            }
        }

        return false;
    }
}
