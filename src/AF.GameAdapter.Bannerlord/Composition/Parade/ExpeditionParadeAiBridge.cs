using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed partial class ExpeditionParadeBehavior
{
    private readonly ConcurrentDictionary<string, Tuple<string, DateTime>> _actionPermits = new ConcurrentDictionary<string, Tuple<string, DateTime>>();
    private static Regex ActionTags => ExpeditionParadeRules.ActionTags;
    internal static string StripTags(string raw) => ActionTags.Replace(raw ?? "", "").Trim();
    internal static bool CanInjectForExternal(Hero hero) => Mission.Current != null && Current?.GetAvailability(hero).Any == true;
    internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero hero)
    {
        var available = Current?.GetAvailability(hero);
        if (Mission.Current == null || available == null) return new List<PostprocessRuleEntry>();
        return (AIConfigHandler.GetGuardrailRulePostprocessRules(RuleId) ?? new List<PostprocessRuleEntry>())
            .Where(r => r != null && ((r.Tag == "[ACTION:PARADE:EXPEDITION]" && available.ExpeditionAvailable)
                || (r.Tag == "[ACTION:PARADE:VICTORY]" && available.VictoryAvailable)))
            .Select(r => new PostprocessRuleEntry { Tag = r.Tag, Description = r.Description }).ToList();
    }
    internal static string BuildPostprocessContextForExternal(Hero hero)
    {
        var available = Current?.GetAvailability(hero);
        if (available == null || Mission.Current == null) return T("当前不能安排现场阅兵。", "No in-person parade can be arranged now.");
        return T("【阅兵实时条件】\n", "[Live parade conditions]\n")
            + "settlement=" + (available.Settlement?.StringId ?? "none")
            + "\nEXPEDITION=" + available.ExpeditionAvailable + " : " + available.ExpeditionReason
            + "\nVICTORY=" + available.VictoryAvailable + " : " + available.VictoryReason
            + "\n" + T("通过AI交谈只能由此地总督安排已解锁的类型；玩家也可自行使用大地图城镇／城堡菜单中的“举行阅兵”，不要求总督在场。使用主队伍、当地驻军和已集结的玩家军团健康士兵作展示，不转移兵员。步兵三列、骑兵两列，每列旗手。尚待路线校验；安排不表示阅兵已经完成。", "Only this settlement's governor can arrange an unlocked type through AI dialogue. The player can also use Hold a parade in the world-map town/castle menu without a governor present. Healthy troops from the main party, local garrison and assembled player-led army are display-only. Infantry use three columns, mounted troops two, with one banner bearer per column. Route validation is still pending; arranging a parade does not mean it has completed.");
    }
    internal static string NormalizeTags(string raw, List<PostprocessRuleEntry> allowed)
        => ExpeditionParadeRules.NormalizeTags(raw, allowed?.Where(r => r != null).Select(r => r.Tag));

    // Only the selected action-postprocess output can authorize execution, never raw dialogue tags.
    internal static void AuthorizePostprocessedTag(Hero hero, string finalizedTags, List<PostprocessRuleEntry> allowed)
    {
        var owner = Current;
        string tag = NormalizeTags(finalizedTags, allowed);
        if (owner == null || hero == null || tag.Length == 0) return;
        foreach (var pair in owner._actionPermits)
            if (pair.Value.Item2 < DateTime.UtcNow) owner._actionPermits.TryRemove(pair.Key, out _);
        owner._actionPermits[hero.StringId] = Tuple.Create(tag, DateTime.UtcNow.AddSeconds(60));
    }
    internal static bool TryApplyTagsForExternal(Hero hero, ref string content, out List<string> facts, out List<string> notifications)
    {
        facts = new List<string>(); notifications = new List<string>();
        if (!ActionTags.IsMatch(content ?? "")) return false;
        string selected = NormalizeTags(content, BuildRuntimePostprocessRulesForExternal(hero));
        content = StripTags(content);
        var owner = Current;
        bool permitted = owner != null && hero != null && owner._actionPermits.TryRemove(hero.StringId, out var permit)
            && permit.Item1 == selected && permit.Item2 >= DateTime.UtcNow;
        if (selected.Length == 0 || !permitted)
        { notifications.Add(T("当前阅兵请求不满足条件或类型不明确。", "The parade is unavailable or its type is ambiguous.")); return true; }
        var type = selected == "[ACTION:PARADE:VICTORY]" ? ExpeditionParadeType.Victory : ExpeditionParadeType.Expedition;
        if (owner.TryQueue(hero, type, out string message))
            facts.Add(T("总督已接受阅兵安排，尚待集结和路线检查。", "The governor accepted the parade request; muster and route checks are pending."));
        notifications.Add(message);
        return true;
    }
}
