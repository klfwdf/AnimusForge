using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

internal static class WeeklyMaterialAggregationReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type materialType;
    private static readonly Dictionary<string, int> rendered = new Dictionary<string, int>();

    internal static void Run(Assembly af)
    {
        Type groupType = af.GetType("AnimusForge.MyBehavior+WeeklyEventMaterialPreviewGroup", true);
        materialType = af.GetType("AnimusForge.MyBehavior+EventMaterialReference", true);
        Type owner = af.GetType("AnimusForge.WeeklyMaterialAggregationOwner", true);
        Type bucketsType = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(List<>).MakeGenericType(materialType));
        Type renderType = typeof(Func<,,,>).MakeGenericType(groupType, typeof(string), bucketsType, materialType);
        var groupParam = Expression.Parameter(groupType);
        var categoryParam = Expression.Parameter(typeof(string));
        var bucketsParam = Expression.Parameter(bucketsType);
        var render = Expression.Lambda(renderType,
            Expression.Convert(Expression.Call(typeof(WeeklyMaterialAggregationReplay).GetMethod(nameof(Render), Members),
                Expression.Convert(groupParam, typeof(object)), categoryParam,
                Expression.Convert(bucketsParam, typeof(object))), materialType),
            groupParam, categoryParam, bucketsParam).Compile();

        object Material(string type, string kind, string key, string label)
        {
            object value = Activator.CreateInstance(materialType, true);
            Set(value, "MaterialType", type); Set(value, "ActionKind", kind);
            Set(value, "ActionStableKey", key); Set(value, "Label", label);
            return value;
        }
        object group = Activator.CreateInstance(groupType, true);
        IList source = (IList)groupType.GetField("Materials", Members).GetValue(group);
        object plain = Material("source", "other", "plain", "plain");
        source.Add(plain);
        owner.GetMethod("Apply", Members).Invoke(null, new[] { group, render });
        IList noActions = (IList)groupType.GetField("Materials", Members).GetValue(group);
        Check(noActions.Count == 1 && !ReferenceEquals(noActions[0], plain) && rendered.Count == 0,
            "non-action material cloned without rendering");

        source = (IList)groupType.GetField("Materials", Members).GetValue(group);
        source.Add(Material("npc_recent_action", "army_join", "same", "army-a"));
        source.Add(Material("npc_major_action", "army_join", "same", "army-b"));
        source.Add(Material("npc_major_action", "hero_killed", "other", "death"));
        rendered.Clear();
        owner.GetMethod("Apply", Members).Invoke(null, new[] { group, render });
        IList result = (IList)groupType.GetField("Materials", Members).GetValue(group);
        Check(result.Count == 3 && rendered.Count == 2 && rendered["army"] == 1 && rendered["death"] == 1,
            "category partition and event-key deduplication");
        Console.WriteLine("PASS WeeklyMaterialAggregationReplay preserve/clone/category/event buckets live=NOT_RUN");
    }

    private static object Render(object group, string category, object buckets)
    {
        IDictionary events = (IDictionary)buckets;
        rendered[category] = events.Count;
        object value = Activator.CreateInstance(materialType, true);
        Set(value, "MaterialType", "prompt_agg_" + category);
        Set(value, "Label", category);
        return value;
    }

    private static void Set(object value, string field, object data) => value.GetType().GetField(field, Members).SetValue(value, data);
    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyMaterialAggregationReplay: " + name);
    }
}
