using RichExecutions.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Picks the camp site once per camp mission (never per frame): dry, walkable, fairly
/// flat ground clear of trees and rocks, inside the mission boundary. Candidates come
/// from the scene's own spawn paths (where vanilla field battles deploy, always on land)
/// combined with a scene-wide grid in two orientations. Whole-footprint grade/spread
/// rank candidates; tent parcels and passages prefer ideal sites, with a best-site fallback.
/// Every candidate is probed over the camp's real rectangle (palisade extents).
/// Cost, once: at most (600 + 576) x two orientations x 25 cheap probes, then
/// Up to all coarse survivors x 81 probes if the first finalists are unusable.
/// Afterwards small rocks and bushes left inside the palisade are hidden (one entity scan).
/// </summary>
internal static class FieldCampSite
{
	private const string LogSource = "FieldCamp";
	private const int MaxSpawnPaths = 32;
	private const float PathStep = 12f;
	private const int MaxCandidates = 600;
	private const int Finalists = 64;
		private const float WetMargin = 0.3f;
		private const BodyFlags ClearanceExcludeFlags = BodyFlags.CommonCollisionExcludeFlagsForAgent & ~BodyFlags.DoNotCollideWithRaycast;
	/// <summary>A raycast hit this far above the terrain means a tree, rock or building stands there.</summary>
	private const float ObstacleHeight = 0.25f;
	private const float ClutterMaxExtent = 9f;
		private static readonly string[] ClutterNames = { "rock", "stone", "boulder", "bush", "shrub", "tree", "plant", "branch", "stump" };
		private static readonly string[] StructureNames = { "wall", "gate", "house", "building", "bridge", "tower", "cliff", "terrain", "road", "navmesh", "barrier", "ruin", "floor", "stairs" };
		// Never compress spacing without shrinking the real prefab footprints and actor bodies.
		private const int MaxSceneEntities = 100000;
		private const int MaxGroundCells = 98304;
		private const float GroundStep = 0.5f;
		private const float MaxGroundDelta = 0.35f;
		private const int MaxPassageSteps = 768;
		private static readonly ConditionalWeakTable<Scene, GroundCache> GroundCaches = new ConditionalWeakTable<Scene, GroundCache>();
		private static readonly ConditionalWeakTable<Scene, ClutterCache> ClutterCaches = new ConditionalWeakTable<Scene, ClutterCache>();

		private sealed class ClutterCache
		{
			internal readonly List<GameEntity> Roots = new List<GameEntity>();
			internal readonly HashSet<UIntPtr> Removable = new HashSet<UIntPtr>();
			internal bool Complete;
		}

		private sealed class GroundCache
		{
			internal MatrixFrame Frame;
			internal float MinX;
			internal float MinY;
			internal int Width;
			internal int Height;
			internal float[] Heights;
			internal bool[] Open;
            internal bool[] BaseOpen;
            internal bool[] FenceBlocked;
			internal bool Ready;
            internal FieldCampGroundRouter Router;
            internal int CampMinX, CampMinY, CampMaxX, CampMaxY;
		}

	private struct Candidate
	{
		internal Vec2 Position;
		internal Vec2 Forward;
		internal float Score;
		internal int Bad;
        internal bool AnchorBad;
        internal SiteRejection Rejection;
	}

    private enum SiteRejection
    {
        None, Boundary, Terrain, Water, Navigation, Obstacle, EngineError
    }

    private static string RejectionSummary(int[] counts) =>
        "boundary=" + counts[(int)SiteRejection.Boundary]
        + ",terrain=" + counts[(int)SiteRejection.Terrain]
        + ",water=" + counts[(int)SiteRejection.Water]
        + ",navigation=" + counts[(int)SiteRejection.Navigation]
        + ",obstacle=" + counts[(int)SiteRejection.Obstacle]
        + ",engineError=" + counts[(int)SiteRejection.EngineError];

    /// <param name="fitScale">Real prop spacing remains unchanged.</param>
    internal static MatrixFrame Resolve(Mission mission, FieldCampLayout layout, out float fitScale)
        => Resolve(mission, layout, out fitScale, out _);

    internal static MatrixFrame Resolve(Mission mission, FieldCampLayout layout, out float fitScale, out bool approximate)
    {
        Scene scene = mission.Scene;
        bool hasEncounter = TryEncounterPoint(mission, out Vec2 encounter);
        List<Candidate> candidates = CollectCandidates(scene, hasEncounter, encounter);
        int count = candidates.Count;
        for (int i = 0; i < count; i++)
            candidates.Add(new Candidate { Position = candidates[i].Position,
                Forward = new Vec2(-candidates[i].Forward.y, candidates[i].Forward.x) });
        fitScale = 1f;
        approximate = false;
        GroundCaches.Remove(scene);
        CaptureClutter(scene);
        if (candidates.Count == 0)
        {
            scene.GetBoundingBox(out Vec3 min, out Vec3 max);
            candidates.Add(new Candidate { Position = hasEncounter ? encounter :
                new Vec2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f), Forward = Vec2.Forward });
        }
        var sampleHeights = new float[81];
        var tentMeasurements = new Dictionary<string, BoundingBox>(StringComparer.Ordinal);
        var coarseReasons = new int[(int)SiteRejection.EngineError + 1];
        var fineReasons = new int[coarseReasons.Length];
        int coarseRejected = 0, fineRejected = 0, criticalRejected = 0, tentRejected = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            Candidate c = candidates[i];
            c.AnchorBad = !Probe(mission, scene, c.Position, false, out _, out _);
            c.Score = Evaluate(mission, scene, c, layout, 1f, 5, false, sampleHeights, out c.Bad, out c.Rejection);
            if (hasEncounter) c.Score += c.Position.Distance(encounter) / 25f;
            if (c.Bad != 0) { coarseRejected++; coarseReasons[(int)c.Rejection]++; }
            candidates[i] = c;
        }
        candidates.Sort(CompareCandidates);
        Candidate fallback = candidates[0];
        bool hasFineFallback = false, found = false;
        Candidate best = default(Candidate);
        int checkedSites = 0, roughChecked = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (found && checkedSites >= Finalists) break;
            Candidate c = candidates[i];
            // Rough sites remain eligible for generation. Bound their detailed trials,
            // while still considering every coarse-clean candidate for an ideal site.
            if (c.Bad != 0 && roughChecked++ >= Finalists) continue;
            checkedSites++;
            c.Score = Evaluate(mission, scene, c, layout, 1f, 9, true, sampleHeights, out c.Bad, out c.Rejection);
            if (hasEncounter) c.Score += c.Position.Distance(encounter) / 25f;
            if (!hasFineFallback || CompareCandidates(c, fallback) < 0)
            {
                fallback = c;
                hasFineFallback = true;
            }
            if (c.Bad != 0) { fineRejected++; fineReasons[(int)c.Rejection]++; continue; }
            if (found && c.Score >= best.Score) continue;
            if (!ValidateCriticalAreas(mission, scene, c, layout, 1f)) { criticalRejected++; continue; }
            if (!FieldCampProps.CanSupportTents(scene, layout, FrameAt(scene, c.Position, c.Forward), tentMeasurements))
            { tentRejected++; continue; }
            best = c;
            found = true;
        }
        Logger.Log(LogSource, "site search candidates=" + candidates.Count + " coarseRejected=" + coarseRejected
            + " fineChecked=" + checkedSites + " fineRejected=" + fineRejected + " criticalRejected=" + criticalRejected
            + " tentRejected=" + tentRejected + " coarseReasons={" + RejectionSummary(coarseReasons)
            + "} fineReasons={" + RejectionSummary(fineReasons) + "}");
        Candidate selected = found ? best : fallback;
        approximate = !found;
        Logger.Log(LogSource, (approximate ? "[WARN] camp site approximate; continuing generation" : "camp site selected")
            + " pos=" + Format(selected.Position) + " badProbes=" + selected.Bad + " anchorDry=" + !selected.AnchorBad);
        return FrameAt(scene, selected.Position, selected.Forward);
    }

    private static int CompareCandidates(Candidate a, Candidate b)
    {
        int anchor = a.AnchorBad.CompareTo(b.AnchorBad);
        if (anchor != 0) return anchor;
        int bad = a.Bad.CompareTo(b.Bad);
        return bad != 0 ? bad : a.Score.CompareTo(b.Score);
    }

    // Player placement remains independent of whether the whole camp site was ideal.
    internal static bool TryFindPlayerStart(Mission mission, Vec3 desired, out Vec3 grounded)
    {
        if (TryFindSpawnPosition(mission.Scene, desired, 0.45f, out grounded)) return true;
        bool encounter = TryEncounterPoint(mission, out Vec2 point);
        var candidates = CollectCandidates(mission.Scene, encounter, point);
        for (int i = -1; i < candidates.Count; i++)
        {
            Vec2 candidate = i < 0 ? desired.AsVec2 : candidates[i].Position;
            if (!Probe(mission, mission.Scene, candidate, true, out float z, out bool obstacle) || obstacle) continue;
            var position = new Vec3(candidate.x, candidate.y, z + 0.04f);
            if (!CellHasClearance(mission.Scene, position)) continue;
            grounded = position;
            return true;
        }
        grounded = Vec3.Invalid;
        return false;
    }

	private static bool TryEncounterPoint(Mission mission, out Vec2 point)
	{
		point = Vec2.Zero;
		try
		{
			if (mission.GetPatchSceneEncounterPosition(out Vec3 position) && position.IsValid)
			{
				point = position.AsVec2;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static List<Candidate> CollectCandidates(Scene scene, bool hasEncounter, Vec2 encounter)
	{
		var list = new List<Candidate>();
		for (int i = 0; i < MaxSpawnPaths; i++)
		{
			Path path;
			try
			{
				path = scene.GetPathWithName("spawn_path_" + i.ToString("D2"));
			}
			catch
			{
				path = null;
			}
			if (path == null || path.NumberOfPoints < 2)
			{
				continue;
			}
			float length = path.GetTotalLength();
				if (!IsFinite(length) || length < 0f)
				{
					continue;
				}
				// Bound even malformed/modded paths before the final candidate thinning.
				float samplingStep = Math.Max(PathStep, length / 64f);
				for (int sample = 0; sample <= 64 && sample * samplingStep <= length; sample++)
				{
					float d = sample * samplingStep;
					Vec2 a = path.GetFrameForDistance(d).origin.AsVec2;
				Vec2 b = path.GetFrameForDistance(Math.Min(length, d + 2f)).origin.AsVec2;
				if (d + 2f > length)
				{
					Vec2 back = path.GetFrameForDistance(Math.Max(0f, d - 2f)).origin.AsVec2;
					b = a + (a - back);
				}
				Vec2 forward = b - a;
				list.Add(new Candidate { Position = a, Forward = forward.LengthSquared < 0.0001f ? new Vec2(0f, 1f) : forward.Normalized() });
			}
		}
		if (list.Count > MaxCandidates)
		{
			// Keep the candidates nearest the encounter point (or an even spread without one).
			if (hasEncounter)
			{
				list.Sort((x, y) => x.Position.DistanceSquared(encounter).CompareTo(y.Position.DistanceSquared(encounter)));
				list.RemoveRange(MaxCandidates, list.Count - MaxCandidates);
			}
			else
			{
				var thinned = new List<Candidate>(MaxCandidates);
				float step = list.Count / (float)MaxCandidates;
				for (int i = 0; i < MaxCandidates; i++)
				{
					thinned.Add(list[(int)(i * step)]);
				}
				list = thinned;
			}
		}
		// Preserve the bounded path sample AND a scene-wide grid, even when nearby paths exist.
		AddGrid(scene, list, hasEncounter, encounter);
		return list;
	}

	private static void AddGrid(Scene scene, List<Candidate> list, bool hasEncounter, Vec2 encounter)
	{
		Vec3 min;
		Vec3 max;
		try
		{
			scene.GetSceneLimits(out min, out max);
		}
		catch
		{
			scene.GetBoundingBox(out min, out max);
		}
		float extent = Math.Max(max.x - min.x, max.y - min.y);
		if (!(extent > 1f) || float.IsInfinity(extent))
		{
			return;
		}
		float step = Math.Max(30f, extent / 24f);
		for (float x = min.x + step * 0.5f; x < max.x; x += step)
		{
			for (float y = min.y + step * 0.5f; y < max.y; y += step)
			{
				var p = new Vec2(x, y);
				Vec2 forward = hasEncounter && p.DistanceSquared(encounter) > 1f ? (encounter - p).Normalized() : new Vec2(0f, 1f);
				list.Add(new Candidate { Position = p, Forward = forward });
			}
		}
	}

	/// <summary>
	/// Probes a grid x grid lattice over the palisade rectangle. Wet, out-of-bounds and
	/// off-navmesh points veto the site. Obstacles veto it unless their complete small
	/// clutter subtree is in the bounded clearing inventory; then score height spread.
	/// </summary>
    private static float Evaluate(Mission mission, Scene scene, Candidate c, FieldCampLayout layout, float scale, int grid, bool fine, float[] heights, out int bad, out SiteRejection firstRejection)
    {
        bad = 0;
        firstRejection = SiteRejection.None;
        int blocked = 0;
        var profile = new FieldCampSurfaceProfile();
        Vec2 forward = c.Forward;
        Vec2 side = new Vec2(forward.y, -forward.x);
        for (int gx = 0; gx < grid; gx++)
        {
            float lx = (-layout.HalfX + 2f * layout.HalfX * gx / (grid - 1)) * scale;
            for (int gy = 0; gy < grid; gy++)
            {
                int index = gx * grid + gy;
                heights[index] = float.NaN;
                float ly = (layout.Back + (layout.Front - layout.Back) * gy / (grid - 1)) * scale;
                Vec2 point = c.Position + side * lx + forward * ly;
                if (!Probe(mission, scene, point, fine, out float height, out bool obstacle, out SiteRejection rejection))
                {
                    bad++;
                    if (firstRejection == SiteRejection.None) firstRejection = rejection;
                    continue;
                }
                if (obstacle) blocked++;
                heights[index] = height;
                profile.Add(height, gx == 0 ? float.NaN : heights[index - grid],
                    gy == 0 ? float.NaN : heights[index - 1], 2f * layout.HalfX * scale / (grid - 1),
                    (layout.Front - layout.Back) * scale / (grid - 1));
            }
        }
        return bad * 100f + blocked * 12f + profile.Spread * 2f + profile.Grade * 10f;
    }

    private static bool Probe(Mission mission, Scene scene, Vec2 p, bool fine, out float height, out bool obstacle)
        => Probe(mission, scene, p, fine, out height, out obstacle, out _);

    private static bool Probe(Mission mission, Scene scene, Vec2 p, bool fine, out float height, out bool obstacle, out SiteRejection rejection)
    {
        height = 0f;
        obstacle = false;
        rejection = SiteRejection.None;
        try
        {
            if (!mission.IsPositionInsideBoundaries(p))
            {
                rejection = SiteRejection.Boundary;
                return false;
            }
            height = scene.GetTerrainHeight(p, true);
            if (!IsFinite(height))
            {
                rejection = SiteRejection.Terrain;
                return false;
            }
            if (scene.GetWaterLevelAtPosition(p, true, true) > height - WetMargin)
            {
                rejection = SiteRejection.Water;
                return false;
            }
            if (!fine) return true;
            PathFaceRecord record = PathFaceRecord.NullFaceRecord;
            Vec3 terrainPoint = new Vec3(p.x, p.y, height);
            scene.GetNavMeshFaceIndex(ref record, terrainPoint, true);
            var navigation = new WorldPosition(scene, UIntPtr.Zero, terrainPoint, false);
            float navHeight = navigation.GetNavMeshZ();
            if (!record.IsValid() || !IsFinite(navHeight) || Math.Abs(navHeight - height) > 0.3f)
            {
                rejection = SiteRejection.Navigation;
                return false;
            }
            Vec3 top = new Vec3(p.x, p.y, height + 40f);
            Vec3 bottom = new Vec3(p.x, p.y, height - 1f);
            if (scene.RayCastForClosestEntityOrTerrain(top, bottom, out float _, out Vec3 hit, out WeakGameEntity hitEntity, 0.3f, ClearanceExcludeFlags)
                && hit.z > height + ObstacleHeight)
            {
                obstacle = true;
                // Only an entity whose complete small clutter subtree can be removed is tolerated.
                if (!ClutterCaches.TryGetValue(scene, out ClutterCache clutter) || !clutter.Complete
                    || !hitEntity.IsValid || !clutter.Removable.Contains(hitEntity.Pointer))
                {
                    rejection = SiteRejection.Obstacle;
                    return false;
                }
            }
            return true;
        }
        catch
        {
            rejection = SiteRejection.EngineError;
            return false;
        }
    }

	/// <summary>
	/// Hides small scene rocks, bushes and stray trees standing inside the palisade and
	/// switches off their collision, so tents and lanes are not blocked. Large pieces
	/// (cliffs, outer meshes) are left alone. Runs once, before the camp props exist.
	/// </summary>
		internal static int ClearClutter(Scene scene, MatrixFrame campFrame, FieldCampLayout layout, float scale)
		{
			if (!ClutterCaches.TryGetValue(scene, out ClutterCache cache) || !cache.Complete)
			{
				throw new InvalidOperationException("Camp clutter inventory unavailable or exceeds its budget.");
			}
			int hidden = 0;
			for (int i = 0; i < cache.Roots.Count; i++)
			{
				GameEntity entity = cache.Roots[i];
				Vec3 local = campFrame.TransformToLocal(entity.GlobalPosition);
				if (local.x < -layout.HalfX * scale - ClutterMaxExtent || local.x > layout.HalfX * scale + ClutterMaxExtent
					|| local.y < layout.Back * scale - ClutterMaxExtent || local.y > layout.Front * scale + ClutterMaxExtent)
				{
					continue;
				}
				// No parent container is disabled just because one child has a clutter name.
				entity.SetVisibilityExcludeParents(false);
				entity.AddBodyFlags(BodyFlags.Disabled, applyToChildren: true);
				entity.SetPhysicsState(isEnabled: false, setChildren: true);
				hidden++;
			}
			ClutterCaches.Remove(scene);
			return hidden;
		}

		/// <summary>One bounded scene enumeration, shared by candidate validation and clearing.</summary>
		private static void CaptureClutter(Scene scene)
		{
			ClutterCaches.Remove(scene);
			var cache = new ClutterCache();
			ClutterCaches.Add(scene, cache);
			var entities = new List<GameEntity>();
			try
			{
				scene.GetEntities(ref entities);
				if (entities.Count > MaxSceneEntities)
				{
					return;
				}
				var visited = new HashSet<GameEntity>();
				var pending = new Stack<GameEntity>(entities);
				while (pending.Count > 0)
				{
					GameEntity entity = pending.Pop();
					if (entity == null || !visited.Add(entity))
					{
						continue;
					}
					if (visited.Count > MaxSceneEntities || pending.Count + entity.ChildCount > MaxSceneEntities)
					{
						return;
					}
					for (int c = 0; c < entity.ChildCount; c++)
					{
						pending.Push(entity.GetChild(c));
					}
					if (cache.Removable.Contains(entity.Pointer) || !IsClutterName(entity.Name) || !entity.IsVisibleIncludeParents())
					{
						continue;
					}
					Vec3 min = entity.GlobalBoxMin;
					Vec3 max = entity.GlobalBoxMax;
					if (!min.IsValid || !max.IsValid || max.x < min.x || max.y < min.y || max.z < min.z
						|| max.x - min.x > ClutterMaxExtent || max.y - min.y > ClutterMaxExtent
						|| max.z - min.z > (IsVegetationName(entity.Name) ? 35f : ClutterMaxExtent))
					{
						continue;
					}
					// A named leaf/subtree can be cleared even when it belongs to an unnamed scene container.
					var subtree = new Stack<GameEntity>();
					var members = new List<GameEntity>();
					subtree.Push(entity);
					bool safe = true;
					while (subtree.Count > 0 && safe)
					{
						GameEntity child = subtree.Pop();
						if (!IsClutterName(child.Name) && !string.IsNullOrEmpty(child.Name))
						{
							bool structure = false;
							for (int n = 0; n < StructureNames.Length; n++)
							{
								structure |= child.Name.IndexOf(StructureNames[n], StringComparison.OrdinalIgnoreCase) >= 0;
							}
							if (structure)
							{
								safe = false;
								continue;
							}
						}
						members.Add(child);
						Vec3 childMin = child.GlobalBoxMin;
						Vec3 childMax = child.GlobalBoxMax;
						safe = members.Count <= 256 && childMin.IsValid && childMax.IsValid && childMax.x >= childMin.x && childMax.y >= childMin.y && childMax.z >= childMin.z
							&& childMin.x >= min.x - 0.1f && childMin.y >= min.y - 0.1f && childMin.z >= min.z - 0.1f
							&& childMax.x <= max.x + 0.1f && childMax.y <= max.y + 0.1f && childMax.z <= max.z + 0.1f;
						if (subtree.Count + child.ChildCount > 256)
						{
							safe = false;
						}
						for (int c = 0; safe && c < child.ChildCount; c++)
						{
							subtree.Push(child.GetChild(c));
						}
					}
					if (safe)
					{
						cache.Roots.Add(entity);
						for (int c = 0; c < members.Count; c++)
						{
							cache.Removable.Add(members[c].Pointer);
						}
					}
				}
				cache.Complete = true;
			}
			catch (Exception ex)
			{
				Logger.Log(LogSource, "clutter inventory failed: " + ex.Message);
			}
		}

		private static bool ValidateCriticalAreas(Mission mission, Scene scene, Candidate candidate, FieldCampLayout layout, float scale)
		{
			MatrixFrame frame = FrameAt(scene, candidate.Position, candidate.Forward);
			// The gate/main road and the two passages around the command tent must not hide an unremovable collider.
			if (!ProbeStrip(mission, scene, frame, new Vec2(0f, layout.Front), layout.PlayerStart, 1.1f, scale)
				|| !ProbeStrip(mission, scene, frame, new Vec2(-5.5f, 9f), new Vec2(-5.5f, layout.Back + 5f), 0.7f, scale)
				|| !ProbeStrip(mission, scene, frame, new Vec2(5.5f, 9f), new Vec2(5.5f, layout.Back + 5f), 0.7f, scale))
			{
				return false;
			}
			if (!ProbeStrip(mission, scene, frame, new Vec2(-5.5f, 9f), new Vec2(5.5f, 9f), 0.7f, scale))
			{
				return false;
			}
			for (int i = 0; i < layout.HorseSlots.Count; i++)
			{
				Vec2 p = layout.HorseSlots[i].Position;
				if (!ProbeStrip(mission, scene, frame, p - new Vec2(0f, 1.6f), p + new Vec2(0f, 1.6f), 0.85f, scale))
				{
					return false;
				}
			}
			for (int i = 0; i < layout.Props.Count; i++)
			{
				FieldCampLayout.Prop prop = layout.Props[i];
				if (prop.Kind == FieldCampLayout.PropKind.Tent
					&& !ProbeStrip(mission, scene, frame, prop.Position - new Vec2(0f, 2f), prop.Position + new Vec2(0f, 2f), 2f, scale))
				{
					return false;
				}
			}
			return true;
		}

		private static bool ProbeStrip(Mission mission, Scene scene, MatrixFrame frame, Vec2 from, Vec2 to, float halfWidth, float scale)
		{
			Vec2 direction = to - from;
			int steps = Math.Max(1, (int)Math.Ceiling(direction.Length / 1f));
			if (steps > MaxPassageSteps)
			{
				return false;
			}
			Vec2 side = direction.LengthSquared > 0.001f ? new Vec2(direction.y, -direction.x).Normalized() : new Vec2(1f, 0f);
			float previous = float.NaN;
			for (int i = 0; i <= steps; i++)
			{
				float low = float.MaxValue;
				float high = float.MinValue;
				for (int lane = -1; lane <= 1; lane++)
				{
					Vec2 p = frame.TransformToParent(new Vec3((from.x + direction.x * i / steps + side.x * lane * halfWidth) * scale,
						(from.y + direction.y * i / steps + side.y * lane * halfWidth) * scale, 0f)).AsVec2;
					if (!Probe(mission, scene, p, true, out float height, out _))
					{
						return false;
					}
					low = Math.Min(low, height);
					high = Math.Max(high, height);
				}
				float middle = (low + high) * 0.5f;
				if (high - low > 0.5f || (!float.IsNaN(previous) && Math.Abs(middle - previous) > 0.45f))
				{
					return false;
				}
				previous = middle;
			}
			return true;
		}

		/// <summary>
		/// Main-thread initialization only, after ALL camp/custom props and before actors. At most
		/// MaxGroundCells cells, each with one terrain/water/nav lookup and bounded collider rays.
		/// A Scene weak key owns the cache; queries never scan entities, reflect or call the engine.
		/// </summary>
		internal static void InitializeGroundCache(Scene scene, MatrixFrame frame, FieldCampLayout layout, float scale)
		{
			GroundCaches.Remove(scene);
			var cache = new GroundCache { Frame = frame, MinX = -layout.HalfX * scale - 2f, MinY = layout.Back * scale - 2f };
			cache.Width = (int)Math.Ceiling((layout.HalfX * 2f * scale + 4f) / GroundStep) + 1;
			cache.Height = (int)Math.Ceiling(((layout.Front - layout.Back) * scale + 4f) / GroundStep) + 1;
            float margin = 0.8f * scale;
            cache.CampMinX = (int)Math.Floor((-layout.HalfX * scale + margin - cache.MinX) / GroundStep) + 1;
            cache.CampMaxX = (int)Math.Ceiling((layout.HalfX * scale - margin - cache.MinX) / GroundStep) - 1;
            cache.CampMinY = (int)Math.Floor((layout.Back * scale + margin - cache.MinY) / GroundStep) + 1;
            cache.CampMaxY = (int)Math.Ceiling((layout.Front * scale - margin - cache.MinY) / GroundStep) - 1;
			GroundCaches.Add(scene, cache);
			if (cache.Width <= 0 || cache.Height <= 0 || (long)cache.Width * cache.Height > MaxGroundCells)
			{
				throw new InvalidOperationException("Camp ground cache exceeds its initialization budget.");
			}
			cache.Heights = new float[cache.Width * cache.Height];
			cache.Open = new bool[cache.Heights.Length];
			Mission mission = Mission.Current;
			if (mission == null || mission.Scene != scene)
			{
				throw new InvalidOperationException("Camp ground cache must be initialized in its active mission.");
			}
			int open = 0;
			for (int y = 0; y < cache.Height; y++)
			{
				for (int x = 0; x < cache.Width; x++)
				{
					int index = y * cache.Width + x;
					cache.Heights[index] = float.NaN;
					Vec3 p = frame.TransformToParent(new Vec3(cache.MinX + x * GroundStep, cache.MinY + y * GroundStep, 0f));
					try
					{
						float z = scene.GetTerrainHeight(p.AsVec2, true);
						cache.Heights[index] = z;
						if (!IsFinite(z) || !mission.IsPositionInsideBoundaries(p.AsVec2)
							|| scene.GetWaterLevelAtPosition(p.AsVec2, true, true) > z - WetMargin)
						{
							continue;
						}
						p.z = z;
			PathFaceRecord face = PathFaceRecord.NullFaceRecord;
			scene.GetNavMeshFaceIndex(ref face, p, true);
			if (!face.IsValid())
			{
				continue;
			}
			var navigation = new WorldPosition(scene, UIntPtr.Zero, p, false);
			float navHeight = navigation.GetNavMeshZ();
			if (!IsFinite(navHeight) || Math.Abs(navHeight - z) > 0.3f || !CellHasClearance(scene, p))
						{
							continue;
						}
						cache.Open[index] = true;
						open++;
					}
					catch
					{
						// Unknown/failed samples stay closed; never inherit an actor/prop surface height.
					}
				}
			}
			cache.BaseOpen = (bool[])cache.Open.Clone();
            cache.FenceBlocked = new bool[cache.Open.Length];
            // Rasterize only accepted fence pieces, once after placement. Include actor
            // radius and half a grid cell so no route centre can graze a rail or post.
            foreach (FieldCampProps.FenceObstacle fence in FieldCampProps.FenceObstacles)
            {
                Vec3 a = frame.TransformToLocal(fence.From), b = frame.TransformToLocal(fence.To);
                float clearance = fence.HalfWidth + 0.35f + GroundStep * 0.71f;
                int left = Math.Max(0, (int)Math.Floor((Math.Min(a.x, b.x) - clearance - cache.MinX) / GroundStep));
                int right = Math.Min(cache.Width - 1, (int)Math.Ceiling((Math.Max(a.x, b.x) + clearance - cache.MinX) / GroundStep));
                int bottom = Math.Max(0, (int)Math.Floor((Math.Min(a.y, b.y) - clearance - cache.MinY) / GroundStep));
                int top = Math.Min(cache.Height - 1, (int)Math.Ceiling((Math.Max(a.y, b.y) + clearance - cache.MinY) / GroundStep));
                Vec2 segment = b.AsVec2 - a.AsVec2;
                for (int y = bottom; y <= top; y++)
                    for (int x = left; x <= right; x++)
                    {
                        Vec2 point = new Vec2(cache.MinX + x * GroundStep, cache.MinY + y * GroundStep);
                        Vec2 delta = point - a.AsVec2;
                        float t = segment.LengthSquared > 0.0001f
                            ? Math.Max(0f, Math.Min(1f, Vec2.DotProduct(delta, segment) / segment.LengthSquared)) : 0f;
                        if (point.DistanceSquared(a.AsVec2 + segment * t) > clearance * clearance) continue;
                        int cell = y * cache.Width + x;
                        cache.FenceBlocked[cell] = true;
                        if (cache.Open[cell]) { cache.Open[cell] = false; open--; }
                    }
            }
            cache.Router = new FieldCampGroundRouter(cache.Width, cache.Height, cache.Open, cache.Heights);
			cache.Ready = open > 0;
			Logger.Log(LogSource, "ground cache cells=" + cache.Open.Length + " open=" + open + " step=" + GroundStep);
			if (!cache.Ready) throw new InvalidOperationException("Camp ground cache has no walkable cells.");
			Vec3 player = frame.TransformToParent(new Vec3(layout.PlayerStart.x * scale, layout.PlayerStart.y * scale, 0f));
			Vec3 gate = frame.TransformToParent(new Vec3(0f, layout.Front * scale, 0f));
			// A fire, fence or tent can block the straight line without blocking a route.
			// Cache validity belongs to its samples, not to this optional gate connection.
			if (!TryFindNearbyGroundedPosition(scene, player, 0.45f, 6f, out Vec3 entry)
				|| !TryFindNearbyGroundedPosition(scene, gate, 0.45f, 3f, out Vec3 exit)
				|| !TryBuildGroundedRoute(scene, entry, exit, out _, out _))
			{
				Logger.Log(LogSource, "[WARN] camp gate route unavailable; retaining independently validated actor positions");
			}
		}

        // Initialization/spawn only. Search at most 24 rings (49 x 49 cells),
        // retaining the same terrain, collision and body-clearance checks as exact placement.
        internal static bool TryFindNearbyGroundedPosition(Scene scene, Vec3 desired, float clearanceRadius, float maxDistance, out Vec3 grounded)
        {
            grounded = Vec3.Invalid;
            if (!IsFinite(maxDistance) || maxDistance < 0f || maxDistance > 12f || !desired.IsValid) return false;
            if (TryResolveGroundedPosition(scene, desired, clearanceRadius, out grounded)) return true;
            int rings = (int)Math.Floor(maxDistance / GroundStep);
            for (int ring = 1; ring <= rings; ring++)
                for (int dy = -ring; dy <= ring; dy++)
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (Math.Abs(dx) != ring && Math.Abs(dy) != ring) continue;
                        if ((dx * dx + dy * dy) * GroundStep * GroundStep > maxDistance * maxDistance) continue;
                        if (TryResolveGroundedPosition(scene, desired + new Vec3(dx * GroundStep, dy * GroundStep, 0f), clearanceRadius, out grounded))
                            return true;
                    }
            return false;
        }

        // Spawn-only fallback: the cached grid is preferred. If its full-body
        // neighborhood is too restrictive, probe actual individual positions.
        // Bounded to 24 half-metre rings and never used by movement ticks.
        internal static bool TryFindSpawnPosition(Scene scene, Vec3 desired, float clearanceRadius, out Vec3 grounded)
        {
            if (TryFindNearbyGroundedPosition(scene, desired, clearanceRadius, 12f, out grounded)) return true;
            Mission mission = Mission.Current;
            if (scene == null || mission == null || mission.Scene != scene || !desired.IsValid) return false;
            for (int ring = 0; ring <= 24; ring++)
                for (int dy = -ring; dy <= ring; dy++)
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (ring != 0 && Math.Abs(dx) != ring && Math.Abs(dy) != ring) continue;
                        if (dx * dx + dy * dy > 24 * 24) continue;
                        Vec3 point = desired + new Vec3(dx * GroundStep, dy * GroundStep, 0f);
                        try
                        {
                            if (!Probe(mission, scene, point.AsVec2, false, out float z, out _)) continue;
                            point.z = z;
                            PathFaceRecord face = PathFaceRecord.NullFaceRecord;
                            scene.GetNavMeshFaceIndex(ref face, point, true);
                            if (!face.IsValid()) continue;
                            var navigation = new WorldPosition(scene, UIntPtr.Zero, point, false);
                            float navZ = navigation.GetNavMeshZ();
                            if (!IsFinite(navZ) || Math.Abs(navZ - z) > 0.3f || !CellHasClearance(scene, point)) continue;
                            bool clear = true;
                            for (int edge = 0; edge < 4 && clear; edge++)
                            {
                                Vec3 offset = edge < 2 ? new Vec3(edge == 0 ? clearanceRadius : -clearanceRadius, 0f, 0f)
                                    : new Vec3(0f, edge == 2 ? clearanceRadius : -clearanceRadius, 0f);
                                Vec3 body = point + offset;
                                clear = Probe(mission, scene, body.AsVec2, false, out float bodyZ, out _)
                                    && Math.Abs(bodyZ - z) <= MaxGroundDelta && CellHasClearance(scene, body);
                            }
                            if (!clear) continue;
                            grounded = point + new Vec3(0f, 0f, 0.04f);
                            return true;
                        }
                        catch { }
                    }
            return false;
        }

		private static bool CellHasClearance(Scene scene, Vec3 p)
		{
			// One swept-volume ray covers the entire 0.5 m cell (half diagonal 0.354 m),
			// rather than five rays per cell. The lower cap stays above terrain so level
			// ground is not mistaken for a collider; body-height posts/walls are excluded.
			const float halfDiagonal = GroundStep * 0.71f;
			return !scene.RayCastForClosestEntityOrTerrain(p + new Vec3(0f, 0f, halfDiagonal + 0.12f),
				p + new Vec3(0f, 0f, 2.35f), out _, out _, out _, halfDiagonal,
				ClearanceExcludeFlags);
		}

		/// <summary>World input/output. Nearest sampled terrain point only (<=0.36 m); false means DO NOT spawn/move.</summary>
		internal static bool TryResolveGroundedPosition(Scene scene, Vec3 desired, float clearanceRadius, out Vec3 grounded)
		{
			grounded = Vec3.Invalid;
			if (scene == null || !desired.IsValid || !IsFinite(desired.x) || !IsFinite(desired.y)
				|| !IsFinite(clearanceRadius) || clearanceRadius < 0f || clearanceRadius > 2f
				|| !GroundCaches.TryGetValue(scene, out GroundCache cache) || !cache.Ready)
			{
				return false;
			}
			Vec3 local = cache.Frame.TransformToLocal(desired);
			if (!IsFinite(local.x) || !IsFinite(local.y) || local.x < cache.MinX || local.y < cache.MinY
				|| local.x > cache.MinX + (cache.Width - 1) * GroundStep || local.y > cache.MinY + (cache.Height - 1) * GroundStep)
			{
				return false;
			}
			int x = (int)Math.Round((local.x - cache.MinX) / GroundStep);
			int y = (int)Math.Round((local.y - cache.MinY) / GroundStep);
			int reach = (int)Math.Ceiling((clearanceRadius + GroundStep * 0.71f) / GroundStep);
			if (x - reach < 0 || y - reach < 0 || x + reach >= cache.Width || y + reach >= cache.Height)
			{
				return false;
			}
			float height = cache.Heights[y * cache.Width + x];
			float radius = clearanceRadius + GroundStep * 0.71f;
			for (int dy = -reach; dy <= reach; dy++)
			{
				for (int dx = -reach; dx <= reach; dx++)
				{
					if ((dx * dx + dy * dy) * GroundStep * GroundStep > radius * radius)
					{
						continue;
					}
					int index = (y + dy) * cache.Width + x + dx;
					if (!cache.Open[index] || Math.Abs(cache.Heights[index] - height) > MaxGroundDelta)
					{
						return false;
					}
				}
			}
			grounded = cache.Frame.TransformToParent(new Vec3(cache.MinX + x * GroundStep, cache.MinY + y * GroundStep, 0f));
			grounded.z = height + 0.04f;
			return true;
		}

        // Geometry changes only: sample the affected region, reusing the existing router arrays.
        // Never runs in Tick and never rescans Mission.Agents or the whole scene.
        internal static void RefreshGroundObstacles(Scene scene, IReadOnlyList<FieldCampCeremonyArea> areas)
        {
            if (scene == null || areas == null || !GroundCaches.TryGetValue(scene, out GroundCache cache) ||
                !cache.Ready || cache.BaseOpen == null) return;
            var touched = new HashSet<int>();
            for (int i = 0; i < areas.Count; i++)
            {
                FieldCampCeremonyArea area = areas[i];
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                for (int corner = 0; corner < 4; corner++)
                {
                    Vec3 local = cache.Frame.TransformToLocal(new Vec3(
                        (corner & 1) == 0 ? area.Min.x - 1.5f : area.Max.x + 1.5f,
                        (corner & 2) == 0 ? area.Min.y - 1.5f : area.Max.y + 1.5f, cache.Frame.origin.z));
                    minX = Math.Min(minX, local.x); maxX = Math.Max(maxX, local.x);
                    minY = Math.Min(minY, local.y); maxY = Math.Max(maxY, local.y);
                }
                int left = Math.Max(0, (int)Math.Floor((minX - cache.MinX) / GroundStep));
                int right = Math.Min(cache.Width - 1, (int)Math.Ceiling((maxX - cache.MinX) / GroundStep));
                int bottom = Math.Max(0, (int)Math.Floor((minY - cache.MinY) / GroundStep));
                int top = Math.Min(cache.Height - 1, (int)Math.Ceiling((maxY - cache.MinY) / GroundStep));
                for (int y = bottom; y <= top; y++)
                    for (int x = left; x <= right; x++)
                    {
                        int cell = y * cache.Width + x;
                        if (!cache.BaseOpen[cell] || cache.FenceBlocked[cell] || touched.Contains(cell)) continue;
                        Vec3 p = cache.Frame.TransformToParent(new Vec3(cache.MinX + x * GroundStep, cache.MinY + y * GroundStep, 0f));
                        // Rotated component bounds do not occupy the empty corners
                        // of their broad-phase box. Sample each relevant cell once.
                        if (!area.Contains(p, 1.5f)) continue;
                        touched.Add(cell);
                        p.z = cache.Heights[cell];
                        try { cache.Open[cell] = CellHasClearance(scene, p); }
                        catch { cache.Open[cell] = false; }
                    }
            }
        }

        internal static bool TryBuildGroundedRoute(Scene scene, Vec3 from, Vec3 target, out Vec3[] route, out bool covered, bool stayInsideCamp = false)
        {
            route = Array.Empty<Vec3>(); covered = false;
            if (scene == null || !from.IsValid || !target.IsValid || !GroundCaches.TryGetValue(scene, out GroundCache cache)
                || !cache.Ready || cache.Router == null) return false;
            Vec3 localFrom = cache.Frame.TransformToLocal(from), localTarget = cache.Frame.TransformToLocal(target);
            if (!WithinGroundGrid(cache, localFrom) || !WithinGroundGrid(cache, localTarget)) return false;
            covered = true;
            if (!TryResolveGroundedPosition(scene, target, 0.4f, out Vec3 end)) return false;
            // Seated actors may start next to furniture; find the closest traversable neighbouring cell once.
            int sx = (int)Math.Round((localFrom.x - cache.MinX) / GroundStep);
            int sy = (int)Math.Round((localFrom.y - cache.MinY) / GroundStep);
            Vec3 endLocal = cache.Frame.TransformToLocal(end);
            int goal = (int)Math.Round((endLocal.y - cache.MinY) / GroundStep) * cache.Width
                + (int)Math.Round((endLocal.x - cache.MinX) / GroundStep);
            bool constrainToCamp = stayInsideCamp && sx >= cache.CampMinX && sx <= cache.CampMaxX
                && sy >= cache.CampMinY && sy <= cache.CampMaxY;
            int[] corners = null;
            for (int radius = 0; radius <= 3 && corners == null; radius++)
                for (int dy = -radius; dy <= radius && corners == null; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                        int x = sx + dx, y = sy + dy;
                        if (x < 0 || y < 0 || x >= cache.Width || y >= cache.Height) continue;
                        if (!cache.Router.IsTraversable(y * cache.Width + x)) continue;
                        Vec3 start = cache.Frame.TransformToParent(new Vec3(cache.MinX + x * GroundStep, cache.MinY + y * GroundStep, 0f));
                        start.z = cache.Heights[y * cache.Width + x] + 0.04f;
                        // The actor walks the short start connector too; never jump through a solid body.
                        if (scene.RayCastForClosestEntityOrTerrain(from + new Vec3(0f, 0f, 1f), start + new Vec3(0f, 0f, 1f),
                            out _, out _, out _, 0.3f, ClearanceExcludeFlags)) continue;
                        if (cache.Router.TryRoute(y * cache.Width + x, goal, out int[] found,
                            constrainToCamp ? cache.CampMinX : 0, constrainToCamp ? cache.CampMinY : 0,
                            constrainToCamp ? cache.CampMaxX : cache.Width - 1, constrainToCamp ? cache.CampMaxY : cache.Height - 1)) { corners = found; break; }
                        // A valid source in another component must not trigger 48 copies of the same search.
                        if (cache.Router.LastSearchVisited > 0) return false;
                    }
            if (corners == null) return false;
            route = new Vec3[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                int cell = corners[i];
                route[i] = cache.Frame.TransformToParent(new Vec3(cache.MinX + cell % cache.Width * GroundStep,
                    cache.MinY + cell / cache.Width * GroundStep, 0f));
                route[i].z = cache.Heights[cell] + 0.04f;
            }
            // The first BFS cell describes where the actor already is. Requiring an
            // engine movement of 0.2-0.35 m to its centre can leave the actor idle.
            // Omit it only when the actual position has a clear connector to the next leg.
            if (route.Length > 1 && from.AsVec2.DistanceSquared(route[0].AsVec2) <= 0.25f
                && TryValidateGroundedPassage(scene, from, route[1], 0.15f))
            {
                var forwardRoute = new Vec3[route.Length - 1];
                Array.Copy(route, 1, forwardRoute, 0, forwardRoute.Length);
                route = forwardRoute;
            }
            return true;
        }

        private static bool WithinGroundGrid(GroundCache cache, Vec3 local) => IsFinite(local.x) && IsFinite(local.y)
            && local.x >= cache.MinX && local.y >= cache.MinY
            && local.x <= cache.MinX + (cache.Width - 1) * GroundStep
            && local.y <= cache.MinY + (cache.Height - 1) * GroundStep;

		/// <summary>Conservative direct ground corridor, cache-only, capped at MaxPassageSteps. Not a navmesh rebuild/pathfinder.</summary>
		internal static bool TryValidateGroundedPassage(Scene scene, Vec3 from, Vec3 to, float clearanceRadius)
		{
			if (!from.IsValid || !to.IsValid)
			{
				return false;
			}
			float distance = from.AsVec2.Distance(to.AsVec2);
			if (!IsFinite(distance) || distance > MaxPassageSteps * GroundStep * 0.5f)
			{
				return false;
			}
			int steps = (int)Math.Ceiling(distance / (GroundStep * 0.5f));
			if (steps > MaxPassageSteps)
			{
				return false;
			}
			steps = Math.Max(1, steps);
			Vec3 previous = Vec3.Invalid;
			for (int i = 0; i <= steps; i++)
			{
				if (!TryResolveGroundedPosition(scene, from + (to - from) * (i / (float)steps), clearanceRadius, out Vec3 p)
					|| (previous.IsValid && Math.Abs(previous.z - p.z) > MaxGroundDelta))
				{
					return false;
				}
				previous = p;
			}
			return true;
		}

		/// <summary>Height only, not a spawn authorization; no prop/tree surface or engine query.</summary>
		internal static bool TryGetCachedTerrainHeight(Scene scene, Vec2 world, out float height)
		{
			height = 0f;
			if (scene == null || !IsFinite(world.x) || !IsFinite(world.y)
				|| !GroundCaches.TryGetValue(scene, out GroundCache cache) || !cache.Ready)
			{
				return false;
			}
			Vec3 local = cache.Frame.TransformToLocal(new Vec3(world.x, world.y, 0f));
			if (!IsFinite(local.x) || !IsFinite(local.y) || local.x < cache.MinX || local.y < cache.MinY
				|| local.x > cache.MinX + (cache.Width - 1) * GroundStep || local.y > cache.MinY + (cache.Height - 1) * GroundStep)
			{
				return false;
			}
			int x = (int)Math.Round((local.x - cache.MinX) / GroundStep);
			int y = (int)Math.Round((local.y - cache.MinY) / GroundStep);
			if (x < 0 || y < 0 || x >= cache.Width || y >= cache.Height)
			{
				return false;
			}
			height = cache.Heights[y * cache.Width + x];
			return IsFinite(height);
		}

		internal static void ReleaseCaches(Scene scene)
		{
			if (scene != null)
			{
				GroundCaches.Remove(scene);
				ClutterCaches.Remove(scene);
			}
		}

		private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsVegetationName(string name) => !string.IsNullOrEmpty(name)
        && (name.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("bush", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("shrub", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("plant", StringComparison.OrdinalIgnoreCase) >= 0);

	private static bool IsClutterName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
			for (int i = 0; i < StructureNames.Length; i++)
			{
				if (name.IndexOf(StructureNames[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return false;
				}
			}
			for (int i = 0; i < ClutterNames.Length; i++)
		{
			if (name.IndexOf(ClutterNames[i], StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Camp frame: origin on the ground, camp +y (toward the parade ground) along <paramref name="forward"/>.</summary>
	private static MatrixFrame FrameAt(Scene scene, Vec2 position, Vec2 forward)
	{
		Vec2 f = forward.LengthSquared < 0.0001f ? new Vec2(0f, 1f) : forward.Normalized();
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation.s = new Vec3(f.y, -f.x, 0f);
		frame.rotation.f = new Vec3(f.x, f.y, 0f);
		frame.rotation.u = new Vec3(0f, 0f, 1f);
		frame.origin = new Vec3(position.x, position.y, 0f);
		frame.origin.z = scene.GetTerrainHeight(position, true);
		return frame;
	}

	private static string Format(Vec2 p)
	{
		return p.x.ToString("0") + "," + p.y.ToString("0");
	}
}
