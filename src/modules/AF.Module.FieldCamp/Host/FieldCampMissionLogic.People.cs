using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
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

	private readonly List<Agent> _prisoners = new List<Agent>();
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
				Vec3 pos = ToWorld(slot.Position);
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
		for (int i = 0; i < picks.Count; i++)
		{
			Agent agent = SpawnPrisoner(picks[i], _layout.PrisonerSlots[i]);
			if (agent != null)
			{
				_prisoners.Add(agent);
			}
		}
	}

	private Agent SpawnPrisoner(CharacterObject character, FieldCampLayout.Slot slot)
	{
		try
		{
			Vec3 pos = ToWorld(slot.Position);
			Vec2 dir = ToWorldDirection(slot.Facing);
			AgentBuildData data = new AgentBuildData(character)
				.Team(_team)
				// The prisoner origin writes a slaughter kill back to the prison roster.
				.TroopOrigin(new PrisonerAgentOrigin(character))
				.Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement"))
				.CivilianEquipment(true)
				.NoHorses(true)
				.InitialPosition(in pos)
				.InitialDirection(in dir)
				.Controller(AgentControllerType.AI);
			Agent agent = Mission.SpawnAgent(data);
			if (agent != null)
			{
				ApplyPrisonerPose(agent);
			}
			return agent;
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "prisoner spawn failed troop=" + character.StringId + " error=" + ex.Message);
			return null;
		}
	}

	/// <summary>Unarmed, unhurtable and frozen in a cowering idle until a slaughter order makes regulars mortal.</summary>
	private void ApplyPrisonerPose(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.SetMortalityState(Agent.MortalityState.Invulnerable);
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
			ActionIndexCache action = ResolveAction("act_scared_idle_1");
			if (action != ActionIndexCache.act_none)
			{
				agent.SetActionChannel(0, in action, true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "prisoner pose failed agent=" + agent.Index + " error=" + ex.Message);
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

	/// <summary>
	/// The camp mission has no BattleAgentLogic, so a prisoner's death or knock-out is
	/// written to the prison roster here (captured lords are invulnerable and never targeted).
	/// </summary>
	private static void WritePrisonerCasualty(Agent agent, AgentState state)
	{
		if (!(agent.Origin is PrisonerAgentOrigin origin) || !(agent.Character is CharacterObject c) || c.IsHero)
		{
			return;
		}
		try
		{
			if (state == AgentState.Killed)
			{
				origin.SetKilled();
			}
			else if (state == AgentState.Unconscious)
			{
				origin.SetWounded();
			}
			Logger.Log(LogSource, "prisoner casualty troop=" + c.StringId + " state=" + state);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "prisoner casualty write failed troop=" + c.StringId + " error=" + ex.Message);
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
