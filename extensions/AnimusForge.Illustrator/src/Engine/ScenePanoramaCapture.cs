using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using TaleWorlds.Engine;
using TaleWorlds.GauntletUI.BaseTypes;
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

        internal static bool IsUsablePanoramaFrame(MatrixFrame expected, MatrixFrame actual, float horizontalFov)
        {
            return Vec3.DotProduct(expected.rotation.u, actual.rotation.u) > 0.999f &&
                expected.origin.DistanceSquared(actual.origin) < 0.01f && horizontalFov >= (float)Math.PI / 2f;
        }

        internal static async Task<IReadOnlyList<IllustrationReferenceImage>> CaptureConversationSceneReferencesAsync(CancellationToken token)
        {
            await SceneCaptureLock.WaitAsync(token).ConfigureAwait(false);
            PanoramaSession session = null;
            var references = new List<IllustrationReferenceImage>(5);
            try
            {
                session = await RunOnGameThreadAsync(() =>
                {
                    var created = new PanoramaSession();
                    _activePanorama = created;
                    try { created.HideOverlays(); }
                    catch { created.Restore(); throw; }
                    return created;
                }, token).ConfigureAwait(false);
                if (session == null) return references;
                await WaitForSceneFramesAsync(token).ConfigureAwait(false);
                string initial = await RunOnGameThreadAsync(() => session.IsCurrent ? CaptureConversationSceneBase64() : null, token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(initial)) return references;
                references.Add(new IllustrationReferenceImage(initial, SceneReferenceLabel(-1), IllustrationReferenceKind.Scene));
                bool started = await RunOnGameThreadAsync(() => session.BeginPanorama(), token).ConfigureAwait(false);
                if (started)
                {
                    for (int index = 0; index < PanoramaDirectionCount; index++)
                    {
                        token.ThrowIfCancellationRequested();
                        bool placed = await RunOnGameThreadAsync(() => session.SetDirection(index), token).ConfigureAwait(false);
                        if (!placed) break;
                        await WaitForSceneFramesAsync(token).ConfigureAwait(false);
                        string capture = await RunOnGameThreadAsync(() => session.IsRenderedDirection(index) ? CaptureConversationSceneBase64() : null, token).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(capture)) break;
                        references.Add(new IllustrationReferenceImage(capture, SceneReferenceLabel(index), IllustrationReferenceKind.Scene));
                    }
                }
                TaleWorlds.Library.Debug.Print($"[Illustrator] Scene references captured={references.Count}, panoramicDirections={references.Count - 1}");
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
                var restored = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                IllustratorRuntime.PostCritical(() =>
                {
                    try { session?.Restore(); }
                    finally { restored.TrySetResult(true); }
                });
                try { await restored.Task.ConfigureAwait(false); }
                finally { SceneCaptureLock.Release(); }
            }
        }

        private static async Task WaitForSceneFramesAsync(CancellationToken token)
        {
            var wait = await RunOnGameThreadAsync(() => IllustratorRuntime.AfterFramesAsync(3, token), token).ConfigureAwait(false);
            if (wait == null) throw new InvalidOperationException("无法调度现场截图。");
            await wait.ConfigureAwait(false);
        }

        // Created, mutated and restored only on the main thread, including Reset/Shutdown.
        private sealed class PanoramaSession
        {
            private readonly ScreenBase _owner = ScreenManager.TopScreen;
            private readonly Mission _mission = Mission.Current;
            private readonly List<Tuple<Widget, bool>> _hidden = new List<Tuple<Widget, bool>>();
            private MissionScreen _screen;
            private Camera _camera;
            private MatrixFrame _originalFrame;
            private float _originalFov, _near, _far;
            private bool _finished;

            internal bool IsCurrent => !_finished && ReferenceEquals(_owner, ScreenManager.TopScreen) && ReferenceEquals(_mission, Mission.Current);
            internal bool OwnsCamera => IsCurrent && _camera != null && ReferenceEquals(_screen?.CustomCamera, _camera);

            internal void HideOverlays()
            {
                foreach (var root in new[] { UI.Overlays.IllustrationCardPopup.VisualRoot, UI.Gallery.IllustratorGalleryPopup.VisualRoot, UI.Patches.WeeklyReportPopupIllustrationPatch.VisualRoot })
                    if (root != null) { _hidden.Add(Tuple.Create(root, root.IsVisible)); root.IsVisible = false; }
            }

            internal bool BeginPanorama()
            {
                _screen = _owner as MissionScreen;
                if (!IsCurrent || _mission == null || _screen?.CombatCamera == null ||
                    !ReferenceEquals(_screen.Mission, _mission) || _screen.CustomCamera != null || !_screen.MissionStartedRendering()) return false;
                _originalFrame = _screen.CombatCamera.Frame;
                _originalFov = _screen.CombatCamera.HorizontalFov;
                _near = _screen.CombatCamera.Near;
                _far = _screen.CombatCamera.Far;
                _camera = Camera.CreateCamera();
                _camera.SetFovHorizontal(PanoramaFov, Screen.AspectRatio, _near, _far);
                _camera.Frame = _originalFrame;
                _screen.CustomCamera = _camera;
                return true;
            }

            internal bool SetDirection(int index)
            {
                if (!OwnsCamera) return false;
                _camera.Frame = BuildPanoramaFrame(_originalFrame, index);
                return true;
            }

            internal bool IsRenderedDirection(int index)
            {
                if (!OwnsCamera || _screen.CombatCamera == null) return false;
                var expected = BuildPanoramaFrame(_originalFrame, index);
                var actual = _screen.CombatCamera.Frame;
                // Fixed/zoom/custom camera controllers may have prevented the requested view.
                // Do not mislabel repeated screenshots as different panoramic directions.
                return IsUsablePanoramaFrame(expected, actual, _screen.CombatCamera.HorizontalFov);
            }

            internal void Restore()
            {
                IllustratorRuntime.AssertMainThread();
                if (_finished) return;
                bool restoreNative = OwnsCamera;
                _finished = true;
                if (ReferenceEquals(_activePanorama, this)) _activePanorama = null;
                try
                {
                    // Managed owner check: never overwrite a camera acquired by another system.
                    if (_camera != null && ReferenceEquals(_screen?.CustomCamera, _camera)) _screen.CustomCamera = null;
                    if (restoreNative && _screen.CombatCamera != null)
                    {
                        _screen.CombatCamera.Frame = _originalFrame;
                        _screen.CombatCamera.SetFovHorizontal(_originalFov, Screen.AspectRatio, _near, _far);
                    }
                }
                catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Scene camera restore: " + ex.Message); }
                finally
                {
                    // MissionScreen copies CustomCamera parameters into CombatCamera; the renderer
                    // never receives this temporary camera or a new Scene/TableauView.
                    try { _camera?.ReleaseCamera(); }
                    catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Scene camera release: " + ex.Message); }
                    _camera = null;
                    foreach (var entry in _hidden)
                        try { entry.Item1.IsVisible = entry.Item2; }
                        catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Scene overlay restore: " + ex.Message); }
                }
            }
        }
    }
}
