using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed class ExpeditionParadeRoute
{
    internal readonly List<Vec3> Points = new List<Vec3>();
    internal readonly List<float> Distances = new List<float>();
    internal float GateDistance;
    internal float Length => Distances.Count == 0 ? 0 : Distances[Distances.Count - 1];
    internal void Add(Vec3 point)
    {
        float distance = Points.Count == 0 ? 0 : (point - Points[Points.Count - 1]).AsVec2.Length;
        if (Points.Count > 0 && distance < 0.05f) return;
        Distances.Add(Length + distance); Points.Add(point);
    }
    internal Vec3 At(float distance, float lateral, out Vec2 direction)
    {
        distance = Math.Max(0, Math.Min(Length, distance));
        int i = Distances.BinarySearch(distance);
        if (i < 0) i = ~i;
        i = Math.Max(1, Math.Min(Points.Count - 1, i));
        Vec3 a = Points[i - 1], b = Points[i];
        direction = (b - a).AsVec2.Normalized();
        float t = (distance - Distances[i - 1]) / (Distances[i] - Distances[i - 1]);
        Vec3 center = a + (b - a) * t;
        return center + new Vec3(direction.y, -direction.x, 0) * lateral;
    }
}

// Runs on the mission thread in small batches; all route/offset points must pass navmesh checks.
internal sealed class ExpeditionParadeRoutePlanner
{
    private readonly Scene _scene;
    private readonly List<ExpeditionParadeFormationSpec> _formations;
    private readonly ExpeditionParadeType _type;
    private static readonly Dictionary<string, ExpeditionParadeRoute> Cache = new Dictionary<string, ExpeditionParadeRoute>();
    internal ExpeditionParadeRoute Result { get; private set; }
    internal string Failure { get; private set; } = "No gate-crossing route with sufficient clearance was found.";
    private readonly string _cacheKey;

    internal ExpeditionParadeRoutePlanner(Scene scene, List<ExpeditionParadeFormationSpec> formations, ExpeditionParadeType type)
    {
        _scene = scene; _formations = formations; _type = type;
        _cacheKey = "parade-v1:" + scene.GetName() + ":" + type + ":" + string.Join(",", formations.Select(f => f.Mounted + ":" + f.Rows).Distinct());
    }

    internal IEnumerator<bool> Plan()
    {
        if (_formations.Count == 0) { Failure = "No healthy troops remain."; yield break; }
        if (Cache.TryGetValue(_cacheKey, out var cached))
        {
            bool valid = true;
            for (int i = 1; i < cached.Points.Count; i++)
            {
                if (!ValidateSlice(cached, i)) { valid = false; break; }
                if (i % 8 == 0) yield return true;
            }
            if (valid) { Result = cached; yield break; }
            Cache.Remove(_cacheKey);
        }
        var gates = Tagged("outer_gate", "inner_gate", "gate");
        var gateEntities = new List<GameEntity>();
        _scene.GetAllEntitiesWithScriptComponent<CastleGate>(ref gateEntities);
        gates.AddRange(gateEntities.Select(e => e.GlobalPosition));
        gates = Unique(gates).Take(12).ToList();
        if (gates.Count == 0) { Failure = "No city gate anchor is present in this scene."; yield break; }
        var starts = Unique(Tagged("spawnpoint_player", "sp_player_conversation", "sp_governor", "npc_common", "sp_merchant", "sp_notable")).Take(36).ToList();
        var exits = Unique(Tagged("spawnpoint_player_outside", "sp_outside_near_town_main_gate")).ToList();
        bool mounted = _formations.Any(f => f.Mounted);
        float radius = mounted ? 1.65f : 1.35f;
        float depth = _formations.Max(f => (f.Rows - 1) * (f.Mounted ? 2.15f : 1.05f)) + 3f;
        int tried = 0;
        foreach (Vec3 gate in gates)
        {
            foreach (Vec3 start in starts.Where(p => (p - gate).AsVec2.Length > depth + 8f)
                .OrderBy(p => Math.Abs((p - gate).AsVec2.Length - 65f)).Take(8))
            {
                Vec2 outward = (gate - start).AsVec2.Normalized();
                var candidates = new List<Vec3>(exits);
                candidates.Add(gate + new Vec3(outward.x, outward.y, 0) * (depth + 14f));
                candidates.Add(gate + new Vec3(outward.x, outward.y, 0) * (depth + 28f));
                foreach (Vec3 end in Unique(candidates))
                {
                    if (++tried > 192) { Failure = "Route candidate budget exhausted."; yield break; }
                    yield return true;
                    if (Vec2.DotProduct((end - gate).AsVec2, outward) < 6f) continue;
                    ExpeditionParadeRoute route = BuildPath(start, end, radius);
                    if (route == null || route.Length < depth * 2 + 15f || route.Length > 450f) continue;
                    int gateIndex = Enumerable.Range(0, route.Points.Count).OrderBy(i => (route.Points[i] - gate).AsVec2.LengthSquared).First();
                    if ((route.Points[gateIndex] - gate).AsVec2.Length > 8f) continue;
                    route.GateDistance = route.Distances[gateIndex];
                    if (route.GateDistance < depth + 6f || route.Length - route.GateDistance < depth + 4f) continue;
                    // Passing near a gate is insufficient: the local path must cross its plane.
                    Vec3 beforeGate = route.At(route.GateDistance - 6f, 0, out _);
                    Vec3 afterGate = route.At(route.GateDistance + 6f, 0, out _);
                    if (Vec2.DotProduct((beforeGate - gate).AsVec2, outward) > -1f
                        || Vec2.DotProduct((afterGate - gate).AsVec2, outward) < 1f) continue;
                    if (_type == ExpeditionParadeType.Victory)
                    {
                        var reversed = new ExpeditionParadeRoute();
                        for (int i = route.Points.Count - 1; i >= 0; i--) reversed.Add(route.Points[i]);
                        reversed.GateDistance = reversed.Length - route.GateDistance;
                        route = reversed;
                    }
                    bool valid = true;
                    for (int i = 1; i < route.Points.Count; i++)
                    {
                        if (!ValidateSlice(route, i)) { valid = false; break; }
                        if (i % 8 == 0) yield return true;
                    }
                    if (!valid) continue;
                    Result = route;
                    if (Cache.Count >= 32) Cache.Clear();
                    Cache[_cacheKey] = route;
                    ExpeditionParadeBehavior.Log("route=" + _scene.GetName() + " points=" + route.Points.Count + " length=" + route.Length.ToString("F1") + " gate=" + route.GateDistance.ToString("F1") + " candidates=" + tried);
                    ExpeditionParadeBehavior.Log("route samples=" + string.Join(";", route.Points.Where((p, i) => i % 5 == 0 || i == route.Points.Count - 1)));
                    yield break;
                }
            }
        }
    }

    private List<Vec3> Tagged(params string[] tags)
    {
        var points = new List<Vec3>();
        foreach (string tag in tags)
            foreach (GameEntity entity in _scene.FindEntitiesWithTag(tag)) points.Add(entity.GlobalPosition);
        return points;
    }
    private static IEnumerable<Vec3> Unique(IEnumerable<Vec3> input)
    {
        var seen = new List<Vec3>();
        foreach (Vec3 p in input)
            if (!seen.Any(x => (x - p).LengthSquared < 4f)) { seen.Add(p); yield return p; }
    }

    private ExpeditionParadeRoute BuildPath(Vec3 start, Vec3 end, float radius)
    {
        if (!TryGround(_scene, start, out start) || !TryGround(_scene, end, out end)) return null;
        WorldPosition from = new WorldPosition(_scene, start), to = new WorldPosition(_scene, end);
        var path = new NavigationPath();
        if (!_scene.GetPathBetweenAIFaces(from.GetNavMesh(), to.GetNavMesh(), start.AsVec2, end.AsVec2, radius, path, null)) return null;
        var raw = new List<Vec3> { start };
        for (int i = 0; i < path.Size; i++) raw.Add(new Vec3(path[i].x, path[i].y, raw[raw.Count - 1].z));
        // Engine versions may include endpoints; normalize direction before resampling.
        if (path.Size > 1 && (raw[1] - end).AsVec2.Length < (raw[1] - start).AsVec2.Length)
        { raw.RemoveAt(0); raw.Reverse(); raw.Insert(0, start); }
        raw.Add(end);
        var route = new ExpeditionParadeRoute();
        route.Add(start);
        for (int i = 1; i < raw.Count; i++)
        {
            Vec3 a = route.Points[route.Points.Count - 1], b = raw[i];
            int steps = Math.Max(1, (int)Math.Ceiling((b - a).AsVec2.Length / 1.0f));
            for (int step = 1; step <= steps; step++)
            {
                Vec3 point = a + (b - a) * ((float)step / steps);
                if (!TryGround(_scene, point, out point)) return null;
                route.Add(point);
                if (route.Points.Count > 600) return null;
            }
        }
        return route.Points.Count >= 2 ? route : null;
    }

    private bool ValidateSlice(ExpeditionParadeRoute route, int index)
    {
        foreach (bool mounted in _formations.Select(f => f.Mounted).Distinct())
        {
            int columns = mounted ? 2 : 3;
            float spacing = mounted ? 1.65f : 0.9f;
            float radius = mounted ? 0.65f : 0.35f;
            // Both occupied tracks and outer clearance edges must remain connected without detours.
            for (int c = -1; c <= columns; c++)
            {
                float lateral = c < 0 ? -(columns - 1) * spacing / 2 - radius
                    : c == columns ? (columns - 1) * spacing / 2 + radius
                    : (c - (columns - 1) / 2f) * spacing;
                Vec3 a = route.At(route.Distances[index - 1], lateral, out _);
                Vec3 b = route.At(route.Distances[index], lateral, out _);
                if (!TryGround(_scene, a, out a) || !TryGround(_scene, b, out b)) return false;
                if (Math.Abs(a.z - route.Points[index - 1].z) > 1.25f || Math.Abs(b.z - route.Points[index].z) > 1.25f) return false;
                WorldPosition from = new WorldPosition(_scene, a), to = new WorldPosition(_scene, b);
                float straight = (b - a).AsVec2.Length;
                if (!_scene.GetPathDistanceBetweenPositions(ref from, ref to, 0.25f, out float distance)
                    || distance > straight * 1.2f + 0.2f || float.IsNaN(distance)) return false;
            }
        }
        return true;
    }

    internal static bool TryGround(Scene scene, Vec3 point, out Vec3 grounded)
    {
        WorldPosition world = new WorldPosition(scene, point);
        if (world.GetNavMesh() == UIntPtr.Zero) { grounded = point; return false; }
        grounded = world.GetNavMeshVec3();
        return !float.IsNaN(grounded.z) && !float.IsInfinity(grounded.z);
    }
}
