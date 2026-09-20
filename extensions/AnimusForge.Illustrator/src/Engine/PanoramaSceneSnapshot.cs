using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

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
        internal int ResourceCopiedComponents { get; set; }
        internal int SkippedAnimated { get; set; }
        internal int SkippedNonGeometry { get; set; }
        internal int SkippedByRadius { get; set; }
        internal int SkippedInvalidBounds { get; set; }
        internal Vec3 CaptureCenter { get; set; }
        internal bool CenterFromPlayer { get; set; }
        internal double CopiedGeometryRadius { get; private set; }
        internal int CopiedBoundsFallbacks { get; set; }
        internal bool RenderDistanceLimited { get; set; }
        internal int InspectedNodes { get; set; }
        internal int SourceRoots { get; set; }
        internal int Batches { get; set; }
        internal double TotalMilliseconds { get; set; }
        internal double MaxBatchMilliseconds { get; set; }
        internal bool TerrainOmitted { get; private set; }
        internal string Notes =>
            (CenterFromPlayer ? "只采集玩家现场位置周围约30米的静态网格。" : "未取得玩家实体，以当前镜头位置为中心采集约30米范围。") +
            "按网格所属实体包围盒与范围相交筛选，跨越边界的大墙或屋顶保留整块；画面外与范围外内容保持未知。" +
            "多镜头来自当前现场可见静态网格的独立冻结副本；保留建筑、门窗、陈设和实体材质的空间关系。" +
            "缺少的静态建筑几何可由当前场景实际资源文件补齐，按文件中的资产与摆放还原；这部分不代表已确认的实时破坏状态，脚本或动态变体不作补造。均匀中灰底色代表没有覆盖的区域，不是墙面或地面。" +
            "环境快照主动省略人物、坐骑及含骨骼或布料模拟的动态实体，不能据此推断现场人数；人物位置以现场事实与当前画面为准。" +
            "环境参考用于识别家具、建筑、布局和材质图案；采用三盏中性无阴影补光与固定曝光，关闭副本的默认太阳光，不复制原场景灯光、阴影缓存、粒子或物理组件，不代表现场采光。" +
            "完整天空、环境光、烘焙间接光和曝光无法从引擎公开接口精确回读；现场昼夜、光照及颜色以附加的真实当前画面为准。" +
            (RenderDistanceLimited ? "部分已复制几何所需的渲染距离超过200米上限，远端可能被裁切；这些截面或空缺不是实际建筑开口。" : string.Empty) +
            (SkippedNonGeometry > 0 ? "没有普通静态网格的特殊组件已省略，不以空缺推断现场没有装饰。" : string.Empty) +
            (SkippedInvalidBounds > 0 ? "部分网格边界无法确认，已省略，不能据此断言现场不存在该物体。" : string.Empty) +
            (TerrainOmitted ? "本现场含原生地形，副本没有复制地形高度场、地形混合材质和水面；这些空缺不是悬空、平地或室内的证据。" : string.Empty);

        internal PanoramaSceneSnapshot(Scene scene, UIntPtr sourcePointer, bool terrainOmitted)
        {
            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _sourcePointer = sourcePointer;
            TerrainOmitted = terrainOmitted;
            AssertPrivateScene();
        }

        // Reuse bounds already obtained during bounded copy batches. Selection stays
        // within 30 m, but an intersecting wall/roof remains one complete mesh.
        internal void IncludeCopiedBounds(Vec3 minimum, Vec3 maximum)
        {
            if (!ScreenCaptureHelper.HasUsablePanoramaBounds(minimum, maximum)) return;
            double dx = Math.Max(Math.Abs((double)minimum.x - CaptureCenter.x), Math.Abs((double)maximum.x - CaptureCenter.x));
            double dy = Math.Max(Math.Abs((double)minimum.y - CaptureCenter.y), Math.Abs((double)maximum.y - CaptureCenter.y));
            double dz = Math.Max(Math.Abs((double)minimum.z - CaptureCenter.z), Math.Abs((double)maximum.z - CaptureCenter.z));
            CopiedGeometryRadius = Math.Max(CopiedGeometryRadius, Math.Sqrt(dx * dx + dy * dy + dz * dz));
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
            try { ReleaseSourceHandles?.Invoke(); }
            finally
            {
                try { owned.ClearAll(); }
                finally { try { owned.ManualInvalidate(); } finally { _retirement.TrySetResult(true); } }
            }
        }
    }

    public static partial class ScreenCaptureHelper
    {
        internal static PanoramaSceneSnapshot _pendingPanoramaSnapshot;
        private static int _nextPanoramaSceneId;
        // Enumeration handles are not rendered copies. Large towns exceed 4096
        // roots; total inspected nodes and per-frame work remain independently capped.
        internal const int PanoramaSnapshotMaxRoots = 32768;
        internal const int PanoramaSnapshotBatchRoots = 8;
        internal const double PanoramaSnapshotBatchMilliseconds = 4;
        internal const int PanoramaSnapshotMaxNodes = 32768;
        internal const int PanoramaSnapshotMaxCopies = 1024;
        internal const float PanoramaCaptureRadius = 30f;

        internal static bool HasUsablePanoramaBounds(Vec3 minimum, Vec3 maximum)
            => FinitePanoramaVector(minimum) && FinitePanoramaVector(maximum) &&
                minimum.x <= maximum.x && minimum.y <= maximum.y && minimum.z <= maximum.z;

        private static bool FinitePanoramaVector(Vec3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        internal static bool IntersectsPanoramaRadius(Vec3 minimum, Vec3 maximum, Vec3 center)
        {
            if (!HasUsablePanoramaBounds(minimum, maximum) || !FinitePanoramaVector(center)) return false;
            double dx = Math.Max(Math.Max(minimum.x - center.x, 0), center.x - maximum.x);
            double dy = Math.Max(Math.Max(minimum.y - center.y, 0), center.y - maximum.y);
            double dz = Math.Max(Math.Max(minimum.z - center.z, 0), center.z - maximum.z);
            return dx * dx + dy * dy + dz * dz <= PanoramaCaptureRadius * PanoramaCaptureRadius;
        }

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
            PanoramaResourceSupplement supplement = null;
            PanoramaBatchPump runtimeBatches = null, resourceBatches = null;
            bool handedToCaller = false;
            var diagnostics = GenerationDiagnostics.Current;
            var inventory = diagnostics == null ? null : new PanoramaSnapshotInventory();
            try
            {
                var resourceInput = await RunOnGameThreadAsync(() =>
                {
                    token.ThrowIfCancellationRequested();
                    if (mission == null || !ReferenceEquals(Mission.Current, mission) || mission.Scene == null)
                        throw new InvalidOperationException("当前场景已切换，不能读取其资源。");
                    if (!Utilities.TryGetFullFilePathOfScene(mission.SceneName, out string sceneFile))
                        throw new InvalidOperationException("未能解析当前场景实际资源文件，已停止环境重建。");
                    return Tuple.Create(sceneFile, mission.Scene.GetUpgradeLevelMask(), mission.Scene.Pointer);
                }, token).ConfigureAwait(false);
                if (resourceInput == null) throw new InvalidOperationException("场景资源读取调度已停止。");
                diagnostics?.RecordStage("scene_resource_plan_start", new JObject
                { ["sourceFile"] = resourceInput.Item1, ["activeMask"] = resourceInput.Item2, ["reader"] = "root-subtree-stream" });
                var planWatch = Stopwatch.StartNew();
                var resourcePlan = await Task.Run(() => SceneResourceGeometryPlan.Load(resourceInput.Item1, resourceInput.Item2, token), token).ConfigureAwait(false);
                diagnostics?.RecordStage("scene_resource_plan_ready", new JObject
                {
                    ["xmlElements"] = resourcePlan.XmlElementCount, ["entities"] = resourcePlan.InspectedNodes,
                    ["peakSubtreeElements"] = resourcePlan.PeakSubtreeElements, ["entries"] = resourcePlan.Entries.Count,
                    ["planMs"] = planWatch.ElapsedMilliseconds, ["selectionRadiusMeters"] = PanoramaCaptureRadius,
                    ["maxRenderedMeshes"] = PanoramaSnapshotMaxCopies
                });
                builder = await RunOnGameThreadAsync(() =>
                {
                    if (mission.Scene?.Pointer != resourceInput.Item3 || mission.Scene.GetUpgradeLevelMask() != resourceInput.Item2)
                        throw new InvalidOperationException("场景或可见等级在资源读取期间改变，请重新采集。");
                    return PanoramaSnapshotBuilder.Begin(mission, token, inventory);
                }, token).ConfigureAwait(false);
                if (builder == null) throw new InvalidOperationException("无法在游戏线程创建环境快照。");
                runtimeBatches = await RunOnGameThreadAsync(() =>
                    PanoramaBatchPump.Start(() => builder.CopyBatch(token), token), token).ConfigureAwait(false);
                if (runtimeBatches == null) throw new InvalidOperationException("环境快照分帧调度已停止。");
                await runtimeBatches.Completion.ConfigureAwait(false);
                supplement = await RunOnGameThreadAsync(() =>
                {
                    builder.ValidateCurrent(token);
                    var created = new PanoramaResourceSupplement(builder.Snapshot, resourcePlan, builder.CopiedGeometry, builder.ObservedPlacements);
                    Action releaseLiveHandles = builder.Snapshot.ReleaseSourceHandles;
                    builder.Snapshot.ReleaseSourceHandles = () =>
                    {
                        try { releaseLiveHandles?.Invoke(); }
                        finally { created.DisposeTemplates(); }
                    };
                    return created;
                }, token).ConfigureAwait(false);
                if (supplement == null) throw new InvalidOperationException("场景资源补齐调度已停止。");
                resourceBatches = await RunOnGameThreadAsync(() => PanoramaBatchPump.Start(() =>
                {
                    builder.ValidateCurrent(token);
                    return supplement.CopyBatch(token);
                }, token), token).ConfigureAwait(false);
                if (resourceBatches == null) throw new InvalidOperationException("场景资源补齐分帧调度已停止。");
                await resourceBatches.Completion.ConfigureAwait(false);
                bool templatesRetired = await RunOnGameThreadAsync(() => { supplement.DisposeTemplates(); return true; }, token).ConfigureAwait(false);
                if (!templatesRetired) throw new InvalidOperationException("场景模板清理调度已停止。");
                if (builder.Snapshot.CopiedRoots == 0)
                    throw new InvalidOperationException("现场与资源中均没有可用静态几何，不能生成空白环境参考。");
                token.ThrowIfCancellationRequested();
                handedToCaller = true;
                return builder.Snapshot;
            }
            finally
            {
                // All game-thread batches have returned. Persist managed records once, on
                // this worker, including partial/cancelled copies and before native rendering.
                if (runtimeBatches != null) diagnostics?.RecordStage("scene_snapshot_batches", runtimeBatches.Describe("runtime"));
                if (resourceBatches != null) diagnostics?.RecordStage("scene_snapshot_batches", resourceBatches.Describe("resources"));
                if (builder != null) diagnostics?.RecordSceneInventory(inventory, builder.Snapshot, handedToCaller);
                try { if (supplement != null) diagnostics?.RecordSceneResourceSupplement(supplement.Describe()); }
                catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Resource diagnostic skipped: " + ex.GetType().Name); }
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
                internal PanoramaSnapshotInventory.Entry InventoryEntry;
            }
            private struct PendingEntity
            {
                internal GameEntity Entity;
                internal int ParentNode;
                internal PendingEntity(GameEntity entity, int parentNode = 0) { Entity = entity; ParentNode = parentNode; }
            }
            private readonly Mission _mission;
            private readonly Scene _source;
            private readonly uint _sourceLevelMask;
            private readonly PanoramaSnapshotInventory _inventory;
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
            private BoundingBox _nodeBounds;
            private readonly Stopwatch _totalWatch = Stopwatch.StartNew();
            internal PanoramaSceneSnapshot Snapshot { get; }
            internal Dictionary<string, List<MatrixFrame>> CopiedGeometry { get; } = new Dictionary<string, List<MatrixFrame>>(StringComparer.Ordinal);
            internal Dictionary<string, List<MatrixFrame>> ObservedPlacements { get; } = new Dictionary<string, List<MatrixFrame>>(StringComparer.Ordinal);
            internal bool RootsReleased => _roots == null;
            internal void ValidateCurrent(CancellationToken token)
            {
                ValidateMission(_mission, _source, token);
                if (_source.GetUpgradeLevelMask() != _sourceLevelMask)
                    throw new InvalidOperationException("场景可见等级在采集期间改变，请重新采集。");
            }

            private PanoramaSnapshotBuilder(Mission mission, Scene source, NativeObjectArray roots,
                PanoramaSceneSnapshot snapshot, HashSet<UIntPtr> agents, PanoramaSnapshotInventory inventory)
            {
                _mission = mission; _source = source; _roots = roots;
                _sourceLevelMask = source.GetUpgradeLevelMask();
                _inventory = inventory;
                _rootCount = roots.Count; Snapshot = snapshot; _agentRoots = agents;
                snapshot.SourceRoots = _rootCount;
                snapshot.ReleaseSourceHandles = ReleaseRoots;
            }

            internal static PanoramaSnapshotBuilder Begin(Mission mission, CancellationToken token, PanoramaSnapshotInventory inventory)
            {
                IllustratorRuntime.AssertMainThread();
                token.ThrowIfCancellationRequested();
                Scene source = mission?.Scene;
                ValidateMission(mission, source, token);
                if (inventory != null)
                    try { inventory.SourceSceneName = mission.SceneName; }
                    catch (Exception) { /* Scene identity metadata is optional. */ }
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
                    // The native GBuffer builder repeats this name in 128-byte labels.
                    // Keep this ASCII identifier <=16 bytes; never put a full GUID here.
                    string nativeSceneName = "afi_s" + Interlocked.Increment(ref _nextPanoramaSceneId).ToString("x8");
                    if (nativeSceneName.Length > 16) throw new InvalidOperationException("原生全景场景名称超过安全长度。");
                    TaleWorlds.Library.Debug.Print("[IllustratorPanorama] Preparing mesh-only private scene name=" + nativeSceneName);
                    // Match vanilla Item/BannerTableau infrastructure. This creates no source
                    // bodies: only visual meshes are attached to this otherwise empty scene.
                    owned = Scene.CreateNewScene(initialize_physics: true, enable_decals: false,
                        sceneName: nativeSceneName);
                    snapshot = new PanoramaSceneSnapshot(owned, source.Pointer, source.ContainsTerrain || source.HasTerrainHeightmap);
                    _pendingPanoramaSnapshot = snapshot;
                    var player = mission.MainAgent;
                    var screen = ScreenManager.TopScreen as MissionScreen;
                    if (player != null)
                    { snapshot.CaptureCenter = player.Position; snapshot.CenterFromPlayer = true; }
                    else if (screen != null && ReferenceEquals(screen.Mission, mission) && screen.CombatCamera != null)
                        snapshot.CaptureCenter = screen.CombatCamera.Frame.origin;
                    else throw new InvalidOperationException("无法确定玩家附近环境的采集中心。");
                    if (!FinitePanoramaVector(snapshot.CaptureCenter)) throw new InvalidOperationException("环境采集中心无效。");
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
                    // SetDefaultLighting introduces a strong directional sun. It is not
                    // neutral observation light and can turn roof seams into false daylight
                    // bands. Keep the render/shadow infrastructure; supply no solar energy.
                    Vec3 sunColor = new Vec3(0f, 0f, 0f);
                    Vec3 sunDirection = source.GetSunDirection();
                    owned.SetSunLight(ref sunColor, ref sunDirection);
                    owned.SetMinExposure(0f);
                    owned.SetMaxExposure(0f);
                    owned.SetTargetExposure(0f);
                    // Visibility was resolved against the source already. New mesh carriers
                    // use their default upgrade level; do not hide them with a copied level mask.
                    // Do not create weather/particle simulation in a geometry reference scene.
                    owned.EnsurePostfxSystem();
                    return new PanoramaSnapshotBuilder(mission, source, roots, snapshot, agents, inventory);
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
                "当前场景根实体超过32768个遍历预算，已停止环境快照，不能将截断副本标为完整全景。");

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
                ValidateCurrent(token);
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
                        var entry = node.InventoryEntry;
                        if (!node.CanCopy || !source.WeakEntity.IsValid || source.Scene?.Pointer != _source.Pointer || !source.IsVisibleIncludeParents())
                        {
                            if (entry != null) entry.CopyState = "source_invalid_or_hidden_before_copy";
                            _nodeIndex++; _meshIndex = 0; continue;
                        }
                        int count = source.MultiMeshComponentCount;
                        if (_meshIndex >= count)
                        {
                            if (count == 0) Snapshot.SkippedNonGeometry++;
                            if (entry != null) entry.CopyState = count == 0 ? "source_meshes_removed_before_copy" :
                                entry.MissingComponents > 0 ? "finished_with_missing_components" : "copied";
                            _nodeIndex++; _meshIndex = 0; continue;
                        }
                        if (Snapshot.CopiedRoots >= PanoramaSnapshotMaxCopies)
                            throw new InvalidOperationException("环境快照超过1024个静态网格副本，已停止，不能将局部覆盖标为完整全景。");
                        if (_meshIndex == 0)
                        {
                            BoundingBox bounds = source.GetGlobalBoundingBox();
                            if (!IntersectsPanoramaRadius(bounds.min, bounds.max, Snapshot.CaptureCenter))
                            {
                                if (entry != null) entry.CopyState = "outside_radius_or_invalid_bounds_before_copy";
                                Snapshot.SkippedByRadius++; _nodeIndex++; continue;
                            }
                            _nodeFrame = source.GetGlobalFrame();
                            _nodeBounds = bounds;
                            if (entry != null) entry.FrozenFrame = PanoramaSnapshotInventory.Frame(_nodeFrame);
                        }
                        if (entry != null) entry.CopyState = "copy_in_progress";
                        if (CopyStaticMesh(source, _meshIndex++, _nodeFrame, entry))
                        {
                            Snapshot.CopiedRoots++; copies++;
                            if (entry != null) entry.CopiedComponents++;
                        }
                        else if (entry != null) entry.MissingComponents++;
                    }
                }
                ValidateCurrent(token);
                Snapshot.Batches++;
                Snapshot.TotalMilliseconds = _totalWatch.Elapsed.TotalMilliseconds;
                Snapshot.MaxBatchMilliseconds = Math.Max(Snapshot.MaxBatchMilliseconds, watch.Elapsed.TotalMilliseconds);
                if (_phase < 2) return false;
                TaleWorlds.Library.Debug.Print($"[IllustratorPanorama] Mesh snapshot ready: meshes={Snapshot.CopiedRoots}, nodes={Snapshot.InspectedNodes}, batches={Snapshot.Batches}, elapsedMs={Snapshot.TotalMilliseconds:F0}; no source light/physics/script components copied.");
                ReleaseRoots();
                return true;
            }

            private void InspectOne(PendingEntity pending, CancellationToken token)
            {
                GameEntity entity = pending.Entity;
                if (Snapshot.InspectedNodes >= PanoramaSnapshotMaxNodes) ThrowTooManyNodes();
                Snapshot.InspectedNodes++;
                int nodeNumber = Snapshot.InspectedNodes;
                bool valid = entity != null && entity.WeakEntity.IsValid && entity.Scene?.Pointer == _source.Pointer;
                var entry = _inventory?.Inspect(entity, nodeNumber, pending.ParentNode, valid);
                if (!valid) return;
                bool visible = entity.IsVisibleIncludeParents();
                {
                    // Runtime instances are authoritative, even when hidden or stripped
                    // of meshes. Resources only supplement instances absent from enumeration.
                    MatrixFrame observedFrame = entity.GetGlobalFrame();
                    string prefab = entity.GetPrefabName();
                    string entityName = entity.Name;
                    if (!string.IsNullOrWhiteSpace(prefab)) PanoramaResourceSupplement.RegisterGeometry(ObservedPlacements, "prefab:" + prefab, observedFrame);
                    if (!string.IsNullOrWhiteSpace(entityName)) PanoramaResourceSupplement.RegisterGeometry(ObservedPlacements, "entity:" + entityName, observedFrame);
                }
                bool agent = _agentRoots.Contains(entity.Pointer);
                bool animated = entity.Skeleton != null || entity.ClothSimulatorComponentCount > 0;
                EntityFlags flags = entity.EntityFlags;
                bool helper = (flags & (EntityFlags.IsHelper | EntityFlags.Ignore)) != 0;
                bool eligible = ShouldCopyPanoramaRoot(valid, visible, helper, animated, agent);
                int meshCount = eligible ? entity.MultiMeshComponentCount : 0;
                if (entry != null)
                {
                    entry.Flags = (uint)flags; entry.Visible = visible;
                    if (eligible) entry.MetaMeshComponents = meshCount;
                    else
                        try { entry.MetaMeshComponents = entity.MultiMeshComponentCount; }
                        catch (Exception ex) { entry.MetadataError = ex.GetType().Name; }
                    entry.Selection = agent ? "agent_branch" : animated ? "animated_or_cloth_branch" :
                        !visible ? "not_visible" : helper ? "helper_or_ignored" : "no_meta_mesh";
                }
                if (eligible && meshCount > 0)
                {
                    BoundingBox bounds = entity.GetGlobalBoundingBox();
                    if (entry != null)
                    {
                        entry.BoundsMin = PanoramaSnapshotInventory.Vector(bounds.min);
                        entry.BoundsMax = PanoramaSnapshotInventory.Vector(bounds.max);
                    }
                    if (!HasUsablePanoramaBounds(bounds.min, bounds.max))
                    {
                        Snapshot.SkippedInvalidBounds++;
                        if (entry != null) entry.Selection = "invalid_bounds";
                    }
                    else if (!IntersectsPanoramaRadius(bounds.min, bounds.max, Snapshot.CaptureCenter))
                    {
                        Snapshot.SkippedByRadius++;
                        if (entry != null) entry.Selection = "outside_radius";
                    }
                    else
                    {
                        if (entry != null) { entry.Selection = "selected"; entry.CopyState = "pending"; }
                        _nodes.Add(new EntitySelection { Entity = entity, CanCopy = true, InventoryEntry = entry });
                    }
                }
                else if (eligible) Snapshot.SkippedNonGeometry++;
                if (agent || animated) { Snapshot.SkippedAnimated++; return; }
                // Hidden containers still contribute observed child placements. They
                // remain ineligible for copying, but must suppress authored fallback.
                int children = entity.ChildCount;
                if ((long)Snapshot.InspectedNodes + _pending.Count + children > PanoramaSnapshotMaxNodes) ThrowTooManyNodes();
                for (int i = children - 1; i >= 0; i--)
                {
                    token.ThrowIfCancellationRequested();
                    _pending.Push(new PendingEntity(entity.GetChild(i), nodeNumber));
                }
            }

            private static void ThrowTooManyNodes() => throw new InvalidOperationException(
                "当前环境实体层级超过32768个节点，已停止快照，不能将不完整副本标为完整全景。");

            private bool CopyStaticMesh(GameEntity source, int index, MatrixFrame frame, PanoramaSnapshotInventory.Entry entry)
            {
                MetaMesh original = source.GetMetaMesh(index);
                var detail = PanoramaSnapshotInventory.BeginMesh(entry, index, original);
                if (original == null) return false;
                MetaMesh mesh = null;
                GameEntity entity = null;
                bool attached = false;
                try
                {
                    mesh = original.CreateCopy();
                    if (mesh == null || mesh.Pointer == UIntPtr.Zero || mesh.Pointer == original.Pointer)
                        throw new InvalidOperationException("无法取得独立的现场静态网格副本。");
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
                    PanoramaResourceSupplement.RegisterGeometry(CopiedGeometry, original.GetName(), frame.TransformToParent(original.Frame));
                    if (detail != null) detail.Result = "copied";
                    bool targetBoundsRecorded = false;
                    try
                    {
                        // This is the existing per-copy metadata read, not another scene
                        // traversal. Actual target bounds also cover retained local frames.
                        var targetBounds = entity.GetGlobalBoundingBox();
                        if (!HasUsablePanoramaBounds(targetBounds.min, targetBounds.max))
                            throw new InvalidOperationException("静态副本网格边界无效。");
                        Snapshot.IncludeCopiedBounds(targetBounds.min, targetBounds.max);
                        targetBoundsRecorded = true;
                        if (detail != null)
                        {
                            detail.TargetFrame = PanoramaSnapshotInventory.Frame(entity.GetGlobalFrame());
                            detail.TargetBoundsMin = PanoramaSnapshotInventory.Vector(targetBounds.min);
                            detail.TargetBoundsMax = PanoramaSnapshotInventory.Vector(targetBounds.max);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!targetBoundsRecorded)
                        {
                            Snapshot.IncludeCopiedBounds(_nodeBounds.min, _nodeBounds.max);
                            Snapshot.CopiedBoundsFallbacks++;
                        }
                        if (detail != null) detail.MetadataError = ex.GetType().Name;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    if (entry != null) entry.CopyState = "copy_failed:" + ex.GetType().Name;
                    if (detail != null) detail.Result = "copy_failed:" + ex.GetType().Name;
                    throw;
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
