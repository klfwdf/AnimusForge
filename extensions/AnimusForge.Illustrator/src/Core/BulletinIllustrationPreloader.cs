using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using TaleWorlds.Library;
using AnimusForge.Illustrator.Context;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator.UI.Patches;

namespace AnimusForge.Illustrator.Core
{
    // Event-driven, one request per issue, bounded metadata only. No per-frame queue polling.
    internal static class BulletinIllustrationPreloader
    {
        internal sealed class Job
        {
            internal string Key;
            internal IllustrationScope Scope;
            internal bool Pending = true;
            internal bool Ready;
            internal bool EditingCurrentImage;
            internal int Attempt;
            internal string Status = "本期配图正在提前生成…";
            // Main-thread only; each waiter runs exactly once when the job settles, is cancelled or times out.
            internal List<Action> Waiters;
        }

        private const int NoticeWaitTimeoutMs = 90000;

        private static readonly Dictionary<string, Job> Jobs = new Dictionary<string, Job>(StringComparer.Ordinal);
        internal static Action<string, WeeklyReportPopupIllustrationPatch.GenerationResult> Updated;

        internal static string KeyFor(string eventId, string title, string subtitle, string body)
        {
            // A new namespace excludes old weekly/gallery images; content guards reused issue IDs.
            return "bulletin_prepared_v2_16x9:" + DiskImageCacheManager.ComputeHash(string.Join("\n", eventId, title, subtitle, body));
        }

        internal static string KeyFor(global::AnimusForge.WorldBulletinIllustrationPlan plan)
        {
            var identity = new StringBuilder();
            AppendKeyPart(identity, plan.Identity);
            AppendKeyPart(identity, plan.Facts);
            foreach (var p in (plan.Participants ?? new List<global::AnimusForge.WorldBulletinParticipant>()).Where(p => p != null))
            {
                AppendKeyPart(identity, p.HeroId);
                AppendKeyPart(identity, p.Role);
            }
            return "bulletin_selected_v2_16x9:" + DiskImageCacheManager.ComputeHash(identity.ToString());
        }

        private static void AppendKeyPart(StringBuilder output, string value)
        {
            value = value ?? "";
            output.Append(value.Length).Append(':').Append(value);
        }

        internal static void PrepareSelection(global::AnimusForge.WorldBulletinIllustrationPlan plan)
        {
            if (plan == null || !IllustratorRuntime.IsEnabled("weekly_report")) return;
            string key = KeyFor(plan);
            if (Find(key) != null) return;
            var context = WeeklyReportContextExtractor.ExtractFromPlan(plan);
            if (context != null) Ensure(key, context, published: true);
        }

        internal static void CancelSelection(global::AnimusForge.WorldBulletinIllustrationPlan plan)
        {
            if (plan == null) return;
            var job = Find(KeyFor(plan));
            if (job == null) return;
            job.Pending = false;
            job.Ready = false;
            job.Status = "本期事件已作废。";
            job.Scope?.Close();
            ReleaseWaiters(job);
            Updated?.Invoke(job.Key, null);
        }

        // Lets the bulletin hold its map notice until the selected illustration is ready.
        // Returns false when nothing is generating, so the caller notifies immediately.
        internal static bool AwaitSelection(global::AnimusForge.WorldBulletinIllustrationPlan plan, Action release)
        {
            if (plan == null || release == null) return false;
            IllustratorRuntime.AssertMainThread();
            var job = Find(KeyFor(plan));
            if (job == null || !job.Pending) return false;
            bool done = false;
            Action once = () => { if (done) return; done = true; release(); };
            (job.Waiters ?? (job.Waiters = new List<Action>())).Add(once);
            // Real time, not campaign time: the map may be paused while the image is generating.
            Task.Delay(NoticeWaitTimeoutMs).ContinueWith(_ => IllustratorRuntime.PostCritical(() =>
            {
                if (done) return;
                Debug.Print("[Illustrator] Bulletin notice released by timeout: " + job.Key);
                once();
            }));
            return true;
        }

        private static void ReleaseWaiters(Job job)
        {
            var waiters = job.Waiters;
            job.Waiters = null;
            if (waiters == null) return;
            foreach (var waiter in waiters)
            {
                try { waiter(); }
                catch (Exception ex) { Debug.Print("[Illustrator] Bulletin notice release failed: " + ex.Message); }
            }
        }

        internal static Job Find(string key) => key != null && Jobs.TryGetValue(key, out var job) ? job : null;

        internal static void Prepare(string eventId, string title, string subtitle, string body)
        {
            if (!IllustratorRuntime.IsEnabled("weekly_report")) return;
            var context = WeeklyReportContextExtractor.ExtractFromWeeklyReport(title, subtitle, body);
            if (context != null) Ensure(KeyFor(eventId, title, subtitle, body), context, published: true);
        }

        internal static Job Ensure(string key, WeeklyReportVisualContext context, bool redraw = false, bool published = false,
            bool generateOnCacheMiss = true, string playerRedrawPrompt = null)
        {
            IllustratorRuntime.AssertMainThread();
            Job previous = Find(key);
            if (previous != null && (previous.Pending || !redraw)) return previous;
            if (Jobs.Count >= 48)
            {
                string expired = null;
                foreach (var pair in Jobs) if (!pair.Value.Pending && pair.Key != key) { expired = pair.Key; break; }
                if (expired != null) Jobs.Remove(expired);
            }
            var job = new Job { Key = key, Attempt = (previous?.Attempt ?? 0) + 1 };
            Jobs[key] = job;
            try
            {
                job.Scope = new IllustrationScope(null, "weekly_report", () =>
                {
                    if (Find(key) == job && job.Pending) Finish(job, "本期配图已取消，可点击重绘。", null);
                }, campaignOwned: true);
                if (published || redraw)
                    Generate(job, context, playerRedrawPrompt);
                else
                    job.Scope.Run(token => Task.Run(() => DiskImageCacheManager.LoadImage(key, job.Scope.CampaignKey, "weekly_report"), token), cached =>
                    {
                        if (cached == null)
                        {
                            if (generateOnCacheMiss) Generate(job, context, playerRedrawPrompt);
                            else Finish(job, "暂无已生成插画，可点击重绘。", null);
                            return;
                        }
                        var result = new WeeklyReportPopupIllustrationPatch.GenerationResult
                        {
                            Saved = cached, Prompt = cached.Prompt,
                            Result = new ImageGenerationResult { Success = true, ImageBytes = cached.ImageData }
                        };
                        Finish(job, cached.DisplayStatusText, result);
                        cached.ImageData = null;
                    }, error => Finish(job, error + "；可点击重绘。", null));
            }
            catch (Exception ex) { Finish(job, "配图准备失败：" + ex.Message + "；可点击重绘。", null); }
            return job;
        }

        internal static void EditCurrentImage(CachedIllustrationItem source, string prompt, IllustrationOptions options)
        {
            string key = source.SubjectKey;
            var previous = Find(key);
            if (previous?.Pending == true) return;
            var job = new Job { Key = key, EditingCurrentImage = true, Attempt = (previous?.Attempt ?? 0) + 1 };
            Jobs[key] = job;
            job.Scope = new IllustrationScope(null, "weekly_report", () => {
                if (Find(key) == job && job.Pending) Finish(job, "本图重绘已取消。", null);
            }, campaignOwned: true);
            CurrentImageRedraw.Start(job.Scope, source, prompt, options, null, value => {
                if (value.Saved != null)
                    Finish(job, "重绘完成，已保存为新版本。", new WeeklyReportPopupIllustrationPatch.GenerationResult {
                        Result = value.Result, Saved = value.Saved, Prompt = value.Result.ResolvedPrompt });
                else Finish(job, "重绘失败：" + (value.Result?.ErrorMessage ?? "未能保存图片"), null);
            }, error => Finish(job, "重绘失败：" + error, null));
        }

        private static void Generate(Job job, WeeklyReportVisualContext context, string playerRedrawPrompt = null)
        {
            WeeklyReportPopupIllustrationPatch.StartGeneration(job.Scope, context, job.Key, true, job.Attempt,
                result =>
                {
                    bool saved = result.Result?.Success == true && result.Saved != null;
                    Finish(job, saved ? result.Saved.DisplayStatusText : "配图失败：" + (result.Result?.ErrorMessage ?? "无法保存") + "；可点击重绘。", saved ? result : null);
                    if (result.Saved != null) result.Saved.ImageData = null;
                }, error => Finish(job, error + "；可点击重绘。", null), status =>
                {
                    if (Find(job.Key) != job || !job.Pending) return;
                    job.Status = status;
                    Updated?.Invoke(job.Key, null);
                }, playerRedrawPrompt);
        }

        private static void Finish(Job job, string status, WeeklyReportPopupIllustrationPatch.GenerationResult result)
        {
            if (Find(job.Key) != job || !job.Pending) return;
            job.Pending = false;
            job.Ready = result != null || job.EditingCurrentImage;
            job.Status = status;
            job.Scope?.Close();
            Updated?.Invoke(job.Key, result);
            ReleaseWaiters(job);
        }

        internal static void MarkDeleted(string key)
        {
            Job job = Find(key);
            if (job == null) return;
            job.Ready = false;
            job.Status = "插画已移入回收区，点击【重绘】重新绘制。";
        }

        internal static void Reset()
        {
            var jobs = Jobs.Values.ToList();
            Jobs.Clear();
            foreach (Job job in jobs)
            {
                job.Scope?.Close();
                // The host's save-generation guard turns releases from a finished campaign into no-ops.
                ReleaseWaiters(job);
            }
        }
    }
}
