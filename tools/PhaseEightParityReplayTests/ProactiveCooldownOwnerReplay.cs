using System;
using System.Collections.Generic;
using System.Reflection;

internal static class ProactiveCooldownOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly)
    {
        Type hostType = assembly.GetType("AnimusForge.ProactiveNpcRequestBehavior", true);
        Type type = hostType.GetNestedType("ProactiveRequestCooldownOwner", BindingFlags.NonPublic);
        if (type == null || hostType.GetField("_cooldownOwner", Members)?.FieldType != type)
            throw new InvalidOperationException("Proactive cooldown: production host does not own a single cooldown state");
        object owner = Activator.CreateInstance(type, true);
        object Call(string name, params object[] args) => type.GetMethod(name, Members).Invoke(owner, args);
        object Read(string name) => type.GetProperty(name, Members).GetValue(owner);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Proactive cooldown: " + label); }

        var hero = new Dictionary<string, float> { [" hero-a "] = 3f };
        var need = new Dictionary<string, float> { [" foodshortage "] = 5f };
        var discussion = new Dictionary<string, float> { [" topic-a "] = 4f };
        Call("Import", hero, need, discussion, 9f, 2f);
        hero.Clear(); need.Clear(); discussion.Clear();
        Check((bool)Call("IsHeroOnCooldown", "HERO-A", 2f), "legacy trimmed/case-folded hero key survives detached import");
        Check(!(bool)Call("IsHeroOnCooldown", "hero-a", 3f), "hero cooldown expires at equality");
        Check((float)Call("GetNeedRemainingDays", "FOODSHORTAGE", 3f) == 2f, "legacy fatigue is read without losing duration");
        Check((bool)Call("IsDiscussionOnCooldown", "TOPIC-A", 3f), "discussion key survives import");
        Check(!(bool)Call("TryBeginScan", 2.5f, 1), "scan is throttled before interval");
        Check((bool)Call("TryBeginScan", 3f, 1), "scan runs at interval boundary");
        Check((float)Read("LastScanHour") == 3f, "scan stores last start time");
        Check(!(bool)Call("TryBeginScan", 3f, 1), "same hour cannot rescan");
        Check((bool)Call("IsGlobalCooldownActive", 8f), "global cooldown blocks before boundary");
        Check(!(bool)Call("IsGlobalCooldownActive", 9f), "global cooldown ends at boundary");
        Call("StartGlobalCooldown", 10f, 2);
        Check((float)Read("GlobalCooldownUntilHours") == 12f, "start records absolute cooldown hour");

        Call("RecordHeroCooldown", "hero-b", 10f, 2);
        Check((bool)Call("IsHeroOnCooldown", "hero-b", 11f), "completed hero is cooled");
        Call("RecordNeedFatigue", "FoodShortage", 10f, 2);
        Check((float)Call("GetNeedRemainingDays", "foodshortage", 11f) == 1f, "need fatigue is recorded");
        Call("RecordNeedFatigue", "FoodShortage", 11f, 0);
        Check((float)Call("GetNeedRemainingDays", "foodshortage", 11f) == 0f, "disabled fatigue clears old value");
        Call("RecordDiscussion", "topic-b", 10f, 7);
        Check((bool)Call("IsDiscussionOnCooldown", "topic-b", 16f), "discussion retention is recorded");

        var many = new Dictionary<string, float>();
        for (int i = 0; i < 258; i++) many["topic-" + i] = i + 1;
        Call("Import", null, new Dictionary<string, float> { ["expired"] = 2f, ["live"] = 4f }, many, 0f, -99999f);
        Call("PruneExpiredNeedTypeFatigue", 2f);
        Check((float)Call("GetNeedRemainingDays", "expired", 1f) == 0f, "expired need entry is removed");
        Check((float)Call("GetNeedRemainingDays", "live", 2f) == 2f, "live need entry remains");
        Call("PruneExpiredDiplomacyDiscussionKeys", 0f);
        var kept = (Dictionary<string, float>)Read("DiplomacyDiscussionKeysUntilDays");
        Check(kept.Count == 256 && !kept.ContainsKey("topic-0") && !kept.ContainsKey("topic-1") && kept.ContainsKey("topic-257"), "oldest discussion entries trimmed at 256");
        Call("ResetDictionaries");
        Check(((Dictionary<string, float>)Read("HeroCooldownUntilDays")).Count == 0
            && ((Dictionary<string, float>)Read("NeedTypeFatigueUntilDays")).Count == 0
            && ((Dictionary<string, float>)Read("DiplomacyDiscussionKeysUntilDays")).Count == 0,
            "load failure resets dictionaries");
        Console.WriteLine("PASS proactiveCooldownOwnerReplay import=1 scan=1 global=1 hero=1 fatigue=1 discussion=1 prune=1 cap=1 reset=1; no live campaign acceptance");
    }
}
