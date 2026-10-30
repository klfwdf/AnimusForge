using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Picks the camp site once per camp mission (never per frame): dry, walkable, fairly
/// flat ground clear of trees and rocks, inside the mission boundary. Candidates come
/// from the scene's own spawn paths (where vanilla field battles deploy, always on land)
/// ordered by distance to the map-patch encounter point, with a coarse grid only when a
/// scene has too few paths. A coastal patch scene therefore never puts the camp in the sea.
/// Every candidate is probed over the camp's real rectangle (palisade extents).
/// Cost, once: at most MaxCandidates x 25 cheap probes per scale step, then
/// Finalists x 81 probes, each with a navmesh lookup and one downward raycast.
/// Afterwards small rocks and bushes left inside the palisade are hidden (one entity scan).
/// </summary>
internal static class FieldCampSite
{
	private const string LogSource = "FieldCamp";
	private const int MaxSpawnPaths = 32;
	private const float PathStep = 12f;
	private const int MaxCandidates = 600;
	private const int Finalists = 12;
	private const float WetMargin = 0.3f;
	/// <summary>A raycast hit this far above the terrain means a tree, rock or building stands there.</summary>
	private const float ObstacleHeight = 0.9f;
	private const float ClutterMaxExtent = 9f;
	private static readonly string[] ClutterNames = { "rock", "stone", "boulder", "bush", "shrub", "tree", "plant", "branch", "stump" };
	/// <summary>Only distances shrink, not the props, so below 0.8 tents and benches would overlap.</summary>
	private static readonly float[] Scales = { 1f, 0.9f, 0.8f };

	private struct Candidate
	{
		internal Vec2 Position;
		internal Vec2 Forward;
		internal float Score;
		internal int Bad;
	}

	/// <param name="fitScale">Camp-local distances are multiplied by this (0.8..1) when no site fits the full layout.</param>
	internal static MatrixFrame Resolve(Mission mission, FieldCampLayout layout, out float fitScale)
	{
		Scene scene = mission.Scene;
		bool hasEncounter = TryEncounterPoint(mission, out Vec2 encounter);
		List<Candidate> candidates = CollectCandidates(scene, hasEncounter, encounter);
		fitScale = 1f;
		if (candidates.Count == 0)
		{
			Logger.Log(LogSource, "[WARN] camp site: no candidates; using scene centre");
			return FrameAt(scene, SceneCentre(scene), new Vec2(0f, 1f));
		}
		Candidate fallback = default(Candidate);
		bool hasFallback = false;
		float fallbackScale = Scales[Scales.Length - 1];
		for (int s = 0; s < Scales.Length; s++)
		{
			float k = Scales[s];
			for (int i = 0; i < candidates.Count; i++)
			{
				Candidate c = candidates[i];
				c.Score = Evaluate(mission, scene, c, layout, k, grid: 5, fine: false, out c.Bad) + Proximity(c.Position, hasEncounter, encounter);
				candidates[i] = c;
			}
			candidates.Sort((a, b) => a.Score.CompareTo(b.Score));
			Candidate best = default(Candidate);
			bool found = false;
			for (int i = 0; i < candidates.Count && i < Finalists; i++)
			{
				Candidate c = candidates[i];
				c.Score = Evaluate(mission, scene, c, layout, k, grid: 9, fine: true, out c.Bad) + Proximity(c.Position, hasEncounter, encounter);
				if (c.Bad == 0 && (!found || c.Score < best.Score))
				{
					best = c;
					found = true;
				}
				if (!hasFallback || c.Bad < fallback.Bad || (c.Bad == fallback.Bad && c.Score < fallback.Score))
				{
					fallback = c;
					fallbackScale = Scales[s];
					hasFallback = true;
				}
			}
			if (found)
			{
				fitScale = Scales[s];
				Logger.Log(LogSource, "camp site pos=" + Format(best.Position) + " scale=" + fitScale.ToString("0.00") + " candidates=" + candidates.Count + " encounter=" + (hasEncounter ? Format(encounter) : "none"));
				return FrameAt(scene, best.Position, best.Forward);
			}
		}
		fitScale = fallbackScale;
		Logger.Log(LogSource, "[WARN] camp site: no fully dry site; best pos=" + Format(fallback.Position) + " badProbes=" + fallback.Bad + " scale=" + fitScale.ToString("0.00"));
		return FrameAt(scene, fallback.Position, fallback.Forward);
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
			for (float d = 0f; d <= length; d += PathStep)
			{
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

	/// <summary>
	/// Probes a grid x grid lattice over the palisade rectangle. Wet, out-of-bounds and
	/// off-navmesh points count as bad (they veto the site); trees and rocks cost less,
	/// since small ones are cleared afterwards; then height spread.
	/// </summary>
	private static float Evaluate(Mission mission, Scene scene, Candidate c, FieldCampLayout layout, float scale, int grid, bool fine, out int bad)
	{
		bad = 0;
		int blocked = 0;
		float low = float.MaxValue;
		float high = float.MinValue;
		Vec2 f = c.Forward;
		Vec2 side = new Vec2(f.y, -f.x);
		for (int gx = 0; gx < grid; gx++)
		{
			float lx = (-layout.HalfX + 2f * layout.HalfX * gx / (grid - 1)) * scale;
			for (int gy = 0; gy < grid; gy++)
			{
				float ly = (layout.Back + (layout.Front - layout.Back) * gy / (grid - 1)) * scale;
				Vec2 p = c.Position + side * lx + f * ly;
				if (!Probe(mission, scene, p, fine, out float height, out bool obstacle))
				{
					bad++;
					continue;
				}
				if (obstacle)
				{
					blocked++;
				}
				low = Math.Min(low, height);
				high = Math.Max(high, height);
			}
		}
		float spread = high >= low ? high - low : 0f;
		return bad * 100f + blocked * 12f + spread * 2f;
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
			if (scene.GetWaterLevelAtPosition(p, true, true) > height - WetMargin)
			{
				return false;
			}
			if (!fine)
			{
				return true;
			}
			PathFaceRecord record = PathFaceRecord.NullFaceRecord;
			scene.GetNavMeshFaceIndex(ref record, new Vec3(p.x, p.y, height), false);
			if (!record.IsValid())
			{
				return false;
			}
			Vec3 top = new Vec3(p.x, p.y, height + 40f);
			Vec3 bottom = new Vec3(p.x, p.y, height - 1f);
			if (scene.RayCastForClosestEntityOrTerrain(top, bottom, out float _, out Vec3 hit, 0.3f) && hit.z > height + ObstacleHeight)
			{
				obstacle = true;
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
		var entities = new List<GameEntity>();
		try
		{
			scene.GetEntities(ref entities);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "clutter scan failed: " + ex.Message);
			return 0;
		}
		float halfX = layout.HalfX * scale + 1f;
		float front = layout.Front * scale + 1f;
		float back = layout.Back * scale - 1f;
		int hidden = 0;
		for (int i = 0; i < entities.Count; i++)
		{
			GameEntity entity = entities[i];
			try
			{
				if (entity == null || entity.Parent != null || !IsClutterName(entity.Name))
				{
					continue;
				}
				Vec3 local = campFrame.TransformToLocal(entity.GlobalPosition);
				if (local.x < -halfX || local.x > halfX || local.y < back || local.y > front)
				{
					continue;
				}
				Vec3 min = entity.GlobalBoxMin;
				Vec3 max = entity.GlobalBoxMax;
				if (max.x - min.x > ClutterMaxExtent || max.y - min.y > ClutterMaxExtent)
				{
					continue;
				}
				entity.SetVisibilityExcludeParents(false);
				entity.SetBodyFlags(entity.BodyFlag | BodyFlags.Disabled);
				hidden++;
			}
			catch
			{
			}
		}
		return hidden;
	}

	private static bool IsClutterName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
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

	/// <summary>Mild pull toward the encounter point: 100 m costs as much as 2 m of height spread.</summary>
	private static float Proximity(Vec2 p, bool hasEncounter, Vec2 encounter)
	{
		return hasEncounter ? p.Distance(encounter) / 25f : 0f;
	}

	private static Vec2 SceneCentre(Scene scene)
	{
		scene.GetBoundingBox(out Vec3 min, out Vec3 max);
		return new Vec2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
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
		frame.origin.z = FieldCampMissionLogic.GroundHeight(scene, frame.origin);
		return frame;
	}

	private static string Format(Vec2 p)
	{
		return p.x.ToString("0") + "," + p.y.ToString("0");
	}
}
