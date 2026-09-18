using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        private static PanoramaSession _activePanorama;
        internal const int PanoramaDirectionCount = 4;
        private const float PanoramaFov = 100f * (float)Math.PI / 180f;

        internal static void CancelSceneCapture(Mission mission)
        {
            IllustratorRuntime.AssertMainThread();
            if (_activePanorama?.BelongsTo(mission) == true) _activePanorama.Restore();
        }

        // Rotate in place: no teleporting agents, moving through walls or creating a second Scene.
        internal static MatrixFrame BuildPanoramaFrame(MatrixFrame original, int index)
        {
            if (index < 0 || index >= PanoramaDirectionCount) throw new ArgumentOutOfRangeException(nameof(index));
            var frame = original;
            Vec3 forward = -original.rotation.u;
            forward.z = 0f;
            if (forward.LengthSquared < 0.001f)
            {
                // Looking straight up/down: use the original camera's horizontal side vector.
                forward = Vec3.CrossProduct(Vec3.Up, original.rotation.s);
                forward.z = 0f;
            }
            forward.Normalize();
            Vec3 side = Vec3.CrossProduct(forward, Vec3.Up);
            side.Normalize();
            frame.rotation = new Mat3(side, Vec3.Up, -forward);
            frame.rotation.RotateAboutAnArbitraryVector(Vec3.Up, index * (float)Math.PI / 2f);
            return frame;
        }

        internal static string SceneReferenceLabel(int index)
        {
            string view = index < 0 ? "当前玩家视角的完整现场画面" : $"同一拍摄点的环境环视 {index + 1}/4（相对水平起始朝向旋转 {index * 90} 度，水平视野100度）";
            return view + "：与其他现场图是同一空间，不是多个房间或重复人物。" +
                "当前视角用于人物关系，其余方向补充环境；保留可见墙面材质与配色、楼梯、门窗、层高及桌椅分布，不按地点名称重造房间。" +
                "采集期间人物可能轻微移动，以当前视角和文字现场事实为准；忽略UI、字幕、血条和名牌，不将界面内容画入作品。";
        }

        internal static async Task<IReadOnlyList<IllustrationReferenceImage>> CaptureConversationSceneReferencesAsync(CancellationToken token)
        {
            await SceneCaptureLock.WaitAsync(token).ConfigureAwait(false);
            PanoramaSession session = null;
            var references = new List<IllustrationReferenceImage>(5);
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(12000);
            var captureToken = budget.Token;
            bool stageAcquired = false;
            try
            {
                // The native final-image exporter also dumps shared diagnostic passes.
                // Serialize with the existing portrait/banner exporter, not just scene requests.
                await _stageLock.WaitAsync(captureToken).ConfigureAwait(false);
                stageAcquired = true;
                session = await RunOnGameThreadAsync(() =>
                {
                    var created = new PanoramaSession();
                    _activePanorama = created;
                    try { created.BeginPanorama(); }
                    catch { created.Restore(); throw; }
                    return created;
                }, captureToken).ConfigureAwait(false);
                if (session == null) return references;
                if (session.HasView)
                {
                    // The first image uses the original FOV/pitch; the four following
                    // images rotate only our private camera, never MissionScreen's camera.
                    for (int index = -1; index < PanoramaDirectionCount; index++)
                    {
                        captureToken.ThrowIfCancellationRequested();
                        bool placed = await RunOnGameThreadAsync(() => session.SetDirection(index), captureToken).ConfigureAwait(false);
                        if (!placed) break;
                        bool ready = false;
                        for (int warmup = 0; warmup < 10 && !ready; warmup++)
                        {
                            await WaitForSceneFramesAsync(captureToken).ConfigureAwait(false);
                            ready = await RunOnGameThreadAsync(() => session.Ready, captureToken).ConfigureAwait(false);
                        }
                        if (!ready) break;
                        string path = await RunOnGameThreadAsync(() => session.RequestExport(index), captureToken).ConfigureAwait(false);
                        if (path == null) break;
                        byte[] png = await ReadSceneExportAsync(path, captureToken).ConfigureAwait(false);
                        await RunOnGameThreadAsync(() => { session.StopExport(); return true; }, captureToken).ConfigureAwait(false);
                        if (png == null) break;
                        references.Add(new IllustrationReferenceImage(Convert.ToBase64String(png), SceneReferenceLabel(index), IllustrationReferenceKind.Scene));
                    }
                }
                if (references.Count == 0)
                {
                    // Unsupported/unready offscreen rendering and map conversations:
                    // keep one actual view without hiding UI or moving the player camera.
                    string initial = await RunOnGameThreadAsync(() => session.IsCurrent ? CaptureConversationSceneBase64() : null, captureToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(initial))
                        references.Add(new IllustrationReferenceImage(initial, SceneReferenceLabel(-1), IllustrationReferenceKind.Scene));
                }
                TaleWorlds.Library.Debug.Print($"[Illustrator] Scene references captured={references.Count}, offscreen={session.HasView}, playerCameraUntouched=True");
                return references;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                TaleWorlds.Library.Debug.Print("[Illustrator] Offscreen scene capture budget expired; retaining completed views.");
                return references;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[Illustrator] Scene panorama unavailable; retaining captured views: " + ex.Message);
                return references;
            }
            finally
            {
                if (session != null && !session.Retired.IsCompleted)
                {
                    IllustratorRuntime.PostCritical(session.Restore);
                    await session.Retired.ConfigureAwait(false);
                }
                session?.CleanupFiles();
                if (stageAcquired) _stageLock.Release();
                SceneCaptureLock.Release();
            }
        }

        private static async Task<byte[]> ReadSceneExportAsync(string path, CancellationToken token)
        {
            long previousLength = -1;
            for (int attempt = 0; attempt < 25; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var file = new FileInfo(path);
                    if (!file.Exists) file = new FileInfo(path + ".png");
                    if (file.Exists && file.Length > 0)
                    {
                        if (file.Length > ImagePayload.MaxBytes) return null;
                        if (file.Length == previousLength) return ImagePayload.ReadFile(file.FullName);
                        previousLength = file.Length;
                    }
                }
                catch (IOException) { }
                catch (ArgumentException) { } // A partial PNG is not a finished export.
                await Task.Delay(80, token).ConfigureAwait(false);
            }
            return null;
        }

        private static async Task WaitForSceneFramesAsync(CancellationToken token)
        {
            var wait = await RunOnGameThreadAsync(() => IllustratorRuntime.AfterFramesAsync(3, token), token).ConfigureAwait(false);
            if (wait == null) throw new InvalidOperationException("无法调度现场截图。");
            await wait.ConfigureAwait(false);
        }

        // Owns only a SceneView, render target and private camera. The borrowed Mission
        // scene is never ticked, mutated or cleared here, even on cancel/Reset/Shutdown.
        private sealed class PanoramaSession
        {
            private readonly ScreenBase _owner = ScreenManager.TopScreen;
            private readonly Mission _mission = Mission.Current;
            private readonly Scene _scene = Mission.Current?.Scene;
            private readonly TaskCompletionSource<bool> _retired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly List<string> _files = new List<string>(5);
            private SceneView _view;
            private Texture _target;
            private Camera _camera;
            private MatrixFrame _originalFrame;
            private float _originalFov, _aspect, _near, _far;
            private string _directory;
            private bool _finished;

            internal bool IsCurrent => !_finished && _owner != null && !_owner.IsFinalized &&
                ReferenceEquals(_owner, ScreenManager.TopScreen) && ReferenceEquals(_mission, Mission.Current) &&
                (_mission == null || (_mission.CurrentState != Mission.State.EndingNextFrame && _mission.CurrentState != Mission.State.Over && ReferenceEquals(_scene, _mission.Scene)));
            internal bool HasView => _view != null;
            internal bool BelongsTo(Mission mission) => ReferenceEquals(_mission, mission);
            internal Task Retired => _retired.Task;
            internal bool Ready => IsCurrent && _view != null && _view.CheckSceneReadyToRender() && _view.ReadyToRender();

            internal bool BeginPanorama()
            {
                var screen = _owner as MissionScreen;
                if (!IsCurrent || _mission?.Scene == null || screen?.CombatCamera == null ||
                    !ReferenceEquals(screen.Mission, _mission) || !screen.MissionStartedRendering()) return false;
                _originalFrame = screen.CombatCamera.Frame;
                _originalFov = screen.CombatCamera.HorizontalFov;
                _near = screen.CombatCamera.Near;
                _far = screen.CombatCamera.Far;
                _aspect = Math.Max(0.5f, Math.Min(3f, Screen.AspectRatio));
                int width = _aspect >= 1 ? 1024 : (int)(1024 * _aspect);
                int height = _aspect >= 1 ? (int)(1024 / _aspect) : 1024;
                _directory = Path.Combine(Path.GetTempPath(), "AnimusForgeIllustrator", "scene_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_directory);
                _camera = Camera.CreateCamera();
                _camera.SetFovHorizontal(_originalFov, _aspect, _near, _far);
                _camera.Frame = _originalFrame;
                _target = Texture.CreateRenderTarget("af_scene_" + Guid.NewGuid().ToString("N"), width, height, false, false);
                _view = SceneView.CreateSceneView();
                _view.SetEnable(false);
                _view.SetRenderTarget(_target);
                _view.SetAutoDepthTargetCreation(true);
                _view.SetScene(_scene);
                _view.SetCamera(_camera);
                _view.SetSceneUsesSkybox(true);
                _view.SetSceneUsesShadows(true);
                _view.SetSceneUsesContour(false);
                _view.SetAcceptGlobalDebugRenderObjects(false);
                _view.SetClearColor(0xff000000);
                _view.SetClearGbuffer(true);
                // The native exporter also accesses depth/shadow passes. Keep the
                // full render path initialized; never CPU-read a GPU render target.
                _view.SetRenderWithPostfx(true);
                _view.SetPostfxConfigParams(0);
                var shadowCenter = _originalFrame.origin;
                _view.SetFocusedShadowmap(true, ref shadowCenter, 30f);
                _view.SetRenderOnDemand(false);
                _view.SetEnable(true);
                return true;
            }

            internal bool SetDirection(int index)
            {
                if (!IsCurrent || _view == null) return false;
                _view.SetSaveFinalResultToDisk(false);
                _camera.Frame = index < 0 ? _originalFrame : BuildPanoramaFrame(_originalFrame, index);
                _camera.SetFovHorizontal(index < 0 ? _originalFov : PanoramaFov, _aspect, _near, _far);
                return true;
            }

            internal string RequestExport(int index)
            {
                if (!Ready) return null;
                string name = "view_" + (index + 1) + ".png";
                string path = Path.Combine(_directory, name);
                _files.Add(path);
                _files.Add(path + ".png"); // Some native exporters append the selected format again.
                _view.SetFilePathToSaveResult(_directory + Path.DirectorySeparatorChar);
                _view.SetFileNameToSaveResult(name);
                _view.SetFileTypeToSave(View.TextureSaveFormat.TextureTypePng);
                _view.SetSaveFinalResultToDisk(true);
                return path;
            }

            internal void StopExport() { if (!_finished) _view?.SetSaveFinalResultToDisk(false); }

            internal void Restore()
            {
                IllustratorRuntime.AssertMainThread();
                if (_finished) return;
                _finished = true;
                if (ReferenceEquals(_activePanorama, this)) _activePanorama = null;
                try
                {
                    _view?.SetSaveFinalResultToDisk(false);
                    _view?.SetEnable(false);
                    // true is essential: the scene belongs to Mission, not this view.
                    _view?.AddClearTask(clearOnlySceneview: true);
                }
                catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Scene camera restore: " + ex.Message); }
                finally
                {
                    try { _camera?.ReleaseCamera(); }
                    catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Scene camera release: " + ex.Message); }
                    _camera = null;
                    try { _target?.Release(); }
                    catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Scene target release: " + ex.Message); }
                    _target = null;
                    _view = null;
                    _retired.TrySetResult(true);
                }
            }

            internal void CleanupFiles()
            {
                foreach (string path in _files)
                    try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                if (_directory != null)
                    try { Directory.Delete(_directory, false); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
    }
}
