using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AnimusForge;

// Storage/game boundaries only. Product methods are extracted without changes by the runner.
internal sealed class IDataStore
{
    public bool IsSaving;
    public string Json;
}
internal static class CampaignSaveChunkHelper
{
    public static void SaveChunkedString(IDataStore store, string key, string json, string category) => store.Json = json;
    public static string LoadChunkedString(IDataStore store, string key, string category) => store.Json;
}
internal static class Logger { public static void Log(string category, string message) { } }

public partial class MyBehavior
{
    private sealed class WorldBulletinDeathSnapshot { }
    private sealed class EventRecordEntry
    {
        public string EventId;
        public string EventKind = "world";
        public int CreatedDay;
    }
    private List<EventRecordEntry> _eventRecordEntries = new List<EventRecordEntry>();
    private static int GetCurrentGameDayIndexSafe() => 100;
    private static int _checks;
    private static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception("FAIL " + name);
        _checks++;
        Console.WriteLine("PASS " + name);
    }
    private static EventRecordEntry Record(int seq, int day, string kind = "world") => new EventRecordEntry
    {
        EventId = "weekly_report:" + kind + ":bulletin:" + seq + ":" + day,
        EventKind = kind, CreatedDay = day
    };
    public static int Main()
    {
        var h = new MyBehavior();
        h.EnsureWorldBulletinState().World.Sequence = 10;
        var old = Record(1, 10);
        var recent = Record(9, 20);
        var newest = Record(10, 20);
        h._eventRecordEntries.AddRange(new[] { old, newest, recent, Record(50, 90, "kingdom") });
        Check(ReferenceEquals(h.FindLatestWorldBulletinRecord(), newest), "unsorted records: numeric issue breaks same-day tie");
        h._eventRecordEntries = h._eventRecordEntries.OrderByDescending(x => x.CreatedDay).ToList();
        h.ResetWorldBulletinTransientState();
        Check(ReferenceEquals(h.FindLatestWorldBulletinRecord(), newest), "load/newest-first order returns newest world issue");
        h._eventRecordEntries.Reverse();
        Check(ReferenceEquals(h.FindLatestWorldBulletinRecord(), newest), "in-place reorder invalidates moved cached index");
        var appended = Record(11, 21);
        h._eventRecordEntries.Add(appended);
        Check(ReferenceEquals(h.FindLatestWorldBulletinRecord(), appended), "append invalidates cached selection");
        h._eventRecordEntries.Remove(appended);
        Check(ReferenceEquals(h.FindLatestWorldBulletinRecord(), newest), "deleted newest falls back to previous issue");
        h._eventRecordEntries = new List<EventRecordEntry> { Record(20, 30) };
        Check(h.FindLatestWorldBulletinRecord().CreatedDay == 30, "replaced list invalidates cache");
        h._eventRecordEntries.Clear();
        Check(h.FindLatestWorldBulletinRecord() == null && h.FindLatestWorldBulletinRecord() == null, "empty archive caches no result");
        h._eventRecordEntries.Add(old);
        Check(ReferenceEquals(h.FindLatestWorldBulletinRecord(), old), "first append after cached miss is found");

        const string broken = "{\"Events\":[坏档😀";
        var store = new IDataStore { Json = broken };
        h.SyncWorldBulletinData(store);
        store.IsSaving = true;
        h.SyncWorldBulletinData(store);
        Check(store.Json == broken, "uninitialized bad state round-trips exact raw input");
        // Same initializer the hourly tick calls, before any facts are captured.
        h.EnsureWorldBulletinState();
        h.SyncWorldBulletinData(store);
        var restored = JsonConvert.DeserializeObject<WorldBulletinSaveState>(store.Json);
        Check(restored.PreservedUnreadableState == broken, "hourly initialization keeps exact recovery payload");
        h._worldBulletinState.Events.Add(new WorldBulletinEvent { Key = "new-event", Sentence = "新事实" });
        h._worldBulletinState.World.Sequence = 12;
        h.SyncWorldBulletinData(store);
        store.IsSaving = false;
        var reloaded = new MyBehavior();
        reloaded.SyncWorldBulletinData(store);
        Check(reloaded._worldBulletinCorruptRaw == null, "recovered state is valid JSON on next load");
        Check(reloaded._worldBulletinState.PreservedUnreadableState == broken, "recovery payload survives successful reload");
        Check(reloaded._worldBulletinState.Events.Single().Key == "new-event" && reloaded._worldBulletinState.World.Sequence == 12, "new facts and sequence survive beside recovery payload");
        store.IsSaving = true;
        reloaded.SyncWorldBulletinData(store);
        Check(JsonConvert.DeserializeObject<WorldBulletinSaveState>(store.Json).PreservedUnreadableState == broken, "subsequent save still keeps recovery payload");
        store.IsSaving = false;
        store.Json = "{\"Events\":[],\"World\":{\"Sequence\":3}}";
        reloaded.SyncWorldBulletinData(store);
        Check(reloaded.EnsureWorldBulletinState().World.Sequence == 3 && reloaded._worldBulletinState.PreservedUnreadableState == null, "old valid saves need no migration and retain no prior bad payload");
        reloaded.ResetWorldBulletinForRuntime("new_game_created");
        Check(reloaded.EnsureWorldBulletinState().PreservedUnreadableState == null, "new campaign carries no recovery payload");
        bool enabled = true;
        var news = new WorldBulletinStateOwner();
        news.Bind(new WorldBulletinPort {
            Enabled = () => enabled, CurrentDay = () => 100, CurrentHour = () => 2400,
            Render = s => s, Focus = () => new WorldBulletinFocus(), Log = (a, b) => {} });
        foreach (string kind in new[] { "ruler_policy", "noble_gathering", "tournament_finished", "diplomatic_declaration" })
            news.CaptureCivilNewsMaterial(kind, kind, "已记录的" + kind, "事实细节", false, "a", "b");
        var newsState = news.EnsureWorldBulletinState();
        Check(newsState.Events.Count == 4 && newsState.Events.All(e => e.KingdomIds.Count == 2),
            "policy, gathering, tournament and declaration enter bulletin candidates");
        for (int i = 0; i < 70; i++) news.CaptureCivilNewsMaterial("ruler_policy", "filler:" + i, "后续事件", "细节", false, "a");
        news.CaptureCivilNewsMaterial("diplomatic_declaration", "diplomatic_declaration", "更新的公开主张", "仅发布", false, "a", "b");
        Check(newsState.Events.Count == 74 && newsState.Events[3].Sentence == "更新的公开主张",
            "declaration projections update one fact even beyond recent-tail deduplication");
        newsState.World.CutoffHour = 2400;
        news.CaptureCivilNewsMaterial("diplomatic_declaration", "diplomatic_declaration", "迟到投影", "新细节", false, "a");
        Check(newsState.Events[3].Sentence == "更新的公开主张", "published declaration is not changed by late projection");
        enabled = false;
        news.CaptureCivilNewsMaterial("ruler_policy", "disabled", "模式关闭", "细节", false, "a");
        Check(newsState.Events.Count == 74, "weekly mode does not add bulletin facts");
        Console.WriteLine("ALL PASS " + _checks + " host checks (fake storage/game boundary)");
        return 0;
    }
}
