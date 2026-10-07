using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Core;

namespace AnimusForge.Illustrator.Engine
{
    internal sealed class MissionScreenshotPair
    {
        internal readonly byte[] Current, Reverse;
        internal MissionScreenshotPair(byte[] current, byte[] reverse) { Current = current; Reverse = reverse; }
    }

    public static partial class ScreenCaptureHelper
    {
        internal static bool TryAcquireMissionScreenshotStage() => _stageLock.Wait(0);
        internal static void ReleaseMissionScreenshotStage() => _stageLock.Release();
    }

    // One request, two native exports. Tick touches only this held mission/camera and task state.
    // File completion is a unique, exclusively readable, validated native BMP, NOT a guessed GPU fence.
    internal sealed class MissionScreenshotCapture
    {
        private const int PauseRequestId = 2147482007;
        private const int CaptureBudgetMs = 8000;
        private const int WarmupFrames = 4;
        private const int WarmupMilliseconds = 250;
        private readonly Mission _mission;
        private readonly MissionScreen _screen;
        private readonly MissionState _missionState;
        private readonly GameStateManager _states;
        private readonly Camera _oldCustom;
        private Camera _backup, _captureCamera;
        private readonly bool _oldHideUi, _oldPaused;
        private readonly float _oldTimeSpeed;
        private readonly Vec3 _pivot, _frontPosition, _rearPosition;
        private readonly bool _clean;
        private readonly string[] _paths;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private readonly TaskCompletionSource<MissionScreenshotPair> _done = new TaskCompletionSource<MissionScreenshotPair>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private Task<byte[]> _read;
        private byte[] _first;
        private int _index;
        private long _notBeforeFrame;
        private long _notBeforeMs;
        private bool _watchingLayers, _layersChanged;
        private bool _closed, _ownsStage, _ownsPause, _ownsStateDisable, _changedUi, _changedCamera, _changedPaused;
        internal Task<MissionScreenshotPair> Completion => _done.Task;

        internal MissionScreenshotCapture(Mission mission, Vec3 pivot, Vec3 bodyForward, float subjectRadius, bool clean)
        {
            IllustratorRuntime.AssertMainThread();
            _mission = mission;
            _screen = ScreenManager.TopScreen as MissionScreen;
            _missionState = MissionState.Current;
            _states = Game.Current?.GameStateManager;
            if (_mission?.Scene == null || _screen?.CombatCamera == null || _screen.SceneView == null || !ReferenceEquals(_screen.Mission, mission)
                || !_screen.MissionStartedRendering() || mission.MissionEnded)
                throw new InvalidOperationException("当前任务尚无可采集的游戏画面。");
            _oldCustom = _screen.CustomCamera;
            _oldHideUi = MBDebug.DisableAllUI;
            _oldPaused = _missionState?.Paused == true;
            _oldTimeSpeed = mission.Scene.TimeSpeed;
            _pivot = pivot;
            _clean = clean;
            string root = IllustratorStoragePaths.EnsureDirectory(IllustratorStoragePaths.TempDirectory);
            string id = Guid.NewGuid().ToString("N");
            _paths = new[] { System.IO.Path.Combine(root, "mission_" + id + "_a.bmp"), System.IO.Path.Combine(root, "mission_" + id + "_b.bmp") };
            try
            {
                _ownsStage = ScreenCaptureHelper.TryAcquireMissionScreenshotStage();
                if (!_ownsStage) throw new InvalidOperationException("原生图片采集正在进行，请等待后再点击。");
                if (mission.GetRequestedTimeSpeed(PauseRequestId, out float _)) throw new InvalidOperationException("截图暂停请求已被占用。");
                _backup = Camera.CreateCamera();
                _backup.FillParametersFrom(_screen.CombatCamera);
                _captureCamera = Camera.CreateCamera();
                // Preserve native camera setup, then replace pose/projection below.
                _captureCamera.FillParametersFrom(_backup);
                float aspect = _backup.GetAspectRatio();
                Vec3[] positions = BuildLevelPositions(pivot, bodyForward, subjectRadius, aspect);
                _frontPosition = ResolveCameraCollision(positions[0]);
                _rearPosition = ResolveCameraCollision(positions[1]);
                // Fixed perspective: the original camera is only a restore snapshot.
                _captureCamera.SetFovVertical((float)Math.PI / 3f, aspect, 0.05f, Math.Max(1000f, _backup.Far));
                _captureCamera.LookAt(_frontPosition, _pivot, Vec3.Up);
                mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(0f, PauseRequestId));
                _ownsPause = true;
                _states?.RegisterActiveStateDisableRequest(this);
                _ownsStateDisable = _states != null;
                if (_missionState != null) { _missionState.Paused = true; _changedPaused = true; }
                mission.Scene.TimeSpeed = 0f;
                MBDebug.DisableAllUI = true;
                _changedUi = true;
                foreach (var layer in _screen.Layers)
                    if (layer is GauntletLayer gauntlet && layer.IsActive && !layer.IsFinalized)
                    {
                        // Clear already-submitted UI commands as vanilla deactivation does.
                        // Keep layer activation/visibility ownership with the original UI.
                        gauntlet.TwoDimensionPlatform.Clear();
                        gauntlet.TwoDimensionView.Clear();
                    }
                _screen.OnAddLayer += OnLayerAdded;
                _watchingLayers = true;
                _changedCamera = true;
                _screen.CustomCamera = _captureCamera;
                ApplyCamera();
                BeginWarmup();
                _cancel.CancelAfter(CaptureBudgetMs);
            }
            catch { Restore(); _cancel.Dispose(); throw; }
        }

        internal void Tick()
        {
            IllustratorRuntime.AssertMainThread();
            if (_closed) return;
            try
            {
                if (!ReferenceEquals(Mission.Current, _mission) || !ReferenceEquals(ScreenManager.TopScreen, _screen)
                    || _screen.IsFinalized || _mission.MissionEnded)
                    throw new InvalidOperationException("双截图尚未完成时已离开场景，采集已终止。");
                if (_cancel.IsCancellationRequested || _clock.ElapsedMilliseconds >= CaptureBudgetMs)
                    throw new TimeoutException("双截图采集超时，已恢复原画面；未开始导演或生图。");
                if (!ReferenceEquals(_screen.CustomCamera, _captureCamera))
                    throw new InvalidOperationException("采集期间相机被其他功能接管，已终止采集。");
                if (!MBDebug.DisableAllUI || _layersChanged)
                    throw new InvalidOperationException("截图期间界面发生变化，已停止采集，未发送生图请求。");
                // Keep the fixed pose/FOV despite native zoom handling, without scanning scene agents.
                ApplyCamera();
                if (_read == null)
                {
                    if (IllustratorRuntime.ApplicationFrame < _notBeforeFrame || _clock.ElapsedMilliseconds < _notBeforeMs) return;
                    // More than the vanilla photo-mode UI warmup; still not a GPU completion fence.
                    Utilities.TakeScreenshot(_paths[_index]);
                    string path = _paths[_index];
                    CancellationToken token = _cancel.Token;
                    _read = Task.Run(() => MissionScreenshotImageCodec.ReadCompleteAsync(path, token), token);
                    return;
                }
                if (!_read.IsCompleted) return;
                byte[] raw = _read.GetAwaiter().GetResult();
                _read = null;
                if (_index == 0)
                {
                    _first = raw;
                    _index = 1;
                    _captureCamera.LookAt(_rearPosition, _pivot, Vec3.Up);
                    ApplyCamera();
                    BeginWarmup();
                    return;
                }
                var pair = new MissionScreenshotPair(_first, raw);
                _first = null;
                Restore();
                _done.TrySetResult(pair);
            }
            catch (Exception ex) { Cancel(ex.Message); }
        }

        // Pure geometry, evaluated once per click. No mouse camera position, pitch or zoom input.
        internal static Vec3[] BuildLevelPositions(Vec3 pivot, Vec3 bodyForward, float subjectRadius, float aspect)
        {
            Vec3 forward = new Vec3(bodyForward.x, bodyForward.y, 0f);
            float length = forward.Length;
            if (float.IsNaN(length) || float.IsInfinity(length) || length < 0.001f
                || float.IsNaN(aspect) || float.IsInfinity(aspect) || aspect <= 0f
                || float.IsNaN(subjectRadius) || float.IsInfinity(subjectRadius) || subjectRadius < 0f)
                throw new InvalidOperationException("人物朝向或截图视野无效，无法建立前后平视机位。");
            forward *= 1f / length;
            // Fit the subject region using the narrower screen dimension, with near-side depth margin.
            float radius = Math.Max(1.6f, subjectRadius);
            float halfFovTangent = (float)Math.Tan(Math.PI / 6) * Math.Min(1f, aspect);
            float distance = radius + radius / halfFovTangent;
            return new[] { pivot + forward * distance, pivot - forward * distance };
        }

        private Vec3 ResolveCameraCollision(Vec3 position)
        {
            Vec3 ray = position - _pivot;
            float distance = ray.Length;
            if (_mission.Scene.RayCastForClosestEntityOrTerrain(_pivot, position, out float hitDistance, 0.2f,
                BodyFlags.CameraCollisionRayCastExludeFlags | BodyFlags.DontCollideWithCamera))
            {
                if (hitDistance < 0.8f) throw new InvalidOperationException("前后平视机位被场景结构遮挡，已停止截图。");
                position = _pivot + ray * (Math.Min(distance, hitDistance - 0.3f) / distance);
            }
            return position;
        }

        private void OnLayerAdded(ScreenLayer layer) { if (layer is GauntletLayer) _layersChanged = true; }

        private void BeginWarmup()
        {
            _notBeforeFrame = IllustratorRuntime.ApplicationFrame + WarmupFrames;
            _notBeforeMs = _clock.ElapsedMilliseconds + WarmupMilliseconds;
        }

        private void ApplyCamera()
        {
            _captureCamera.SetFovVertical((float)Math.PI / 3f, _backup.GetAspectRatio(), 0.05f, Math.Max(1000f, _backup.Far));
            _screen.CombatCamera.FillParametersFrom(_captureCamera);
            _screen.SceneView.SetCamera(_screen.CombatCamera);
        }

        internal void Cancel(string reason)
        {
            IllustratorRuntime.AssertMainThread();
            if (_closed) return;
            Restore();
            _done.TrySetException(new InvalidOperationException(reason));
        }

        private void Restore()
        {
            if (_closed) return;
            _closed = true;
            _cancel.Cancel();
            _first = null;
            if (_watchingLayers) { _screen.OnAddLayer -= OnLayerAdded; _watchingLayers = false; }
            // Restore each owned resource independently, including failure/mission exit.
            if (_changedCamera && !_screen.IsFinalized && ReferenceEquals(_screen.CustomCamera, _captureCamera))
            {
                RestorePart(() => _screen.CustomCamera = _oldCustom, "custom_camera");
                RestorePart(() => {
                    _screen.CombatCamera.FillParametersFrom(_backup);
                    _screen.SceneView.SetCamera(_screen.CombatCamera);
                    SoundManager.SetListenerFrame(_backup.Frame);
                }, "combat_camera");
            }
            if (_changedUi) MBDebug.DisableAllUI = _oldHideUi;
            if (_ownsPause) RestorePart(() => { if (_mission.GetRequestedTimeSpeed(PauseRequestId, out float _)) _mission.RemoveTimeSpeedRequest(PauseRequestId); }, "time_request");
            if (_changedPaused && ReferenceEquals(MissionState.Current, _missionState)) _missionState.Paused = _oldPaused;
            if (ReferenceEquals(Mission.Current, _mission) && _mission.Scene != null)
                RestorePart(() => _mission.Scene.TimeSpeed = _oldTimeSpeed, "scene_time");
            if (_ownsStateDisable) RestorePart(() => _states.UnregisterActiveStateDisableRequest(this), "state_disable");
            if (_captureCamera != null) RestorePart(() => _captureCamera.ReleaseCamera(), "capture_camera_release");
            if (_backup != null) RestorePart(() => _backup.ReleaseCamera(), "backup_camera_release");
            if (_ownsStage) { ScreenCaptureHelper.ReleaseMissionScreenshotStage(); _ownsStage = false; }
            CleanTempFiles();
            // Dispose the token owner after the file reader has observed cancellation.
            Task reader = _read;
            if (reader == null || reader.IsCompleted) _cancel.Dispose();
            else reader.ContinueWith(t => { var observed = t.Exception; CleanTempFiles(); _cancel.Dispose(); }, TaskScheduler.Default);
        }

        private void CleanTempFiles()
        {
            if (_clean && _paths != null)
                foreach (string path in _paths) RestorePart(() => { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }, "temp_file");
        }

        private static void RestorePart(Action action, string part)
        {
            try { action(); }
            catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Screenshot restore " + part + ": " + ex.GetType().Name); }
        }
    }
}
