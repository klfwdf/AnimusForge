using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Context;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator.UI.Patches;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
    public static void Main()
    {
        var context = new WeeklyReportVisualContext();
        int notifications = 0;
        BulletinIllustrationPreloader.Updated = (key, result) => notifications++;
        string key = BulletinIllustrationPreloader.KeyFor("issue", "title", "date", "body");
        Check(key != BulletinIllustrationPreloader.KeyFor("issue", "title", "date", "different"), "Content changes key");
        Check(key != BulletinIllustrationPreloader.KeyFor("next", "title", "date", "body"), "Issue changes key");
        var first = BulletinIllustrationPreloader.Ensure(key, context, published: true);
        Check(first.Pending && first.Scope.CampaignOwned && DiskImageCacheManager.Loads == 0, "Publication starts fresh campaign job");
        Check(ReferenceEquals(first, BulletinIllustrationPreloader.Ensure(key, context)), "Open joins pending");
        Check(ReferenceEquals(first, BulletinIllustrationPreloader.Ensure(key, context, published: true)), "Duplicate publication joins");
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == 1, "One generation");
        var result = WeeklyReportPopupIllustrationPatch.Success();
        WeeklyReportPopupIllustrationPatch.Requests[0](result);
        Check(first.Ready && !first.Pending && first.Scope.Closed && result.Saved.ImageData == null, "Ready releases scope and bytes");
        BulletinIllustrationPreloader.Ensure(key, context);
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == 1, "Reopen ready never regenerates");
        var redraw = BulletinIllustrationPreloader.Ensure(key, context, true);
        Check(redraw.Pending && redraw.Attempt == 2, "Explicit redraw");
        BulletinIllustrationPreloader.Ensure(key, context, true);
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == 2, "Duplicate redraw coalesces");
        WeeklyReportPopupIllustrationPatch.Requests[1](new WeeklyReportPopupIllustrationPatch.GenerationResult { Result = new ImageGenerationResult { ErrorMessage = "fixture fail" } });
        Check(!redraw.Ready && !redraw.Pending && redraw.Status.Contains("fixture fail"), "Failed request visible");
        BulletinIllustrationPreloader.Ensure(key, context);
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == 2, "No automatic paid retry");
        var retry = BulletinIllustrationPreloader.Ensure(key, context, true);
        var oldCompletion = WeeklyReportPopupIllustrationPatch.Requests[2];
        BulletinIllustrationPreloader.Reset();
        Check(retry.Scope.Closed && BulletinIllustrationPreloader.Find(key) == null, "Campaign reset cancels and clears");
        var current = BulletinIllustrationPreloader.Ensure(key, context, published: true);
        int before = notifications;
        oldCompletion(WeeklyReportPopupIllustrationPatch.Success());
        Check(ReferenceEquals(current, BulletinIllustrationPreloader.Find(key)) && current.Pending && notifications == before, "Late old completion ignored");
        WeeklyReportPopupIllustrationPatch.Requests[3](WeeklyReportPopupIllustrationPatch.Success());
        BulletinIllustrationPreloader.MarkDeleted(key);
        Check(!current.Ready && !current.Pending, "Delete invalidates prepared state");
        BulletinIllustrationPreloader.Ensure(key, context);
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == 4, "Deleted picture not silently regenerated");
        BulletinIllustrationPreloader.Reset();
        DiskImageCacheManager.Cached = new CachedIllustrationItem { ImageData = new byte[] { 1 }, Prompt = "saved" };
        var cached = BulletinIllustrationPreloader.Ensure(key, context);
        Check(cached.Ready && WeeklyReportPopupIllustrationPatch.Requests.Count == 4, "Read archived prepared image without generation");
        Check(DiskImageCacheManager.LastKey == key, "Cache loads exact issue key");
        BulletinIllustrationPreloader.Reset(); DiskImageCacheManager.Cached = null;
        var miss = BulletinIllustrationPreloader.Ensure(key, context);
        Check(miss.Pending && WeeklyReportPopupIllustrationPatch.Requests.Count == 5, "Archive cache miss starts one job");
        BulletinIllustrationPreloader.Reset(); DiskImageCacheManager.Throw = true;
        var error = BulletinIllustrationPreloader.Ensure(key, context);
        Check(!error.Pending && !error.Ready, "Cache failure leaves manual retry");
        DiskImageCacheManager.Throw = false;
        BulletinIllustrationPreloader.Reset();
        IllustratorRuntime.Enabled = false;
        BulletinIllustrationPreloader.Prepare("disabled", "t", "s", "b");
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == 5, "Disabled system does not pre-generate");
        IllustratorRuntime.Enabled = true;
        for (int i = 0; i < 60; i++)
        {
            BulletinIllustrationPreloader.Ensure("bounded" + i, context, published: true);
            WeeklyReportPopupIllustrationPatch.Requests[WeeklyReportPopupIllustrationPatch.Requests.Count - 1](WeeklyReportPopupIllustrationPatch.Success());
        }
        var jobs = (IDictionary)typeof(BulletinIllustrationPreloader).GetField("Jobs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        Check(jobs.Count <= 48, "Metadata cache bounded");
        BulletinIllustrationPreloader.Reset();
        var plan = new AnimusForge.WorldBulletinIllustrationPlan { Identity = "selection:1", Facts = "甲发动政变，制伏乙。", Title = "旧标题" };
        plan.Participants.Add(new AnimusForge.WorldBulletinParticipant { HeroId = "a", Role = "政变发动者" });
        plan.Participants.Add(new AnimusForge.WorldBulletinParticipant { HeroId = "b", Role = "原国王" });
        string selectedKey = BulletinIllustrationPreloader.KeyFor(plan);
        plan.Title = "润色后标题";
        Check(selectedKey == BulletinIllustrationPreloader.KeyFor(plan), "Prose title never invalidates selected art");
        plan.Facts += "政变失败。";
        Check(selectedKey != BulletinIllustrationPreloader.KeyFor(plan), "Changed outcome invalidates key");
        plan.Facts = "甲发动政变，制伏乙。";
        plan.Participants[1].HeroId = "c";
        Check(selectedKey != BulletinIllustrationPreloader.KeyFor(plan), "Changed actor invalidates key");
        plan.Participants[1].HeroId = "b";
        int requests = WeeklyReportPopupIllustrationPatch.Requests.Count;
        BulletinIllustrationPreloader.PrepareSelection(plan);
        var selected = BulletinIllustrationPreloader.Find(selectedKey);
        Check(selected.Pending && WeeklyReportPopupIllustrationPatch.Requests.Count == requests + 1, "Selection immediately starts generation");
        BulletinIllustrationPreloader.PrepareSelection(plan);
        BulletinIllustrationPreloader.Ensure(selectedKey, context);
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == requests + 1, "Publication/open joins selected image job");
        BulletinIllustrationPreloader.CancelSelection(plan);
        WeeklyReportPopupIllustrationPatch.Requests[requests](WeeklyReportPopupIllustrationPatch.Success());
        Check(!selected.Pending && !selected.Ready && selected.Scope.Closed && selected.Status.Contains("作废"), "Invalidated selection rejects late completion");
        BulletinIllustrationPreloader.Reset();
        BulletinIllustrationPreloader.PrepareSelection(plan);
        WeeklyReportPopupIllustrationPatch.Requests[WeeklyReportPopupIllustrationPatch.Requests.Count - 1](WeeklyReportPopupIllustrationPatch.Success());
        BulletinIllustrationPreloader.CancelSelection(plan);
        Check(!BulletinIllustrationPreloader.Find(selectedKey).Ready, "Invalidation also clears already-completed selection");
        BulletinIllustrationPreloader.Reset();
        TestNoticeWaits(context);
        Console.WriteLine("PASS " + checks + " production preloader checks with fake scope, disk and generator; no game/network.");
    }
    private static void TestNoticeWaits(WeeklyReportVisualContext context)
    {
        foreach (string outcome in new[] { "success", "failure", "cancel", "timeout", "reset" })
        {
            BulletinIllustrationPreloader.Reset();
            var plan = new AnimusForge.WorldBulletinIllustrationPlan { Identity = "notice:" + outcome, Facts = "fact" };
            Check(!BulletinIllustrationPreloader.AwaitSelection(plan, () => { throw new Exception("no job"); }), "No job releases immediately: " + outcome);
            BulletinIllustrationPreloader.PrepareSelection(plan);
            var callback = WeeklyReportPopupIllustrationPatch.Requests[WeeklyReportPopupIllustrationPatch.Requests.Count - 1];
            int released = 0;
            Check(BulletinIllustrationPreloader.AwaitSelection(plan, () => released++), "Pending job registers waiter: " + outcome);
            Check(released == 0, "Waiter not released early: " + outcome);
            if (outcome == "success") callback(WeeklyReportPopupIllustrationPatch.Success());
            else if (outcome == "failure") callback(new WeeklyReportPopupIllustrationPatch.GenerationResult { Result = new ImageGenerationResult { ErrorMessage = "failure" } });
            else if (outcome == "cancel") BulletinIllustrationPreloader.CancelSelection(plan);
            else if (outcome == "reset") BulletinIllustrationPreloader.Reset();
            else BulletinTestTask.Expire();
            Check(released == 1, "One release after " + outcome);
            callback(WeeklyReportPopupIllustrationPatch.Success());
            BulletinTestTask.Expire();
            Check(released == 1, "Late completion and timeout cannot duplicate " + outcome);
        }
        BulletinIllustrationPreloader.Reset();
        var multi = new AnimusForge.WorldBulletinIllustrationPlan { Identity = "multi", Facts = "fact" };
        BulletinIllustrationPreloader.PrepareSelection(multi);
        int a = 0, b = 0;
        BulletinIllustrationPreloader.AwaitSelection(multi, () => a++);
        BulletinIllustrationPreloader.AwaitSelection(multi, () => b++);
        BulletinTestTask.Expire();
        WeeklyReportPopupIllustrationPatch.Requests[WeeklyReportPopupIllustrationPatch.Requests.Count - 1](WeeklyReportPopupIllustrationPatch.Success());
        Check(a == 1 && b == 1, "Independent waiters each release once");
        Check(!BulletinIllustrationPreloader.AwaitSelection(multi, () => a++), "Completed job never registers another timer");
        BulletinIllustrationPreloader.Reset();
        int promptIndex = WeeklyReportPopupIllustrationPatch.Requests.Count;
        BulletinIllustrationPreloader.Ensure("guided", context, true, playerRedrawPrompt: "PLAYER_REDRAW");
        Check(WeeklyReportPopupIllustrationPatch.PlayerPrompts[promptIndex] == "PLAYER_REDRAW", "manual bulletin redraw carries prompt into shared generation pipeline");
        BulletinIllustrationPreloader.Ensure("guided", context, true, playerRedrawPrompt: "NOT_AN_EXTRA_REQUEST");
        Check(WeeklyReportPopupIllustrationPatch.Requests.Count == promptIndex + 1 && WeeklyReportPopupIllustrationPatch.PlayerPrompts[promptIndex] == "PLAYER_REDRAW", "pending guided job is not overwritten or duplicated");
        WeeklyReportPopupIllustrationPatch.Requests[promptIndex](WeeklyReportPopupIllustrationPatch.Success());
        BulletinIllustrationPreloader.Ensure("guided", context, true);
        Check(WeeklyReportPopupIllustrationPatch.PlayerPrompts[promptIndex + 1] == null, "ordinary redraw does not reuse previous player input");
        BulletinIllustrationPreloader.Reset();
    }

}
namespace AnimusForge.Illustrator.Context
{
    internal class WeeklyReportVisualContext { }
    internal static class WeeklyReportContextExtractor {
        internal static WeeklyReportVisualContext ExtractFromWeeklyReport(string t,string s,string b) => new WeeklyReportVisualContext();
        internal static WeeklyReportVisualContext ExtractFromPlan(AnimusForge.WorldBulletinIllustrationPlan plan) => new WeeklyReportVisualContext();
    }
}
namespace AnimusForge
{
    internal class WorldBulletinParticipant { internal string HeroId, Role; }
    internal class WorldBulletinIllustrationPlan { internal string Identity, Facts, Title; internal List<WorldBulletinParticipant> Participants = new(); }
}
namespace AnimusForge.Illustrator.Core
{
    internal static class IllustratorRuntime { internal static bool Enabled = true; internal static void AssertMainThread() { } internal static bool IsEnabled(string c) => Enabled; internal static readonly Queue<Action> Critical = new(); internal static void PostCritical(Action action) { lock (Critical) Critical.Enqueue(action); } internal static void Drain() { while (true) { Action action; lock (Critical) { if (Critical.Count == 0) return; action = Critical.Dequeue(); } action(); } } }
    internal sealed class IllustrationScope
    {
        internal bool Closed, CampaignOwned;
        internal string CampaignKey => "campaign";
        internal IllustrationScope(object screen, string category, Action close, bool campaignOwned = false) { CampaignOwned = campaignOwned; }
        internal bool Run<T>(Func<CancellationToken,Task<T>> work, Action<T> complete, Action<string> fail)
        { try { complete(work(CancellationToken.None).GetAwaiter().GetResult()); } catch (Exception e) { fail(e.Message); } return true; }
        internal void Close() { Closed = true; }
    }
    internal class ImageGenerationResult { internal bool Success; internal byte[] ImageBytes; internal string ErrorMessage; }
}
namespace AnimusForge.Illustrator.Engine
{
    internal class CachedIllustrationItem { internal byte[] ImageData; internal string Prompt; internal string DisplayStatusText => "ready"; }
    internal static class DiskImageCacheManager
    {
        internal static int Loads; internal static string LastKey; internal static CachedIllustrationItem Cached; internal static bool Throw;
        internal static string ComputeHash(string s) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s)));
        internal static CachedIllustrationItem LoadImage(string key,string campaign,string category) { Loads++; LastKey = key; if (Throw) throw new Exception("disk"); return Cached; }
    }
}
namespace AnimusForge.Illustrator.UI.Patches
{
    internal static class WeeklyReportPopupIllustrationPatch
    {
        internal class GenerationResult { internal ImageGenerationResult Result; internal CachedIllustrationItem Saved; internal string Prompt; }
        internal static readonly List<Action<GenerationResult>> Requests = new List<Action<GenerationResult>>();
        internal static readonly List<string> PlayerPrompts = new List<string>();
        internal static bool StartGeneration(IllustrationScope scope, WeeklyReportVisualContext context, string key, bool bulletin, int attempt,
            Action<GenerationResult> complete, Action<string> fail, Action<string> status, string playerRedrawPrompt = null) { Requests.Add(complete); PlayerPrompts.Add(playerRedrawPrompt); return true; }
        internal static GenerationResult Success() => new GenerationResult { Saved = new CachedIllustrationItem { ImageData = new byte[] { 1 } }, Result = new ImageGenerationResult { Success = true, ImageBytes = new byte[] { 1 } }, Prompt = "prompt" };
    }
}

namespace TaleWorlds.Library
{
    internal static class Debug { internal static void Print(string text) { } }
}
// Manual clock: only the production preloader's Task.Delay is replaced; Task<T> workers stay real.
internal static class BulletinTestTask
{
    private static readonly List<TaskCompletionSource<bool>> Timers = new();
    internal static System.Threading.Tasks.Task Delay(int milliseconds)
    {
        if (milliseconds != 90000) throw new Exception("unexpected notice timeout");
        var done = new TaskCompletionSource<bool>(); Timers.Add(done); return done.Task;
    }
    internal static System.Threading.Tasks.Task<T> Run<T>(Func<T> work, CancellationToken token)
        => System.Threading.Tasks.Task.Run(work, token);
    internal static void Expire()
    {
        var timers = Timers.ToArray(); Timers.Clear();
        foreach (var timer in timers) timer.SetResult(true);
        if (!SpinWait.SpinUntil(() => { lock (IllustratorRuntime.Critical) return IllustratorRuntime.Critical.Count >= timers.Length; }, 5000))
            throw new Exception("timeout continuation did not reach fake main thread");
        IllustratorRuntime.Drain();
    }
}
