using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

internal static class WeeklyReportMaterialRevisionReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type ownerType = af.GetType("AnimusForge.WeeklyReportMaterialRevisionOwner", true);
        object owner = Activator.CreateInstance(ownerType, true);
        object Capture() => ownerType.GetMethod("Capture", Members).Invoke(owner, new object[] { 0, 6 });
        bool Current(object snapshot) => (bool)ownerType.GetMethod("IsCurrent", Members).Invoke(owner, new[] { snapshot });
        void Mark(string method, int? day = null) => ownerType.GetMethod(method, Members).Invoke(owner, day.HasValue ? new object[] { day.Value } : null);

        object initial = Capture();
        Mark("MarkDay", 7);
        Check(Current(initial), "next-week action does not invalidate completed-week materials");
        Mark("MarkDay", 3);
        Check(!Current(initial), "same-week source append invalidates captured materials");
        object sameWeek = Capture();
        Mark("MarkOpening");
        Check(!Current(sameWeek), "opening summary edit invalidates captured materials");
        object beforeRemoval = Capture();
        Mark("MarkAll");
        Check(!Current(beforeRemoval), "source removal or owner merge invalidates captured materials");

        Type behavior = af.GetType("AnimusForge.MyBehavior", true);
        object host = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(behavior);
        behavior.GetField("_weeklyReportMaterialRevisions", Members).SetValue(host, owner);
        Type groupType = behavior.GetNestedType("WeeklyEventMaterialPreviewGroup", BindingFlags.NonPublic);
        object world = Activator.CreateInstance(groupType, true);
        groupType.GetField("GroupKind", Members).SetValue(world, "world");
        Type contextType = behavior.GetNestedType("PendingWeeklyReportCommitContext", BindingFlags.NonPublic);
        object context = Activator.CreateInstance(contextType, true);
        contextType.GetField("WeekIndex", Members).SetValue(context, 1);
        object captured = Capture();
        contextType.GetField("SourceSnapshot", Members).SetValue(context, captured);
        contextType.GetField("CapturedRecordStates", Members).SetValue(context,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["world"] = null });
        Type materialType = behavior.GetNestedType("EventMaterialReference", BindingFlags.NonPublic);
        Type cursorType = af.GetType("AnimusForge.WeeklyReportBlockMaterialCursor`1", true).MakeGenericType(materialType);
        object emptyMaterials = Activator.CreateInstance(typeof(List<>).MakeGenericType(materialType));
        object cursor = Activator.CreateInstance(cursorType, Members, null, new[] { emptyMaterials }, null);
        Type blockType = behavior.GetNestedType("PendingWeeklyReportBlockCommit", BindingFlags.NonPublic);
        object block = Activator.CreateInstance(blockType, true);
        blockType.GetField("Group", Members).SetValue(block, world);
        blockType.GetField("ReportId", Members).SetValue(block, "world");
        blockType.GetField("MaterialCursor", Members).SetValue(block, cursor);
        contextType.GetField("CurrentBlockCommit", Members).SetValue(context, block);
        Mark("MarkDay", 2);
        bool processed = (bool)behavior.GetMethod("ProcessPendingWeeklyReportBlockCommit", Members).Invoke(host,
            new object[] { context, Stopwatch.GetTimestamp(), 1000.0 });
        Check(processed && (bool)contextType.GetField("CurrentBlockRejected", Members).GetValue(context)
            && contextType.GetField("CurrentBlockCommit", Members).GetValue(context) == null,
            "real block writer rejects changed source after staged material cloning");
        Console.WriteLine("PASS WeeklyReportMaterialRevisionReplay next-week/same-week/opening/removal/real-block-write-guard live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyReportMaterialRevisionReplay: " + name);
    }
}
