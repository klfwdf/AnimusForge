using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Engine;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Illustrator.Core
{
    public sealed class IllustrationOptions
    {
        public bool EnableImageGeneration { get; }
        public bool EnableMultimodalVision { get; }
        public bool EnableSceneOffscreenRendering { get; }
        public bool EnableLlmPromptExpansion { get; }
        public int MaxCacheCount { get; }
        public string ApiBaseUrl { get; }
        public bool UsePlayer2ImageApi { get; }
        public string Player2GameClientId { get; }
        public string ApiKey { get; }
        public string ModelName { get; }
        public string ImageSize { get; private set; }
        public string SelectedQuality { get; }
        public string SelectedStyle { get; }
        public string CustomStylePrompt { get; }
        public string CustomDirectorPrompt { get; }
        public string StyleFingerprint { get; }
        public bool UseExactEndpointUrl { get; }
        public bool EnableReferenceImageForGeneration { get; }
        public bool AutoCleanTempFiles { get; }
        public string NegativePrompt { get; }
        public int Randomness { get; }
        public bool PreferChatImageProtocol { get; }
        public string DirectorApiBaseUrl { get; }
        public string DirectorApiKey { get; }
        public string DirectorModelName { get; }
        public int DirectorApproximateTokens { get; }
        public int DirectorApiMaxTokens { get; }
        internal int ForcedImagePromptCharacters { get; }
        public int ImageGenerationTimeoutSeconds { get; }
        internal bool HasPlayerRedrawRequest { get; private set; }
        internal bool IsApiTest { get; private set; }
        internal string OutputFrameRequirement { get; private set; } = string.Empty;

        internal IllustrationOptions ForApiTest()
        {
            var copy = (IllustrationOptions)MemberwiseClone();
            copy.IsApiTest = true;
            return copy;
        }

        internal IllustrationOptions WithPlayerRedrawRequest(string prompt)
        {
            var copy = (IllustrationOptions)MemberwiseClone();
            copy.HasPlayerRedrawRequest = !string.IsNullOrWhiteSpace(prompt);
            return copy;
        }

        // Per-request sizing must never overwrite the player's shared MCM setting.
        internal IllustrationOptions WithImageSize(string imageSize)
        {
            var copy = (IllustrationOptions)MemberwiseClone();
            copy.ImageSize = imageSize;
            copy.OutputFrameRequirement = BuildOutputFrameRequirement(imageSize);
            return copy;
        }

        private static string BuildOutputFrameRequirement(string imageSize)
        {
            var dimensions = (imageSize ?? string.Empty).Split('x');
            if (dimensions.Length != 2 || !int.TryParse(dimensions[0], out int width)
                || !int.TryParse(dimensions[1], out int height) || width <= 0 || height <= 0)
                return string.Empty;
            int divisor = width, remainder = height;
            while (remainder != 0)
            {
                int next = divisor % remainder;
                divisor = remainder;
                remainder = next;
            }
            string orientation = width == height ? "正方形" : width > height ? "横向" : "竖向";
            return "最终成图必须为" + orientation + (width / divisor) + ":" + (height / divisor)
                + "，目标尺寸" + imageSize
                + "。按此画幅重新组织完整画面，保持人物与场景的自然比例；参考图只提供其标注用途，不决定输出画幅。不得改用其他比例，不要拉伸或加边框伪造目标画幅。此画幅要求适用于普通生成和重绘。";
        }

        // Map the existing presets to exact 16:9 sizes with both edges divisible by 16.
        // The smallest tier is 1280x720: 1024x576 falls below GPT Image 2's pixel minimum.
        internal IllustrationOptions WithSceneImageSize()
        {
            string imageSize;
            switch (ImageSize)
            {
                case "2048x2048":
                    imageSize = "2048x1152";
                    break;
                case "1344x768":
                case "1024x1536":
                    imageSize = "1536x864";
                    break;
                default:
                    imageSize = "1280x720";
                    break;
            }
            return WithImageSize(imageSize);
        }

        internal IllustrationOptions(IllustratorSettings settings, string directorUrl, string directorKey, string directorModel)
        {
            EnableImageGeneration = settings.EnableImageGeneration;
            EnableMultimodalVision = settings.EnableMultimodalVision;
            EnableSceneOffscreenRendering = settings.EnableSceneOffscreenRendering;
            EnableLlmPromptExpansion = settings.EnableLlmPromptExpansion;
            MaxCacheCount = Math.Max(20, Math.Min(1000, settings.MaxCacheCount));
            ApiBaseUrl = settings.ApiBaseUrl;
            UsePlayer2ImageApi = settings.UsePlayer2ImageApi;
            Player2GameClientId = (settings.Player2GameClientId ?? "").Trim();
            ApiKey = settings.ApiKey;
            ModelName = settings.ModelName;
            ImageSize = settings.ImageSize;
            OutputFrameRequirement = BuildOutputFrameRequirement(ImageSize);
            SelectedQuality = settings.SelectedQuality;
            SelectedStyle = settings.SelectedStyle;
            CustomStylePrompt = settings.CustomStylePrompt;
            CustomDirectorPrompt = (settings.CustomDirectorPrompt ?? string.Empty).Trim();
            var resolvedStyle = IllustrationStylePresets.Resolve(SelectedStyle, CustomStylePrompt);
            StyleFingerprint = DiskImageCacheManager.ComputeHash(
                (settings.SelectedStyle ?? string.Empty) + "\n" +
                (settings.CustomStylePrompt ?? string.Empty) + "\n" +
                (settings.NegativePrompt ?? string.Empty) + "\n" +
                (resolvedStyle.ImagePrompt ?? string.Empty) + "\n" +
                (resolvedStyle.NegativePrompt ?? string.Empty) + "\n" +
                (resolvedStyle.ApiStyle ?? string.Empty) + (string.IsNullOrEmpty(CustomDirectorPrompt) ? string.Empty : "\n" + CustomDirectorPrompt));
            UseExactEndpointUrl = settings.UseExactEndpointUrl;
            EnableReferenceImageForGeneration = settings.EnableReferenceImageForGeneration;
            AutoCleanTempFiles = settings.AutoCleanTempFiles;
            NegativePrompt = settings.NegativePrompt;
            Randomness = Math.Max(0, Math.Min(100, settings.Randomness));
            PreferChatImageProtocol = settings.PreferChatImageProtocol;
            DirectorApiBaseUrl = directorUrl;
            DirectorApiKey = directorKey;
            DirectorModelName = directorModel;
            DirectorApproximateTokens = Math.Max(600, Math.Min(4000, settings.DirectorMaxTokens));
            DirectorApiMaxTokens = Math.Max(512, Math.Min(64000, settings.DirectorApiMaxTokens));
            ForcedImagePromptCharacters = settings.ForceImagePromptLimit
                ? Math.Max(1000, Math.Min(ImagePromptBudget.MaximumCharacters, settings.ForcedImagePromptCharacters)) : 0;
            ImageGenerationTimeoutSeconds = Math.Max(60, Math.Min(600, settings.ImageGenerationTimeoutSeconds));
        }
    }

    public static class IllustratorRuntime
    {
        // Only accepted workers/stage cleanup use this channel; producer count is bounded by four workers.
        private static readonly ConcurrentQueue<Action> Critical = new ConcurrentQueue<Action>();
        private static readonly Queue<KeyValuePair<long, TaskCompletionSource<bool>>> FrameWaiters = new Queue<KeyValuePair<long, TaskCompletionSource<bool>>>();
        private static long _frame;
        internal static long ApplicationFrame => _frame;
        internal static void PostCritical(Action action) { if (action != null) Critical.Enqueue(action); }
        internal static Task AfterFramesAsync(int frames, CancellationToken token)
        {
            AssertMainThread();
            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            FrameWaiters.Enqueue(new KeyValuePair<long, TaskCompletionSource<bool>>(_frame + Math.Max(2, frames), done));
            return AwaitFrameAsync(done.Task, token);
        }
        private static async Task AwaitFrameAsync(Task task, CancellationToken token)
        {
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (token.Register(() => cancelled.TrySetCanceled()))
            {
                await await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
        }
        private static readonly ConcurrentQueue<Action> Pending = new ConcurrentQueue<Action>();
        private static readonly List<IllustrationScope> Scopes = new List<IllustrationScope>();
        private static int _mainThread;
        private static int _pendingCount;
        private static int _workers;
        private static bool _running;
        public static string CampaignKey { get; private set; }
        internal static bool IsHostRunning => _running && _mainThread != 0;
        public static bool IsMainThread => _mainThread != 0 && Environment.CurrentManagedThreadId == _mainThread;

        public static void Initialize()
        {
            // 注意：OnSubModuleLoad 在 1.4.x 上可能运行于子模块加载线程而非游戏主线程，
            // 不能在此捕获线程 ID；真正的主线程 ID 由首个 OnApplicationTick -> Tick() 捕获。
            _running = true;
        }

        public static void AssertMainThread()
        {
            if (!IsMainThread) throw new InvalidOperationException("Illustrator requires the game thread.");
        }

        public static bool IsEnabled(string category = null)
        {
            AssertMainThread();
            var settings = IllustratorSettings.Instance;
            return _running && settings != null && settings.EnableImageGeneration;
        }

        public static IllustrationOptions CaptureOptions()
        {
            AssertMainThread();
            var settings = IllustratorSettings.Instance;
            if (settings == null) return null;
            string directorUrl;
            string directorKey;
            string directorModel;
            VisualDirectorEngine.ResolveChatConfigForSnapshot(settings, out directorUrl, out directorKey, out directorModel);
            return new IllustrationOptions(settings, directorUrl, directorKey, directorModel);
        }

        public static void SetCampaign(string identity)
        {
            AssertMainThread();
            Reset();
            CampaignKey = identity;
        }

        public static void Reset()
        {
            AssertMainThread();
            IllustratorApiTest.Cancel();
            BulletinIllustrationPreloader.Reset();
            foreach (var scope in Scopes.ToArray()) scope.Close();
            TickScopes();
            BannerEmblemComposer.Reset();
            ScreenCaptureHelper.CancelActiveStage();
            GauntletTextureLoader.ReleaseAllSprites();
            CampaignKey = null;
        }

        public static void Shutdown()
        {
            Reset();
            _running = false;
            while (FrameWaiters.Count > 0) FrameWaiters.Dequeue().Value.TrySetCanceled();
        }

        public static void Tick()
        {
            // OnApplicationTick 保证在游戏主线程执行；首个 Tick 捕获真实主线程 ID。
            if (_mainThread == 0)
            {
                _mainThread = Environment.CurrentManagedThreadId;
                Debug.Print("[Illustrator] Captured game main thread id=" + _mainThread);
            }
            AssertMainThread();
            _frame++;
            while (FrameWaiters.Count > 0 && FrameWaiters.Peek().Key <= _frame) FrameWaiters.Dequeue().Value.TrySetResult(true);
            TickScopes();
            UI.Overlays.IllustrationCardPopup.TickSystemUiVisibility();
            for (int i = 0; i < 2; i++)
            {
                Action action;
                if (!Critical.TryDequeue(out action))
                {
                    if (!Pending.TryDequeue(out action)) break;
                    Interlocked.Decrement(ref _pendingCount);
                }
                try { action(); }
                catch (Exception ex) { Debug.Print("[Illustrator] Main-thread completion failed: " + ex.GetType().Name); }
            }
            // Snapshot work has its own bounded batch. Avoid the worker -> queued
            // frame wait -> worker -> queued batch round trip between every slice.
            PanoramaBatchPump.Tick();
            ScreenCaptureHelper.TickOffscreenStage();
        }

        // Event-driven, at most eight scopes. Never scan the disk cache or game world per frame.
        internal static event Action<IllustrationGenerationUpdate> GenerationUpdated;

        internal static IllustrationScope FindGenerating(string category, string subjectKey, string sessionKey = null)
        {
            AssertMainThread();
            foreach (var scope in Scopes)
                if (scope.MatchesGeneration(category, subjectKey, sessionKey)) return scope;
            return null;
        }

        internal static void PublishGeneration(IllustrationGenerationUpdate update)
        {
            AssertMainThread();
            if (update.Saved != null)
                InformationManager.DisplayMessage(new InformationMessage("[AI画卷] " + update.Saved.Title + " 绘制完成，已更新到画廊。"));
            else
                InformationManager.DisplayMessage(new InformationMessage("[AI画卷] 绘制失败：" + update.Error));
            var listeners = GenerationUpdated;
            if (listeners == null) return;
            foreach (Action<IllustrationGenerationUpdate> listener in listeners.GetInvocationList())
                try { listener(update); }
                catch (Exception ex) { Debug.Print("[Illustrator] Generation subscriber failed: " + ex.GetType().Name); }
        }

        private static void TickScopes()
        {
            for (int i = Scopes.Count - 1; i >= 0; i--)
            {
                var scope = Scopes[i];
                scope.DetachChangedWindow();
                if (!scope.IsCurrent)
                {
                    Scopes.RemoveAt(i);
                    scope.FinishClose();
                }
            }
        }

        internal static void Register(IllustrationScope scope)
        {
            AssertMainThread();
            if (Scopes.Count >= 8) throw new InvalidOperationException("Too many illustration windows.");
            Scopes.Add(scope);
        }

        public static bool Post(Action action)
        {
            if (action == null || Interlocked.Increment(ref _pendingCount) > 32)
            {
                if (action != null) Interlocked.Decrement(ref _pendingCount);
                return false;
            }
            Pending.Enqueue(action);
            return true;
        }

        internal static bool Start<T>(Func<Task<T>> work, Action<T, Exception> complete)
        {
            AssertMainThread();
            if (!_running || Volatile.Read(ref _workers) >= 4) return false;
            Interlocked.Increment(ref _workers);
            Task.Run(async () =>
            {
                T result = default;
                Exception error = null;
                try { result = await work().ConfigureAwait(false); }
                catch (Exception ex) { error = ex; }
                PostCritical(() =>
                {
                    try { complete(result, error); }
                    finally { Interlocked.Decrement(ref _workers); }
                });
            });
            return true;
        }
    }

    internal sealed class IllustrationGenerationUpdate
    {
        internal IllustrationScope Source;
        internal string CampaignKey, Category, SubjectKey, SessionKey, Error;
        internal CachedIllustrationItem Saved;
    }

    internal sealed class IllustrationScope
    {
        private readonly Campaign _campaign;
        private readonly ScreenBase _screen;
        private readonly string _category;
        private readonly Action _onClose;
        private readonly bool _campaignOwned;
        private CancellationTokenSource _request;
        private bool _closed;
        private long _revision;
        private bool _windowDetached;
        private bool _windowCleaned;
        private string _subjectKey, _sessionKey;
        private bool _backgroundGeneration;
        public string CampaignKey { get; }

        public IllustrationScope(ScreenBase screen, string category, Action onClose, bool campaignOwned = false)
        {
            IllustratorRuntime.AssertMainThread();
            _screen = screen;
            _campaignOwned = campaignOwned;
            _campaign = Campaign.Current;
            _category = category;
            _onClose = onClose;
            CampaignKey = IllustratorRuntime.CampaignKey;
            IllustratorRuntime.Register(this);
        }

        public bool IsCurrent => !_closed && _campaign != null && ReferenceEquals(_campaign, Campaign.Current) &&
            !string.IsNullOrEmpty(CampaignKey) && CampaignKey == IllustratorRuntime.CampaignKey &&
            (_campaignOwned || (_backgroundGeneration && _request != null) || (ReferenceEquals(_screen, ScreenManager.TopScreen) && _screen != null && !_screen.IsFinalized)) && IllustratorRuntime.IsEnabled(_category);

        internal bool MatchesGeneration(string category, string subjectKey, string sessionKey) =>
            IsCurrent && _request != null && _backgroundGeneration && _category == category &&
            _subjectKey == subjectKey && string.Equals(_sessionKey, sessionKey, StringComparison.Ordinal);

        internal bool DetachWindowIfGenerating()
        {
            IllustratorRuntime.AssertMainThread();
            if (_closed || !_backgroundGeneration || _request == null || _campaignOwned) return false;
            _windowDetached = true;
            return true;
        }

        internal void DetachChangedWindow()
        {
            if (!_campaignOwned && !_windowDetached &&
                (_screen == null || _screen.IsFinalized || !ReferenceEquals(_screen, ScreenManager.TopScreen)) &&
                DetachWindowIfGenerating()) CleanWindow();
        }

        internal bool RunGeneration<T>(string subjectKey, string sessionKey, Func<CancellationToken, Task<T>> work,
            Action<T> complete, Action<string> fail, Func<T, CachedIllustrationItem> saved,
            Func<T, string> error) => RunCore(work, complete, fail, subjectKey, sessionKey, saved, error);

        public bool Run<T>(Func<CancellationToken, Task<T>> work, Action<T> complete, Action<string> fail) =>
            RunCore(work, complete, fail, null, null, null, null);

        private bool RunCore<T>(Func<CancellationToken, Task<T>> work, Action<T> complete, Action<string> fail,
            string subjectKey, string sessionKey, Func<T, CachedIllustrationItem> saved, Func<T, string> resultError)
        {
            IllustratorRuntime.AssertMainThread();
            if (!IsCurrent)
            {
                Debug.Print($"[Illustrator] Run rejected: scope not current (closed={_closed}, topScreenMismatch={!ReferenceEquals(_screen, ScreenManager.TopScreen)}, finalized={_screen?.IsFinalized}).");
                GenerationDiagnostics.WriteDelivery(null, "scope_rejected", "scope stale/closed/screen changed; category=" + _category);
                fail("生图上下文已失效（界面已切换或弹窗已关闭），请重新打开。");
                return false;
            }
            _request?.Cancel();
            var source = new CancellationTokenSource();
            var token = source.Token;
            long revision = ++_revision;
            _request = source;
            _backgroundGeneration = saved != null;
            _subjectKey = subjectKey;
            _sessionKey = sessionKey;
            string diagnosticId = null;
            var deliveryClock = System.Diagnostics.Stopwatch.StartNew();
            long workerCompletedMs = 0;
            bool started = IllustratorRuntime.Start(async () =>
            {
                using (var diagnostics = GenerationDiagnostics.Begin(CampaignKey, _category))
                {
                    diagnosticId = diagnostics?.Id;
                    diagnostics?.RecordStage("worker_started", new JObject { ["elapsedMs"] = deliveryClock.ElapsedMilliseconds });
                    try
                    {
                        var value = await work(token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        diagnostics?.RecordStage("worker_complete", new JObject { ["elapsedMs"] = deliveryClock.ElapsedMilliseconds });
                        diagnostics?.Finish("completed");
                        return value;
                    }
                    catch (OperationCanceledException) { diagnostics?.Finish("cancelled"); throw; }
                    catch (Exception ex) { diagnostics?.Finish("failed", ex.Message); throw; }
                    finally { workerCompletedMs = deliveryClock.ElapsedMilliseconds; }
                }
            }, (result, error) =>
            {
                GenerationDiagnostics.WriteDelivery(diagnosticId, "main_thread_delivery", "totalMs=" + deliveryClock.ElapsedMilliseconds + "; dispatchWaitMs=" + Math.Max(0, deliveryClock.ElapsedMilliseconds - workerCompletedMs));
                // Capture the caller's cancellation state before cancelling child work for cleanup.
                // HttpClient/local deadlines can throw TaskCanceledException while this token is live.
                bool requestCancelled = token.IsCancellationRequested;
                bool current = IsCurrent && revision == _revision && !requestCancelled;
                if (ReferenceEquals(_request, source)) _request = null;
                if (error != null) source.Cancel();
                source.Dispose();
                if (!current) { GenerationDiagnostics.WriteDelivery(diagnosticId, "ui_delivery_dropped", "closed/stale/cancelled; late result not applied"); return; }
                // Snapshot acceptance before UI callbacks: bulletin completion legitimately closes its scope.
                IllustrationGenerationUpdate update = null;
                if (saved != null)
                {
                    var item = error == null ? saved(result) : null;
                    bool valid = item != null && item.CampaignKey == CampaignKey && item.Category == _category && item.SubjectKey == subjectKey;
                    update = new IllustrationGenerationUpdate { Source = this, CampaignKey = CampaignKey, Category = _category,
                        SubjectKey = subjectKey, SessionKey = sessionKey, Saved = valid ? item : null,
                        Error = error?.Message ?? (valid ? null : resultError?.Invoke(result) ?? "未能保存图像") };
                }
                try
                {
                    if (!_windowDetached)
                    {
                        if (error == null)
                        {
                            try { complete(result); }
                            catch (Exception ex) { GenerationDiagnostics.WriteDelivery(diagnosticId, "ui_callback_failed", ex.GetType().Name); fail(ex.Message); }
                        }
                        else if (!(error is OperationCanceledException)) fail(error.Message);
                        else fail("生成请求超时：上游模型在限定时间内无响应，请稍后重试。");
                    }
                    if (update != null)
                    {
                        GenerationDiagnostics.WriteDelivery(diagnosticId, "generation_published", "background=" + _windowDetached + "; saved=" + (update.Saved != null));
                        IllustratorRuntime.PublishGeneration(update);
                    }
                }
                finally { if (_windowDetached) Close(); }

            });
            if (!started)
            {
                _request = null;
                source.Dispose();
                GenerationDiagnostics.WriteDelivery(null, "worker_admission_rejected", "four-worker capacity or host unavailable");
                fail("生图任务繁忙，请等待已有任务完成后重试。");
            }
            return started;
        }

        public void Close()
        {
            IllustratorRuntime.AssertMainThread();
            if (_closed) return;
            _closed = true;
            _revision++;
            _request?.Cancel();
        }

        internal void FinishClose()
        {
            Close();
            CleanWindow();
        }

        private void CleanWindow()
        {
            if (_windowCleaned) return;
            _windowCleaned = true;
            try { _onClose(); }
            catch (Exception ex) { Debug.Print("[Illustrator] Window cleanup failed: " + ex.GetType().Name); }
        }
    }
}
