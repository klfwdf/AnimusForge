using System;
using System.Collections.Generic;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
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
	private static readonly string[] SentryActions = { "act_guard_cautious_look_around_1", "act_guard_normal_look_around_1", "act_stand_1" };
	private static readonly string[] BenchActions = { "act_sit_1", "act_sit_2", "act_sit_3", "act_sit_4", "act_sit_and_drink_idle" };
	private static readonly string[] EatActions = { "act_sit_eating_1", "act_sit_eating_2", "act_sit_and_drink_idle" };
	private static readonly string[] CookActions = { "act_bartender_meat", "act_citizen_cleanup_idle" };
	private static readonly string[] SmithActions = { "act_smithing_machine_anvil_part_2" };
	private static readonly string[] RepairActions = { "act_repair", "act_repair_upside" };
	private static readonly string[] ArcherActions = { "act_idle_bow_1", "act_idle_bow_2" };
	private static readonly string[] LeanActions = { "act_lean_fence_loop", "act_lean_fence_loop2" };
	private static readonly string[] GossipActions = { "act_gossip", "act_gossip_2" };
	private static readonly string[] LieActions = { "act_dungeon_prisoner_sleep_cycle" };
	private static readonly string[] GroomActions = { "act_stand_1", "act_stand_2" };
	private static readonly string[] StandActions = { "act_stand_1", "act_stand_2" };
	private static readonly string[] CheerActions = { "act_cheer_1", "act_cheer_2" };

	/// <summary>
	/// Camp soldiers use the hideout action set: it is based on the warrior set (so fighting
	/// still works) and adds the sitting, eating, cooking, repairing, leaning, talking and
	/// sleeping clips the camp needs. Applied once per agent at spawn.
	/// </summary>
	internal static void ApplyCampActionSet(Agent agent)
	{
		try
		{
			Monster monster = agent?.Monster;
			if (monster == null)
			{
				return;
			}
			string code = agent.IsFemale ? "as_human_female_hideout_bandit" : "as_human_hideout_bandit";
			AnimationSystemData data = monster.FillAnimationSystemData(MBActionSet.GetActionSet(code), agent.Character?.GetStepSize() ?? 1f, false);
			agent.SetActionSet(ref data);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "camp action set failed agent=" + (agent?.Index ?? -1) + " error=" + ex.Message);
		}
	}
	private readonly Dictionary<string, ActionIndexCache> _actionCache = new Dictionary<string, ActionIndexCache>(StringComparer.Ordinal);

	// ---------- orders (called from the scene-shout postprocess tag) ----------

	/// <summary>Idempotent: several speakers may emit the same order in one shout.</summary>
	internal bool Assemble()
	{
		if (!_spawned || _assembled || SlaughterBusy || _duelists.Count > 0 || _ceremonyActive || _ceremonyWalkers.Count > 0 || _sceneConversationAgents.Count > 0 || _envoy != null)
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
			entry.Returning = false;
			ReleasePose(entry);
			MoveTo(entry, entry.Target, entry.TargetFacing, run: true);
		}
		Logger.Log(LogSource, "assemble agents=" + _agents.Count);
		AnimusForgeQuickInfo.Show(FieldCampText.Assembled());
		return true;
	}

	internal bool Dismiss()
	{
		if (!_spawned || !_assembled || SlaughterBusy || _duelists.Count > 0 || _ceremonyActive || _ceremonyWalkers.Count > 0 || _sceneConversationAgents.Count > 0)
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
			entry.Returning = false;
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
		// Ranks grow back (-y) from the parade line, normally at most MaxRanks deep. When the
		// blocks would be wider than the parade ground (between the training ground and the
		// horse lines), the gaps shrink first and then every block takes more ranks.
		float limit = Math.Max(10f, _layout.ParadeHalfWidth * 2f);
		float gap = BlockGap;
		float fileScale = 1f;
		float totalWidth = BlockWidth(byClass, gap, fileScale);
		if (totalWidth > limit)
		{
			gap = 1.5f;
			totalWidth = BlockWidth(byClass, gap, fileScale);
		}
		for (int pass = 0; pass < 6 && totalWidth > limit; pass++)
		{
			fileScale *= Math.Max(0.5f, limit / totalWidth);
			totalWidth = BlockWidth(byClass, gap, fileScale);
		}
		float x = -totalWidth * 0.5f;
		Vec2 facing = ToWorldDirection(new Vec2(0f, 1f));
		foreach (List<CampAgent> list in byClass.Values)
		{
			int files = FilesFor(list.Count, fileScale);
			for (int i = 0; i < list.Count; i++)
			{
				int file = i % files;
				int rank = i / files;
				Vec2 local = _layout.ParadeCenter + new Vec2(x + file * FileSpacing, -rank * RowSpacing);
				list[i].Target = ToWorld(local);
				list[i].TargetFacing = facing;
			}
			x += files * FileSpacing + gap;
		}
	}

	private static int FilesFor(int count, float fileScale)
	{
		int files = Math.Max(1, Math.Min(count, Math.Max(MinFilesPerRank, (count + MaxRanks - 1) / MaxRanks)));
		return Math.Max(1, (int)(files * fileScale));
	}

	private static float BlockWidth(SortedDictionary<int, List<CampAgent>> byClass, float gap, float fileScale)
	{
		float width = 0f;
		foreach (List<CampAgent> list in byClass.Values)
		{
			width += FilesFor(list.Count, fileScale) * FileSpacing + gap;
		}
		return width - gap;
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

	// Scene interaction owns these agents until its original owner releases them.
	internal bool IsSceneConversationHeld(Agent agent) => agent != null && _sceneConversationAgents.Contains(agent);

	internal bool IsCampAgentBusyForConversation(Agent agent) => agent == null || IsSceneConversationHeld(agent) ||
		_envoy?.Agent == agent || IsDuelist(agent) || SlaughterBusy || _ceremonyActive || IsCeremonyAgentHeld(agent);

	internal bool IsCampAgentSeated(Agent agent)
	{
		if (agent == null || !_campAgentsByAgent.TryGetValue(agent, out CampAgent entry) || !entry.Seated) return false;
		return entry.Work == FieldCampLayout.WorkKind.Sit || entry.Work == FieldCampLayout.WorkKind.Eat ||
			entry.Work == FieldCampLayout.WorkKind.Lie || entry.Activity == Activity.Fire || entry.Activity == Activity.Wounded;
	}

	internal bool IsCampPrisoner(Agent agent) => agent != null && _prisoners.Contains(agent);

	internal bool CanStartNativeConversation(Agent agent) =>
		agent != null && (_campAgentsByAgent.ContainsKey(agent) || _prisoners.Contains(agent)) &&
		!_ceremonyActive && !SlaughterBusy && !IsDuelist(agent) && !IsCeremonyAgentHeld(agent);

	internal bool TryAcquireSceneConversation(Agent agent)
	{
		if (agent == null || !agent.IsActive() || !ReferenceEquals(Mission.Current, Mission) || Mission.IsMissionEnding ||
			SlaughterBusy || IsDuelist(agent) || _ceremonyActive || IsCeremonyAgentHeld(agent)) return false;
		if (!_campAgentsByAgent.TryGetValue(agent, out CampAgent campAgent) && !_prisoners.Contains(agent)) return false;
		if (_sceneConversationAgents.Contains(agent)) return true;
		try
		{
			if (!TryPrepareCampNavigation(agent)) return false;
			if (!agent.IsUsingGameObject && agent.CurrentlyUsedGameObject == null)
			{
				if (campAgent != null)
				{
					ReleasePose(campAgent);
					campAgent.Seated = false;
				}
				StopCampNavigation(agent);
				if (_prisoners.Contains(agent)) agent.SetIsAIPaused(false);
			}
			_sceneConversationAgents.Add(agent);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "conversation acquisition failed agent=" + agent.Index + " error=" + ex.Message);
			return false;
		}
	}

	internal void ReleaseSceneConversation(Agent agent)
	{
		if (agent == _nativeConversationAgent && Campaign.Current?.ConversationManager?.IsConversationInProgress == true) return;
		if (agent == null || !_sceneConversationAgents.Remove(agent) || !agent.IsActive() ||
			!ReferenceEquals(Mission.Current, Mission) || Mission.IsMissionEnding || SlaughterBusy || IsDuelist(agent) || _ceremonyActive) return;
			if (agent.IsUsingGameObject || agent.CurrentlyUsedGameObject != null) return;
			StopCampNavigation(agent);
			if (_envoy?.Agent == agent)
		{
			_envoyNavigationActive = false;
			return;
		}
		if (_campAgentsByAgent.TryGetValue(agent, out CampAgent entry))
		{
			entry.Target = entry.Home;
			entry.TargetFacing = entry.HomeFacing;
			entry.Returning = false;
			entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(25f, 60f);
			MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
		}
		else if (_prisoners.Contains(agent)) ApplyPrisonerPose(agent);
	}

	private void OnCampConversationBegin()
	{
		var agent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
		if (TryAcquireSceneConversation(agent)) _nativeConversationAgent = agent;
	}

	private void OnCampConversationEnd()
	{
		Agent agent = _nativeConversationAgent;
		_nativeConversationAgent = null;
		ReleaseSceneConversation(agent);
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
			if (agent == null || !agent.IsActive() || IsDuelist(agent) || IsEnvoy(entry) || IsSceneConversationHeld(agent)
					|| (_ceremonyMoved.Count > 0 && _ceremonyMoved.Contains(entry)))
			{
				continue;
			}
			if (entry.MoveBlocked)
			{
				// A transient obstruction must not retire this actor for the whole mission.
				// Retry its failed destination once per existing cooldown, with a new deadline.
				if (_elapsed < entry.NextWanderTime) continue;
				entry.Returning = false;
				MoveTo(entry, entry.Target, entry.TargetFacing, run: false);
				continue;
			}
			if (!entry.Seated && _elapsed >= entry.MoveDeadline)
			{
				// One bounded return attempt, never teleport a blocked walker home.
				if (entry.Target.AsVec2.DistanceSquared(entry.Home.AsVec2) > ArriveDistanceSquared)
				{
					entry.Returning = false;
					MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
				}
				else
				{
					agent.DisableScriptedMovement();
					agent.ClearTargetFrame();
					entry.Seated = true;
					entry.MoveBlocked = true;
					entry.NextWanderTime = _elapsed + 30f;
				}
				continue;
			}
				if (!_assembled && entry.Seated && entry.Activity == Activity.Wander && _elapsed >= entry.NextWanderTime)
				{
					entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(25f, 45f);
					if (TryNearbyStroll(entry, out Vec3 stroll)) MoveTo(entry, stroll, entry.HomeFacing, run: false);
					continue;
				}
			if (!_assembled && entry.Seated && _elapsed >= entry.NextWanderTime && entry.Activity != Activity.Wounded && entry.Activity != Activity.Post)
			{
				Refresh(entry);
				continue;
			}
			if (entry.Seated || agent.Position.AsVec2.DistanceSquared(entry.Target.AsVec2) > ArriveDistanceSquared)
			{
				continue;
			}
			if (entry.Returning)
			{
				// End of a stroll: walk back to the post.
				entry.Returning = false;
				entry.Target = entry.Home;
				entry.TargetFacing = entry.HomeFacing;
				MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
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

	/// <summary>
	/// Camp life (every 25-60 s per agent, in the 0.5 s upkeep batch): a seated or working
	/// man may switch pose, take a short walk to a patrol point and come back, or turn to
	/// talk with the nearest comrade. Sentries only change their look-around pose.
	/// </summary>
	private void Refresh(CampAgent entry)
	{
		entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(25f, 60f);
		float roll = MBRandom.RandomFloat;
		if (entry.Activity == Activity.Sentry || roll < 0.45f)
		{
			entry.PoseVariant++;
			ApplyPose(entry);
			return;
		}
			if (roll < 0.75f && TryNearbyStroll(entry, out Vec3 stroll))
			{
				entry.Returning = entry.Activity != Activity.Wander;
				MoveTo(entry, stroll, entry.HomeFacing, run: false);
				return;
			}
		CampAgent mate = NearestIdleMate(entry, 9f);
		if (mate == null)
		{
			entry.PoseVariant++;
			ApplyPose(entry);
			return;
		}
		// Turn to each other and talk for a while.
		Vec2 between = (mate.Agent.Position.AsVec2 - entry.Agent.Position.AsVec2);
		if (between.LengthSquared > 0.01f)
		{
			between = between.Normalized();
			entry.Agent.LookDirection = new Vec3(between.x, between.y, 0f);
			mate.Agent.LookDirection = new Vec3(-between.x, -between.y, 0f);
		}
		PlayOnce(entry.Agent, entry.Work == FieldCampLayout.WorkKind.Sit || entry.Work == FieldCampLayout.WorkKind.Eat ? BenchActions : GossipActions, entry.PoseVariant);
		PlayOnce(mate.Agent, mate.Work == FieldCampLayout.WorkKind.Sit || mate.Work == FieldCampLayout.WorkKind.Eat ? BenchActions : GossipActions, mate.PoseVariant + 1);
		mate.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(25f, 60f);
	}

		private static bool TryPrepareCampNavigation(Agent agent)
		{
			var component = agent.GetComponent<CampaignAgentComponent>();
			if (component == null) return false;
			var navigator = component.AgentNavigator ?? component.CreateAgentNavigator();
			if (navigator.GetBehaviorGroup<DailyBehaviorGroup>() == null) navigator.AddBehaviorGroup<DailyBehaviorGroup>();
			return true;
		}

		private static void StopCampNavigation(Agent agent)
		{
			var navigator = agent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			navigator?.GetBehaviorGroup<DailyBehaviorGroup>()?.DisableScriptedBehavior();
			navigator?.ClearTarget();
			agent?.DisableScriptedMovement();
			agent?.ClearTargetFrame();
		}

		private bool TryNearbyStroll(CampAgent entry, out Vec3 target)
		{
			target = entry.Home;
			int count = _layout.WanderSlots.Count;
			if (count == 0) return false;
			int start = MBRandom.RandomInt(count);
			for (int i = 0; i < Math.Min(count, 12); i++)
			{
				Vec3 candidate = ToWorld(_layout.WanderSlots[(start + i) % count].Position);
				if (candidate.AsVec2.DistanceSquared(entry.Home.AsVec2) > 64f ||
					candidate.AsVec2.DistanceSquared(entry.Agent.Position.AsVec2) < 4f ||
					!FieldCampSite.TryResolveGroundedPosition(Mission.Scene, candidate, 0.4f, out target) ||
					!FieldCampSite.TryValidateGroundedPassage(Mission.Scene, entry.Agent.Position, target, 0.4f)) continue;
				return true;
			}
			return false;
		}

		private CampAgent NearestIdleMate(CampAgent entry, float radius)
	{
		CampAgent best = null;
		float bestDistance = radius * radius;
		Vec2 here = entry.Agent.Position.AsVec2;
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent other = _agents[i];
			if (other == entry || !other.Seated || other.Agent == null || !other.Agent.IsActive()
				|| IsSceneConversationHeld(other.Agent) || IsEnvoy(other) || IsDuelist(other.Agent)
					|| (_ceremonyMoved.Count > 0 && _ceremonyMoved.Contains(other))
					|| other.Activity == Activity.Sentry || other.Activity == Activity.Wounded || other.Activity == Activity.Post)
			{
				continue;
			}
			float d = other.Agent.Position.AsVec2.DistanceSquared(here);
			if (d < bestDistance)
			{
				bestDistance = d;
				best = other;
			}
		}
		return best;
	}

	private void PlayOnce(Agent agent, string[] pool, int variant)
	{
		ActionIndexCache action = ResolveAction(pool[Math.Abs(variant) % pool.Length]);
		if (action == ActionIndexCache.act_none)
		{
			return;
		}
		try
		{
			agent.SetActionChannel(0, in action, true);
		}
		catch
		{
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
				ReleasePose(entry);
				entry.Seated = false;
				entry.Target = target;
				entry.TargetFacing = facing;
					entry.MoveDeadline = _elapsed + 25f;
					entry.MoveBlocked = false;
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
		string[] pool = PoseFor(entry);
		if (pool == null)
		{
			return;
		}
		ActionIndexCache action = ResolveAction(pool[(entry.Agent.Index + entry.PoseVariant) % pool.Length]);
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

	private static string[] PoseFor(CampAgent entry)
	{
		switch (entry.Work)
		{
			case FieldCampLayout.WorkKind.Sit:
				return entry.Activity == Activity.Wounded ? SitActions : BenchActions;
			case FieldCampLayout.WorkKind.Eat:
				return EatActions;
			case FieldCampLayout.WorkKind.Cook:
				return CookActions;
			case FieldCampLayout.WorkKind.Smith:
				return SmithActions;
			case FieldCampLayout.WorkKind.Repair:
				return RepairActions;
			case FieldCampLayout.WorkKind.Archer:
				return WieldBow(entry.Agent) ? ArcherActions : StandActions;
			case FieldCampLayout.WorkKind.Groom:
				return GroomActions;
			case FieldCampLayout.WorkKind.Lean:
				return LeanActions;
			case FieldCampLayout.WorkKind.Gossip:
				return GossipActions;
			case FieldCampLayout.WorkKind.Lie:
				return LieActions;
		}
		switch (entry.Activity)
		{
			case Activity.Fire:
			case Activity.Wounded:
				return SitActions;
			case Activity.Sentry:
				return SentryActions;
			default:
				return null;
		}
	}

	/// <summary>An archer at the butts takes his bow in hand; men without one just stand and watch.</summary>
	private static bool WieldBow(Agent agent)
	{
		try
		{
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
			{
				MissionWeapon weapon = agent.Equipment[slot];
				if (weapon.IsEmpty || weapon.CurrentUsageItem == null)
				{
					continue;
				}
				WeaponClass weaponClass = weapon.CurrentUsageItem.WeaponClass;
				if (weaponClass == WeaponClass.Bow || weaponClass == WeaponClass.Crossbow)
				{
					agent.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.Instant, false);
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
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
				if (FieldCampSite.TryGetCachedTerrainHeight(scene, position.AsVec2, out float height)) return height;
				return scene.GetTerrainHeight(position.AsVec2);
		}
		catch
		{
			return position.z;
		}
	}
}
