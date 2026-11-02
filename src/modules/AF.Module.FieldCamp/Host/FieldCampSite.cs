using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Selects a dry, level whole-camp rectangle once on the main thread. Always searches
/// bounded spawn paths plus the scene-wide grid; a safe player point never authorizes camp props.
/// At most 1200 seeds x two orientations x 25 terrain probes, then 32 x 81 detailed
/// probes and existing critical-area checks. Failed selection keeps manual player control only.
/// Collision/terrain route caches are built once after all props; no per-frame scene scan.
/// </summary>
internal static class FieldCampSite
{
	private const string LogSource = "FieldCamp";
	private const int MaxSpawnPaths = 32;
	private const float PathStep = 12f;
	private const int MaxCandidates = 600;
		private const float WetMargin = 0.3f;
		private const BodyFlags ClearanceExcludeFlags = BodyFlags.CommonCollisionExcludeFlagsForAgent & ~BodyFlags.DoNotCollideWithRaycast;
	/// <summary>A raycast hit this far above the terrain means a tree, rock or building stands there.</summary>
	private const float ObstacleHeight = 0.25f;
	private const float ClutterMaxExtent = 9f;
		private static readonly string[] ClutterNames = { "rock", "stone", "boulder", "bush", "shrub", "tree", "plant", "branch", "stump" };
		private static readonly string[] StructureNames = { "wall", "gate", "house", "building", "bridge", "tower", "cliff", "terrain", "road", "navmesh", "barrier", "ruin", "floor", "stairs" };
		// Never compress spacing without shrinking the real prefab footprints and actor bodies.
		private static readonly float[] Scales = { 1f };
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
			internal bool Ready;
			internal FieldCampGroundRouter Router;
		}

	private struct Candidate
	{
		internal Vec2 Position;
		internal Vec2 Forward;
	}

	/// <param name="fitScale">Always 1; real prop footprints are not compressed.</param>
	internal static MatrixFrame Resolve(Mission mission, FieldCampLayout layout, out float fitScale)
	{
		Scene scene = mission.Scene;
		fitScale = Scales[0];
		GroundCaches.Remove(scene);
		CaptureClutter(scene);
		bool hasEncounter = TryEncounterPoint(mission, out Vec2 encounter);
		List<Candidate> candidates = CollectCandidates(scene, hasEncounter, encounter);
		// Always include the scene-wide grid. Near-encounter spawn paths alone can all be on a hillside.
		var grid = new List<Candidate>();
		AddGrid(scene, grid, hasEncounter, encounter);
		candidates.AddRange(grid);
		var seeds = new List<FieldCampSiteSeed>(Math.Min(candidates.Count, FieldCampSitePlanner.MaxSeeds));
		for (int i = 0; i < candidates.Count && i < FieldCampSitePlanner.MaxSeeds; i++)
			seeds.Add(new FieldCampSiteSeed(candidates[i].Position.x, candidates[i].Position.y,
				candidates[i].Forward.x, candidates[i].Forward.y));
		bool chosen = FieldCampSitePlanner.TryChoose(seeds, layout.HalfX, layout.Back, layout.Front,
			(float x, float y, bool detailed, out float height) => Probe(mission, scene, new Vec2(x, y), detailed, out height, out _),
			seed => ValidateCriticalAreas(mission, scene, new Candidate { Position = new Vec2(seed.X, seed.Y), Forward = new Vec2(seed.ForwardX, seed.ForwardY) }, layout, 1f),
			out FieldCampSiteSeed site, out float spread);
		if (!chosen) throw new InvalidOperationException("No level whole-camp footprint found; refusing to spread the camp over an arbitrary player-safe hillside.");
		Logger.Log(LogSource, "level whole-camp site pos=" + Format(new Vec2(site.X, site.Y)) + " spread=" + spread.ToString("0.00")
			+ " footprint=" + (layout.HalfX * 2f).ToString("0") + "x" + (layout.Front - layout.Back).ToString("0") + " candidates=" + seeds.Count);
		return FrameAt(scene, new Vec2(site.X, site.Y), new Vec2(site.ForwardX, site.ForwardY));
	}

    /// <summary>Once at player spawn; cache scan then bounded native spawn candidates, never from upkeep/Tick.</summary>
    internal static bool TryFindPlayerStart(Mission mission, Vec3 desired, out Vec3 grounded)
    {
        grounded = Vec3.Invalid;
        Scene scene = mission.Scene;
        if (GroundCaches.TryGetValue(scene, out GroundCache cache) && cache.Ready)
        {
            float best = float.MaxValue;
            for (int y = 0; y < cache.Height; y++)
                for (int x = 0; x < cache.Width; x++)
                {
                    if (!cache.Open[y * cache.Width + x]) continue;
                    Vec3 p = cache.Frame.TransformToParent(new Vec3(cache.MinX + x * GroundStep, cache.MinY + y * GroundStep, 0f));
                    float distance = p.AsVec2.DistanceSquared(desired.AsVec2);
                    if (distance >= best || !TryResolveGroundedPosition(scene, p, 0.45f, out Vec3 safe)) continue;
                    best = distance;
                    grounded = safe;
                }
            if (grounded.IsValid) return true;
        }
        if (TryProbePlayerPosition(mission, desired.AsVec2, out grounded)) return true;
        bool hasEncounter = TryEncounterPoint(mission, out Vec2 encounter);
        var candidates = CollectCandidates(scene, hasEncounter, encounter);
        for (int i = 0; i < candidates.Count; i++)
            if (TryProbePlayerPosition(mission, candidates[i].Position, out grounded)) return true;
        return false;
    }

    private static bool TryProbePlayerPosition(Mission mission, Vec2 position, out Vec3 grounded)
    {
        grounded = Vec3.Invalid;
        float centre = 0f;
        for (int sample = 0; sample < 5; sample++)
        {
            Vec2 offset = sample == 0 ? Vec2.Zero : sample == 1 ? new Vec2(0.5f, 0f)
                : sample == 2 ? new Vec2(-0.5f, 0f) : sample == 3 ? new Vec2(0f, 0.5f) : new Vec2(0f, -0.5f);
            Vec2 p = position + offset;
            if (!Probe(mission, mission.Scene, p, true, out float height, out bool obstacle) || obstacle) return false;
            if (sample == 0) centre = height;
            else if (Math.Abs(height - centre) > MaxGroundDelta) return false;
            if (!CellHasClearance(mission.Scene, new Vec3(p.x, p.y, height))) return false;
        }
        grounded = new Vec3(position.x, position.y, centre + 0.04f);
        return true;
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
		if (list.Count < 20)
		{
			AddGrid(scene, list, hasEncounter, encounter);
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

	private static bool Probe(Mission mission, Scene scene, Vec2 p, bool fine, out float height, out bool obstacle)
	{
		height = 0f;
		obstacle = false;
		try
		{
			if (!mission.IsPositionInsideBoundaries(p))
			{
				return false;
			}
			height = scene.GetTerrainHeight(p, true);
			if (!IsFinite(height) || scene.GetWaterLevelAtPosition(p, true, true) > height - WetMargin)
			{
				return false;
			}
			if (!fine)
			{
				return true;
			}
			PathFaceRecord record = PathFaceRecord.NullFaceRecord;
				Vec3 terrainPoint = new Vec3(p.x, p.y, height);
				scene.GetNavMeshFaceIndex(ref record, terrainPoint, true);
				var navigation = new WorldPosition(scene, UIntPtr.Zero, terrainPoint, false);
				float navHeight = navigation.GetNavMeshZ();
				if (!record.IsValid() || !IsFinite(navHeight) || Math.Abs(navHeight - height) > 0.3f)
			{
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
						return false;
					}
				}
				return true;
		}
		catch
		{
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
						|| max.z - min.z > ClutterMaxExtent)
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
			cache.Ready = true;
			// Cache pedestrian clearance once, after the final camp props. No native queries in route search.
			var walkable = new bool[cache.Open.Length];
			for (int y = 0; y < cache.Height; y++)
				for (int x = 0; x < cache.Width; x++)
				{
					int index = y * cache.Width + x;
					if (!cache.Open[index]) continue;
					Vec3 sample = frame.TransformToParent(new Vec3(cache.MinX + x * GroundStep, cache.MinY + y * GroundStep, 0f));
					walkable[index] = TryResolveGroundedPosition(scene, sample, 0.4f, out _);
				}
			cache.Router = new FieldCampGroundRouter(cache.Width, cache.Height, walkable, cache.Heights);
			Logger.Log(LogSource, "ground cache cells=" + cache.Open.Length + " open=" + open + " step=" + GroundStep);
			Vec3 player = frame.TransformToParent(new Vec3(layout.PlayerStart.x * scale, layout.PlayerStart.y * scale, 0f));
			Vec3 gate = frame.TransformToParent(new Vec3(0f, layout.Front * scale, 0f));
			if (!TryResolveGroundedPosition(scene, player, 0.45f, out _)
				|| !TryValidateGroundedPassage(scene, player, gate, 0.45f))
			{
				// A blocked straight gate corridor does not invalidate every other safe cell.
				Logger.Log(LogSource, "[WARN] preferred player/gate corridor blocked; keeping valid ground cells for alternate player placement");
			}
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

        /// <summary>Routes around runtime camp bodies on the already sampled grid. Covered-but-blocked
        /// must not fall back to the baked navmesh, which does not know those tents/fences.</summary>
        internal static bool TryBuildGroundedRoute(Scene scene, Vec3 from, Vec3 target, out Vec3[] route, out bool covered)
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
                        if (radius > 0 && scene.RayCastForClosestEntityOrTerrain(from + new Vec3(0f, 0f, 1f), start + new Vec3(0f, 0f, 1f),
                            out _, out _, out _, 0.3f, ClearanceExcludeFlags)) continue;
                        if (cache.Router.TryRoute(y * cache.Width + x, goal, out int[] found)) { corners = found; break; }
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
