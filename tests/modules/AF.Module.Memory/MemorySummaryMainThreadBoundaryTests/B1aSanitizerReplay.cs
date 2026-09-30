using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

internal static class B1aSanitizerReplay
{
    private const BindingFlags Fields = BindingFlags.Public | BindingFlags.Instance;
    private const BindingFlags Methods = BindingFlags.Static | BindingFlags.NonPublic;
    private static int _checks;

    internal static void Run(Assembly assembly)
    {
        Type rules = Require(assembly, "AnimusForge.MemoryRecordRules");
        Type weekly = Require(assembly, "AnimusForge.WeeklyMemoryMaterialTrigger");
        Type draft = Require(assembly, "AnimusForge.DailyMemoryDraft");
        Type line = Require(assembly, "AnimusForge.DailyMemoryLine");
        Type block = Require(assembly, "AnimusForge.CompressedMemoryBlock");
        Type overview = Require(assembly, "AnimusForge.MemoryOverviewState");
        Type major = Require(assembly, "AnimusForge.MajorActionSummaryJob");
        Type parallel = Type.GetType("TaleWorlds.Library.TWParallel, TaleWorlds.Library");
        MethodInfo isMainThread = parallel.GetMethod("IsMainThread", BindingFlags.Public | BindingFlags.Static);
        Check((bool)isMainThread.Invoke(null, null), "offline main thread recognized");
        bool workerRecognized = !Task.Run(() => (bool)isMainThread.Invoke(null, null)).GetAwaiter().GetResult();

        object legacy = New(weekly);
        Set(legacy, "MemoryId", " HERO ");
        Set(legacy, "FootholdKingdomId", " kingdom ");
        Set(legacy, "Tags", new List<string> { "[ACTION:DUEL]", "[action:duel]" });
        Set(legacy, "OutcomeReceiptId", "bad-digest");
        IList weeklyInput = List(weekly, legacy);
        IList mainWeekly = Sanitize(rules, "SanitizeWeeklyMemoryMaterialTriggers", weeklyInput);
        Check(mainWeekly.Count == 1 && ReferenceEquals(mainWeekly[0], legacy), "main-thread weekly identity");
        Check((string)Get(legacy, "MemoryId") == "hero", "main-thread weekly normalization");
        Check(((IList)Get(legacy, "Tags")).Count == 1, "weekly tag casefold dedupe");
        Check((string)Get(legacy, "OutcomeReceiptId") == "", "bad legacy provenance cleared");
        Check(((string)Get(legacy, "StableKey")).StartsWith("weekly_memory_trigger:"), "legacy stable key generated");

        object workerWeekly = New(weekly);
        Set(workerWeekly, "MemoryId", " WORKER ");
        Set(workerWeekly, "FootholdKingdomId", " kingdom ");
        Set(workerWeekly, "Tags", new List<string> { "[ACTION:DUEL]" });
        IList workerWeeklyOutput = Task.Run(() => Sanitize(rules, "SanitizeWeeklyMemoryMaterialTriggers", List(weekly, workerWeekly))).GetAwaiter().GetResult();
        if (workerRecognized)
        {
            Check(workerWeeklyOutput.Count == 1 && !ReferenceEquals(workerWeeklyOutput[0], workerWeekly), "worker weekly detached");
            Check((string)Get(workerWeekly, "MemoryId") == " WORKER " && (string)Get(workerWeeklyOutput[0], "MemoryId") == "worker", "worker weekly source unchanged");
        }

        object daily = New(draft), dailyLine = New(line);
        Set(daily, "HeroId", " DAILY "); Set(daily, "GameDayIndex", 7); Set(daily, "GameDate", "date");
        Set(dailyLine, "Text", " text "); Set(dailyLine, "GameHour", 99);
        Set(dailyLine, "MemoryCommitId", "bad"); Set(dailyLine, "MemoryCommitPart", "bad"); Set(dailyLine, "MemoryCommitHash", "bad");
        ((IList)Get(daily, "Lines")).Add(dailyLine);
        IList dailyResult = Sanitize(rules, "SanitizeDailyMemoryDrafts", List(draft, daily));
        Check(dailyResult.Count == 1 && ReferenceEquals(dailyResult[0], daily), "main-thread daily identity");
        Check(ReferenceEquals(((IList)Get(daily, "Lines"))[0], dailyLine), "main-thread daily line identity");
        Check((int)Get(dailyLine, "GameHour") == 23 && (string)Get(dailyLine, "Text") == "text", "daily line bounds/trim");
        Check((string)Get(dailyLine, "MemoryCommitId") == "", "invalid marker cleared");
        object detachedDaily = New(draft), detachedLine = New(line);
        Set(detachedDaily, "HeroId", " WORKER "); Set(detachedDaily, "GameDayIndex", 9);
        Set(detachedLine, "Text", " text "); ((IList)Get(detachedDaily, "Lines")).Add(detachedLine);
        IList workerDaily = Task.Run(() => Sanitize(rules, "SanitizeDailyMemoryDrafts", List(draft, detachedDaily))).GetAwaiter().GetResult();
        if (workerRecognized)
        {
            Check(workerDaily.Count == 1 && !ReferenceEquals(workerDaily[0], detachedDaily), "worker daily detached");
            Check(!ReferenceEquals(((IList)Get(workerDaily[0], "Lines"))[0], detachedLine), "worker daily line detached");
            Check((string)Get(detachedDaily, "HeroId") == " WORKER " && (string)Get(detachedLine, "Text") == " text ", "worker daily source unchanged");
        }

        object memoryBlock = New(block);
        Set(memoryBlock, "HeroId", " BLOCK "); Set(memoryBlock, "GameDayIndex", 2); Set(memoryBlock, "Summary", " summary ");
        Set(memoryBlock, "Scenes", new List<string> { "Town", "town" });
        IList blockResult = Sanitize(rules, "SanitizeCompressedMemoryBlocks", List(block, memoryBlock));
        Check(blockResult.Count == 1 && ReferenceEquals(blockResult[0], memoryBlock), "main-thread block identity");
        Check((string)Get(memoryBlock, "Id") == "block:2" && ((IList)Get(memoryBlock, "Scenes")).Count == 1, "block id/scenes");
        object workerBlock = New(block);
        Set(workerBlock, "HeroId", " WORKER "); Set(workerBlock, "GameDayIndex", 3); Set(workerBlock, "Summary", " text ");
        IList workerBlocks = Task.Run(() => Sanitize(rules, "SanitizeCompressedMemoryBlocks", List(block, workerBlock))).GetAwaiter().GetResult();
        if (workerRecognized)
        {
            Check(workerBlocks.Count == 1 && !ReferenceEquals(workerBlocks[0], workerBlock), "worker block detached");
            Check((string)Get(workerBlock, "Summary") == " text " && (string)Get(workerBlocks[0], "Summary") == "text", "worker block source unchanged");
        }

        object state = New(overview);
        Set(state, "HeroId", " OVERVIEW "); Set(state, "Summary", "a\rb");
        Set(state, "IncludedBlockIds", new List<string> { "One", "one" });
        object sanitizedState = rules.GetMethod("SanitizeMemoryOverviewState", Methods).Invoke(null, new[] { state });
        Check(ReferenceEquals(state, sanitizedState) && (string)Get(state, "Summary") == "ab", "overview state in-place");
        Check(((IList)Get(state, "IncludedBlockIds")).Count == 1, "overview ids casefold dedupe");
        object job = New(major); Set(job, "HeroId", " JOB "); Set(job, "RetryCount", 99);
        IList jobs = Sanitize(rules, "SanitizeMajorActionSummaryQueue", List(major, job));
        Check(jobs.Count == 1 && ReferenceEquals(jobs[0], job) && (int)Get(job, "RetryCount") == 3, "major job in-place retry clamp");
        Console.WriteLine("PASS B1a sanitizer checks=" + _checks + " main=1 worker=" + (workerRecognized ? "1" : "NOT_RUN_TWParallel_offline") + " badInput=1");
    }

    private static Type Require(Assembly assembly, string name) => assembly.GetType(name, false) ?? throw new Exception("missing " + name);
    private static object New(Type type) => Activator.CreateInstance(type, true);
    private static object Get(object item, string name) => item.GetType().GetField(name, Fields).GetValue(item);
    private static void Set(object item, string name, object value) => item.GetType().GetField(name, Fields).SetValue(item, value);
    private static IList List(Type itemType, params object[] items)
    {
        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
        foreach (object item in items) list.Add(item);
        return list;
    }
    private static IList Sanitize(Type rules, string method, IList input) => (IList)rules.GetMethod(method, Methods).Invoke(null, new object[] { input });
    private static void Check(bool ok, string message) { _checks++; if (!ok) throw new Exception("FAIL " + message); }
}
