using System;
using System.Collections.Generic;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Engine
{
    // Managed diagnostic data only. Capture within the existing game-thread batches;
    // serialize once on the requesting worker, before native rendering starts.
    internal sealed class PanoramaSnapshotInventory
    {
        internal const int MaximumEntries = 1024;
        private const int MaximumMeshesPerEntry = 4;
        private const int MaximumNameLength = 160;
        private readonly List<Entry> _entries = new List<Entry>();
        private int _omittedEntries;
        internal string SourceSceneName { get; set; }

        internal sealed class Entry
        {
            public int Node { get; set; }
            public int ParentNode { get; set; }
            public string Name { get; set; }
            public string Prefab { get; set; }
            public float[] Position { get; set; }
            public float[] BoundsMin { get; set; }
            public float[] BoundsMax { get; set; }
            public float[] FrozenFrame { get; set; }
            public uint? Flags { get; set; }
            public bool? Visible { get; set; }
            public int? MetaMeshComponents { get; set; }
            public int? CompositeComponents { get; set; }
            public string Selection { get; set; } = "inspection_incomplete";
            public string CopyState { get; set; } = "not_selected";
            public int CopiedComponents { get; set; }
            public int MissingComponents { get; set; }
            public int MeshDetailsOmitted { get; set; }
            public string MetadataError { get; set; }
            public List<MeshEntry> Meshes { get; } = new List<MeshEntry>();
        }

        internal sealed class MeshEntry
        {
            public int Component { get; set; }
            public string Name { get; set; }
            public int? SubMeshCount { get; set; }
            public float[] LocalFrame { get; set; }
            public string Result { get; set; } = "copy_pending";
            public string MetadataError { get; set; }
        }

        internal Entry Inspect(GameEntity entity, int node, int parent, bool valid)
        {
            IllustratorRuntime.AssertMainThread();
            if (_entries.Count >= MaximumEntries) { _omittedEntries++; return null; }
            var entry = new Entry { Node = node, ParentNode = parent };
            _entries.Add(entry);
            if (!valid) { entry.Selection = "invalid_source"; return entry; }
            // Diagnostic getters are best-effort and never change selection or native state.
            try
            {
                entry.Name = Limit(entity.Name);
                entry.Prefab = Limit(entity.GetPrefabName());
                entry.Position = Vector(entity.GlobalPosition);
                entry.CompositeComponents = entity.GetComponentCount(GameEntity.ComponentType.CompositeComponent);
            }
            catch (Exception ex) { entry.MetadataError = ex.GetType().Name; }
            return entry;
        }

        internal static MeshEntry BeginMesh(Entry entry, int index, MetaMesh mesh)
        {
            if (entry == null) return null;
            if (entry.Meshes.Count >= MaximumMeshesPerEntry) { entry.MeshDetailsOmitted++; return null; }
            var detail = new MeshEntry { Component = index };
            entry.Meshes.Add(detail);
            if (mesh == null) { detail.Result = "source_mesh_null"; return detail; }
            try
            {
                detail.Name = Limit(mesh.GetName());
                detail.SubMeshCount = mesh.MeshCount;
                detail.LocalFrame = Frame(mesh.Frame);
            }
            catch (Exception ex) { detail.MetadataError = ex.GetType().Name; }
            return detail;
        }

        internal JObject ToDocument(PanoramaSceneSnapshot snapshot, bool complete)
        {
            return new JObject
            {
                ["schema"] = 1, ["complete"] = complete,
                ["sourceScene"] = Limit(SourceSceneName),
                ["sourceRoots"] = snapshot.SourceRoots, ["inspectedNodes"] = snapshot.InspectedNodes,
                ["copiedMetaMeshComponents"] = snapshot.CopiedRoots,
                ["center"] = new JArray(Vector(snapshot.CaptureCenter)), ["radiusMeters"] = ScreenCaptureHelper.PanoramaCaptureRadius,
                ["entryLimit"] = MaximumEntries, ["meshDetailLimitPerEntry"] = MaximumMeshesPerEntry,
                ["omittedEntries"] = _omittedEntries,
                ["frameLayout"] = "origin.xyz, rotation.s.xyz, rotation.f.xyz, rotation.u.xyz; local frames relative to source entity, frozen frames in world space",
                ["coverage"] = "Enumerated runtime entities only; absent entries do not prove an object is absent from the scene. Branch skips and diagnostic limits may omit descendants. Copied means attached to the private scene, not proven visible in the exported pixels.",
                ["entries"] = JArray.FromObject(_entries)
            };
        }

        internal static float[] Vector(Vec3 value) => new[] { value.x, value.y, value.z };
        internal static float[] Frame(MatrixFrame value) => new[]
        {
            value.origin.x, value.origin.y, value.origin.z,
            value.rotation.s.x, value.rotation.s.y, value.rotation.s.z,
            value.rotation.f.x, value.rotation.f.y, value.rotation.f.z,
            value.rotation.u.x, value.rotation.u.y, value.rotation.u.z
        };
        private static string Limit(string value) => value == null || value.Length <= MaximumNameLength ? value : value.Substring(0, MaximumNameLength);
    }
}
