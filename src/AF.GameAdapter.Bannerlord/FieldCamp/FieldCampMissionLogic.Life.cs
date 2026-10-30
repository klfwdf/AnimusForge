using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>Camp life, assemble/dismiss orders and camp-local coordinates.</summary>
public sealed partial class FieldCampMissionLogic
{
	private const float ArriveDistanceSquared = 1.2f;
	private const float RowSpacing = 1.6f;
	private const float FileSpacing = 1.3f;
	private const int MinFilesPerRank = 10;
	private const int MaxRanks = 6;
	private const float BlockGap = 4f;

	private static readonly string[] SitActions = { "act_sit_idle_on_floor_1", "act_sit_idle_on_floor_2", "act_sit_idle_on_floor_3" };
	private static readonly string[] SentryActions = { "act_guard_cautious_look_around_1", "act_stand_1" };
	private readonly Dictionary<string, ActionIndexCache> _actionCache = new Dictionary<string, ActionIndexCache>(StringComparer.Ordinal);

	// ---------- orders (called from the scene-shout postprocess tag) ----------

	/// <summary>Idempotent: several speakers may emit the same order in one shout.</summary>
	internal bool Assemble()
	{
		if (!_spawned || _assembled || SlaughterBusy)
		{
			return false;
		}
		_assembled = true;
		ComputeFormationTargets();
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
			if (entry.Activity == Activity.Wounded || entry.Activity == Activity.Post)
			{
				continue;
			}
			ReleasePose(entry);
			MoveTo(entry, entry.Target, entry.TargetFacing, run: true);
		}
		Logger.Log(LogSource, "assemble agents=" + _agents.Count);
		AnimusForgeQuickInfo.Show(FieldCampText.Assembled());
		return true;
	}

	internal bool Dismiss()
	{
		if (!_spawned || !_assembled || SlaughterBusy)
		{
			return false;
		}
		_assembled = false;
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
			entry.Target = entry.Home;
			entry.TargetFacing = entry.HomeFacing;
			entry.Seated = false;
			MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
		}
		Logger.Log(LogSource, "dismiss agents=" + _agents.Count);
		AnimusForgeQuickInfo.Show(FieldCampText.Dismissed());
		return true;
	}

	/// <summary>
	/// Blocks of up to 10 files per rank, one block per troop class, left to right
	/// in infantry / ranged / cavalry / horse-archer order, facing the player start.
	/// </summary>
	private void ComputeFormationTargets()
	{
		var byClass = new SortedDictionary<int, List<CampAgent>>();
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
			if (entry.Activity == Activity.Wounded || entry.Activity == Activity.Post)
			{
				continue;
			}
			int key = ClassOrder(entry.TroopClass);
			if (!byClass.TryGetValue(key, out List<CampAgent> list))
			{
				byClass[key] = list = new List<CampAgent>();
			}
			list.Add(entry);
		}
		// Ranks grow back (-y) from the parade line, at most MaxRanks deep (to y ~ 6),
		// so a large camp widens its blocks instead of reaching the tents and posts behind.
		float totalWidth = 0f;
		foreach (List<CampAgent> list in byClass.Values)
		{
			totalWidth += FilesFor(list.Count) * FileSpacing + BlockGap;
		}
		float x = -totalWidth * 0.5f;
		Vec2 facing = ToWorldDirection(new Vec2(0f, 1f));
		foreach (List<CampAgent> list in byClass.Values)
		{
			int files = FilesFor(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				int file = i % files;
				int rank = i / files;
				Vec2 local = _layout.ParadeCenter + new Vec2(x + file * FileSpacing, -rank * RowSpacing);
				list[i].Target = ToWorld(local);
				list[i].TargetFacing = facing;
			}
			x += files * FileSpacing + BlockGap;
		}
	}

	private static int FilesFor(int count)
	{
		return Math.Max(1, Math.Min(count, Math.Max(MinFilesPerRank, (count + MaxRanks - 1) / MaxRanks)));
	}

	private static int ClassOrder(FormationClass troopClass)
	{
		switch (troopClass)
		{
			case FormationClass.Ranged:
				return 1;
			case FormationClass.Cavalry:
				return 2;
			case FormationClass.HorseArcher:
				return 3;
			default:
				return 0;
		}
	}

	// ---------- camp life ----------

	private void UpkeepBatchOfAgents()
	{
		int count = _agents.Count;
		if (count == 0)
		{
			return;
		}
		int batch = Math.Min(UpkeepBatch, count);
		for (int n = 0; n < batch; n++)
		{
			_upkeepCursor = (_upkeepCursor + 1) % count;
			CampAgent entry = _agents[_upkeepCursor];
			Agent agent = entry.Agent;
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			if (!_assembled && entry.Activity == Activity.Wander && _elapsed >= entry.NextWanderTime && _layout.WanderSlots.Count > 0)
			{
				entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(8f, 20f);
				Vec2 local = _layout.WanderSlots[MBRandom.RandomInt(_layout.WanderSlots.Count)].Position
					+ new Vec2(MBRandom.RandomFloatRanged(-1.5f, 1.5f), MBRandom.RandomFloatRanged(-1.5f, 1.5f));
				entry.Target = ToWorld(local);
				MoveTo(entry, entry.Target, entry.TargetFacing, run: false);
				continue;
			}
			if (entry.Seated || agent.Position.AsVec2.DistanceSquared(entry.Target.AsVec2) > ArriveDistanceSquared)
			{
				continue;
			}
			// Arrived: settle once. The scripted hold stays set; it is not re-issued (that jitters).
			agent.LookDirection = new Vec3(entry.TargetFacing.x, entry.TargetFacing.y, 0f);
			if (!_assembled)
			{
				ApplyPose(entry);
			}
			entry.Seated = true;
		}
	}

	private void HoldAt(CampAgent entry)
	{
		entry.Seated = false;
		MoveTo(entry, entry.Target, entry.TargetFacing, run: false);
	}

	private void MoveTo(CampAgent entry, Vec3 target, Vec2 facing, bool run)
	{
		Agent agent = entry.Agent;
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			entry.Seated = false;
			var world = new WorldPosition(Mission.Scene, UIntPtr.Zero, target, false);
			Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.NoAttack;
			if (!run)
			{
				flags |= Agent.AIScriptedFrameFlags.DoNotRun;
			}
			agent.SetScriptedPositionAndDirection(ref world, facing.RotationInRadians, false, flags);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "move failed agent=" + agent.Index + " error=" + ex.Message);
		}
	}

	private void ApplyPose(CampAgent entry)
	{
		string[] pool = entry.Activity == Activity.Fire || entry.Activity == Activity.Wounded
			? SitActions
			: entry.Activity == Activity.Sentry ? SentryActions : null;
		if (pool == null)
		{
			return;
		}
		ActionIndexCache action = ResolveAction(pool[entry.Agent.Index % pool.Length]);
		if (action != ActionIndexCache.act_none)
		{
			try
			{
				entry.Agent.SetActionChannel(0, in action, true);
			}
			catch
			{
			}
		}
	}

	private static void ReleasePose(CampAgent entry)
	{
		try
		{
			entry.Agent?.SetActionChannel(0, in ActionIndexCache.act_none, true);
		}
		catch
		{
		}
	}

	private ActionIndexCache ResolveAction(string name)
	{
		if (!_actionCache.TryGetValue(name, out ActionIndexCache action))
		{
			try
			{
				action = ActionIndexCache.Create(name);
			}
			catch
			{
				action = ActionIndexCache.act_none;
			}
			_actionCache[name] = action;
		}
		return action;
	}

	/// <summary>Scene line for the shout prompt while the camp mission runs (built per request, no I/O after first load).</summary>
	internal static string BuildSceneDescription(string location)
	{
		FieldCampMissionLogic camp = Current;
		return ConversationRuntimeTexts.FormatPrompt(camp != null && camp._assembled ? "AF_FC_SceneAssembled" : "AF_FC_Scene", ("LOCATION", location ?? ""));
	}

	// ---------- camp-local coordinates ----------

	private Vec3 ToWorld(Vec2 local)
	{
		Vec3 world = _campFrame.TransformToParent(new Vec3(local.x * _campScale, local.y * _campScale, 0f));
		world.z = GroundHeight(Mission.Scene, world);
		return world;
	}

	private Vec2 ToWorldDirection(Vec2 local)
	{
		Vec3 dir = _campFrame.rotation.TransformToParent(new Vec3(local.x, local.y, 0f));
		Vec2 flat = dir.AsVec2;
		return flat.LengthSquared < 0.0001f ? new Vec2(0f, 1f) : flat.Normalized();
	}

	internal static float GroundHeight(Scene scene, Vec3 position)
	{
		try
		{
			float height = position.z;
			if (scene.GetHeightAtPoint(position.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
			{
				return height;
			}
			return scene.GetGroundHeightAtPosition(position);
		}
		catch
		{
			return position.z;
		}
	}

	/// <summary>
	/// Camp centre = the playable area's centre (hard/soft boundary centroid, else
	/// bounding box), oriented along world +y. <paramref name="fitScale"/> (at most 1)
	/// shrinks camp-local distances when the layout radius would leave the playable area.
	/// </summary>
	private static MatrixFrame ResolveCampFrame(Scene scene, float radius, out float fitScale)
	{
		MatrixFrame frame = MatrixFrame.Identity;
		Vec2 center = Vec2.Zero;
		float inner = float.MaxValue;
		var points = new List<Vec2>();
		try
		{
			int count = scene.GetHardBoundaryVertexCount();
			bool hard = count >= 3;
			if (!hard)
			{
				count = scene.GetSoftBoundaryVertexCount();
			}
			for (int i = 0; count >= 3 && i < count; i++)
			{
				points.Add(hard ? scene.GetHardBoundaryVertex(i) : scene.GetSoftBoundaryVertex(i));
			}
		}
		catch
		{
			points.Clear();
		}
		if (points.Count >= 3)
		{
			for (int i = 0; i < points.Count; i++)
			{
				center += points[i];
			}
			center *= 1f / points.Count;
			// Distance from the centre to the nearest boundary edge.
			for (int i = 0; i < points.Count; i++)
			{
				inner = Math.Min(inner, DistanceToSegment(center, points[i], points[(i + 1) % points.Count]));
			}
		}
		else
		{
			scene.GetBoundingBox(out Vec3 min, out Vec3 max);
			center = new Vec2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
			inner = Math.Min(max.x - min.x, max.y - min.y) * 0.5f;
		}
		float usable = inner - 5f;
		fitScale = radius <= 0f || usable >= radius ? 1f : Math.Max(0.45f, usable / radius);
		frame.origin = new Vec3(center.x, center.y, 0f);
		frame.origin.z = GroundHeight(scene, frame.origin);
		return frame;
	}

	private static float DistanceToSegment(Vec2 p, Vec2 a, Vec2 b)
	{
		Vec2 ab = b - a;
		float lengthSquared = ab.LengthSquared;
		float t = lengthSquared < 0.0001f ? 0f : Math.Max(0f, Math.Min(1f, Vec2.DotProduct(p - a, ab) / lengthSquared));
		return (a + ab * t).Distance(p);
	}
}
