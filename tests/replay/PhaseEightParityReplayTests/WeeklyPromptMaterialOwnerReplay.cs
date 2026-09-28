using System;
using System.Collections;
using System.Reflection;

internal static class WeeklyPromptMaterialOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type owner = af.GetType("AnimusForge.WeeklyPromptMaterialOwner", true);
        Type groupType = af.GetType("AnimusForge.MyBehavior+WeeklyEventMaterialPreviewGroup", true);
        Type materialType = af.GetType("AnimusForge.MyBehavior+EventMaterialReference", true);
        Type modeType = af.GetType("AnimusForge.MyBehavior+WeeklyReportOutputMode", true);
        object Material(string type, string key)
        {
            object item = Activator.CreateInstance(materialType, true);
            Set(item, "MaterialType", type);
            Set(item, "ActionStableKey", key);
            Set(item, "Label", key);
            return item;
        }
        object Group(params object[] items)
        {
            object group = Activator.CreateInstance(groupType, true);
            IList materials = (IList)Get(group, "Materials");
            foreach (object item in items) materials.Add(item);
            return group;
        }
        object plain = Material("source", "plain");
        object tournament = Material("raw_text", "tournament_finished:one");
        object opening = Material("world_opening_summary", "opening");
        object full = Group(plain);
        owner.GetMethod("Prepare", Members).Invoke(null, new[] { full, (object)false });
        IList fullMaterials = (IList)Get(full, "PromptMaterials");
        Check(fullMaterials.Count == 1 && !ReferenceEquals(fullMaterials[0], plain)
            && Get(full, "OutputMode").Equals(Enum.Parse(modeType, "FullReport"))
            && (bool)Get(full, "IncludePreviousReportInPrompt"), "full mode clones source and retains previous report");

        object shortGroup = Group(plain, tournament, opening);
        owner.GetMethod("Prepare", Members).Invoke(null, new[] { shortGroup, (object)true });
        IList shortMaterials = (IList)Get(shortGroup, "PromptMaterials");
        Check(shortMaterials.Count == 1 && !ReferenceEquals(shortMaterials[0], plain)
            && Get(shortGroup, "OutputMode").Equals(Enum.Parse(modeType, "TitleShortTagsOnly"))
            && !(bool)Get(shortGroup, "IncludePreviousReportInPrompt"),
            "short mode excludes tournament/opening and previous report");

        object started = Material("raw_text", "village_raid_started:village-one");
        object completed = Material("raw_text", "raid_completed:village-one:Defender");
        object raid = Group(started, completed);
        owner.GetMethod("Prepare", Members).Invoke(null, new[] { raid, (object)false });
        IList raidMaterials = (IList)Get(raid, "PromptMaterials");
        Check(raidMaterials.Count == 1 && (string)Get(raidMaterials[0], "MaterialType") == "prompt_resolved_village_raid"
            && (int)Get(raidMaterials[0], "SourceMaterialCount") == 2,
            "full mode resolves start and completion to one village raid material");
        Console.WriteLine("PASS WeeklyPromptMaterialOwnerReplay full/short/clone/exclusions/raid-resolution live=NOT_RUN");
    }

    private static object Get(object value, string field) => value.GetType().GetField(field, Members).GetValue(value);
    private static void Set(object value, string field, object data) => value.GetType().GetField(field, Members).SetValue(value, data);
    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyPromptMaterialOwnerReplay: " + name);
    }
}
