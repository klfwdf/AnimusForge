using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal static class WeeklyMaterialPipelineParityReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type groupType = af.GetType("AnimusForge.MyBehavior+WeeklyEventMaterialPreviewGroup", true);
        Type materialType = af.GetType("AnimusForge.MyBehavior+EventMaterialReference", true);
        Type batchType = af.GetType("AnimusForge.MyBehavior+WeeklyReportBatchRequest", true);
        Type aggregation = af.GetType("AnimusForge.WeeklyMaterialAggregationOwner", true);
        Type prompt = af.GetType("AnimusForge.WeeklyPromptMaterialOwner", true);
        Type planner = af.GetType("AnimusForge.WeeklyMaterialBatchPlanner", true);
        Type cursorType = af.GetType("AnimusForge.WeeklyMaterialStageCursor`1", true);

        object BuildMaterial(string type, string key, int day)
        {
            object item = Activator.CreateInstance(materialType, true);
            Set(item, "MaterialType", type); Set(item, "ActionStableKey", key);
            Set(item, "Label", key); Set(item, "ActionDay", day);
            return item;
        }
        object BuildGroup(string kind, string id)
        {
            object group = Activator.CreateInstance(groupType, true);
            Set(group, "GroupKind", kind); Set(group, "KingdomId", id); Set(group, "Title", id);
            IList materials = (IList)Get(group, "Materials");
            materials.Add(BuildMaterial("source", id + ":event", 43));
            materials.Add(BuildMaterial("raw_text", "tournament_finished:" + id, 44));
            return group;
        }
        IList BuildGroups()
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(groupType));
            list.Add(BuildGroup("kingdom", "far"));
            list.Add(BuildGroup("world", ""));
            list.Add(BuildGroup("kingdom", "near"));
            return list;
        }
        void Prepare(object groupList, bool staged)
        {
            IList groups = (IList)groupList;
            void ForEachStage(Action<object> action)
            {
                if (!staged)
                {
                    foreach (object group in groups) action(group);
                    return;
                }
                Type closed = cursorType.MakeGenericType(groupType);
                object cursor = Activator.CreateInstance(closed, Members, null, new[] { groupList }, null);
                MethodInfo take = closed.GetMethod("TryTake", Members);
                while (true)
                {
                    object[] args = { null };
                    if (!(bool)take.Invoke(cursor, args)) break;
                    action(args[0]);
                }
            }
            ForEachStage(group => aggregation.GetMethod("Apply", Members).Invoke(null, new[] { group, null }));
            var selected = planner.GetMethod("SelectFullReportKingdomIds", Members).Invoke(null,
                new object[] { groupList, new List<string> { "near" } });
            ForEachStage(group =>
            {
                bool shortOnly = (bool)planner.GetMethod("IsShortOnly", Members).Invoke(null, new[] { group, selected });
                prompt.GetMethod("Prepare", Members).Invoke(null, new object[] { group, shortOnly });
            });
        }
        object BuildBatches(IList groups)
        {
            object ordered = planner.GetMethod("OrderGroups", Members).Invoke(null,
                new object[] { groups, new List<string> { "near", "far" } });
            return planner.GetMethod("BuildBatches", Members).Invoke(null,
                new[] { ordered, (object)7, 42, 48, 2 });
        }
        string Snapshot(IList groups, IList batches)
        {
            string Group(object group)
            {
                IList materials = (IList)Get(group, "PromptMaterials");
                return Get(group, "GroupKind") + ":" + Get(group, "KingdomId") + ":" + Get(group, "OutputMode")
                    + ":" + string.Join(",", materials.Cast<object>().Select(x => Get(x, "MaterialType") + "/" + Get(x, "ActionStableKey") + "/" + Get(x, "ActionDay")));
            }
            string Batch(object batch)
            {
                IList members = (IList)Get(batch, "Groups");
                return Get(batch, "WeekIndex") + ":" + Get(batch, "StartDay") + ":" + Get(batch, "EndDay")
                    + ":" + Get(batch, "OutputMode") + ":" + string.Join(",", members.Cast<object>().Select(x => Get(x, "KingdomId")));
            }
            return string.Join("|", groups.Cast<object>().Select(Group)) + "#" + string.Join("|", batches.Cast<object>().Select(Batch));
        }

        IList synchronous = BuildGroups(), deferred = BuildGroups();
        Prepare(synchronous, staged: false);
        Prepare(deferred, staged: true);
        IList syncBatches = (IList)BuildBatches(synchronous);
        IList deferredBatches = (IList)BuildBatches(deferred);
        Type batchCursor = cursorType.MakeGenericType(batchType);
        object batchStage = Activator.CreateInstance(batchCursor, Members, null, new object[] { deferredBatches }, null);
        MethodInfo takeBatch = batchCursor.GetMethod("TryTake", Members);
        int taken = 0;
        while (true)
        {
            object[] args = { null };
            if (!(bool)takeBatch.Invoke(batchStage, args)) break;
            Check(ReferenceEquals(args[0], deferredBatches[taken++]), "batch stage retains original identity");
        }
        Check(taken == deferredBatches.Count && Snapshot(synchronous, syncBatches) == Snapshot(deferred, deferredBatches),
            "same detached materials/week produce equal synchronous and staged output");
        Console.WriteLine("PASS WeeklyMaterialPipelineParityReplay same-input/sync-vs-staged/order/mode/week live=NOT_RUN");
    }

    private static object Get(object value, string field) => value.GetType().GetField(field, Members).GetValue(value);
    private static void Set(object value, string field, object data) => value.GetType().GetField(field, Members).SetValue(value, data);
    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyMaterialPipelineParityReplay: " + name);
    }
}
