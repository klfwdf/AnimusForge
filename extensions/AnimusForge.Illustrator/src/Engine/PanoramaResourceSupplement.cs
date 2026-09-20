using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Engine
{
    // Resource names here come from the engine-resolved scene file, never from a
    // place/culture/name heuristic. Templates are private, unrendered and unticked.
    internal sealed class PanoramaResourceSupplement
    {
        private static int _nextId;
        private readonly PanoramaSceneSnapshot _snapshot;
        private readonly SceneResourceGeometryPlan _plan;
        private readonly Dictionary<string, List<MatrixFrame>> _existing;
        private readonly Dictionary<string, List<MatrixFrame>> _observed;
        private readonly Stack<GameEntity> _pending = new Stack<GameEntity>();
        private readonly JArray _records = new JArray();
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private Scene _templates;
        private GameEntity _root, _current;
        private SceneResourceGeometryPlan.Entry _entry;
        private MatrixFrame _currentFrame;
        private int _entryIndex, _meshIndex, _nodeCount, _omittedRecords;
        private int _copied, _duplicates, _outside, _unsupported, _missing, _observedSkipped;
        private bool _disposed, _complete;
        private string _boundsAsset;
        private Vec3 _boundsMin, _boundsMax;
        private bool _boundsAny, _boundsValid, _measuringBounds, _groupBoundsUsable;
        private int _boundsRejected, _templatesLoaded, _cachedBoundsCount, _boundsProbes, _instanceLoads;

        internal PanoramaResourceSupplement(PanoramaSceneSnapshot snapshot, SceneResourceGeometryPlan plan,
            Dictionary<string, List<MatrixFrame>> existing, Dictionary<string, List<MatrixFrame>> observed)
        { _snapshot = snapshot; _plan = plan; _existing = existing; _observed = observed; }

        internal bool CopyBatch(CancellationToken token)
        {
            IllustratorRuntime.AssertMainThread();
            token.ThrowIfCancellationRequested();
            if (_disposed || _snapshot.IsDisposed) throw new OperationCanceledException("资源补齐已取消。", token);
            if (_templates == null)
            {
                string name = "afi_t" + Interlocked.Increment(ref _nextId).ToString("x8");
                if (name.Length > 16) throw new InvalidOperationException("资源模板场景名称过长。");
                _templates = Scene.CreateNewScene(initialize_physics: false, enable_decals: false, sceneName: name);
                _templates.SetDoNotAddEntitiesToTickList(true);
                _templates.SetClothSimulationState(false);
                _templates.SetPlaySoundEventsAfterReadyToRender(false);
                _templates.SetUpgradeLevelVisibility(_plan.ActiveMask);
            }
            var batch = Stopwatch.StartNew();
            int work = 0, expensive = 0;
            while (work++ < 64 && expensive < 8 && (work == 1 || batch.Elapsed.TotalMilliseconds < 4))
            {
                token.ThrowIfCancellationRequested();
                if (_current != null)
                {
                    int count = _current.MultiMeshComponentCount;
                    if (_meshIndex < count)
                    {
                        CopyMesh(_current, _meshIndex++, _currentFrame);
                        expensive++;
                        continue;
                    }
                    _current = null;
                }
                if (_pending.Count > 0)
                {
                    GameEntity node = _pending.Pop();
                    if (++_nodeCount > ScreenCaptureHelper.PanoramaSnapshotMaxNodes)
                        throw new InvalidOperationException("资源补齐超过实体遍历预算，已停止采集。");
                    if (node == null || !node.WeakEntity.IsValid) { _boundsValid = false; continue; }
                    bool scripted;
                    using (var scripts = node.GetScriptComponents().GetEnumerator()) scripted = scripts.MoveNext();
                    if (scripted || node.GetMobility() != GameEntity.Mobility.Stationary || node.Skeleton != null || node.ClothSimulatorComponentCount > 0 ||
                        (node.EntityFlags & (EntityFlags.IsHelper | EntityFlags.Ignore)) != 0 ||
                        (node.EntityVisibilityFlags & (EntityVisibilityFlags.VisibleOnlyWhenEditing | EntityVisibilityFlags.VisibleOnlyForEnvmap)) != 0 ||
                        !node.IsVisibleIncludeParents())
                    {
                        _unsupported++;
                        Record("template_branch_omitted", node.Name, new JObject
                        {
                            ["visible"] = node.IsVisibleIncludeParents(), ["mask"] = (uint)node.GetUpgradeLevelMask(),
                            ["cumulativeMask"] = (uint)node.GetUpgradeLevelMaskCumulative(), ["scripted"] = scripted,
                            ["mobility"] = node.GetMobility().ToString()
                        });
                        continue;
                    }
                    int children = node.ChildCount;
                    if ((long)_nodeCount + _pending.Count + children > ScreenCaptureHelper.PanoramaSnapshotMaxNodes)
                        throw new InvalidOperationException("资源模板层级超过遍历预算。");
                    for (int i = children - 1; i >= 0; i--) _pending.Push(node.GetChild(i));
                    _current = node; _meshIndex = 0; _currentFrame = node.GetGlobalFrame();
                    continue;
                }
                if (_root != null)
                {
                    // Only completed traversal yields a conservative bound for reuse.
                    if (_measuringBounds)
                    {
                        _groupBoundsUsable = _boundsValid && _boundsAny;
                        if (_groupBoundsUsable) _cachedBoundsCount++;
                    }
                    _root.Remove(0); _root.ManualInvalidate(); _root = null;
                    expensive++;
                    continue;
                }
                if (_entryIndex >= _plan.Entries.Count) { _complete = true; return true; }
                _entry = _plan.Entries[_entryIndex++];
                _measuringBounds = false;
                if ((!string.IsNullOrEmpty(_entry.PrefabName) && ContainsGeometry(_observed, "prefab:" + _entry.PrefabName, _entry.WorldFrame)) ||
                    (!string.IsNullOrEmpty(_entry.SourceName) && ContainsGeometry(_observed, "entity:" + _entry.SourceName, _entry.WorldFrame)))
                { _observedSkipped++; Record("runtime_instance_authoritative", _entry.PrefabName ?? _entry.MeshName, null); continue; }
                if (!string.Equals(_boundsAsset, _entry.AssetKey, StringComparison.Ordinal))
                {
                    _boundsAsset = _entry.AssetKey;
                    _boundsAny = false; _boundsValid = true; _groupBoundsUsable = false;
                    // Single-use assets cannot benefit from a separate bounds probe.
                    _measuringBounds = _plan.HasRepeatedAsset(_entry.AssetKey);
                }
                if (_measuringBounds)
                {
                    // Identity root makes the complete measured bounds asset-local,
                    // including child/mesh offsets. Revisit this entry after the probe.
                    _boundsProbes++;
                    _entryIndex--;
                }
                else if (_groupBoundsUsable)
                {
                    if (TryTransformBounds(_boundsMin, _boundsMax, _entry.WorldFrame, out var minimum, out var maximum) &&
                        !ScreenCaptureHelper.IntersectsPanoramaRadius(minimum, maximum, _snapshot.CaptureCenter))
                    { _boundsRejected++; Record("outside_radius_cached_bounds", _entry.PrefabName ?? _entry.MeshName, null); continue; }
                }
                // Failed native lookups also consume budget. No inverse or normalization
                // of the authored scaled frame is used for range rejection.
                expensive++;
                MatrixFrame templateFrame = _measuringBounds ? MatrixFrame.Identity : _entry.WorldFrame;
                if (!string.IsNullOrEmpty(_entry.PrefabName))
                {
                    _root = GameEntity.Instantiate(_templates, _entry.PrefabName, callScriptCallbacks: false, createPhysics: false);
                    if (_root == null) { _missing++; Record("prefab_unavailable", _entry.PrefabName, null); continue; }
                    if (_root.Scene?.Pointer != _templates.Pointer)
                        throw new InvalidOperationException("资源模板不属于自有场景，拒绝设置其变换。");
                    _root.SetFrame(ref templateFrame);
                }
                else
                {
                    MetaMesh mesh = MetaMesh.GetCopy(_entry.MeshName, showErrors: false, mayReturnNull: true);
                    if (mesh == null) { _missing++; Record("mesh_unavailable", _entry.MeshName, null); continue; }
                    bool attached = false;
                    try
                    {
                        _root = GameEntity.CreateEmpty(_templates, isModifiableFromEditor: false, createPhysics: false, callScriptCallbacks: false);
                        if (_root == null) throw new InvalidOperationException("无法创建静态资源模板。");
                        _root.AddMultiMesh(mesh);
                        attached = true;
                        _root.SetFrame(ref templateFrame);
                        _root.RecomputeBoundingBox(); _root.UpdateGlobalBounds(); _root.UpdateVisibilityMask();
                    }
                    finally { if (attached) mesh.ManualInvalidate(); }
                }
                if (_root == null || _root.Scene?.Pointer != _templates.Pointer)
                    throw new InvalidOperationException("无法在独立模板场景加载资源几何。");
                _root.EntityFlags |= EntityFlags.DoNotTick | EntityFlags.DontTickChildren;
                _templatesLoaded++;
                if (!_measuringBounds) _instanceLoads++;
                _pending.Push(_root);
            }
            return false;
        }

        private void CopyMesh(GameEntity source, int index, MatrixFrame frame)
        {
            MetaMesh original = source.GetMetaMesh(index);
            if (original == null) { _boundsValid = false; _missing++; return; }
            string name = original.GetName();
            if (original.HasClothData()) { _unsupported++; Record("cloth_mesh_omitted", name, null); return; }
            MatrixFrame effective = frame.TransformToParent(original.Frame);
            // The template carries the exact authored global transform, so its native
            // world bounds include parent scale/rotation without an origin-only guess.
            BoundingBox bounds = source.GetGlobalBoundingBox();
            if (_measuringBounds)
            {
                IncludeTemplateBounds(bounds);
                return;
            }
            if (ContainsGeometry(_existing, name, effective))
            { _duplicates++; Record("already_present", name, null); return; }
            if (!ScreenCaptureHelper.IntersectsPanoramaRadius(bounds.min, bounds.max, _snapshot.CaptureCenter))
            { _outside++; Record("outside_radius_or_invalid_bounds", name, null); return; }
            if (_snapshot.CopiedRoots >= ScreenCaptureHelper.PanoramaSnapshotMaxCopies)
                throw new InvalidOperationException("现场与资源补齐合计超过1024个网格组件预算。");
            MetaMesh copy = original.CreateCopy();
            if (copy == null || copy.Pointer == UIntPtr.Zero || copy.Pointer == original.Pointer)
                throw new InvalidOperationException("无法取得资源网格的独立副本。");
            GameEntity target = null;
            try
            {
                copy.Frame = original.Frame;
                target = _snapshot.Scene.AddItemEntity(ref frame, copy);
                if (target == null || target.Scene?.Pointer != _snapshot.Scene.Pointer || target.Pointer == source.Pointer)
                    throw new InvalidOperationException("资源几何未正确进入独立渲染场景。");
                target.EntityFlags |= EntityFlags.DoNotTick | EntityFlags.DontTickChildren;
                target.RecomputeBoundingBox(); target.UpdateGlobalBounds(); target.UpdateVisibilityMask();
                RegisterGeometry(_existing, name, effective);
                _copied++; _snapshot.CopiedRoots++; _snapshot.ResourceCopiedComponents++;
                var detail = new JObject
                {
                    ["sourceFrame"] = new JArray(PanoramaSnapshotInventory.Frame(frame)),
                    ["sourceMin"] = new JArray(PanoramaSnapshotInventory.Vector(bounds.min)),
                    ["sourceMax"] = new JArray(PanoramaSnapshotInventory.Vector(bounds.max))
                };
                bool targetBoundsRecorded = false;
                try
                {
                    var targetBounds = target.GetGlobalBoundingBox();
                    if (!ScreenCaptureHelper.HasUsablePanoramaBounds(targetBounds.min, targetBounds.max))
                        throw new InvalidOperationException("资源副本网格边界无效。");
                    _snapshot.IncludeCopiedBounds(targetBounds.min, targetBounds.max);
                    targetBoundsRecorded = true;
                    detail["targetFrame"] = new JArray(PanoramaSnapshotInventory.Frame(target.GetGlobalFrame()));
                    detail["targetMin"] = new JArray(PanoramaSnapshotInventory.Vector(targetBounds.min));
                    detail["targetMax"] = new JArray(PanoramaSnapshotInventory.Vector(targetBounds.max));
                }
                catch (Exception ex)
                {
                    if (!targetBoundsRecorded)
                    {
                        _snapshot.IncludeCopiedBounds(bounds.min, bounds.max);
                        _snapshot.CopiedBoundsFallbacks++;
                    }
                    detail["targetMetadataError"] = ex.GetType().Name;
                }
                Record("copied", name, detail);
            }
            finally
            {
                if (target != null) { target.ManualInvalidate(); copy.ManualInvalidate(); }
            }
        }

        private void IncludeTemplateBounds(BoundingBox bounds)
        {
            if (!ScreenCaptureHelper.HasUsablePanoramaBounds(bounds.min, bounds.max))
            { _boundsValid = false; return; }
            Vec3 minimum = bounds.min;
            Vec3 maximum = bounds.max;
            if (!_boundsAny) { _boundsMin = minimum; _boundsMax = maximum; _boundsAny = true; return; }
            _boundsMin = new Vec3(Math.Min(_boundsMin.x, minimum.x), Math.Min(_boundsMin.y, minimum.y), Math.Min(_boundsMin.z, minimum.z));
            _boundsMax = new Vec3(Math.Max(_boundsMax.x, maximum.x), Math.Max(_boundsMax.y, maximum.y), Math.Max(_boundsMax.z, maximum.z));
        }

        // All eight corners are required for rotations, negative/nonuniform scale and
        // affine parent transforms. Invalid bounds fall back to native instance checks.
        private static bool TryTransformBounds(Vec3 minimum, Vec3 maximum, MatrixFrame frame,
            out Vec3 worldMin, out Vec3 worldMax)
        {
            worldMin = new Vec3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            worldMax = new Vec3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 8; i++)
            {
                double x = (i & 1) == 0 ? minimum.x : maximum.x;
                double y = (i & 2) == 0 ? minimum.y : maximum.y;
                double z = (i & 4) == 0 ? minimum.z : maximum.z;
                var point = new Vec3(
                    (float)(frame.origin.x + frame.rotation.s.x * x + frame.rotation.f.x * y + frame.rotation.u.x * z),
                    (float)(frame.origin.y + frame.rotation.s.y * x + frame.rotation.f.y * y + frame.rotation.u.y * z),
                    (float)(frame.origin.z + frame.rotation.s.z * x + frame.rotation.f.z * y + frame.rotation.u.z * z));
                if (!ScreenCaptureHelper.HasUsablePanoramaBounds(point, point)) return false;
                worldMin = new Vec3(Math.Min(worldMin.x, point.x), Math.Min(worldMin.y, point.y), Math.Min(worldMin.z, point.z));
                worldMax = new Vec3(Math.Max(worldMax.x, point.x), Math.Max(worldMax.y, point.y), Math.Max(worldMax.z, point.z));
            }
            // Asset-space margin propagated through scale, plus world roundoff margin.
            var r = frame.rotation;
            var padding = new Vec3(
                0.25f * (1 + Math.Abs(r.s.x) + Math.Abs(r.f.x) + Math.Abs(r.u.x)) + 0.00001f * Math.Max(Math.Abs(worldMin.x), Math.Abs(worldMax.x)),
                0.25f * (1 + Math.Abs(r.s.y) + Math.Abs(r.f.y) + Math.Abs(r.u.y)) + 0.00001f * Math.Max(Math.Abs(worldMin.y), Math.Abs(worldMax.y)),
                0.25f * (1 + Math.Abs(r.s.z) + Math.Abs(r.f.z) + Math.Abs(r.u.z)) + 0.00001f * Math.Max(Math.Abs(worldMin.z), Math.Abs(worldMax.z)));
            worldMin -= padding; worldMax += padding;
            return ScreenCaptureHelper.HasUsablePanoramaBounds(worldMin, worldMax);
        }

        internal static void RegisterGeometry(Dictionary<string, List<MatrixFrame>> map, string name, MatrixFrame frame)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!map.TryGetValue(name, out var frames)) map.Add(name, frames = new List<MatrixFrame>());
            frames.Add(frame);
        }

        private static bool ContainsGeometry(Dictionary<string, List<MatrixFrame>> map, string name, MatrixFrame frame)
        {
            if (string.IsNullOrWhiteSpace(name) || !map.TryGetValue(name, out var frames)) return false;
            foreach (var candidate in frames)
                if (candidate.origin.DistanceSquared(frame.origin) < 0.0004f &&
                    candidate.rotation.s.DistanceSquared(frame.rotation.s) < 0.000025f &&
                    candidate.rotation.f.DistanceSquared(frame.rotation.f) < 0.000025f &&
                    candidate.rotation.u.DistanceSquared(frame.rotation.u) < 0.000025f) return true;
            return false;
        }

        private void Record(string result, string mesh, JObject detail)
        {
            if (_records.Count >= 1024) { _omittedRecords++; return; }
            var row = detail ?? new JObject();
            row["sourceId"] = _entry?.SourceId; row["prefab"] = _entry?.PrefabName;
            row["boundsProbe"] = _measuringBounds;
            row["mesh"] = mesh == null || mesh.Length <= 160 ? mesh : mesh.Substring(0, 160);
            row["result"] = result; _records.Add(row);
        }

        internal JObject Describe() => new JObject
        {
            ["sourceFile"] = _plan.ResolvedSceneFile, ["activeMask"] = _plan.ActiveMask,
            ["implicitBaseMask"] = _plan.ImplicitBaseMask, ["requiredVariantMask"] = _plan.ActiveMask & ~_plan.ImplicitBaseMask,
            ["complete"] = _complete, ["planEntries"] = _plan.Entries.Count, ["processedEntries"] = _entryIndex,
            ["assetGroups"] = _plan.AssetGroups, ["boundsMode"] = "grouped_identity_template",
            ["planXmlElements"] = _plan.XmlElementCount, ["planEntities"] = _plan.InspectedNodes,
            ["planPeakSubtreeElements"] = _plan.PeakSubtreeElements,
            ["planSkippedLevels"] = _plan.SkippedByLevel, ["planSkippedUnsupported"] = _plan.SkippedUnsupported,
            ["planUnsupportedReasons"] = JObject.FromObject(_plan.UnsupportedReasons),
            ["planUnsupportedSamples"] = JArray.FromObject(_plan.UnsupportedSamples),
            ["copied"] = _copied, ["duplicates"] = _duplicates, ["outsideRadius"] = _outside,
            ["runtimeObservedSkipped"] = _observedSkipped,
            ["templateLoads"] = _templatesLoaded, ["outsideRadiusBeforeLoad"] = _boundsRejected, ["cachedAssetBounds"] = _cachedBoundsCount,
            ["boundsProbes"] = _boundsProbes, ["instanceLoads"] = _instanceLoads,
            ["omittedTemplateBranches"] = _unsupported, ["missingResources"] = _missing,
            ["elapsedMs"] = _watch.ElapsedMilliseconds, ["omittedRecords"] = _omittedRecords,
            ["coverage"] = "Authored static geometry supplements runtime meshes. Scripted/dynamic/unsupported overrides and native terrain are not reconstructed; this is not proof of the current destruction state.",
            ["records"] = _records
        };

        internal void DisposeTemplates()
        {
            IllustratorRuntime.AssertMainThread();
            if (_disposed) return;
            _disposed = true; _pending.Clear(); _current = null; _root = null;
            _boundsAsset = null; _groupBoundsUsable = false;
            var scene = _templates; _templates = null;
            if (scene != null) { try { scene.ClearAll(); } finally { scene.ManualInvalidate(); } }
        }
    }
}
