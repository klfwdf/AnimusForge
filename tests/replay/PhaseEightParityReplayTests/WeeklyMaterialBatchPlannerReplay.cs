using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

internal static class WeeklyMaterialBatchPlannerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type groupType = af.GetType("AnimusForge.MyBehavior+WeeklyEventMaterialPreviewGroup", true);
        Type planner = af.GetType("AnimusForge.WeeklyMaterialBatchPlanner", true);
        Type modeType = af.GetType("AnimusForge.MyBehavior+WeeklyReportOutputMode", true);
        object shortMode = Enum.Parse(modeType, "TitleShortTagsOnly");

        object Group(string kind, string id, string title, bool shortOnly = false)
        {
            object group = Activator.CreateInstance(groupType, true);
            groupType.GetField("GroupKind", Members).SetValue(group, kind);
            groupType.GetField("KingdomId", Members).SetValue(group, id);
            groupType.GetField("Title", Members).SetValue(group, title);
            if (shortOnly) groupType.GetField("OutputMode", Members).SetValue(group, shortMode);
            return group;
        }

        IList groups = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(groupType));
        object world = Group("world", "", "world");
        object near = Group("kingdom", "near", "near");
        object far = Group("kingdom", "far", "far", shortOnly: true);
        object other = Group("kingdom", "other", "other");
        object otherTwo = Group("kingdom", "other-two", "other-two");
        groups.Add(otherTwo); groups.Add(other); groups.Add(far); groups.Add(world); groups.Add(near);
        var selected = (HashSet<string>)planner.GetMethod("SelectFullReportKingdomIds", Members).Invoke(null,
            new object[] { groups, new List<string> { " FAR ", "far", "NEAR" } });
        Check(selected.Count == 2 && selected.Contains("far") && selected.Contains("near"),
            "nearby full-report IDs are trimmed and deduplicated");
        Check(!(bool)planner.GetMethod("IsShortOnly", Members).Invoke(null, new object[] { near, selected })
            && !(bool)planner.GetMethod("IsShortOnly", Members).Invoke(null, new object[] { world, selected })
            && (bool)planner.GetMethod("IsShortOnly", Members).Invoke(null, new object[] { other, selected }),
            "only non-selected kingdom uses short prompt");
        var fallback = (HashSet<string>)planner.GetMethod("SelectFullReportKingdomIds", Members).Invoke(null,
            new object[] { groups, new List<string>() });
        Check(fallback.Count == 3 && fallback.Contains("other-two") && fallback.Contains("other")
            && fallback.Contains("far") && !fallback.Contains("near"), "empty proximity falls back to first three kingdoms");
        IList ordered = (IList)planner.GetMethod("OrderGroups", Members).Invoke(null,
            new object[] { groups, new List<string> { "near", "far" } });
        Check(ordered.Count == 5 && ReferenceEquals(ordered[0], near) && ReferenceEquals(ordered[1], world)
            && ReferenceEquals(ordered[2], far) && ReferenceEquals(ordered[3], other)
            && ReferenceEquals(ordered[4], otherTwo),
            "nearby kingdom/world/fallback ordering");

        IList batches = (IList)planner.GetMethod("BuildBatches", Members).Invoke(null,
            new object[] { ordered, 7, 42, 48, 2 });
        Check(batches.Count == 4, "one world, two bounded full and one short batch");
        IList first = (IList)batches[0].GetType().GetField("Groups", Members).GetValue(batches[0]);
        IList second = (IList)batches[1].GetType().GetField("Groups", Members).GetValue(batches[1]);
        IList third = (IList)batches[2].GetType().GetField("Groups", Members).GetValue(batches[2]);
        IList fourth = (IList)batches[3].GetType().GetField("Groups", Members).GetValue(batches[3]);
        Check(first.Count == 1 && ReferenceEquals(first[0], world)
            && second.Count == 2 && ReferenceEquals(second[0], near) && ReferenceEquals(second[1], other)
            && third.Count == 1 && ReferenceEquals(third[0], otherTwo)
            && fourth.Count == 1 && ReferenceEquals(fourth[0], far), "batch identity and mode partition");
        Check((int)batches[0].GetType().GetField("WeekIndex", Members).GetValue(batches[0]) == 7
            && (int)batches[0].GetType().GetField("StartDay", Members).GetValue(batches[0]) == 42
            && (int)batches[0].GetType().GetField("EndDay", Members).GetValue(batches[0]) == 48,
            "batch week and date bounds");
        Check(batches[3].GetType().GetField("OutputMode", Members).GetValue(batches[3]).Equals(shortMode),
            "short batch keeps short-only mode");
        Console.WriteLine("PASS WeeklyMaterialBatchPlannerReplay full-selection/fallback/order/world/full/short/identity/date live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyMaterialBatchPlannerReplay: " + name);
    }
}
