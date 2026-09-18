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
        internal int SplitNodes { get; set; }
        internal int InspectedNodes { get; set; }
        internal int SourceRoots { get; set; }
        internal int Batches { get; set; }
        internal double TotalMilliseconds { get; set; }
        internal double MaxBatchMilliseconds { get; set; }
        internal bool TerrainOmitted { get; private set; }
        internal string Notes =>
            "多镜头来自当前现场可见实体的独立冻结副本；保留建筑、门窗、陈设和实体材质的空间关系。" +
            "环境快照主动省略人物、坐骑及含骨骼的动态实体，不能据此推断现场人数；人物位置以现场事实与当前画面为准。" +
            "全景用于识别网格形态、布局、装饰和材质图案；采用中性材质观察光与固定曝光，保留可复制的局部灯光，但不代表现场采光。" +
            "完整天空、环境光、烘焙间接光和曝光无法从引擎公开接口精确回读；现场昼夜、光照及颜色以附加的真实当前画面为准。" +
            (SplitNodes > 0 ? "混合骨骼父节点已拆分保留自身静态网格与安全子树，其自身粒子、布料及特殊灯光组件未复制。" : string.Empty) +
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

        // 0: omit, 1: copy complete safe subtree, 2: preserve only this mixed parent's own meshes.
        internal static int SelectPanoramaCopyMode(bool canCopy, bool hasAnimatedSubtree, bool ancestorCopied)
            => !canCopy || ancestorCopied ? 0 : hasAnimatedSubtree ? 2 : 1;

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
                            try { cleanup(); retired.TrySetResult(true); }
                            catch (Exception ex) { retired.TrySetException(ex); }
                        });
                        await retired.Task.ConfigureAwait(false);
                    }
                }
            }
        }

        private sealed class PanoramaSnapshotBuilder
        {
            private sealed class EntitySelection
            {
                internal GameEntity Entity;
                internal int Parent;
                internal bool CanCopy;
                internal bool HasAnimatedSubtree;
                internal bool Covered;
            }
            private struct PendingEntity
            {
                internal GameEntity Entity;
                internal int Parent;
                internal PendingEntity(GameEntity entity, int parent) { Entity = entity; Parent = parent; }
            }
            private readonly Mission _mission;
            private readonly Scene _source;
            private NativeObjectArray _roots;
            private readonly int _rootCount;
            private int _nextRoot;
            private readonly HashSet<UIntPtr> _agentRoots;
            private readonly Stack<PendingEntity> _pending = new Stack<PendingEntity>();
            private readonly List<EntitySelection> _nodes = new List<EntitySelection>();
            private int _phase; // collect once, propagate unsafe children once, copy maximal safe subtrees
            private int _nodeIndex;
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
                    owned = Scene.CreateNewScene(initialize_physics: false, enable_decals: true,
                        sceneName: "af_environment_snapshot_" + Guid.NewGuid().ToString("N"));
                    snapshot = new PanoramaSceneSnapshot(owned, source.Pointer, source.ContainsTerrain || source.HasTerrainHeightmap);
                    _pendingPanoramaSnapshot = snapshot;
                    owned.SetDoNotAddEntitiesToTickList(true);
                    owned.SetClothSimulationState(false);
                    owned.SetPlaySoundEventsAfterReadyToRender(false);
                    owned.SetUseConstantTime(true);
                    owned.TimeSpeed = 0;
                    owned.TimeOfDay = source.TimeOfDay;
                    // Only the private copy gets neutral inspection lighting. This is deliberately
                    // labelled in Notes rather than misrepresented as the mission's real exposure.
                    owned.SetDefaultLighting();
                    owned.SetMinExposure(0f);
                    owned.SetMaxExposure(0f);
                    owned.SetTargetExposure(0f);
                    Vec3 sunDirection = source.GetSunDirection();
                    owned.SetSunDirection(ref sunDirection);
                    owned.SetRainDensity(source.GetRainDensity());
                    owned.SetSnowDensity(source.GetSnowDensity());
                    owned.SetWinterTimeFactor(source.GetWinterTimeFactor());
                    owned.SetUpgradeLevelVisibility(source.GetUpgradeLevelMask());
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
                while (_phase < 3 && work < 64 && ContinuePanoramaSnapshotBatch(copies, watch.Elapsed.TotalMilliseconds) &&
                    (work == 0 || watch.Elapsed.TotalMilliseconds < PanoramaSnapshotBatchMilliseconds))
                {
                    token.ThrowIfCancellationRequested();
                    work++;
                    if (_phase == 0)
                    {
                        if (_pending.Count == 0 && _nextRoot < _rootCount)
                            _pending.Push(new PendingEntity(_roots.GetElementAt(_nextRoot++) as GameEntity, -1));
                        if (_pending.Count == 0) { _phase = 1; _nodeIndex = _nodes.Count - 1; continue; }
                        InspectOne(_pending.Pop(), token);
                    }
                    else if (_phase == 1)
                    {
                        if (_nodeIndex < 0) { _phase = 2; _nodeIndex = 0; continue; }
                        EntitySelection node = _nodes[_nodeIndex--];
                        if (node.HasAnimatedSubtree && node.Parent >= 0) _nodes[node.Parent].HasAnimatedSubtree = true;
                    }
                    else
                    {
                        if (_nodeIndex >= _nodes.Count) { _phase = 3; continue; }
                        EntitySelection node = _nodes[_nodeIndex++];
                        node.Covered = node.Parent >= 0 && _nodes[node.Parent].Covered;
                        int mode = SelectPanoramaCopyMode(node.CanCopy, node.HasAnimatedSubtree, node.Covered);
                        if (mode == 0) continue;
                        if (Snapshot.CopiedRoots >= PanoramaSnapshotMaxCopies)
                            throw new InvalidOperationException("环境快照超过1024个实际预制体副本，已停止，不能将局部覆盖标为完整全景。");
                        GameEntity source = node.Entity;
                        if (!source.WeakEntity.IsValid || source.Scene?.Pointer != _source.Pointer || !source.IsVisibleIncludeParents()) continue;
                        GameEntity copied;
                        if (mode == 2)
                        {
                            Snapshot.SplitNodes++;
                            copied = CopyOwnStaticMeshes(source);
                            if (copied == null) continue;
                        }
                        else copied = GameEntity.CopyFrom(Snapshot.Scene, source, createPhysics: false, callScriptCallbacks: false);
                        if (copied == null || copied.Scene?.Pointer != Snapshot.Scene.Pointer || copied.Pointer == source.Pointer)
                            throw new InvalidOperationException("环境实体副本没有正确进入独立场景。");
                        // Split static subtrees retain their global placement, not the former parent's local frame.
                        MatrixFrame frame = source.GetGlobalFrame();
                        copied.SetFrame(ref frame);
                        copied.EntityFlags |= EntityFlags.DoNotTick | EntityFlags.DontTickChildren;
                        node.Covered = mode == 1;
                        Snapshot.CopiedRoots++;
                        copies++;
                    }
                }
                ValidateMission(_mission, _source, token);
                Snapshot.Batches++;
                Snapshot.TotalMilliseconds = _totalWatch.Elapsed.TotalMilliseconds;
                Snapshot.MaxBatchMilliseconds = Math.Max(Snapshot.MaxBatchMilliseconds, watch.Elapsed.TotalMilliseconds);
                if (_phase < 3) return false;
                if (Snapshot.CopiedRoots == 0) throw new InvalidOperationException("没有可复制的现场环境实体，不能生成空白全景。");
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
                bool animated = entity.Skeleton != null;
                bool helper = (entity.EntityFlags & (EntityFlags.IsHelper | EntityFlags.Ignore)) != 0;
                int index = _nodes.Count;
                _nodes.Add(new EntitySelection { Entity = entity, Parent = pending.Parent,
                    CanCopy = ShouldCopyPanoramaRoot(valid, visible, helper, animated, agent), HasAnimatedSubtree = agent || animated });
                Snapshot.InspectedNodes = _nodes.Count;
                if (agent || animated) { Snapshot.SkippedAnimated++; return; }
                int children = entity.ChildCount;
                if ((long)_nodes.Count + _pending.Count + children > PanoramaSnapshotMaxNodes) ThrowTooManyNodes();
                for (int i = children - 1; i >= 0; i--)
                {
                    token.ThrowIfCancellationRequested();
                    _pending.Push(new PendingEntity(entity.GetChild(i), index));
                }
            }

            private static void ThrowTooManyNodes() => throw new InvalidOperationException(
                "当前环境实体层级超过32768个节点，已停止快照，不能将不完整副本标为完整全景。");

            private GameEntity CopyOwnStaticMeshes(GameEntity source)
            {
                int count = source.MultiMeshComponentCount;
                if (count == 0) return null;
                var copy = GameEntity.CreateEmpty(Snapshot.Scene, isModifiableFromEditor: false,
                    createPhysics: false, callScriptCallbacks: false);
                for (int i = 0; i < count; i++)
                {
                    MetaMesh original = source.GetMetaMesh(i);
                    if (original == null) continue;
                    MetaMesh mesh = original.CreateCopy();
                    mesh.Frame = original.Frame;
                    copy.AddMultiMesh(mesh, updateVisMask: false);
                }
                return copy;
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
