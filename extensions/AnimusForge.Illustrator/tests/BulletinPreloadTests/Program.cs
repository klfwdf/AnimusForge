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
        Console.WriteLine("PASS " + checks + " production preloader checks with fake scope, disk and generator; no game/network.");
    }
}
namespace AnimusForge.Illustrator.Context
{
    internal class WeeklyReportVisualContext { }
    internal static class WeeklyReportContextExtractor { internal static WeeklyReportVisualContext ExtractFromWeeklyReport(string t,string s,string b) => new WeeklyReportVisualContext(); }
}
namespace AnimusForge.Illustrator.Core
{
    internal static class IllustratorRuntime { internal static bool Enabled = true; internal static void AssertMainThread() { } internal static bool IsEnabled(string c) => Enabled; }
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
        internal static bool StartGeneration(IllustrationScope scope, WeeklyReportVisualContext context, string key, bool bulletin, int attempt,
            Action<GenerationResult> complete, Action<string> fail, Action<string> status) { Requests.Add(complete); return true; }
        internal static GenerationResult Success() => new GenerationResult { Saved = new CachedIllustrationItem { ImageData = new byte[] { 1 } }, Result = new ImageGenerationResult { Success = true, ImageBytes = new byte[] { 1 } }, Prompt = "prompt" };
    }
}
