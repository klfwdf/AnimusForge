using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

internal static class WorldEventInboxOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly assembly)
    {
        Type behaviorType = assembly.GetType("AnimusForge.AnimusForgeWorldEventBehavior", true);
        Type entryType = assembly.GetType("AnimusForge.AnimusForgeWorldEventInboxEntry", true);
        Activator.CreateInstance(behaviorType);

        object first = Entry(entryType, "first", "same-event", "Original", 1);
        Upsert(behaviorType, first, true);
        long insertedVersion = Version(behaviorType);
        Require(insertedVersion > 0 && UnreadCount(behaviorType) == 1, "first publication is unread and acknowledged");
        Require((bool)StaticCall(behaviorType, "MarkEventReadForExternal", "first"), "first event can be read");
        Require(UnreadCount(behaviorType) == 0, "read removes unread count");

        object retry = Entry(entryType, "retry-id", "same-event", "Updated", 2);
        Upsert(behaviorType, retry, true);
        IList snapshot = Snapshot(behaviorType, 80);
        Require(Version(behaviorType) > insertedVersion, "idempotent publication still acknowledges policy caller");
        Require(snapshot.Count == 1, "stable key owns one inbox record despite a different event ID");
        Require((string)Property(snapshot[0], "EventId") == "first", "stable-key retry keeps original UI identity");
        Require((string)Property(snapshot[0], "Title") == "Updated", "stable-key retry refreshes projection");
        Require((bool)Property(snapshot[0], "IsRead") && UnreadCount(behaviorType) == 0,
            "retry never resurrects an already-read event");

        Upsert(behaviorType, Entry(entryType, "quiet", "quiet-event", "Quiet", 3), false);
        snapshot = Snapshot(behaviorType, 80);
        Require(snapshot.Count == 2 && UnreadCount(behaviorType) == 0, "non-notifying insert stays out of unread set");
        foreach (object item in snapshot)
        {
            if ((string)Property(item, "EventId") == "quiet")
                Require((bool)Property(item, "IsRead"), "non-notifying insert has coherent read projection");
        }

        Type ownerType = assembly.GetType("AnimusForge.WorldEventInboxOwner", true);
        object imported = Activator.CreateInstance(ownerType, true);
        var stored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["old"] = JsonSerializer.Serialize(new { EventId = "old", StableKey = "legacy-key", Title = "Old", Day = 1, CreatedUtcTicks = 1L, IsRead = false }),
            ["new"] = JsonSerializer.Serialize(new { EventId = "new", StableKey = "legacy-key", Title = "New", Day = 2, CreatedUtcTicks = 2L, IsRead = true }),
            ["bad"] = "{not json"
        };
        InstanceCall(imported, "Import", stored, new List<string> { "old" });
        IList loaded = (IList)InstanceCall(imported, "Snapshot", 80);
        Require(loaded.Count == 1 && (string)Property(loaded[0], "EventId") == "new", "load keeps newest stable-key record and ignores bad JSON");
        Require(!(bool)Property(loaded[0], "IsRead") && (int)Property(imported, "UnreadCount") == 1,
            "load transfers unread state from an older duplicate to the surviving record");
        var exported = (Dictionary<string, string>)InstanceCall(imported, "ExportRecords");
        Require(exported.Count == 1 && exported.ContainsKey("new"), "export retains existing EventId save shape");

        for (int i = 0; i < 245; i++)
            Upsert(behaviorType, Entry(entryType, "capacity-" + i, "capacity-key-" + i, "Capacity", 100 + i), true);
        Require(UnreadCount(behaviorType) <= 240 && !(bool)StaticCall(behaviorType, "MarkEventReadForExternal", "first"),
            "capacity trims old records and their unread state");
        StaticCall(behaviorType, "MarkAllReadForExternal");
        Require(UnreadCount(behaviorType) == 0, "mark-all clears unread state");
        Console.WriteLine("PASS WorldEventInboxOwnerReplay dedup/read/version/load/capacity; live/save=NOT_RUN");
    }

    private static object Entry(Type entryType, string id, string stableKey, string title, int day)
    {
        object entry = Activator.CreateInstance(entryType);
        entryType.GetProperty("EventId").SetValue(entry, id);
        entryType.GetProperty("StableKey").SetValue(entry, stableKey);
        entryType.GetProperty("Title").SetValue(entry, title);
        entryType.GetProperty("Day").SetValue(entry, day);
        entryType.GetProperty("CreatedUtcTicks").SetValue(entry, (long)day);
        return entry;
    }

    private static void Upsert(Type behaviorType, object entry, bool markUnread) => StaticCall(behaviorType, "UpsertWorldEventForExternal", entry, markUnread);
    private static long Version(Type behaviorType) => (long)StaticCall(behaviorType, "GetInboxVersionForExternal");
    private static int UnreadCount(Type behaviorType) => (int)StaticCall(behaviorType, "GetUnreadCountForExternal");
    private static IList Snapshot(Type behaviorType, int count) => (IList)StaticCall(behaviorType, "GetInboxSnapshotForExternal", count);
    private static object StaticCall(Type type, string name, params object[] args) => type.GetMethod(name, Members).Invoke(null, args);
    private static object InstanceCall(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, Members).Invoke(instance, args);
    private static object Property(object instance, string name) => instance.GetType().GetProperty(name, Members).GetValue(instance);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("WorldEventInboxOwnerReplay: " + message);
    }
}
