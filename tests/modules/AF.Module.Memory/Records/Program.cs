using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

// Contract for the J05a Memory record ledgers, compiled directly from the production owner files.
internal static class Program
{
    private static int _checks;
    private static void Check(bool c, string m) { _checks++; if (!c) throw new Exception("FAIL: " + m); }

    private sealed class Entry { public int Day, Order, Sequence; public string GameDate, Text, StableKey; }
    private sealed class Day { public int Index; public string Date; public List<string> Lines = new List<string>(); }

    private static void Main()
    {
        ActionLedger();
        HistoryLedger();
        Console.WriteLine("PASS memory-records checks=" + _checks);
    }

    private static int Cmp(Entry a, Entry b) => NpcActionLedger.CompareTimeline(a.Day, a.Sequence, a.Order, a.GameDate, b.Day, b.Sequence, b.Order, b.GameDate);

    private static void ActionLedger()
    {
        Check(NpcActionLedger.NormalizeText(" a\r\nb ") == "a  b", "text newlines collapse to spaces");
        Check(NpcActionLedger.NormalizeStableKey(" K\n", "x") == "k" && NpcActionLedger.NormalizeStableKey(null, "  Fallback ") == "fallback" && NpcActionLedger.NormalizeStableKey(" ", " ") == "", "stable key normalization");
        Check(NpcActionLedger.RecentWindowMinimumDay(20) == 11 && NpcActionLedger.RecentWindowDays == 10 && NpcActionLedger.MaxRecentEntriesPerHero == 96 && NpcActionLedger.MaxMajorEntriesPerHero == 160, "constants");

        var list = new List<Entry> { new Entry { Day = 5, Text = "old" }, null, new Entry { Day = 12, Text = " " }, new Entry { Day = 15, Text = "keep" } };
        Check(NpcActionLedger.RemoveInvalid(list, 11, true, e => e.Text, e => e.Day) && list.Count == 1 && list[0].Text == "keep", "recent window drops old/null/blank");
        var major = new List<Entry> { new Entry { Day = 1, Text = "a" } };
        Check(!NpcActionLedger.RemoveInvalid(major, 100, false, e => e.Text, e => e.Day) && major.Count == 1, "major list ignores window");
        Check(!NpcActionLedger.RemoveInvalid<Entry>(null, 0, true, e => e.Text, e => e.Day) && !NpcActionLedger.RemoveInvalid(new List<Entry>(), 0, true, e => e.Text, e => e.Day), "empty inputs");

        var entries = new List<Entry> { new Entry { Day = 3, StableKey = "k1", Text = "t1", Order = 2 }, new Entry { Day = 3, StableKey = "k2", Text = "t2", Order = 5 }, new Entry { Day = 4, StableKey = "k3", Text = "t3", Order = 1 } };
        Check(NpcActionLedger.ContainsStableKey(entries, "K1", e => e.StableKey) && !NpcActionLedger.ContainsStableKey(entries, "k9", e => e.StableKey) && !NpcActionLedger.ContainsStableKey<Entry>(null, "k1", e => e.StableKey), "stable key lookup case-insensitive");
        Check(NpcActionLedger.ContainsForDay(entries, 3, "zz", "t2", e => e.Day, e => e.StableKey, e => e.Text) && NpcActionLedger.ContainsForDay(entries, 3, "K2", "zz", e => e.Day, e => e.StableKey, e => e.Text) && !NpcActionLedger.ContainsForDay(entries, 4, "k1", "t1", e => e.Day, e => e.StableKey, e => e.Text), "same-day dedupe by key or exact text");
        Check(NpcActionLedger.NextOrder(entries, 3, e => e.Day, e => e.Order) == 6 && NpcActionLedger.NextOrder(entries, 9, e => e.Day, e => e.Order) == 1 && NpcActionLedger.NextOrder<Entry>(null, 3, e => e.Day, e => e.Order) == 1, "next order per day");

        Check(Cmp(new Entry { Day = 1 }, new Entry { Day = 2 }) < 0, "day first");
        Check(Cmp(new Entry { Day = 1, Sequence = 5 }, new Entry { Day = 1, Sequence = 0 }) < 0, "known sequence before unknown");
        Check(Cmp(new Entry { Day = 1, Sequence = 0, Order = 2 }, new Entry { Day = 1, Sequence = 0, Order = 3 }) < 0, "order when sequences unknown");
        Check(Cmp(new Entry { Day = 1, Order = 1, GameDate = "b" }, new Entry { Day = 1, Order = 1, GameDate = "a" }) > 0, "date text last");

        var timeline = new List<Entry> { new Entry { Day = 1, Sequence = 1 }, new Entry { Day = 2, Sequence = 2 } };
        NpcActionLedger.Append(timeline, new Entry { Day = 3, Sequence = 3 }, 0, Cmp);
        Check(timeline.Select(e => e.Day).SequenceEqual(new[] { 1, 2, 3 }), "in-order append does not sort");
        NpcActionLedger.Append(timeline, new Entry { Day = 0, Sequence = 0 }, 0, Cmp);
        Check(timeline.Select(e => e.Day).SequenceEqual(new[] { 0, 1, 2, 3 }), "out-of-order append sorts");
        NpcActionLedger.Append(timeline, new Entry { Day = 4, Sequence = 4 }, 3, Cmp);
        Check(timeline.Select(e => e.Day).SequenceEqual(new[] { 2, 3, 4 }), "cap removes oldest from front");
    }

    private static void HistoryLedger()
    {
        Check(DialogueHistoryLedger.TagSceneSession(" x ", 7) == "[AF_SCENE_SESSION:7] x" && DialogueHistoryLedger.TagSceneSession("x", -1) == "x" && DialogueHistoryLedger.TagSceneSession("  ", 3) == "", "scene session tagging");
        Check(DialogueHistoryLedger.TryStripSceneSessionMarker(" [AF_SCENE_SESSION:12]  body ", out string body, out int sid) && sid == 12 && body == "body", "strip marker: outer trim then TrimStart after marker");
        Check(!DialogueHistoryLedger.TryStripSceneSessionMarker("plain", out body, out sid) && sid == -1 && body == "plain", "no marker");
        Check(!DialogueHistoryLedger.TryStripSceneSessionMarker("[AF_SCENE_SESSION:]x", out body, out sid) && sid == -1, "empty id rejected");
        Check(!DialogueHistoryLedger.TryStripSceneSessionMarker("[AF_SCENE_SESSION:ab]x", out body, out sid) && sid == -1, "non-numeric id rejected");

        Check(DialogueHistoryLedger.NormalizeAfefFact(" fact ") == "[AFEF玩家行为补充] fact" && DialogueHistoryLedger.NormalizeAfefFact("[AFEF NPC行为补充] x") == "[AFEF NPC行为补充] x" && DialogueHistoryLedger.NormalizeAfefFact(" [AFEF玩家行为补充] y") == "[AFEF玩家行为补充] y", "afef prefixing");
        Check(DialogueHistoryLedger.NormalizeNpcLine("阿尔文", " 你好 ") == "阿尔文: 你好" && DialogueHistoryLedger.NormalizeNpcLine("阿尔文", "[场景喊话] 喊") == "[场景喊话] 喊", "npc line prefixing");

        Func<string, bool> firstMeeting = b => b.StartsWith("初次");
        Check(DialogueHistoryLedger.IsSingleUseNpcFactLine("[AFEF NPC行为补充] 初次见面", firstMeeting), "first meeting fact is single-use");
        Check(DialogueHistoryLedger.IsSingleUseNpcFactLine("[AF_SCENE_SESSION:1] [AFEF NPC行为补充] 今天稍早时候刚与你见过面。", firstMeeting), "earlier-today fact under scene marker");
        Check(DialogueHistoryLedger.IsSingleUseNpcFactLine("[AFEF NPC行为补充] 距离你上次与他见面，已有3天了。", firstMeeting), "days-since fact");
        Check(!DialogueHistoryLedger.IsSingleUseNpcFactLine("[AFEF NPC行为补充] 其他事实", firstMeeting) && !DialogueHistoryLedger.IsSingleUseNpcFactLine("[AFEF玩家行为补充] 初次", firstMeeting) && !DialogueHistoryLedger.IsSingleUseNpcFactLine("[AFEF NPC行为补充]", firstMeeting), "non single-use facts");

        Func<string, bool> single = l => l.Contains("SINGLE");
        Func<string, bool> convo = l => l.Contains("TALK");
        var lines = new List<(int, string, string)> { (1, "d1", "SINGLE-old"), (1, "d1", "TALK-1"), (2, "d2", "SINGLE-new"), (2, "d2", "other") };
        var kept = DialogueHistoryLedger.ExpireSingleUseFacts(lines, single, convo);
        Check(kept != null && kept.Select(k => k.Line).SequenceEqual(new[] { "TALK-1", "SINGLE-new", "other" }), "single-use facts older than the newest conversation are removed; newer ones kept: " + (kept == null ? "null" : string.Join(",", kept.Select(k => k.Line))));
        Check(DialogueHistoryLedger.ExpireSingleUseFacts(new List<(int, string, string)> { (1, "d", "SINGLE"), (1, "d", "x") }, single, convo) == null, "no conversation → nothing removed");
        Check(DialogueHistoryLedger.ExpireSingleUseFacts(null, single, convo) == null && DialogueHistoryLedger.ExpireSingleUseFacts(new List<(int, string, string)>(), single, convo) == null, "empty inputs");

        var days = new List<Day> { new Day { Index = 1, Date = "d1", Lines = new List<string> { "a", " ", "b" } }, null, new Day { Index = 2, Date = "d2", Lines = null }, new Day { Index = 3, Date = "d3", Lines = new List<string> { "c" } } };
        var flat = DialogueHistoryLedger.Flatten(days, d => d.Index, d => d.Date, d => d.Lines);
        Check(flat.Select(f => f.Line).SequenceEqual(new[] { "a", "b", "c" }), "flatten skips null days/lines/blank");
        Check(DialogueHistoryLedger.TrimToNewest(flat, 2).Select(f => f.Line).SequenceEqual(new[] { "b", "c" }) && DialogueHistoryLedger.TrimToNewest(flat, 10).Count == 3 && DialogueHistoryLedger.TrimToNewest(null, 1).Count == 0, "trim to newest");
        var regrouped = DialogueHistoryLedger.Regroup(new List<(int, string, string)> { (1, "d1", "x"), (2, "d2", "y"), (1, "d1", "z") }, (i, d) => new Day { Index = i, Date = d }, d => d.Index, d => d.Lines);
        Check(regrouped.Count == 2 && regrouped[0].Index == 1 && regrouped[0].Lines.SequenceEqual(new[] { "x", "z" }) && regrouped[1].Lines.SequenceEqual(new[] { "y" }), "regroup by first-seen day");
        Check(DialogueHistoryLedger.MaxLines == 260, "line cap");
    }
}
