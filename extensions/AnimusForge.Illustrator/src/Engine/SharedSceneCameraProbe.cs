using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using Path = System.IO.Path;

namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        private static SharedSceneProbeSession _activeSharedSceneProbe;

        // Explicit capture-only experiment. This is never called by the normal generation
        // route, and contains no provider requests, scene copies or screen capture fallback.
        internal static async Task<byte[]> CaptureSharedSceneProbeAsync(ConversationSceneCaptureSource source, CancellationToken token)
        {
            if (source == null || source.IsMapConversation)
                throw new InvalidOperationException("单镜头试采需要已进入的任务场景，不支持大地图对话布景。");
            await SceneCaptureLock.WaitAsync(token).ConfigureAwait(false);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(8));
            CancellationToken captureToken = deadline.Token;
            bool stageHeld = false;
            SharedSceneProbeSession session = null;
            byte[] raw = null;
            var watch = Stopwatch.StartNew();
            var diagnostics = GenerationDiagnostics.Current;
            diagnostics?.SetSubject("shared-scene-single-camera-probe");
            diagnostics?.RecordStage("shared_scene_probe_start", new JObject
            {
                ["experimental"] = true, ["views"] = 1, ["size"] = 512,
                ["providerRequests"] = 0, ["sceneCopy"] = false
            });
            try
            {
                await _stageLock.WaitAsync(captureToken).ConfigureAwait(false);
                stageHeld = true;
                session = await RunOnGameThreadAsync(() =>
                {
                    var created = new SharedSceneProbeSession(source, captureToken);
                    _activeSharedSceneProbe = created;
                    try { created.Begin(); return created; }
                    catch { created.Retire(); created.CleanupFiles(); throw; }
                }, captureToken).ConfigureAwait(false);
                if (session == null) throw new InvalidOperationException("单镜头试采调度已停止。");
                diagnostics?.RecordStage("shared_scene_probe_view_ready", session.Describe());
                bool started = await RunOnGameThreadAsync(() => { session.StartRendering(); return true; }, captureToken).ConfigureAwait(false);
                if (!started) throw new InvalidOperationException("单镜头试采渲染调度已停止。");
                bool ready = false;
                for (int attempt = 0; attempt < 60 && !ready; attempt++)
                {
                    var frames = await RunOnGameThreadAsync(() => IllustratorRuntime.AfterFramesAsync(2, captureToken), captureToken).ConfigureAwait(false);
                    if (frames == null) throw new InvalidOperationException("单镜头试采无法等待渲染帧。");
                    await frames.ConfigureAwait(false);
                    ready = await RunOnGameThreadAsync(session.Ready, captureToken).ConfigureAwait(false);
                }
                if (!ready) throw new InvalidOperationException("原场景副视图未就绪，未取得试采图。");
                string path = await RunOnGameThreadAsync(session.RequestExport, captureToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("原场景试采未提交导出。");
                raw = await ReadPanoramaFaceAsync(path, captureToken).ConfigureAwait(false);
                if (raw == null) throw new InvalidOperationException("原场景试采未读到完整PNG。");
                // Stop rendering as soon as the complete native file is available. Pixel
                // conversion and diagnostic writes happen only after native retirement.
                await RunOnGameThreadAsync(() => { session.Retire(); return true; }, captureToken).ConfigureAwait(false);
                if (!session.Retired.IsCompleted) IllustratorRuntime.PostCritical(session.Retire);
                await session.Retired.ConfigureAwait(false);
                await source.EnsureCurrentAsync(captureToken).ConfigureAwait(false);
                byte[] image = PanoramaProjection.ConvertSingleNativeView(raw);
                captureToken.ThrowIfCancellationRequested();
                diagnostics?.RecordSceneProbeImages(raw, image);
                return image;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            { throw new TimeoutException("单镜头试采超过8秒预算，已停止。"); }
            finally
            {
                try
                {
                    if (session != null)
                    {
                        if (!session.Retired.IsCompleted) IllustratorRuntime.PostCritical(session.Retire);
                        await session.Retired.ConfigureAwait(false);
                    }
                }
                finally
                {
                    session?.CleanupFiles();
                    if (stageHeld) _stageLock.Release();
                    SceneCaptureLock.Release();
                    diagnostics?.RecordStage("shared_scene_probe_end", new JObject
                    {
                        ["totalMs"] = watch.ElapsedMilliseconds, ["nativeImageReceived"] = raw != null,
                        ["applicationFrames"] = session?.Frames ?? 0, ["maxApplicationFrameMs"] = session?.MaxFrameMs ?? 0,
                        ["cameraSubmissions"] = session?.CameraSubmissions ?? 0,
                        ["retirementError"] = session?.RetirementError,
                        ["providerRequests"] = 0
                    });
                }
            }
        }

        internal static void TickSharedSceneProbe(float dt) => _activeSharedSceneProbe?.Tick(dt);

        internal static void CancelSharedSceneProbe()
        {
            IllustratorRuntime.AssertMainThread();
            _activeSharedSceneProbe?.Retire();
        }

        private sealed class SharedSceneProbeSession
        {
            private static int _nextId;
            private readonly ConversationSceneCaptureSource _source;
            private readonly CancellationToken _token;
            private readonly TaskCompletionSource<bool> _retired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly Stopwatch _watch = Stopwatch.StartNew();
            private Scene _borrowedScene;
            private SceneView _view;
            private Camera _camera;
            private Texture _target;
            private MatrixFrame _frame;
            private float _fov, _near, _far;
            private string _directory, _path, _nativeName;
            private bool _closed, _rendering;
            private Exception _failure;
            internal int Frames { get; private set; }
            internal int CameraSubmissions { get; private set; }
            internal double MaxFrameMs { get; private set; }
            internal string RetirementError { get; private set; }
            internal Task Retired => _retired.Task;

            internal SharedSceneProbeSession(ConversationSceneCaptureSource source, CancellationToken token)
            { _source = source; _token = token; }

            internal void Begin()
            {
                IllustratorRuntime.AssertMainThread();
                _source.EnsureCurrent(_token);
                var screen = _source.MissionScreen;
                if (screen?.CombatCamera == null || !screen.MissionStartedRendering() || _source.Mission?.Scene == null)
                    throw new InvalidOperationException("任务场景尚未渲染，暂时不能试采。");
                _borrowedScene = _source.Mission.Scene;
                _frame = screen.CombatCamera.Frame;
                _fov = screen.CombatCamera.HorizontalFov;
                _near = screen.CombatCamera.Near;
                _far = screen.CombatCamera.Far;
                if (!FinitePanoramaVector(_frame.origin) || !FinitePanoramaVector(_frame.rotation.s) ||
                    !FinitePanoramaVector(_frame.rotation.f) || !FinitePanoramaVector(_frame.rotation.u) ||
                    float.IsNaN(_fov) || _fov <= 0 || _fov >= Math.PI || float.IsNaN(_near) || _near <= 0 ||
                    float.IsNaN(_far) || float.IsInfinity(_far) || _far <= _near)
                    throw new InvalidOperationException("现场相机参数无效，未启动试采。");
                _directory = Path.Combine(Path.GetTempPath(), "AnimusForgeIllustrator", "shared_probe_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_directory);
                _path = Path.Combine(_directory, "front.png");
                _nativeName = "afi_p" + Interlocked.Increment(ref _nextId).ToString("x8");
                if (_nativeName.Length > 16) throw new InvalidOperationException("试采渲染名称超过安全长度。");
                _camera = Camera.CreateCamera();
                _camera.SetFovHorizontal(_fov, 1f, _near, _far);
                _camera.Frame = _frame;
                _target = Texture.CreateRenderTarget(_nativeName, 512, 512, false, false);
                _view = SceneView.CreateSceneView();
                _view.SetEnable(false);
                _view.SetRenderTarget(_target);
                _view.SetAutoDepthTargetCreation(true);
                _view.SetScene(_borrowedScene);
                SubmitCamera();
                _view.SetSceneUsesSkybox(true);
                _view.SetSceneUsesShadows(true);
                _view.SetSceneUsesContour(false);
                _view.SetAcceptGlobalDebugRenderObjects(false);
                _view.SetClearColor(0xff000000);
                _view.SetClearGbuffer(true);
                _view.SetRenderWithPostfx(true);
                _view.SetPostfxConfigParams(0);
                // Borrowed-scene experiment: retain the normal shadow/postfx path,
                // but do not request a new focused shadow region over the live scene.
                // This requests normal engine scheduling, not a manual Scene.Tick or
                // unverified one-shot/clear option. The bounded session stops it explicitly.
                _view.SetRenderOnDemand(false);
            }

            internal void StartRendering()
            {
                IllustratorRuntime.AssertMainThread();
                EnsureCurrent();
                SubmitCamera();
                _view.SetEnable(true);
                _rendering = true;
            }

            private void EnsureCurrent()
            {
                _source.EnsureCurrent(_token);
                if (_closed) throw _failure ?? new InvalidOperationException("单镜头试采已经结束。");
                if (_source.Mission.Scene?.Pointer != _borrowedScene?.Pointer)
                    throw new InvalidOperationException("原场景已切换，单镜头试采停止。");
                if (_watch.Elapsed.TotalSeconds > 8) throw new TimeoutException("单镜头试采超过8秒预算。");
            }

            private void SubmitCamera() { _view.SetCamera(_camera); CameraSubmissions++; }

            internal void Tick(float dt)
            {
                IllustratorRuntime.AssertMainThread();
                if (_closed) return;
                try
                {
                    EnsureCurrent();
                    if (!_rendering) return;
                    Frames++;
                    if (!float.IsNaN(dt) && !float.IsInfinity(dt) && dt >= 0) MaxFrameMs = Math.Max(MaxFrameMs, dt * 1000d);
                    SubmitCamera(); // Only our view. The player's camera is never rebound.
                }
                catch (Exception ex) { _failure = ex; Retire(); }
            }

            internal bool Ready()
            {
                IllustratorRuntime.AssertMainThread();
                EnsureCurrent();
                return _rendering && Frames >= 4 && _view.CheckSceneReadyToRender() && _view.ReadyToRender();
            }

            internal string RequestExport()
            {
                if (!Ready()) return null;
                SubmitCamera();
                _view.SetFilePathToSaveResult(_directory + Path.DirectorySeparatorChar);
                _view.SetFileNameToSaveResult("front.png");
                _view.SetFileTypeToSave(View.TextureSaveFormat.TextureTypePng);
                _view.SetSaveFinalResultToDisk(true);
                return _path;
            }

            internal JObject Describe() => new JObject
            {
                ["name"] = _nativeName, ["direction"] = "current-forward", ["width"] = 512, ["height"] = 512,
                ["cameraFrame"] = new JArray(PanoramaSnapshotInventory.Frame(_frame)),
                ["horizontalFov"] = _fov, ["near"] = _near, ["far"] = _far,
                ["sharedScene"] = true, ["sourceSceneParameterWrites"] = false, ["screenPixels"] = false,
                ["focusedShadowRequested"] = false,
                ["note"] = "Experimental shared-scene render; independent camera does not establish native render-state isolation. Readiness is not a GPU completion fence."
            };

            internal void Retire()
            {
                IllustratorRuntime.AssertMainThread();
                if (_closed) return;
                _closed = true;
                _rendering = false;
                if (ReferenceEquals(_activeSharedSceneProbe, this)) _activeSharedSceneProbe = null;
                void Step(Action action)
                {
                    try { action(); }
                    catch (Exception ex) { RetirementError = ex.GetType().Name; }
                }
                try
                {
                    if (_view != null)
                    {
                        Step(() => _view.SetSaveFinalResultToDisk(false));
                        Step(() => _view.SetEnable(false));
                        // Mission owns its Scene. Never call ClearAll or clearOnlySceneview:false.
                        Step(() => _view.AddClearTask(clearOnlySceneview: true));
                    }
                    if (_camera != null) Step(_camera.ReleaseCamera);
                    if (_target != null) Step(_target.Release);
                }
                finally
                {
                    _view = null; _camera = null; _target = null; _borrowedScene = null;
                    if (RetirementError == null) _retired.TrySetResult(true); // Submitted; not a GPU fence.
                    else _retired.TrySetException(new InvalidOperationException("试采资源清理失败：" + RetirementError));
                }
            }

            internal void CleanupFiles()
            {
                if (_path != null)
                    foreach (string path in new[] { _path, _path + ".png" })
                        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                if (_directory != null)
                    try { Directory.Delete(_directory, false); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
    }
}
