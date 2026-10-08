using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.UI.Overlays;

namespace AnimusForge.Illustrator.Engine
{
    internal sealed class MissionScreenshotPair
    {
        internal readonly byte[] Current, Reverse;
        internal MissionScreenshotPair(byte[] current, byte[] reverse) { Current = current; Reverse = reverse; }
        internal int Count => Reverse == null ? 1 : 2;
    }
    public static partial class ScreenCaptureHelper
    {
        internal static bool TryAcquireMissionScreenshotStage() => _stageLock.Wait(0);
        internal static void ReleaseMissionScreenshotStage() => _stageLock.Release();
    }

    // Interactive free camera: no game ticks/scans or generation worker while the player composes.
    // Each Enter has a finite native file-export deadline; a user-controlled session has no aim timeout.
    internal sealed class MissionScreenshotCapture
    {
        private const int PauseRequestId = 2147482007;
        private const int CaptureBudgetMs = 8000;
        private readonly Mission _mission;
        private readonly MissionScreen _screen;
        private readonly MissionState _missionState;
        private readonly GameStateManager _states;
        private readonly Camera _oldCustom;
        private Camera _backup, _captureCamera;
        private MissionPhotoOverlay _overlay;
        private readonly bool _oldHideUi, _oldPaused;
        private readonly float _oldTimeSpeed;
        private readonly bool _clean;
        private readonly TaskCompletionSource<MissionScreenshotPair> _done = new TaskCompletionSource<MissionScreenshotPair>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _lastCameraMs, _exportStart, _exportFrame;
        private Task<byte[]> _read;
        private CancellationTokenSource _shotCancel;
        private string _shotPath;
        private byte[] _first, _second;
        private float _yaw, _pitch;
        // Aim, Export, Decide, CancelChoice. Buttons are guarded by their matching phase.
        private int _phase;
        private bool _keysReleased;
        private bool _closed, _ownsStage, _ownsPause, _ownsStateDisable, _changedUi, _changedCamera, _changedPaused;
        internal Task<MissionScreenshotPair> Completion => _done.Task;

        internal MissionScreenshotCapture(Mission mission, bool clean)
        {
            IllustratorRuntime.AssertMainThread();
            _mission = mission;
            _screen = ScreenManager.TopScreen as MissionScreen;
            _missionState = MissionState.Current;
            _states = Game.Current?.GameStateManager;
            if (_mission?.Scene == null || _screen?.CombatCamera == null || _screen.SceneView == null || !ReferenceEquals(_screen.Mission, mission)
                || !_screen.MissionStartedRendering() || mission.MissionEnded || GameNetwork.IsMultiplayer)
                throw new InvalidOperationException("当前任务尚无可采集的单人游戏画面。");
            // A different custom/photo camera remains its owner's responsibility.
            if (_screen.IsPhotoModeEnabled) throw new InvalidOperationException("请先退出原版摄影模式，再从生图按钮开始取景。");
            _oldCustom = _screen.CustomCamera;
            _oldHideUi = MBDebug.DisableAllUI;
            _oldPaused = _missionState?.Paused == true;
            _oldTimeSpeed = mission.Scene.TimeSpeed;
            _clean = clean;
            try
            {
                _ownsStage = ScreenCaptureHelper.TryAcquireMissionScreenshotStage();
                if (!_ownsStage) throw new InvalidOperationException("原生图片采集正在进行，请等待后再点击。");
                if (mission.GetRequestedTimeSpeed(PauseRequestId, out float _)) throw new InvalidOperationException("截图暂停请求已被占用。");
                _backup = Camera.CreateCamera();
                _backup.FillParametersFrom(_screen.CombatCamera);
                _captureCamera = Camera.CreateCamera();
                _captureCamera.FillParametersFrom(_oldCustom ?? _backup);
                Vec3 forward = _captureCamera.Frame.rotation.u * -1f;
                _yaw = (float)Math.Atan2(forward.x, forward.y);
                _pitch = (float)Math.Asin(Math.Max(-1f, Math.Min(1f, forward.z)));
                mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(0f, PauseRequestId));
                _ownsPause = true;
                _states?.RegisterActiveStateDisableRequest(this);
                _ownsStateDisable = _states != null;
                if (_missionState != null) { _missionState.Paused = true; _changedPaused = true; }
                mission.Scene.TimeSpeed = 0f;
                _changedUi = true;
                MBDebug.DisableAllUI = false; // Only our hint/buttons remain; previous Gauntlet roots are hidden.
                _changedCamera = true;
                _screen.CustomCamera = _captureCamera;
                _overlay = new MissionPhotoOverlay(_screen, Submit, Continue, Restart, () => Cancel("本次取景已取消。"));
                ResumeAim();
                ApplyCamera();
            }
            catch { Restore(); throw; }
        }
        internal void Tick()
        {
            IllustratorRuntime.AssertMainThread();
            if (_closed) return;
            try
            {
                if (!ReferenceEquals(Mission.Current, _mission) || !ReferenceEquals(ScreenManager.TopScreen, _screen)
                    || _screen.IsFinalized || _mission.MissionEnded)
                    throw new InvalidOperationException("取景尚未完成时已离开场景，采集已终止。");
                if (!ReferenceEquals(_screen.CustomCamera, _captureCamera))
                    throw new InvalidOperationException("取景期间相机被其他功能接管，已终止采集。");
                _overlay.HideOtherUi();
                bool enter = Input.IsKeyDown(InputKey.Enter) || Input.IsKeyDown(InputKey.NumpadEnter);
                bool escape = Input.IsKeyDown(InputKey.Escape);
                if (!enter && !escape) _keysReleased = true;
                else if (_keysReleased)
                {
                    _keysReleased = false;
                    if (escape) Escape();
                    else if (_phase == 0) BeginExport();
                }
                if (_phase == 0) MoveCamera();
                ApplyCamera();
                if (_phase == 1) ProcessExport();
            }
            catch (Exception ex) { Fail(ex.Message); }
        }
        private void MoveCamera()
        {
            long now = _clock.ElapsedMilliseconds;
            float dt = Math.Min(0.05f, Math.Max(0f, (now - _lastCameraMs) / 1000f));
            _lastCameraMs = now;
            _yaw += Input.MouseMoveX * 0.0025f;
            _pitch = Math.Max(-1.5f, Math.Min(1.5f, _pitch - Input.MouseMoveY * 0.0025f));
            Vec3 forward = new Vec3((float)Math.Sin(_yaw) * (float)Math.Cos(_pitch), (float)Math.Cos(_yaw) * (float)Math.Cos(_pitch), (float)Math.Sin(_pitch));
            Vec3 right = new Vec3((float)Math.Cos(_yaw), -(float)Math.Sin(_yaw), 0f);
            Vec3 movement = Vec3.Zero;
            if (Input.IsKeyDown(InputKey.W)) movement += forward;
            if (Input.IsKeyDown(InputKey.S)) movement -= forward;
            if (Input.IsKeyDown(InputKey.D)) movement += right;
            if (Input.IsKeyDown(InputKey.A)) movement -= right;
            if (Input.IsKeyDown(InputKey.E)) movement += Vec3.Up;
            if (Input.IsKeyDown(InputKey.Q)) movement -= Vec3.Up;
            float speed = Input.IsKeyDown(InputKey.LeftShift) ? 9f : 3f;
            if (movement.Length > 1f) movement *= 1f / movement.Length;
            Vec3 position = _captureCamera.Position;
            Vec3 next = position + movement * (speed * dt);
            // Camera collision only, never an Agent query. No movement means no collision ray.
            if (movement.Length > 0.001f && _mission.Scene.RayCastForClosestEntityOrTerrain(position, next, out float hit, 0.15f,
                BodyFlags.CameraCollisionRayCastExludeFlags | BodyFlags.DontCollideWithCamera))
            {
                float distance = (next - position).Length;
                next = position + (next - position) * Math.Max(0f, Math.Min(1f, (hit - 0.15f) / Math.Max(0.001f, distance)));
            }
            _captureCamera.LookAt(next, next + forward, Vec3.Up);
            if (Input.DeltaMouseScroll != 0f)
                _captureCamera.SetFovVertical(Math.Max(0.25f, Math.Min(1.6f, _captureCamera.GetFovVertical() - Input.DeltaMouseScroll * 0.0005f)),
                    _backup.GetAspectRatio(), 0.05f, _backup.Far);
        }
        private void ApplyCamera()
        {
            _screen.CombatCamera.FillParametersFrom(_captureCamera);
            _screen.SceneView.SetCamera(_screen.CombatCamera);
        }
        private void BeginExport()
        {
            if (_phase != 0 || _second != null) return;
            _phase = 1;
            _overlay.Capturing();
            MBDebug.DisableAllUI = true;
            _exportStart = _clock.ElapsedMilliseconds;
            _exportFrame = IllustratorRuntime.ApplicationFrame;
        }
        private void ProcessExport()
        {
            if (_clock.ElapsedMilliseconds - _exportStart >= CaptureBudgetMs) throw new TimeoutException("截图导出超时，已恢复原画面；未发送生图请求。");
            if (_read == null)
            {
                if (IllustratorRuntime.ApplicationFrame - _exportFrame < 4 || _clock.ElapsedMilliseconds - _exportStart < 250) return;
                _shotPath = System.IO.Path.Combine(IllustratorStoragePaths.EnsureDirectory(IllustratorStoragePaths.TempDirectory), "mission_" + Guid.NewGuid().ToString("N") + ".bmp");
                _shotCancel = new CancellationTokenSource(CaptureBudgetMs);
                var path = _shotPath;
                var token = _shotCancel.Token;
                Utilities.TakeScreenshot(path);
                _read = Task.Run(() => MissionScreenshotImageCodec.ReadCompleteAsync(path, token), token);
                return;
            }
            if (!_read.IsCompleted) return;
            byte[] raw = _read.GetAwaiter().GetResult();
            ClearReader();
            if (_first == null) _first = raw; else _second = raw;
            _phase = 2;
            MBDebug.DisableAllUI = false;
            _overlay.Decision(_second == null ? 1 : 2);
        }
        private void Continue()
        {
            if (_closed || _phase != 2 || _first == null || _second != null) return;
            ResumeAim();
        }
        private void Restart()
        {
            if (_closed || (_phase != 2 && _phase != 3)) return;
            ClearReader();
            _first = _second = null;
            ResumeAim();
        }
        private void ResumeAim()
        {
            _phase = 0;
            _keysReleased = false;
            _lastCameraMs = _clock.ElapsedMilliseconds;
            MBDebug.DisableAllUI = false;
            _overlay.Aim(_first == null ? 0 : 1);
        }
        private void Escape()
        {
            ClearReader();
            _first = _second = null;
            _phase = 3;
            MBDebug.DisableAllUI = false;
            _overlay.CancelChoice();
        }
        private void Submit()
        {
            if (_closed || _phase != 2 || _first == null) return;
            var result = new MissionScreenshotPair(_first, _second);
            Restore();
            _done.TrySetResult(result);
        }
        internal void Cancel(string reason)
        {
            IllustratorRuntime.AssertMainThread();
            if (_closed) return;
            Restore();
            _done.TrySetCanceled();
        }
        private void Fail(string reason)
        {
            Restore();
            _done.TrySetException(new InvalidOperationException(reason));
        }
        private void ClearReader()
        {
            var reader = _read; var source = _shotCancel; var path = _shotPath;
            _read = null; _shotCancel = null; _shotPath = null;
            source?.Cancel();
            Action cleanup = () => {
                if (_clean && path != null) RestorePart(() => { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }, "temp_file");
                source?.Dispose();
            };
            if (reader == null || reader.IsCompleted) { var observed = reader?.Exception; cleanup(); }
            else reader.ContinueWith(t => { var observed = t.Exception; cleanup(); }, TaskScheduler.Default);
        }
        private void Restore()
        {
            if (_closed) return;
            _closed = true;
            ClearReader();
            _first = _second = null;
            RestorePart(() => _overlay?.Dispose(), "photo_ui");
            if (_changedCamera && !_screen.IsFinalized && ReferenceEquals(_screen.CustomCamera, _captureCamera))
            {
                RestorePart(() => _screen.CustomCamera = _oldCustom, "custom_camera");
                RestorePart(() => { _screen.CombatCamera.FillParametersFrom(_backup); _screen.SceneView.SetCamera(_screen.CombatCamera); SoundManager.SetListenerFrame(_backup.Frame); }, "combat_camera");
            }
            if (_changedUi) MBDebug.DisableAllUI = _oldHideUi;
            if (_ownsPause) RestorePart(() => { if (_mission.GetRequestedTimeSpeed(PauseRequestId, out float _)) _mission.RemoveTimeSpeedRequest(PauseRequestId); }, "time_request");
            if (_changedPaused && ReferenceEquals(MissionState.Current, _missionState)) _missionState.Paused = _oldPaused;
            if (ReferenceEquals(Mission.Current, _mission) && _mission.Scene != null) RestorePart(() => _mission.Scene.TimeSpeed = _oldTimeSpeed, "scene_time");
            if (_ownsStateDisable) RestorePart(() => _states.UnregisterActiveStateDisableRequest(this), "state_disable");
            if (_captureCamera != null) RestorePart(() => _captureCamera.ReleaseCamera(), "capture_camera_release");
            if (_backup != null) RestorePart(() => _backup.ReleaseCamera(), "backup_camera_release");
            if (_ownsStage) { ScreenCaptureHelper.ReleaseMissionScreenshotStage(); _ownsStage = false; }
        }
        private static void RestorePart(Action action, string part)
        {
            try { action(); }
            catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Screenshot restore " + part + ": " + ex.GetType().Name); }
        }
    }
}
