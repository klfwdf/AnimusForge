using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Illustrator.Engine
{
    /// <summary>A private, frozen copy of visible scene entities. It never owns the source mission scene.</summary>
    internal sealed class PanoramaSceneSnapshot
    {
        private readonly UIntPtr _sourcePointer;
        private bool _rendered;
        private bool _disposed;
        private readonly TaskCompletionSource<bool> _retirement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task Retirement => _retirement.Task;
        internal bool IsDisposed => _disposed;
        internal Action ReleaseSourceHandles { get; set; }
        internal Scene Scene { get; private set; }
        internal int CopiedRoots { get; set; }
        internal int SkippedAnimated { get; set; }
        internal int SkippedNonGeometry { get; set; }
        internal int InspectedNodes { get; set; }
        internal int SourceRoots { get; set; }
        internal int Batches { get; set; }
        internal double TotalMilliseconds { get; set; }
        internal double MaxBatchMilliseconds { get; set; }
        internal bool TerrainOmitted { get; private set; }
        internal string Notes =>
            "多镜头来自当前现场可见静态网格的独立冻结副本；保留建筑、门窗、陈设和实体材质的空间关系。" +
            "环境快照主动省略人物、坐骑及含骨骼或布料模拟的动态实体，不能据此推断现场人数；人物位置以现场事实与当前画面为准。" +
            "全景用于识别网格形态、布局、装饰和材质图案；采用中性材质观察光与固定曝光，不复制原场景灯光、阴影缓存、粒子或物理组件，不代表现场采光。" +
            "完整天空、环境光、烘焙间接光和曝光无法从引擎公开接口精确回读；现场昼夜、光照及颜色以附加的真实当前画面为准。" +
            (SkippedNonGeometry > 0 ? "没有普通静态网格的特殊组件已省略，不以空缺推断现场没有装饰。" : string.Empty) +
            (TerrainOmitted ? "本现场含原生地形，副本没有复制地形高度场、地形混合材质和水面；这些空缺不是悬空、平地或室内的证据。" : string.Empty);

        internal PanoramaSceneSnapshot(Scene scene, UIntPtr sourcePointer, bool terrainOmitted)
        {
            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _sourcePointer = sourcePointer;
            TerrainOmitted = terrainOmitted;
            AssertPrivateScene();
        }

        private void AssertPrivateScene()
        {
            if (Scene == null || Scene.Pointer == UIntPtr.Zero || Scene.Pointer == _sourcePointer)
                throw new InvalidOperationException("全景快照未获得独立场景，拒绝操作当前任务场景。");
        }

        // The renderer calls this before enabling its view; its deferred AddClearTask(false)
        // then owns retirement. Do not ClearAll a scene after the engine has rendered it.
        internal void MarkRendered()
        {
            IllustratorRuntime.AssertMainThread();
            if (_disposed) throw new ObjectDisposedException(nameof(PanoramaSceneSnapshot));
            AssertPrivateScene();
            _rendered = true;
            ReleaseSourceHandles?.Invoke();
            _retirement.TrySetResult(true);
        }

        internal void DisposeUnrendered()
        {
            IllustratorRuntime.AssertMainThread();
            if (_disposed || _rendered) return;
            AssertPrivateScene();
            _disposed = true;
            Scene owned = Scene;
            Scene = null;
            try { ReleaseSourceHandles?.Invoke(); owned.ClearAll(); }
            finally { try { owned.ManualInvalidate(); } finally { _retirement.TrySetResult(true); } }
        }
    }

    public static partial class ScreenCaptureHelper
    {
        internal static PanoramaSceneSnapshot _pendingPanoramaSnapshot;
        internal const int PanoramaSnapshotMaxRoots = 4096;
        internal const int PanoramaSnapshotBatchRoots = 8;
        internal const double PanoramaSnapshotBatchMilliseconds = 4;
        internal const int PanoramaSnapshotMaxNodes = 32768;
        internal const int PanoramaSnapshotMaxCopies = 1024;

        internal static bool ShouldCopyPanoramaRoot(bool valid, bool visible, bool helper, bool animated, bool agent)
            => valid && visible && !helper && !animated && !agent;

        internal static bool ContinuePanoramaSnapshotBatch(int processed, double elapsedMilliseconds)
            => processed < PanoramaSnapshotBatchRoots &&
                (processed == 0 || elapsedMilliseconds < PanoramaSnapshotBatchMilliseconds);

        internal static bool PanoramaSnapshotRootCountAllowed(int roots)
            => roots >= 0 && roots <= PanoramaSnapshotMaxRoots;

        internal static async Task<PanoramaSceneSnapshot> CreatePanoramaSnapshotAsync(Mission mission, CancellationToken token)
        {
            PanoramaSnapshotBuilder builder = null;
            bool handedToCaller = false;
            try
            {
                builder = await RunOnGameThreadAsync(() => PanoramaSnapshotBuilder.Begin(mission, token), token).ConfigureAwait(false);
                if (builder == null) throw new InvalidOperationException("无法在游戏线程创建环境快照。");
                while (!await RunOnGameThreadAsync(() => builder.CopyBatch(token), token).ConfigureAwait(false))
                {
                    Task nextFrame = await RunOnGameThreadAsync(() => IllustratorRuntime.AfterFramesAsync(1, token), token).ConfigureAwait(false);
                    if (nextFrame == null) throw new InvalidOperationException("环境快照分帧调度已停止。");
                    await nextFrame.ConfigureAwait(false);
                }
                token.ThrowIfCancellationRequested();
                handedToCaller = true;
                return builder.Snapshot;
            }
            finally
            {
                if (builder != null && !(builder.RootsReleased && (handedToCaller || builder.Snapshot.Retirement.IsCompleted)))
                {
                    Action cleanup = () =>
                    {
                        try { builder.ReleaseRoots(); }
                        finally { if (!handedToCaller) builder.Snapshot.DisposeUnrendered(); }
                    };
                    if (builder.Snapshot.Retirement.IsCompleted) { /* Shutdown already released the source handles synchronously. */ }
                    else if (IllustratorRuntime.IsMainThread) cleanup();
                    else
                    {
                        var retired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        IllustratorRuntime.PostCritical(() =>
                        {
                            try { cleanup(); }
                            catch (Exception ex) { TaleWorlds.Library.Debug.Print("[IllustratorPanorama] Snapshot cleanup failed: " + ex.GetType().Name); }
                            finally { retired.TrySetResult(true); }
                        });
                        // Shutdown can synchronously retire the registered snapshot and stop
                        // ticks before this queued callback runs. Either acknowledgement releases us.
                        Task retirement = await Task.WhenAny(retired.Task, builder.Snapshot.Retirement).ConfigureAwait(false);
                        await retirement.ConfigureAwait(false);
                    }
                }
            }
        }

        private sealed class PanoramaSnapshotBuilder
        {
            private sealed class EntitySelection
            {
                internal GameEntity Entity;
                internal bool CanCopy;
            }
            private struct PendingEntity
            {
                internal GameEntity Entity;
                internal PendingEntity(GameEntity entity) { Entity = entity; }
            }
            private readonly Mission _mission;
            private readonly Scene _source;
            private NativeObjectArray _roots;
            private readonly int _rootCount;
            private int _nextRoot;
            private readonly HashSet<UIntPtr> _agentRoots;
            private readonly Stack<PendingEntity> _pending = new Stack<PendingEntity>();
            private readonly List<EntitySelection> _nodes = new List<EntitySelection>();
            private int _phase; // inspect once, then copy individual static mesh components
            private int _nodeIndex;
            private int _meshIndex;
            private MatrixFrame _nodeFrame;
            private readonly Stopwatch _totalWatch = Stopwatch.StartNew();
            internal PanoramaSceneSnapshot Snapshot { get; }
            internal bool RootsReleased => _roots == null;

            private PanoramaSnapshotBuilder(Mission mission, Scene source, NativeObjectArray roots,
                PanoramaSceneSnapshot snapshot, HashSet<UIntPtr> agents)
            {
                _mission = mission; _source = source; _roots = roots;
                _rootCount = roots.Count; Snapshot = snapshot; _agentRoots = agents;
                snapshot.SourceRoots = _rootCount;
                snapshot.ReleaseSourceHandles = ReleaseRoots;
            }

            internal static PanoramaSnapshotBuilder Begin(Mission mission, CancellationToken token)
            {
                IllustratorRuntime.AssertMainThread();
                token.ThrowIfCancellationRequested();
                Scene source = mission?.Scene;
                ValidateMission(mission, source, token);
                if (!PanoramaSnapshotRootCountAllowed(source.RootEntityCount)) ThrowTooManyRoots();
                NativeObjectArray roots = null;
                Scene owned = null;
                PanoramaSceneSnapshot snapshot = null;
                try
                {
                    roots = NativeObjectArray.Create();
                    // The native enumeration is one bounded request; copying is batched below.
                    source.GetRootEntities(roots);
                    if (!PanoramaSnapshotRootCountAllowed(roots.Count)) ThrowTooManyRoots();
                    var agents = new HashSet<UIntPtr>();
                    foreach (Agent agent in mission.Agents)
                    {
                        var visuals = agent?.AgentVisuals;
                        if (visuals == null || !visuals.IsValid()) continue;
                        GameEntity entity = visuals.GetEntity();
                        if (entity != null && entity.WeakEntity.IsValid)
                            agents.Add((entity.Root ?? entity).Pointer);
                    }
                    token.ThrowIfCancellationRequested();
                    TaleWorlds.Library.Debug.Print("[IllustratorPanorama] Preparing mesh-only private scene.");
                    // Match vanilla Item/BannerTableau infrastructure. This creates no source
                    // bodies: only visual meshes are attached to this otherwise empty scene.
                    owned = Scene.CreateNewScene(initialize_physics: true, enable_decals: false,
                        sceneName: "af_environment_snapshot_" + Guid.NewGuid().ToString("N"));
                    snapshot = new PanoramaSceneSnapshot(owned, source.Pointer, source.ContainsTerrain || source.HasTerrainHeightmap);
                    _pendingPanoramaSnapshot = snapshot;
                    owned.SetDoNotAddEntitiesToTickList(true);
                    owned.SetClothSimulationState(false);
                    owned.SetPlaySoundEventsAfterReadyToRender(false);
                    owned.SetUseConstantTime(true);
                    owned.TimeSpeed = 0;
                    owned.TimeOfDay = source.TimeOfDay;
                    owned.DisableStaticShadows(true);
                    owned.SetAtmosphereWithName("character_menu_a");
                    // Only the private copy gets neutral inspection lighting. This is deliberately
                    // labelled in Notes rather than misrepresented as the mission's real exposure.
                    owned.SetDefaultLighting();
                    owned.SetMinExposure(0f);
                    owned.SetMaxExposure(0f);
                    owned.SetTargetExposure(0f);
                    Vec3 sunDirection = source.GetSunDirection();
                    owned.SetSunDirection(ref sunDirection);
                    // Visibility was resolved against the source already. New mesh carriers
                    // use their default upgrade level; do not hide them with a copied level mask.
                    // Do not create weather/particle simulation in a geometry reference scene.
                    owned.EnsurePostfxSystem();
                    return new PanoramaSnapshotBuilder(mission, source, roots, snapshot, agents);
                }
                catch
                {
                    try { if (roots != null) { roots.Clear(); roots.ManualInvalidate(); } }
                    finally
                    {
                        if (snapshot != null) snapshot.DisposeUnrendered();
                        else if (owned != null && owned.Pointer != source.Pointer)
                        {
                            try { owned.ClearAll(); } finally { owned.ManualInvalidate(); }
                        }
                    }
                    throw;
                }
            }

            private static void ThrowTooManyRoots() => throw new InvalidOperationException(
                "当前场景根实体超过4096个，已停止环境快照，不能将截断副本标为完整全景。");

            private static void ValidateMission(Mission mission, Scene source, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                if (mission == null || source == null || !ReferenceEquals(Mission.Current, mission) ||
                    mission.CurrentState == Mission.State.EndingNextFrame || mission.CurrentState == Mission.State.Over ||
                    mission.Scene == null || mission.Scene.Pointer != source.Pointer)
                    throw new OperationCanceledException("环境快照对应的场景已退出或切换。", token);
            }

            internal bool CopyBatch(CancellationToken token)
            {
                IllustratorRuntime.AssertMainThread();
                ValidateMission(_mission, _source, token);
                if (Snapshot.IsDisposed) throw new OperationCanceledException("环境快照已取消。", token);
                var watch = Stopwatch.StartNew();
                int work = 0, copies = 0;
                // Inspection is bounded separately from costly native copies. A single native copy
                // cannot be interrupted, so 4ms is a soft budget, not a maximum frame duration.
                while (_phase < 2 && work < 64 && ContinuePanoramaSnapshotBatch(copies, watch.Elapsed.TotalMilliseconds) &&
                    (work == 0 || watch.Elapsed.TotalMilliseconds < PanoramaSnapshotBatchMilliseconds))
                {
                    token.ThrowIfCancellationRequested();
                    work++;
                    if (_phase == 0)
                    {
                        if (_pending.Count == 0 && _nextRoot < _rootCount)
                            _pending.Push(new PendingEntity(_roots.GetElementAt(_nextRoot++) as GameEntity));
                        if (_pending.Count == 0) { _phase = 1; _nodeIndex = 0; continue; }
                        InspectOne(_pending.Pop(), token);
                    }
                    else
                    {
                        if (_nodeIndex >= _nodes.Count) { _phase = 2; continue; }
                        EntitySelection node = _nodes[_nodeIndex];
                        GameEntity source = node.Entity;
                        if (!node.CanCopy || !source.WeakEntity.IsValid || source.Scene?.Pointer != _source.Pointer || !source.IsVisibleIncludeParents())
                        {
                            _nodeIndex++; _meshIndex = 0; continue;
                        }
                        int count = source.MultiMeshComponentCount;
                        if (_meshIndex >= count)
                        { if (count == 0) Snapshot.SkippedNonGeometry++; _nodeIndex++; _meshIndex = 0; continue; }
                        if (Snapshot.CopiedRoots >= PanoramaSnapshotMaxCopies)
                            throw new InvalidOperationException("环境快照超过1024个静态网格副本，已停止，不能将局部覆盖标为完整全景。");
                        if (_meshIndex == 0) _nodeFrame = source.GetGlobalFrame();
                        if (CopyStaticMesh(source, _meshIndex++, _nodeFrame)) { Snapshot.CopiedRoots++; copies++; }
                    }
                }
                ValidateMission(_mission, _source, token);
                Snapshot.Batches++;
                Snapshot.TotalMilliseconds = _totalWatch.Elapsed.TotalMilliseconds;
                Snapshot.MaxBatchMilliseconds = Math.Max(Snapshot.MaxBatchMilliseconds, watch.Elapsed.TotalMilliseconds);
                if (_phase < 2) return false;
                if (Snapshot.CopiedRoots == 0) throw new InvalidOperationException("没有可复制的现场环境实体，不能生成空白全景。");
                TaleWorlds.Library.Debug.Print($"[IllustratorPanorama] Mesh snapshot ready: meshes={Snapshot.CopiedRoots}, nodes={Snapshot.InspectedNodes}, batches={Snapshot.Batches}, elapsedMs={Snapshot.TotalMilliseconds:F0}; no source light/physics/script components copied.");
                ReleaseRoots();
                return true;
            }

            private void InspectOne(PendingEntity pending, CancellationToken token)
            {
                GameEntity entity = pending.Entity;
                bool valid = entity != null && entity.WeakEntity.IsValid && entity.Scene?.Pointer == _source.Pointer;
                if (!valid) return;
                if (_nodes.Count >= PanoramaSnapshotMaxNodes) ThrowTooManyNodes();
                bool visible = entity.IsVisibleIncludeParents();
                bool agent = _agentRoots.Contains(entity.Pointer);
                bool animated = entity.Skeleton != null || entity.ClothSimulatorComponentCount > 0;
                bool helper = (entity.EntityFlags & (EntityFlags.IsHelper | EntityFlags.Ignore)) != 0;
                _nodes.Add(new EntitySelection { Entity = entity,
                    CanCopy = ShouldCopyPanoramaRoot(valid, visible, helper, animated, agent) });
                Snapshot.InspectedNodes = _nodes.Count;
                if (agent || animated) { Snapshot.SkippedAnimated++; return; }
                int children = entity.ChildCount;
                if ((long)_nodes.Count + _pending.Count + children > PanoramaSnapshotMaxNodes) ThrowTooManyNodes();
                for (int i = children - 1; i >= 0; i--)
                {
                    token.ThrowIfCancellationRequested();
                    _pending.Push(new PendingEntity(entity.GetChild(i)));
                }
            }

            private static void ThrowTooManyNodes() => throw new InvalidOperationException(
                "当前环境实体层级超过32768个节点，已停止快照，不能将不完整副本标为完整全景。");

            private bool CopyStaticMesh(GameEntity source, int index, MatrixFrame frame)
            {
                MetaMesh original = source.GetMetaMesh(index);
                if (original == null) return false;
                MetaMesh mesh = original.CreateCopy();
                if (mesh == null || mesh.Pointer == UIntPtr.Zero || mesh.Pointer == original.Pointer)
                    throw new InvalidOperationException("无法取得独立的现场静态网格副本。");
                GameEntity entity = null;
                bool attached = false;
                try
                {
                    mesh.Frame = original.Frame;
                    // The same native attachment path used by BannerTableau: no copied
                    // light shadow maps, particle emitters, script instances or physics state.
                    entity = Snapshot.Scene.AddItemEntity(ref frame, mesh);
                    if (entity == null || entity.Scene?.Pointer != Snapshot.Scene.Pointer || entity.Pointer == source.Pointer)
                        throw new InvalidOperationException("静态网格没有正确进入独立场景。");
                    attached = true;
                    entity.EntityFlags |= EntityFlags.DoNotTick | EntityFlags.DontTickChildren;
                    entity.RecomputeBoundingBox();
                    entity.UpdateGlobalBounds();
                    entity.UpdateVisibilityMask();
                    return true;
                }
                finally
                {
                    // After AddItemEntity the scene owns the entity/mesh, as in vanilla BannerTableau.
                    if (attached)
                    {
                        try { entity.ManualInvalidate(); }
                        finally { mesh.ManualInvalidate(); }
                    }
                }
            }

            internal void ReleaseRoots()
            {
                IllustratorRuntime.AssertMainThread();
                _pending.Clear();
                _nodes.Clear();
                _agentRoots.Clear();
                NativeObjectArray roots = _roots;
                _roots = null;
                if (roots == null) return;
                try { roots.Clear(); } finally { roots.ManualInvalidate(); }
            }
        }
    }
}
