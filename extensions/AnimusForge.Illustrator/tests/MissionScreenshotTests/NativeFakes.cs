// Native-free fixture for the PRODUCTION capture state machine. It cannot validate GPU frames/HUD.
using System;
using System.Collections.Generic;
using System.Threading;

namespace TaleWorlds.Library
{
    public struct Vec3
    {
        public float x, y, z;
        public Vec3(float a, float b, float c) { x = a; y = b; z = c; }
        public static Vec3 Up => new Vec3(0, 0, 1);
        public float Length => (float)Math.Sqrt(x * x + y * y + z * z);
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.x * s, a.y * s, a.z * s);
    }
    public struct MatrixFrame { public Vec3 origin; }
    public static class Debug { public static void Print(string text) { } }
}
namespace TaleWorlds.Engine
{
    using TaleWorlds.Library;
    [Flags] public enum BodyFlags { None = 0, CameraCollisionRayCastExludeFlags = 1, DontCollideWithCamera = 2 }
    public sealed class Scene
    {
        public float TimeSpeed = 1;
        public float HitDistance = -1;
        public BodyFlags LastExclude;
        public bool RayCastForClosestEntityOrTerrain(Vec3 a, Vec3 b, out float distance, float radius, BodyFlags flags)
        { LastExclude = flags; distance = HitDistance; return HitDistance >= 0; }
    }
    public sealed class Camera
    {
        public MatrixFrame Frame;
        public Vec3 Position => Frame.origin;
        public bool Released;
        public static int Created, ReleasedCount;
        public static Camera CreateCamera() { Created++; return new Camera(); }
        public void FillParametersFrom(Camera other) { Frame = other.Frame; }
        public void LookAt(Vec3 position, Vec3 target, Vec3 up) { Frame = new MatrixFrame { origin = position }; }
        public void ReleaseCamera() { if (Released) throw new Exception("camera double release"); Released = true; ReleasedCount++; }
    }
    public sealed class SceneView { public Camera Camera; public void SetCamera(Camera camera) { Camera = camera; } }
    public static class Utilities { public static Action<string> Export; public static void TakeScreenshot(string path) => Export(path); }
    public static class SoundManager { public static void SetListenerFrame(MatrixFrame frame) { } }
}
namespace TaleWorlds.Core
{
    public sealed class Game { public static Game Current; public GameStateManager GameStateManager = new GameStateManager(); }
    public sealed class GameStateManager
    {
        private readonly HashSet<object> _owners = new HashSet<object>();
        public int Count => _owners.Count;
        public void RegisterActiveStateDisableRequest(object owner) => _owners.Add(owner);
        public void UnregisterActiveStateDisableRequest(object owner) => _owners.Remove(owner);
    }
}
namespace TaleWorlds.MountAndBlade
{
    using TaleWorlds.Engine;
    public static class MBDebug { public static bool DisableAllUI; }
    public sealed class MissionState { public static MissionState Current; public bool Paused; }
    public sealed class Mission
    {
        public static Mission Current;
        public Scene Scene = new Scene();
        public bool MissionEnded;
        private readonly Dictionary<int, float> _requests = new Dictionary<int, float>();
        public int RequestCount => _requests.Count;
        public sealed class TimeSpeedRequest { public float Speed; public int Id; public TimeSpeedRequest(float speed, int id) { Speed = speed; Id = id; } }
        public bool GetRequestedTimeSpeed(int id, out float speed) => _requests.TryGetValue(id, out speed);
        public void AddTimeSpeedRequest(TimeSpeedRequest request) => _requests.Add(request.Id, request.Speed);
        public void RemoveTimeSpeedRequest(int id) => _requests.Remove(id);
    }
}
namespace TaleWorlds.ScreenSystem
{
    public class ScreenBase { public bool IsFinalized; }
    public static class ScreenManager { public static ScreenBase TopScreen; }
}
namespace TaleWorlds.MountAndBlade.View.Screens
{
    using TaleWorlds.Engine;
    using TaleWorlds.ScreenSystem;
    public sealed class MissionScreen : ScreenBase
    {
        public Mission Mission;
        public Camera CombatCamera = new Camera(), CustomCamera;
        public SceneView SceneView = new SceneView();
        public bool Rendered = true;
        public bool MissionStartedRendering() => Rendered;
    }
}
namespace AnimusForge.Illustrator.Core
{
    public static class IllustratorRuntime
    {
        public static long ApplicationFrame;
        public static int OwnerThread;
        public static void AssertMainThread() { if (Thread.CurrentThread.ManagedThreadId != OwnerThread) throw new Exception("off-thread native access"); }
    }
    public static class IllustratorStoragePaths
    {
        public static string TempDirectory;
        public static string EnsureDirectory(string path) { System.IO.Directory.CreateDirectory(path); return path; }
    }
}
namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper { private static readonly SemaphoreSlim _stageLock = new SemaphoreSlim(1, 1); }
}
