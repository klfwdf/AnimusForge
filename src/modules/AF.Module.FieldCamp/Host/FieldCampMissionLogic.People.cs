using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

/// <summary>
/// Companions at their party-role posts, army lords beside their tents and the
/// prisoner pen (field-camp-v1 step 5). Everything is spawned once with the camp;
/// the prisoner slaughter reuses the troop-inspection runtime and its tag.
/// </summary>
public sealed partial class FieldCampMissionLogic
{
	private const int MaxExtraCompanions = 24;
	private const int MaxPrisonerLords = 3;
	private const int AttackersPerPrisoner = 2;
	private const int MaxAttackers = 16;

	private sealed class CampPrisoner
	{
		internal CharacterObject Character;
		internal PartyBase Captor;
		internal bool SpawnedWounded;
		internal Agent FatalAffector;
		internal int FatalBlowOwnerId = -1;
		internal float FatalHitTime = -1f;
		internal bool FatalCanKill;
	}

	private readonly List<Agent> _prisoners = new List<Agent>();
	// Only these exact Agent instances may change the campaign prison roster.
	private readonly Dictionary<Agent, CampPrisoner> _campPrisoners = new Dictionary<Agent, CampPrisoner>();
	private Team _prisonerTeam;
	private Team _prisonerPlayerTeam;
	private int _standbyUsed;
	private TroopInspectionPrisonerSlaughterRuntime _slaughter;

	private bool SlaughterBusy => _slaughter?.IsBusy == true;

	// ---------- companions and lords ----------

	private void SpawnCompanions()
	{
		MobileParty main = MobileParty.MainParty;
		TroopRoster roster = main?.MemberRoster;
		if (roster == null)
		{
			return;
		}
		var posts = new Dictionary<Hero, string>();
		AddPost(posts, main, PartyRole.Quartermaster, "quartermaster");
		AddPost(posts, main, PartyRole.Surgeon, "surgeon");
		AddPost(posts, main, PartyRole.Scout, "scout");
		AddPost(posts, main, PartyRole.Engineer, "engineer");
		int extra = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			CharacterObject character = roster.GetElementCopyAtIndex(i).Character;
			Hero hero = character?.HeroObject;
			if (hero == null || hero == Hero.MainHero || !hero.IsAlive)
			{
				continue;
			}
			FieldCampLayout.Slot slot;
			Activity activity = Activity.Post;
			if (posts.TryGetValue(hero, out string post) && _layout.CompanionSlots.TryGetValue(post, out slot))
			{
			}
			else if (hero.IsWounded && _woundedSlotsUsed < _layout.WoundedSlots.Count)
			{
				slot = _layout.WoundedSlots[_woundedSlotsUsed++];
				activity = Activity.Wounded;
			}
			else if (extra < MaxExtraCompanions)
			{
				slot = StandbySlot(_standbyUsed++);
				extra++;
			}
			else
			{
				continue;
			}
			Agent agent = SpawnCharacter(character, PartyBase.MainParty, slot.Position, slot.Facing, civilian: true);
			if (agent != null)
			{
				Track(agent, activity, slot.Position, slot.Facing, character.DefaultFormationClass, slot.Work);
			}
		}
	}

	/// <summary>Rows of twelve in front of the banner, each 1.4 m further toward the parade ground.</summary>
	private FieldCampLayout.Slot StandbySlot(int n)
	{
		Vec2 origin = _layout.StandbyOrigin;
		return new FieldCampLayout.Slot { Position = new Vec2(origin.x + (n % 12) * 1.6f, origin.y + (n / 12) * 1.4f), Facing = new Vec2(0f, 1f) };
	}

	private static void AddPost(Dictionary<Hero, string> posts, MobileParty main, PartyRole role, string post)
	{
		try
		{
			Hero holder = main.GetRoleHolder(role);
			if (holder != null && holder != Hero.MainHero && !posts.ContainsKey(holder))
			{
				posts[holder] = post;
			}
		}
		catch
		{
		}
	}

	private void SpawnArmyLords()
	{
		int fallback = 0;
		for (int i = 1; i < _parties.Count; i++)
		{
			MobileParty party = _parties[i];
			Hero lord = party?.LeaderHero;
			if (lord == null || !lord.IsAlive || lord == Hero.MainHero)
			{
				continue;
			}
			if (!_layout.LordSlots.TryGetValue(party, out FieldCampLayout.Slot slot))
			{
				// More parties than tents: they join the standby rows in front of the banner.
				slot = StandbySlot(_standbyUsed++);
				fallback++;
			}
			Agent agent = SpawnCharacter(lord.CharacterObject, party.Party, slot.Position, slot.Facing, civilian: true);
			if (agent != null)
			{
				Track(agent, Activity.Post, slot.Position, slot.Facing, lord.CharacterObject.DefaultFormationClass);
			}
		}
	}

	// ---------- horse lines ----------

	private static readonly string[] FallbackHorses = { "sumpter_horse", "saddle_horse", "old_horse" };

	/// <summary>
	/// Real horse agents tied along the barriers (never wander), taken from the main party's
	/// own horses when it has any; at most one per tether slot. Spawned once with the camp.
	/// </summary>
	private int SpawnHorses()
	{
		if (_layout.HorseSlots.Count == 0)
		{
			return 0;
		}
		var kinds = new List<ItemObject>();
		int owned = 0;
		try
		{
			ItemRoster items = MobileParty.MainParty?.ItemRoster;
			for (int i = 0; items != null && i < items.Count; i++)
			{
				ItemRosterElement e = items.GetElementCopyAtIndex(i);
				ItemObject item = e.EquipmentElement.Item;
				if (item != null && e.Amount > 0 && item.HasHorseComponent && item.HorseComponent.IsMount)
				{
					kinds.Add(item);
					owned += e.Amount;
				}
			}
			owned += MobileParty.MainParty?.MemberRoster?.TotalManCount > 0 ? CountMounted(MobileParty.MainParty.MemberRoster) : 0;
			if (kinds.Count == 0)
			{
				for (int i = 0; i < FallbackHorses.Length && kinds.Count == 0; i++)
				{
					ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(FallbackHorses[i]);
					if (item != null && item.HasHorseComponent)
					{
						kinds.Add(item);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "horse pick failed: " + ex.Message);
		}
		if (kinds.Count == 0)
		{
			return 0;
		}
		int wanted = Math.Min(_layout.HorseSlots.Count, Math.Max(2, owned));
		int spawned = 0;
		for (int i = 0; i < wanted; i++)
		{
			FieldCampLayout.Slot slot = _layout.HorseSlots[i];
			try
			{
				// Props already exist. Never use their top surface as the horse's floor.
				Vec3 desired = _campFrame.TransformToParent(new Vec3(slot.Position.x * _campScale, slot.Position.y * _campScale, 0f));
				if (!FieldCampSite.TryResolveGroundedPosition(Mission.Scene, desired, 1.5f, out Vec3 pos))
				{
					Logger.Log(LogSource, "horse slot skipped: no safe ground slot=" + i);
					continue;
				}
				Vec2 dir = ToWorldDirection(slot.Facing);
				var element = new ItemRosterElement(kinds[i % kinds.Count], 1);
				Agent horse = Mission.SpawnMonster(element, default(ItemRosterElement), in pos, in dir);
				if (horse != null)
				{
					horse.SetAgentFlags(horse.GetAgentFlags() & ~AgentFlag.CanWander);
					spawned++;
				}
			}
			catch (Exception ex)
			{
				Logger.Log(LogSource, "horse spawn failed error=" + ex.Message);
				break;
			}
		}
		return spawned;
	}

	private static int CountMounted(TroopRoster roster)
	{
		int mounted = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement e = roster.GetElementCopyAtIndex(i);
			if (e.Character != null && e.Character.IsMounted)
			{
				mounted += e.Number;
			}
		}
		return mounted;
	}

	// ---------- prisoner pen ----------

	/// <summary>Up to one agent per pen slot: captured lords first (at most 3), then one regular per troop type in turn.</summary>
	private void SpawnPrisoners()
	{
		TroopRoster roster = PartyBase.MainParty?.PrisonRoster;
		if (roster == null || roster.Count == 0 || _layout.PrisonerSlots.Count == 0)
		{
			return;
		}
		EnsureCampPrisonerCombatTeams();
		var picks = new List<CharacterObject>();
		var regulars = new List<TroopRosterElement>();
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement e = roster.GetElementCopyAtIndex(i);
			if (e.Character == null || e.Number <= 0)
			{
				continue;
			}
			if (e.Character.IsHero)
			{
				if (picks.Count < MaxPrisonerLords && e.Character.HeroObject?.IsAlive == true)
				{
					picks.Add(e.Character);
				}
			}
			else
			{
				regulars.Add(e);
			}
		}
		var used = new int[regulars.Count];
		bool added = true;
		while (picks.Count < _layout.PrisonerSlots.Count && added)
		{
			added = false;
			for (int i = 0; i < regulars.Count && picks.Count < _layout.PrisonerSlots.Count; i++)
			{
				if (used[i] < regulars[i].Number)
				{
					used[i]++;
					picks.Add(regulars[i].Character);
					added = true;
				}
			}
		}
		var occurrences = new Dictionary<CharacterObject, int>();
		for (int i = 0; i < picks.Count; i++)
		{
			CharacterObject character = picks[i];
			occurrences.TryGetValue(character, out int occurrence);
			occurrences[character] = occurrence + 1;
			int rosterIndex = roster.FindIndexOfTroop(character);
			if (rosterIndex < 0)
			{
				continue;
			}
			TroopRosterElement element = roster.GetElementCopyAtIndex(rosterIndex);
			bool wounded = character.IsHero ? character.HeroObject.IsWounded : occurrence >= element.Number - element.WoundedNumber;
			Agent agent = SpawnPrisoner(character, _layout.PrisonerSlots[i], wounded);
			if (agent != null)
			{
				_prisoners.Add(agent);
				_campPrisoners.Add(agent, new CampPrisoner { Character = character, Captor = PartyBase.MainParty, SpawnedWounded = wounded });
				ApplyPrisonerPose(agent);
			}
		}
		MaintainCampPrisonerCombatIsolation();
	}

	private Agent SpawnPrisoner(CharacterObject character, FieldCampLayout.Slot slot, bool wounded)
	{
		try
		{
				if (!FieldCampSite.TryFindSpawnPosition(Mission.Scene, ToWorld(slot.Position), 0.35f, out Vec3 pos))
				{
					Logger.Log(LogSource, "prisoner spawn skipped blocked slot troop=" + character.StringId);
					return null;
				}
				Vec2 dir = ToWorldDirection(slot.Facing);
			var origin = new PrisonerAgentOrigin(character);
			// This camp owns casualties, including the real killer and wounded samples.
			// Mark before SpawnAgent so no native/third-party origin callback can settle twice.
			origin.MarkCampaignCasualtyHandledExternally("field_camp_captive_owner");
			Agent agent = Mission.GetMissionBehavior<FieldCampAgentHandler>().SpawnResident(
				character, PartyBase.MainParty, _prisonerTeam, pos, dir, civilian: true, origin: origin);
			if (agent != null && character.IsHero)
			{
				agent.Health = Math.Max(1, character.HeroObject.HitPoints);
			}
			else if (agent != null && wounded)
			{
				agent.Health = Math.Max(1f, agent.HealthLimit * 0.3f);
			}
			return agent;
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "prisoner spawn failed troop=" + character.StringId + " error=" + ex.Message);
			return null;
		}
	}

	/// <summary>Registered camp captives remain mortal, unarmed, bound and AI-paused, including after slaughter restore.</summary>
	private void ApplyPrisonerPose(Agent agent)
	{
		if (agent == null || !agent.IsActive() || !_campPrisoners.ContainsKey(agent) || !_prisoners.Contains(agent)
			|| RichExecutions.Core.VengeanceIntegration.IsProtectedVictim(agent))
		{
			return;
		}
		try
		{
			agent.SetMortalityState(Agent.MortalityState.Mortal);
			agent.SetIsAIPaused(isPaused: true);
			agent.DisableScriptedMovement();
			agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanGetAlarmed);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.NumAllWeaponSlots; i++)
			{
				agent.RemoveEquippedWeapon((EquipmentIndex)i);
			}
			agent.UpdateWeapons();
			PlayBoundIdle(agent);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "prisoner pose failed agent=" + agent.Index + " error=" + ex.Message);
		}
	}

	private static readonly string[] BoundIdleActions = { "act_prisoner_conversation_idle_1", "act_prisoner_conversation_idle_2" };

	/// <summary>
	/// Hands tied behind the back. The bound-prisoner idles exist only in the base warrior
	/// action set (the female and settlement sets do not inherit them), so the prisoner is
	/// moved onto it first, as the Vengeance condemned are. Falls back to a cowering idle.
	/// Once per prisoner at spawn and after a slaughter restore.
	/// </summary>
	private void PlayBoundIdle(Agent agent)
	{
		ActionIndexCache bound = ResolveAction(BoundIdleActions[Math.Abs(agent.Index) % BoundIdleActions.Length]);
		try
		{
			MBActionSet warrior = MBActionSet.GetActionSet("as_human_warrior");
			if (bound != ActionIndexCache.act_none && MBActionSet.CheckActionAnimationClipExists(warrior, in bound))
			{
				if (!string.Equals(agent.ActionSet.GetName(), "as_human_warrior", StringComparison.Ordinal))
				{
					AnimationSystemData data = agent.Monster.FillAnimationSystemData(warrior, agent.Character?.GetStepSize() ?? 1f, false);
					agent.SetActionSet(ref data);
				}
				agent.SetActionChannel(0, in bound, true);
				return;
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "bound idle failed agent=" + agent.Index + " error=" + ex.Message);
		}
		ActionIndexCache cower = ResolveAction("act_scared_idle_1");
		if (cower != ActionIndexCache.act_none)
		{
			agent.SetActionChannel(0, in cower, true);
		}
	}

	// ---------- prisoner slaughter (TROOP_INSPECTION_SLAUGHTER_PRISONERS) ----------

	internal bool CanOfferPrisonerSlaughter(int speakerAgentIndex, out int regularPrisonerCount, out int attackerCount)
	{
		List<Agent> targets = LiveRegularPrisoners();
		List<Agent> attackers = ArmedSoldiers(targets.Count);
		regularPrisonerCount = targets.Count;
		attackerCount = attackers.Count;
		return _spawned && !SlaughterBusy && !_ceremonyActive && !Mission.IsMissionEnding && IsCampSoldier(speakerAgentIndex) && targets.Count > 0 && attackers.Count > 0;
	}

	internal bool TryStartPrisonerSlaughter(int speakerAgentIndex, out int attackerCount, out int targetCount, out string reason)
	{
		attackerCount = 0;
		targetCount = 0;
		if (!_spawned || Mission.IsMissionEnding || _ceremonyActive)
		{
			reason = "not_normal_inspection";
			return false;
		}
		if (SlaughterBusy)
		{
			reason = "slaughter_already_active";
			return false;
		}
		if (!IsCampSoldier(speakerAgentIndex))
		{
			reason = "speaker_not_inspected_soldier";
			return false;
		}
		List<Agent> targets = LiveRegularPrisoners();
		if (targets.Count == 0)
		{
			reason = "no_regular_prisoners";
			return false;
		}
		List<Agent> attackers = ArmedSoldiers(targets.Count);
		if (attackers.Count == 0)
		{
			reason = "combat_setup_unavailable";
			return false;
		}
		if (_envoy != null)
		{
			Requeue(_envoyIssue);
			DropEnvoy(sendHome: false);
		}
		MoveToExecutionGround(targets);
		_slaughter = new TroopInspectionPrisonerSlaughterRuntime(Mission, attackers, targets, RestoreAfterSlaughter);
		if (!_slaughter.TryStart(out reason))
		{
			_slaughter.Cleanup("start_failed");
			if (!_slaughter.HasPendingRestore)
			{
				_slaughter = null;
			}
			return false;
		}
		attackerCount = attackers.Count;
		targetCount = targets.Count;
		Logger.Log(LogSource, "prisoner slaughter started attackers=" + attackerCount + " targets=" + targetCount);
		return true;
	}

	/// <summary>With an execution ground, the condemned are led onto it (kneeling rows facing the camp) before the order is carried out.</summary>
	private void MoveToExecutionGround(List<Agent> targets)
	{
		if (!TryGetExecutionSpot(out Vec3 spot, out Vec2 facing))
		{
			return;
		}
		Vec2 side = new Vec2(facing.y, -facing.x);
		for (int i = 0; i < targets.Count; i++)
		{
			try
			{
				Vec2 flat = spot.AsVec2 + side * ((i % 3 - 1) * 1.3f) - facing * ((i / 3) * 1.2f);
				targets[i].TeleportToPosition(new Vec3(flat.x, flat.y, FieldCampProps.LowestTerrain(Mission.Scene, flat, 0f)));
				targets[i].LookDirection = new Vec3(facing.x, facing.y, 0f);
			}
			catch
			{
			}
		}
	}

	private void RestoreAfterSlaughter(Agent agent, bool prisoner)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		if (prisoner)
		{
			ApplyPrisonerPose(agent);
			return;
		}
		for (int i = 0; i < _agents.Count; i++)
		{
			if (_agents[i].Agent == agent)
			{
				CampAgent entry = _agents[i];
				entry.Target = _assembled && entry.Activity != Activity.Wounded ? entry.Target : entry.Home;
				MoveTo(entry, entry.Target, entry.TargetFacing, run: false);
				return;
			}
		}
	}

	private bool IsCampSoldier(int agentIndex)
	{
		for (int i = 0; i < _agents.Count; i++)
		{
			Agent agent = _agents[i].Agent;
			if (agent != null && agent.Index == agentIndex)
			{
				return agent.IsActive() && _agents[i].Activity != Activity.Post && !(agent.Character is CharacterObject c && c.IsHero)
					&& ReferenceEquals(agent.Origin?.BattleCombatant as PartyBase, PartyBase.MainParty);
			}
		}
		return false;
	}

	private List<Agent> LiveRegularPrisoners()
	{
		var list = new List<Agent>();
		for (int i = 0; i < _prisoners.Count; i++)
		{
			Agent agent = _prisoners[i];
			if (agent != null && agent.IsActive() && agent.Character is CharacterObject c && !c.IsHero)
			{
				list.Add(agent);
			}
		}
		return list;
	}

	/// <summary>
	/// The soldiers nearest the pen carry out the order: two per prisoner, at most 16,
	/// preferring men with a real weapon (camp clothes may leave some unarmed).
	/// </summary>
	private List<Agent> ArmedSoldiers(int targetCount)
	{
		var armed = new List<Agent>();
		var healthy = new List<Agent>();
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
			Agent agent = entry.Agent;
			if (agent == null || !agent.IsActive() || entry.Activity == Activity.Wounded || entry.Activity == Activity.Post
				|| (agent.Character is CharacterObject c && c.IsHero)
				|| !ReferenceEquals(agent.Origin?.BattleCombatant as PartyBase, PartyBase.MainParty))
			{
				continue;
			}
			healthy.Add(agent);
			if (HasCombatWeapon(agent))
			{
				armed.Add(agent);
			}
		}
		List<Agent> pool = armed.Count > 0 ? armed : healthy;
		int wanted = Math.Max(1, Math.Min(MaxAttackers, targetCount * AttackersPerPrisoner));
		if (pool.Count <= wanted || _layout.PrisonerSlots.Count == 0)
		{
			return pool.Count <= wanted ? pool : pool.GetRange(0, wanted);
		}
		Vec2 pen = ToWorld(_layout.PrisonerSlots[_layout.PrisonerSlots.Count / 2].Position).AsVec2;
		pool.Sort((a, b) => a.Position.AsVec2.DistanceSquared(pen).CompareTo(b.Position.AsVec2.DistanceSquared(pen)));
		return pool.GetRange(0, wanted);
	}

	// ---------- ordinary camp-captive damage (never the Vengeance victim) ----------

	/// <summary>
	/// Native singleplayer cancels melee against friends AND non-enemy humans. Two
	/// neutral-side teams isolate the player/captives; the camp army stays peaceful.
	/// Created once, without changing Mission.PlayerTeam or any existing team pair.
	/// </summary>
	private void EnsureCampPrisonerCombatTeams()
	{
		if (_prisonerTeam != null && _prisonerPlayerTeam != null)
		{
			return;
		}
		_prisonerPlayerTeam = Mission.Teams.Add(BattleSideEnum.None, _team.Color, _team.Color2, _team.Banner, false, false);
		_prisonerTeam = Mission.Teams.Add(BattleSideEnum.None, 0xFF7A2020u, 0xFF2A0808u, null, false, false);
		_prisonerPlayerTeam.SetIsEnemyOf(_prisonerTeam, true);
		_prisonerTeam.SetIsEnemyOf(_prisonerPlayerTeam, true);
	}

	/// <summary>
	/// Called after spawn and by the 0.5 s camp upkeep. O(1), no agent/team scans:
	/// duels, slaughter and Vengeance own temporary teams until they release them.
	/// </summary>
	private void MaintainCampPrisonerCombatIsolation()
	{
		if (_prisonerTeam == null || _prisonerPlayerTeam == null || Mission == null || Mission.IsMissionEnding
			|| _ceremonyActive || SlaughterBusy || _duelists.Count != 0 || Mission.Mode == MissionMode.Conversation)
		{
			return;
		}
		Agent player = Mission.MainAgent;
		if (player == null || !player.IsActive())
		{
			return;
		}
		if (_prisoners.Count == 0)
		{
			if (player.Team == _prisonerPlayerTeam)
			{
				player.SetTeam(_team, true);
			}
			return;
		}
		if (player.Team == _team)
		{
			player.SetTeam(_prisonerPlayerTeam, true);
		}
		// Other scoped combat owners may clear this pair; restore only our own pair.
		if (!_prisonerPlayerTeam.IsEnemyOf(_prisonerTeam))
		{
			_prisonerPlayerTeam.SetIsEnemyOf(_prisonerTeam, true);
		}
		if (!_prisonerTeam.IsEnemyOf(_prisonerPlayerTeam))
		{
			_prisonerTeam.SetIsEnemyOf(_prisonerPlayerTeam, true);
		}
	}

	/// <summary>Call before fading the pen duplicate. Its origin is already externally owned.</summary>
	private void ForgetCampPrisonerForCeremony(Agent agent)
	{
		if (agent != null)
		{
			_campPrisoners.Remove(agent);
			_prisoners.Remove(agent);
		}
	}

	/// <summary>Mission teardown only; never turn removal/exit into a campaign casualty.</summary>
	private void CleanupCampPrisonerCombatIsolation()
	{
		_campPrisoners.Clear();
		_prisoners.Clear();
		try
		{
			Agent player = Mission?.MainAgent;
			if (player != null && player.IsActive() && player.Team == _prisonerPlayerTeam)
			{
				player.SetTeam(_team, true);
			}
			if (_prisonerTeam != null && _prisonerPlayerTeam != null)
			{
				_prisonerPlayerTeam.SetIsEnemyOf(_prisonerTeam, false);
				_prisonerTeam.SetIsEnemyOf(_prisonerPlayerTeam, false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "prisoner isolation cleanup failed: " + ex.Message);
		}
	}

	/// <summary>
	/// Main OnScoreHit forwards its native input here. Health has already been reduced
	/// by Agent.HandleBlow, and the death decider runs AFTER this callback. No synthetic
	/// damage, damage multiplier override or most-recent-nonfatal-hit attribution.
	/// </summary>
	private void OnCampPrisonerScoreHit(Agent affected, Agent affector, bool isBlocked, in Blow blow, in AttackCollisionData collision, float damage)
	{
		if (Mission == null || Mission.IsMissionEnding || affected == null || affected.IsFadingOut()
			|| !_campPrisoners.TryGetValue(affected, out CampPrisoner prisoner) || !_prisoners.Contains(affected)
			|| RichExecutions.Core.VengeanceIntegration.IsProtectedVictim(affected)
			|| isBlocked || damage <= 0f || blow.InflictedDamage <= 0 || blow.IsFallDamage || collision.IsFallDamage
			|| collision.CollisionResult != CombatCollisionResult.StrikeAgent || affector == null || affector == affected
			|| blow.OwnerId != affector.Index)
		{
			return;
		}
		Agent actor = affector.IsMount ? affector.RiderAgent : affector;
		if (actor == null || !actor.IsHuman || actor == affected)
		{
			return;
		}
		Hero hero = prisoner.Character.HeroObject;
		if (hero != null && hero.IsAlive && hero.PartyBelongedToAsPrisoner == prisoner.Captor)
		{
			hero.HitPoints = Math.Max(1, (int)Math.Ceiling(affected.Health));
		}
		if (affected.Health < 1f)
		{
			prisoner.FatalAffector = actor;
			prisoner.FatalBlowOwnerId = blow.OwnerId;
			prisoner.FatalHitTime = Mission.CurrentTime;
			prisoner.FatalCanKill = blow.DamageType != DamageTypes.Blunt
				|| (blow.WeaponRecord.WeaponFlags & WeaponFlags.CanKillEvenIfBlunt) != 0;
		}
	}

	/// <summary>One death decision, exact registered instance and this frame's real lethal attack only.</summary>
	private bool ShouldKillCampPrisoner(Agent affected)
	{
		if (Mission == null || Mission.IsMissionEnding || affected == null || affected.Health >= 1f || affected.IsFadingOut()
			|| !_campPrisoners.TryGetValue(affected, out CampPrisoner prisoner) || !_prisoners.Contains(affected)
			|| RichExecutions.Core.VengeanceIntegration.IsProtectedVictim(affected)
			|| !prisoner.FatalCanKill || !HasFreshCampPrisonerFatalHit(prisoner))
		{
			return false;
		}
		return prisoner.FatalAffector == Mission.MainAgent
			|| (_slaughter?.ControlsAgent(prisoner.FatalAffector) == true && _slaughter.ControlsAgent(affected));
	}

	private bool HasFreshCampPrisonerFatalHit(CampPrisoner prisoner)
	{
		float age = Mission.CurrentTime - prisoner.FatalHitTime;
		return prisoner.FatalAffector != null && prisoner.FatalHitTime >= 0f && age >= 0f && age <= 0.1f;
	}

	/// <summary>
	/// Add the returned behavior in the camp launcher. Vengeance captures it as its
	/// downstream decider; unrelated actors return None and retain native surgery.
	/// </summary>
	internal static MissionBehavior CreateCampPrisonerStateDecider() => new CampPrisonerStateDecider();

	private sealed class CampPrisonerStateDecider : MissionLogic, IAgentStateDecider
	{
		public AgentState GetAgentState(Agent affected, float deathProbability, out bool usedSurgery)
		{
			usedSurgery = false;
			FieldCampMissionLogic camp = Mission?.GetMissionBehavior<FieldCampMissionLogic>();
			if (camp != null && camp.ShouldKillCampPrisoner(affected))
			{
				return AgentState.Killed;
			}
			// Native calls only the first decider. Preserve a later duel/other owner,
			// never call an earlier Vengeance wrapper (which would recurse into us).
			bool afterThis = false;
			foreach (MissionBehavior behavior in Mission.MissionBehaviors)
			{
				if (ReferenceEquals(behavior, this))
				{
					afterThis = true;
				}
				else if (afterThis && behavior is IAgentStateDecider downstream)
				{
					return downstream.GetAgentState(affected, deathProbability, out usedSurgery);
				}
			}
			return AgentState.None;
		}
	}

	/// <summary>
	/// Replaces the main OnAgentRemoved prison-list/remove/write block. Claims the
	/// instance before any campaign event can re-enter; FadeOut/exit/rout never settle.
	/// </summary>
	private void OnCampPrisonerRemoved(Agent affected, Agent affector, AgentState state, KillingBlow blow)
	{
		if (affected == null || !_campPrisoners.TryGetValue(affected, out CampPrisoner prisoner))
		{
			return;
		}
		_campPrisoners.Remove(affected);
		bool displayedCaptive = _prisoners.Remove(affected);
		if (!displayedCaptive || Mission == null || Mission.IsMissionEnding || affected.IsFadingOut()
			|| RichExecutions.Core.VengeanceIntegration.IsProtectedVictim(affected)
			|| (state != AgentState.Killed && state != AgentState.Unconscious)
			|| (!(blow.IsValid && blow.InflictedDamage > 0) && !HasFreshCampPrisonerFatalHit(prisoner)))
		{
			return;
		}
		try
		{
			CharacterObject character = prisoner.Character;
			TroopRoster roster = prisoner.Captor?.PrisonRoster;
			if (prisoner.Captor != PartyBase.MainParty || roster == null || roster.GetTroopCount(character) <= 0)
			{
				return;
			}
			Agent actor = null;
			Agent blowOwner = affector;
			if (blow.IsValid && blow.InflictedDamage > 0 && blow.OwnerId >= 0
				&& (blowOwner == null || blowOwner.Index != blow.OwnerId))
			{
				blowOwner = Mission.FindAgentWithIndex(blow.OwnerId);
			}
			if (blow.IsValid && blow.InflictedDamage > 0 && blowOwner != null && blow.OwnerId == blowOwner.Index)
			{
				actor = blowOwner.IsMount ? blowOwner.RiderAgent : blowOwner;
			}
			else if (HasFreshCampPrisonerFatalHit(prisoner)
				&& (!blow.IsValid || blow.OwnerId == prisoner.FatalBlowOwnerId || blow.OwnerId == prisoner.FatalAffector.Index))
			{
				actor = prisoner.FatalAffector;
			}
			Hero killer = (actor?.Character as CharacterObject)?.HeroObject;
			if (character.IsHero)
			{
				Hero hero = character.HeroObject;
				if (hero == null || !hero.IsAlive || hero.PartyBelongedToAsPrisoner != prisoner.Captor)
				{
					return;
				}
				if (hero.DeathMark != KillCharacterAction.KillCharacterActionDetail.None)
				{
					// Another authoritative campaign owner has already accepted this death.
					return;
				}
				if (state == AgentState.Unconscious)
				{
					// Do not add a death mark to a merely wounded hero.
					hero.MakeWounded();
				}
				else
				{
					// Unlike ApplyByBattle(null), this fires native execution relations,
					// honor loss, captivity removal and HeroKilled with the real killer.
					KillCharacterAction.ApplyByExecution(hero, killer, true, true);
					if (hero.IsAlive && hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
					{
						Logger.Log(LogSource, "[ERROR] captive scene death not accepted by campaign hero=" + character.StringId);
					}
				}
			}
			else
			{
				int index = roster.FindIndexOfTroop(character);
				TroopRosterElement element = roster.GetElementCopyAtIndex(index);
				if (state == AgentState.Killed)
				{
					// A sampled pre-wounded man must remove one wounded count too.
					if ((prisoner.SpawnedWounded && element.WoundedNumber <= 0)
						|| (!prisoner.SpawnedWounded && element.Number <= element.WoundedNumber))
					{
						return;
					}
					roster.AddToCounts(character, -1, false, prisoner.SpawnedWounded ? -1 : 0);
				}
				else if (!prisoner.SpawnedWounded && element.Number > element.WoundedNumber)
				{
					roster.WoundTroop(character, 1, default(UniqueTroopDescriptor));
				}
			}
			Logger.Log(LogSource, "prisoner casualty troop=" + character.StringId + " state=" + state
				+ " killer=" + (killer?.StringId ?? "none") + " player=" + (actor == Mission.MainAgent));
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "[ERROR] prisoner casualty write failed troop=" + prisoner.Character.StringId + " error=" + ex);
		}
	}

	/// <summary>
	/// After a slaughter: the runtime restores the hostility it captured, but the enemy
	/// team it created starts hostile, so make it peaceful again for the rest of the camp.
	/// </summary>
	private void EndSlaughter()
	{
		_slaughter = null;
		try
		{
			// The camp team is the attacker; the slaughter's target team is the defender.
			Team enemy = Mission.DefenderTeam;
			if (enemy != null && enemy != _team && _team.IsEnemyOf(enemy))
			{
				_team.SetIsEnemyOf(enemy, false);
				enemy.SetIsEnemyOf(_team, false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "slaughter team reset failed: " + ex.Message);
		}
		Logger.Log(LogSource, "prisoner slaughter ended prisonersLeft=" + _prisoners.Count);
	}

	/// <summary>Soldiers and companions of the main party (not army lords, their men or prisoners).</summary>
	internal bool IsMainPartyMember(int agentIndex)
	{
		for (int i = 0; i < _agents.Count; i++)
		{
			Agent agent = _agents[i].Agent;
			if (agent != null && agent.Index == agentIndex)
			{
				PartyBase party = agent.Origin?.BattleCombatant as PartyBase;
				return agent.IsActive() && party != null && ReferenceEquals(party, PartyBase.MainParty);
			}
		}
		return false;
	}

	private static bool HasCombatWeapon(Agent agent)
	{
		try
		{
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
			{
				MissionWeapon weapon = agent.Equipment[slot];
				if (weapon.IsEmpty || weapon.Item?.Weapons == null)
				{
					continue;
				}
				foreach (WeaponComponentData usage in weapon.Item.Weapons)
				{
					if (usage != null && !usage.IsAmmo && !usage.IsShield && (usage.IsMeleeWeapon || usage.IsRangedWeapon))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private void CleanupSlaughter(string reason)
	{
		try
		{
			_slaughter?.Cleanup(reason);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "prisoner slaughter cleanup failed: " + ex.Message);
		}
		_slaughter = null;
	}
}
