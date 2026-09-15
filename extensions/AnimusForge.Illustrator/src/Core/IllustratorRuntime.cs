using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator.Core
{
    public sealed class IllustrationOptions
    {
        public bool EnableImageGeneration { get; }
        public bool EnableMultimodalVision { get; }
        public bool EnableOffscreenRendering { get; }
        public bool EnableLlmPromptExpansion { get; }
        public bool FixColorChannels { get; }
        public int MaxCacheCount { get; }
        public string ApiBaseUrl { get; }
        public string ApiKey { get; }
        public string ModelName { get; }
        public string ImageSize { get; }
        public string SelectedQuality { get; }
        public string SelectedStyle { get; }
        public string CustomStylePrompt { get; }
        public bool UseExactEndpointUrl { get; }
        public bool EnableReferenceImageForGeneration { get; }
        public bool AutoCleanTempFiles { get; }
        public string NegativePrompt { get; }
        public int Similarity { get; }
        public string DirectorApiBaseUrl { get; }
        public string DirectorApiKey { get; }
        public string DirectorModelName { get; }

        internal IllustrationOptions(IllustratorSettings settings, string directorUrl, string directorKey, string directorModel)
        {
            EnableImageGeneration = settings.EnableImageGeneration;
            EnableMultimodalVision = settings.EnableMultimodalVision;
            EnableOffscreenRendering = settings.EnableOffscreenRendering;
            EnableLlmPromptExpansion = settings.EnableLlmPromptExpansion;
            FixColorChannels = settings.FixColorChannels;
            MaxCacheCount = Math.Max(20, Math.Min(1000, settings.MaxCacheCount));
            ApiBaseUrl = settings.ApiBaseUrl;
            ApiKey = settings.ApiKey;
            ModelName = settings.ModelName;
            ImageSize = settings.ImageSize;
            SelectedQuality = settings.SelectedQuality;
            SelectedStyle = settings.SelectedStyle;
            CustomStylePrompt = settings.CustomStylePrompt;
            UseExactEndpointUrl = settings.UseExactEndpointUrl;
            EnableReferenceImageForGeneration = settings.EnableReferenceImageForGeneration;
            AutoCleanTempFiles = settings.AutoCleanTempFiles;
            NegativePrompt = settings.NegativePrompt;
            Similarity = Math.Max(0, Math.Min(100, settings.Similarity));
            DirectorApiBaseUrl = directorUrl;
            DirectorApiKey = directorKey;
            DirectorModelName = directorModel;
        }
    }

    public static class IllustratorRuntime
    {
        private static readonly ConcurrentQueue<Action> Pending = new ConcurrentQueue<Action>();
        private static readonly List<IllustrationScope> Scopes = new List<IllustrationScope>();
        private static int _mainThread;
        private static int _pendingCount;
        private static int _workers;
        private static bool _running;
        public static string CampaignKey { get; private set; }
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
            return _running && settings != null && settings.EnableImageGeneration &&
                (category != "encyclopedia" || settings.EnableEncyclopediaIllustration) &&
                (category != "conversation" || settings.EnableConversationIllustration);
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
            foreach (var scope in Scopes.ToArray()) scope.Close();
            TickScopes();
            GauntletTextureLoader.ReleaseAllSprites();
            CampaignKey = null;
        }

        public static void Shutdown()
        {
            Reset();
            _running = false;
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
            TickScopes();
            for (int i = 0; i < 2 && Pending.TryDequeue(out var action); i++)
            {
                Interlocked.Decrement(ref _pendingCount);
                try { action(); }
                catch (Exception ex) { Debug.Print("[Illustrator] Main-thread completion failed: " + ex.GetType().Name); }
            }
        }

        private static void TickScopes()
        {
            for (int i = Scopes.Count - 1; i >= 0; i--)
            {
                var scope = Scopes[i];
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
                if (!Post(() =>
                {
                    try { complete(result, error); }
                    finally { Interlocked.Decrement(ref _workers); }
                }))
                {
                    Interlocked.Decrement(ref _workers);
                    Debug.Print("[Illustrator] Completion queue capacity exceeded.");
                }
            });
            return true;
        }
    }

    internal sealed class IllustrationScope
    {
        private readonly Campaign _campaign;
        private readonly ScreenBase _screen;
        private readonly string _category;
        private readonly Action _onClose;
        private CancellationTokenSource _request;
        private bool _closed;
        private bool _disposed;
        private long _revision;
        public string CampaignKey { get; }

        public IllustrationScope(ScreenBase screen, string category, Action onClose)
        {
            IllustratorRuntime.AssertMainThread();
            _screen = screen;
            _campaign = Campaign.Current;
            _category = category;
            _onClose = onClose;
            CampaignKey = IllustratorRuntime.CampaignKey;
            IllustratorRuntime.Register(this);
        }

        public bool IsCurrent => !_closed && _campaign != null && ReferenceEquals(_campaign, Campaign.Current) &&
            !string.IsNullOrEmpty(CampaignKey) && CampaignKey == IllustratorRuntime.CampaignKey &&
            ReferenceEquals(_screen, ScreenManager.TopScreen) && !_screen.IsFinalized && IllustratorRuntime.IsEnabled(_category);

        public bool Run<T>(Func<CancellationToken, Task<T>> work, Action<T> complete, Action<string> fail)
        {
            IllustratorRuntime.AssertMainThread();
            if (!IsCurrent)
            {
                Debug.Print($"[Illustrator] Run rejected: scope not current (closed={_closed}, topScreenMismatch={!ReferenceEquals(_screen, ScreenManager.TopScreen)}, finalized={_screen?.IsFinalized}).");
                fail("生图上下文已失效（界面已切换或弹窗已关闭），请重新打开。");
                return false;
            }
            _request?.Cancel();
            var source = new CancellationTokenSource();
            var token = source.Token;
            long revision = ++_revision;
            _request = source;
            bool started = IllustratorRuntime.Start(() => work(token), (result, error) =>
            {
                bool current = IsCurrent && revision == _revision && !token.IsCancellationRequested;
                if (ReferenceEquals(_request, source)) _request = null;
                source.Dispose();
                if (!current) return;
                if (error == null) complete(result);
                else if (!(error is OperationCanceledException)) fail(error.Message);
                else if (token.IsCancellationRequested) fail("生图请求已取消（界面已切换或发起了新请求）。");
                else
                {
                    Debug.Print($"[Illustrator] Request timed out waiting for upstream ({error.GetType().Name}).");
                    fail("生成请求超时：上游模型在限定时间内无响应，请稍后重试。");
                }
            });
            if (!started)
            {
                _request = null;
                source.Dispose();
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
            if (_disposed) return;
            _disposed = true;
            try { _onClose(); }
            catch (Exception ex) { Debug.Print("[Illustrator] Window cleanup failed: " + ex.GetType().Name); }
        }
    }
}
