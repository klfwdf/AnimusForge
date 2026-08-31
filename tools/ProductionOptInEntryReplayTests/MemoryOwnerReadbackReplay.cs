using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class MemoryOwnerReadbackReplay
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int _assertions;

    public static void Run(Assembly production)
    {
        Require(production != null, "production assembly is required");
        Type ownerType = RequireType(production, "AnimusForge.MyBehavior");
        Type draftType = RequireNestedType(ownerType, "DailyMemoryDraft");
        Type lineType = RequireNestedType(ownerType, "DailyMemoryLine");
        Type dayType = RequireNestedType(ownerType, "DialogueDay");
        MethodInfo saveDaily = RequireMethod(ownerType, "SaveDailyMemoryDraftsById");
        MethodInfo dailyPublished = RequireMethod(ownerType, "IsDailyMemoryLinePublished");
        MethodInfo saveRecent = RequireMethod(ownerType, "SaveDialogueHistoryById");
        MethodInfo recentPublished = RequireMethod(ownerType, "IsDialogueHistoryPublished");
        const string ownerId = "af_nonhero:local7d-fixture";
        const int day = 42;

        object owner = RuntimeHelpers.GetUninitializedObject(ownerType);
        object draft = New(draftType);
        object line = New(lineType);
        Set(draft, "HeroId", ownerId);
        Set(draft, "GameDayIndex", day);
        Set(line, "Text", "same fixture text");
        AddToFieldList(draft, "Lines", line);

        object droppedDraft = New(draftType);
        object droppedLine = New(lineType);
        Set(droppedDraft, "HeroId", ownerId);
        Set(droppedDraft, "GameDayIndex", day);
        Set(droppedLine, "Text", "duplicate draft is sanitized away");
        Set(droppedLine, "GameDayIndex", day);
        AddToFieldList(droppedDraft, "Lines", droppedLine);
        IList drafts = NewList(draftType, draft, droppedDraft);
        saveDaily.Invoke(owner, new object[] { ownerId, drafts });

        Require(InvokeBool(dailyPublished, owner, ownerId, day, draft, line), "published daily line was not confirmed");
        Require(!InvokeBool(dailyPublished, owner, "af_nonhero:other", day, draft, line), "wrong daily owner was accepted");
        Require(!InvokeBool(dailyPublished, owner, ownerId, day + 1, draft, line), "wrong daily day was accepted");
        object sameTextLine = New(lineType);
        Set(sameTextLine, "Text", "same fixture text");
        Set(sameTextLine, "GameDayIndex", day);
        Require(!InvokeBool(dailyPublished, owner, ownerId, day, draft, sameTextLine), "same-text line with a different identity was accepted");
        Require(!InvokeBool(dailyPublished, owner, ownerId, day, droppedDraft, droppedLine), "sanitized-away duplicate draft was accepted");
        Set(draft, "HeroId", "af_nonhero:wrong-owner-in-dictionary");
        Require(!InvokeBool(dailyPublished, owner, ownerId, day, draft, line), "wrong draft owner under the right dictionary key was accepted");
        Set(draft, "HeroId", ownerId);
        Set(line, "GameDayIndex", day + 1);
        Require(!InvokeBool(dailyPublished, owner, ownerId, day, draft, line), "wrong line day in the right draft was accepted");
        Set(line, "GameDayIndex", day);

        object historyDay = New(dayType);
        Set(historyDay, "GameDayIndex", day);
        IList historyLines = (IList)Get(historyDay, "Lines");
        for (int i = 0; i < 260; i++)
        {
            historyLines.Add("fixture line " + i);
        }
        IList records = NewList(dayType, historyDay);
        saveRecent.Invoke(owner, new object[] { ownerId, records });
        Require(InvokeBool(recentPublished, owner, ownerId, records), "published 260-line recent list was not confirmed");

        object copyDay = New(dayType);
        Set(copyDay, "GameDayIndex", day);
        ((IList)Get(copyDay, "Lines")).Add("fixture line 259");
        IList differentRecords = NewList(dayType, copyDay);
        Require(!InvokeBool(recentPublished, owner, ownerId, differentRecords), "different recent list identity was accepted");
        object emptyOwner = RuntimeHelpers.GetUninitializedObject(ownerType);
        Require(!InvokeBool(recentPublished, emptyOwner, ownerId, records), "missing recent owner was accepted");

        Console.WriteLine("PASS memoryOwnerReadback fixtureOnly=1 liveCampaign=0 assertions=" + _assertions);
    }

    private static object New(Type type) => Activator.CreateInstance(type, nonPublic: true)
        ?? throw new InvalidOperationException("could not create " + type.FullName);

    private static IList NewList(Type itemType, params object[] items)
    {
        IList list = (IList)(Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))
            ?? throw new InvalidOperationException("could not create list for " + itemType.FullName));
        foreach (object item in items) list.Add(item);
        return list;
    }

    private static void AddToFieldList(object target, string name, object value) => ((IList)Get(target, name)).Add(value);

    private static object Get(object target, string name) => target.GetType().GetField(name, InstanceFields)?.GetValue(target)
        ?? throw new InvalidOperationException("missing/noninitialized field " + target.GetType().FullName + "." + name);

    private static void Set(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, InstanceFields)
            ?? throw new InvalidOperationException("missing field " + target.GetType().FullName + "." + name);
        field.SetValue(target, value);
    }

    private static bool InvokeBool(MethodInfo method, object owner, params object[] args)
        => (bool)(method.Invoke(owner, args) ?? false);

    private static Type RequireType(Assembly assembly, string name)
        => assembly.GetType(name, throwOnError: false) ?? throw new InvalidOperationException("missing type " + name);

    private static Type RequireNestedType(Type owner, string name)
        => owner.GetNestedType(name, BindingFlags.NonPublic) ?? throw new InvalidOperationException("missing nested type " + name);

    private static MethodInfo RequireMethod(Type owner, string name)
        => owner.GetMethod(name, InstancePrivate) ?? throw new InvalidOperationException("missing method " + name);

    private static void Require(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
