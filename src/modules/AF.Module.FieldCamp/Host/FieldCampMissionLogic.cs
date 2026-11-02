using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Peaceful camp mission (docs/designs/field-camp-v1.md, step 2).
/// Frequency: props and agents are created once after the scene is ready;
/// camp-life upkeep runs every 0.5 s on at most 20 agents; formation slots are
/// computed only when an assemble order arrives.
/// </summary>
public sealed partial class FieldCampMissionLogic : MissionLogic
{
	internal const string MissionName = "AnimusForge_FieldCamp";
	private const string LogSource = "FieldCamp";
	private const float UpkeepIntervalSeconds = 0.5f;
	private const int UpkeepBatch = 20;
	private const float SpawnDelaySeconds = 0.4f;

	internal enum Activity
	{
		Fire,
		Sentry,
		Wander,
		Wounded,
		/// <summary>Companion at a party-role post or army lord at his tent: stays put, never joins the ranks.</summary>
		Post,
		/// <summary>At a bench or a work spot (cooking, smithing, archery, grooming, leaning, talking).</summary>
		Work
	}

	private sealed class CampAgent
	{
		internal Agent Agent;
		internal Activity Activity;
		internal Vec3 Home;
		internal Vec2 HomeFacing;
		internal Vec3 Target;
		internal Vec2 TargetFacing;
		internal bool Seated;
		internal FormationClass TroopClass;
		internal float NextWanderTime;
		internal FieldCampLayout.WorkKind Work;
		internal int PoseVariant;
		/// <summary>On a short stroll; walks back to Home on arrival.</summary>
		internal bool Returning;
		internal float MoveDeadline;
		internal bool MoveBlocked;
	}

	private readonly List<MobileParty> _parties;
	private readonly List<CampAgent> _agents = new List<CampAgent>();
	private readonly Dictionary<Agent, CampAgent> _campAgentsByAgent = new Dictionary<Agent, CampAgent>();
	private readonly HashSet<Agent> _sceneConversationAgents = new HashSet<Agent>();
	private Agent _nativeConversationAgent;
	private FieldCampLayout _layout;
	private MatrixFrame _campFrame = MatrixFrame.Identity;
	private float _campScale = 1f;
	private Team _team;
	private bool _spawned;
	private bool _assembled;
	private float _elapsed;
	private float _upkeepTimer;
	private int _upkeepCursor;
	private int _woundedSlotsUsed;

	internal static FieldCampMissionLogic Current => Mission.Current?.GetMissionBehavior<FieldCampMissionLogic>();

	internal bool IsAssembled => _assembled;

	internal FieldCampMissionLogic(List<MobileParty> parties)
	{
		_parties = parties ?? new List<MobileParty>();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		Mission.SetMissionMode(MissionMode.StartUp, atStart: true);
		// CampaignMissionComponent owns the campaign events and conversation lifetime.
		Campaign.Current.ConversationManager.ConversationBegin += OnCampConversationBegin;
		Campaign.Current.ConversationManager.ConversationEnd += OnCampConversationEnd;
	}

	protected override void OnEndMission()
	{
		CleanupCampPrisonerCombatIsolation();
		CleanupSlaughter("mission_end");
		FieldCampSite.ReleaseCaches(Mission.Scene);
		var conversations = Campaign.Current?.ConversationManager;
		if (conversations != null)
		{
			conversations.ConversationBegin -= OnCampConversationBegin;
			conversations.ConversationEnd -= OnCampConversationEnd;
		}
		_sceneConversationAgents.Clear();
		_campAgentsByAgent.Clear();
		_nativeConversationAgent = null;
		base.OnEndMission();
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		return null;
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		return false;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (Mission.IsMissionEnding)
		{
			return;
		}
		_elapsed += dt;
		if (!_spawned)
		{
			if (_elapsed >= SpawnDelaySeconds)
			{
				_spawned = true;
				SetUpCamp();
			}
			return;
		}
		if (!_campInitialized) return;
		_slaughter?.Tick(dt);
		TickDuelRelease(dt);
		if (SlaughterBusy)
		{
			// The slaughter runtime owns the agents until it restores them.
			return;
		}
		if (_slaughter != null)
		{
			EndSlaughter();
		}
		_upkeepTimer -= dt;
		if (_upkeepTimer <= 0f)
		{
			_upkeepTimer = UpkeepIntervalSeconds;
			MaintainCampPrisonerCombatIsolation();
			TickCeremonyPreparation();
			TickCeremonyReturn();
			if (!_ceremonyActive)
			{
				// Camp life pauses while an execution ceremony holds the camp.
				UpkeepBatchOfAgents();
				TickEnvoys();
			}
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit,
		in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);
		OnCampPrisonerScoreHit(affectedAgent, affectorAgent, isBlocked, in blow, in collisionData, damagedHp);
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent == null)
		{
			return;
		}
		_slaughter?.OnAgentRemoved(affectedAgent, affectorAgent, agentState);
		_duelists.Remove(affectedAgent);
		OnCampPrisonerRemoved(affectedAgent, affectorAgent, agentState, blow);
		_sceneConversationAgents.Remove(affectedAgent);
		for (int i = _agents.Count - 1; i >= 0; i--)
		{
			if (_agents[i].Agent == affectedAgent)
			{
				_campAgentsByAgent.Remove(affectedAgent);
				_agents.RemoveAt(i);
			}
		}
	}

	// ---------- set-up ----------

	private bool _campInitialized;
	private bool _setupFailed;

	private bool RunCampSetup(string stage, Action action)
	{
		try { action(); return true; }
		catch (Exception ex)
		{
			_setupFailed = true;
			Logger.Log(LogSource, "[WARN] camp initialization stage=" + stage + " failed; manual leave remains available: " + ex);
			return false;
		}
	}

	private void SetUpCamp()
	{
        FieldCampProps.ResetPlacementCounts();
		if (!RunCampSetup("layout", () => _layout = FieldCampLayout.Build(_parties, FieldCampSettings.AgentCap, FieldCampSettings.TentCap)))
			_layout = new FieldCampLayout { HalfX = 12f, Back = -12f, Front = 12f, PlayerFacing = Vec2.Forward };
		bool site = RunCampSetup("whole_camp_site", () => _campFrame = FieldCampSite.Resolve(Mission, _layout, out _campScale));
		bool cache = false;
		if (site)
		{
			RunCampSetup("clutter", () => FieldCampSite.ClearClutter(Mission.Scene, _campFrame, _layout, _campScale));
			RunCampSetup("props", () => FieldCampProps.Place(Mission.Scene, _layout, _campFrame, _campScale));
			cache = RunCampSetup("ground_cache", () => FieldCampSite.InitializeGroundCache(Mission.Scene, _campFrame, _layout, _campScale));
		}
		RunCampSetup("team", CreateCampTeam);
		RunCampSetup("player", SpawnPlayer);
		_campInitialized = site && cache && Mission.MainAgent?.IsActive() == true;
		if (_campInitialized)
		{
			RunCampSetup("troops", SpawnTroops);
			RunCampSetup("companions", SpawnCompanions);
			RunCampSetup("lords", SpawnArmyLords);
			RunCampSetup("prisoners", SpawnPrisoners);
			RunCampSetup("horses", () => SpawnHorses());
			RunCampSetup("envoys", QueueEnvoys);
			FieldCampBehavior.Instance?.MarkPatrolPlayerReady();
		}
		Logger.Log(LogSource, "camp initialization ready=" + _campInitialized + " stageFailure=" + _setupFailed + " tents=" + FieldCampProps.PlacedTentCount + "/" + FieldCampProps.RequestedTentCount + " scene=" + Mission.SceneName);
		if (!site) AnimusForgeQuickInfo.Show(FieldCampText.NoCampSite());
		else if (_setupFailed || !_campInitialized || FieldCampProps.PlacedTentCount < FieldCampProps.RequestedTentCount)
			AnimusForgeQuickInfo.Show(FieldCampText.IncompleteCamp(FieldCampProps.PlacedTentCount, FieldCampProps.RequestedTentCount));
	}

	private void CreateCampTeam()
	{
		Hero main = Hero.MainHero;
		uint c1 = main?.MapFaction?.Color ?? 4278190335u;
		uint c2 = main?.MapFaction?.Color2 ?? 4278190208u;
		_team = Mission.Teams.Add(BattleSideEnum.Attacker, c1, c2, main?.Clan?.Banner, true, false);
		Mission.PlayerTeam = _team;

	}

	private void SpawnPlayer()
	{
		CharacterObject hero = Hero.MainHero?.CharacterObject;
		if (hero == null)
		{
			return;
		}
			if (!FieldCampSite.TryFindPlayerStart(Mission, ToWorld(_layout.PlayerStart), out Vec3 pos))
				throw new InvalidOperationException("Player start has no grounded clearance.");
			Vec2 dir = ToWorldDirection(_layout.PlayerFacing);
		AgentBuildData data = new AgentBuildData(hero)
			.Team(_team)
			.TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, hero, -1, default(UniqueTroopDescriptor), false, true))
			.Equipment(Hero.MainHero.CivilianEquipment ?? Hero.MainHero.BattleEquipment)
			.CivilianEquipment(true)
			.NoHorses(true)
			.InitialPosition(in pos)
			.InitialDirection(in dir)
			.Controller(AgentControllerType.Player);
		Agent agent = Mission.SpawnAgent(data);
		if (agent != null)
		{
			Mission.MainAgent = agent;
			agent.Controller = AgentControllerType.Player;
		}
	}

	/// <summary>
	/// Healthy men are dealt out in a fixed rotation so every part of the camp is busy:
	/// sentries first (one in six), then work spots, bench seats, fire seats and patrols.
	/// Wounded men take the infirmary beds and seats.
	/// </summary>
	private void SpawnTroops()
	{
		int sentry = 0;
		int work = 0;
		int bench = 0;
		int fire = 0;
		int wander = 0;
		int wounded = 0;
		int dealt = 0;
		for (int g = 0; g < _layout.Groups.Count; g++)
		{
			FieldCampLayout.TroopGroup group = _layout.Groups[g];
			FormationClass troopClass = group.Character.DefaultFormationClass;
			for (int i = 0; i < group.SpawnWounded && wounded < _layout.WoundedSlots.Count; i++)
			{
				FieldCampLayout.Slot s = _layout.WoundedSlots[wounded++];
				Agent agent = SpawnCharacter(group.Character, group.Party.Party, s.Position, s.Facing, civilian: true);
				if (agent != null)
				{
					Track(agent, Activity.Wounded, s.Position, s.Facing, troopClass, s.Work);
				}
			}
			_woundedSlotsUsed = wounded;
			for (int i = 0; i < group.SpawnHealthy; i++, dealt++)
			{
				FieldCampLayout.Slot s = default(FieldCampLayout.Slot);
				Activity activity = Activity.Wander;
				bool found = false;
				for (int attempt = 0; attempt < 5 && !found; attempt++)
				{
					switch ((dealt + attempt) % 6)
					{
						case 0:
							found = sentry < _layout.SentrySlots.Count;
							if (found)
							{
								s = _layout.SentrySlots[sentry++];
								activity = Activity.Sentry;
							}
							break;
						case 1:
						case 4:
							found = work < _layout.WorkSlots.Count;
							if (found)
							{
								s = _layout.WorkSlots[work++];
								activity = Activity.Work;
							}
							break;
						case 2:
							found = bench < _layout.BenchSlots.Count;
							if (found)
							{
								s = _layout.BenchSlots[bench++];
								activity = Activity.Work;
							}
							break;
						case 3:
							found = fire < _layout.FireSlots.Count;
							if (found)
							{
								s = _layout.FireSlots[fire++];
								activity = Activity.Fire;
							}
							break;
					}
				}
				if (!found)
				{
					if (bench < _layout.BenchSlots.Count)
					{
						s = _layout.BenchSlots[bench++];
						activity = Activity.Work;
					}
					else if (_layout.WanderSlots.Count > 0)
					{
						s = _layout.WanderSlots[wander++ % _layout.WanderSlots.Count];
						s.Position += new Vec2(MBRandom.RandomFloatRanged(-1.2f, 1.2f), MBRandom.RandomFloatRanged(-1.2f, 1.2f));
						activity = Activity.Wander;
					}
					else
					{
						break;
					}
				}
				// Archers keep their bow; sentries keep their arms; everyone else is in camp clothes.
				bool armed = activity == Activity.Sentry || s.Work == FieldCampLayout.WorkKind.Archer;
				Agent agent = SpawnCharacter(group.Character, group.Party.Party, s.Position, s.Facing, civilian: !armed);
				if (agent != null)
				{
					Track(agent, activity, s.Position, s.Facing, troopClass, s.Work);
				}
			}
		}
	}

	private Agent SpawnCharacter(CharacterObject character, PartyBase party, Vec2 local, Vec2 facing, bool civilian)
	{
		if (character == null)
		{
			return null;
		}
		try
		{
				if (!FieldCampSite.TryResolveGroundedPosition(Mission.Scene, ToWorld(local), 0.35f, out Vec3 pos))
				{
					Logger.Log(LogSource, "spawn skipped blocked slot troop=" + character.StringId);
					return null;
				}
				Vec2 dir = ToWorldDirection(facing.LengthSquared < 0.01f ? new Vec2(0f, 1f) : facing);
			AgentBuildData data = new AgentBuildData(character)
				.Team(_team)
				// The camp scene only shows the roster: an invincible origin never writes
				// wounds or deaths back (sampled wounded agents would otherwise double-count).
				.TroopOrigin(new PartyAgentOrigin(party, character, -1, default(UniqueTroopDescriptor), false, true))
				.Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement"))
				.CivilianEquipment(civilian)
				.NoHorses(true)
				.InitialPosition(in pos)
				.InitialDirection(in dir)
				.Controller(AgentControllerType.AI);
			Agent agent = Mission.SpawnAgent(data);
			if (agent != null)
			{
				agent.SetWatchState(Agent.WatchState.Patrolling);
				ApplyCampActionSet(agent);
			}
			return agent;
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "spawn failed troop=" + character.StringId + " error=" + ex.Message);
			return null;
		}
	}

	private void Track(Agent agent, Activity activity, Vec2 local, Vec2 facing, FormationClass troopClass, FieldCampLayout.WorkKind work = FieldCampLayout.WorkKind.None)
	{
		Vec3 home = agent.Position;
		var entry = new CampAgent
		{
			Agent = agent,
			Activity = activity,
			Home = home,
			HomeFacing = ToWorldDirection(facing.LengthSquared < 0.01f ? new Vec2(0f, 1f) : facing),
			Target = home,
			TroopClass = troopClass,
			Work = work,
			NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(4f, 14f)
		};
		entry.TargetFacing = entry.HomeFacing;
			_agents.Add(entry);
			_campAgentsByAgent[agent] = entry;
			HoldAt(entry);
	}
}
