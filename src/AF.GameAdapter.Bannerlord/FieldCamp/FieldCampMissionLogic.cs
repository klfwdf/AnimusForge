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
		Wounded
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
	}

	private readonly List<MobileParty> _parties;
	private readonly List<CampAgent> _agents = new List<CampAgent>();
	private FieldCampLayout _layout;
	private MatrixFrame _campFrame = MatrixFrame.Identity;
	private Team _team;
	private bool _spawned;
	private bool _assembled;
	private float _elapsed;
	private float _upkeepTimer;
	private int _upkeepCursor;

	internal static FieldCampMissionLogic Current => Mission.Current?.GetMissionBehavior<FieldCampMissionLogic>();

	internal bool IsAssembled => _assembled;

	internal FieldCampMissionLogic(List<MobileParty> parties)
	{
		_parties = parties ?? new List<MobileParty>();
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
		_upkeepTimer -= dt;
		if (_upkeepTimer <= 0f)
		{
			_upkeepTimer = UpkeepIntervalSeconds;
			UpkeepBatchOfAgents();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent == null)
		{
			return;
		}
		for (int i = _agents.Count - 1; i >= 0; i--)
		{
			if (_agents[i].Agent == affectedAgent)
			{
				_agents.RemoveAt(i);
			}
		}
	}

	// ---------- set-up ----------

	private void SetUpCamp()
	{
		try
		{
			_layout = FieldCampLayout.Build(_parties, FieldCampSettings.AgentCap, FieldCampSettings.TentCap);
			_campFrame = ResolveCampFrame(Mission.Scene, _layout.Radius);
			FieldCampProps.Place(Mission.Scene, _layout, _campFrame);
			SpawnEveryone();
			AnnounceOpenIssues();
			Logger.Log(LogSource, "camp mission ready agents=" + _agents.Count + " props=" + _layout.Props.Count + " scene=" + (Mission.SceneName ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "camp set-up failed: " + ex);
		}
	}

	/// <summary>Once per camp mission: one localized opening line per open issue (Q34, no LLM).</summary>
	private static void AnnounceOpenIssues()
	{
		FieldCampBehavior behavior = FieldCampBehavior.Instance;
		if (behavior == null || behavior.State != FieldCampBehavior.CampState.Camped)
		{
			return;
		}
		IReadOnlyList<FieldCampIssues.Issue> issues = behavior.Issues;
		for (int i = 0; i < issues.Count; i++)
		{
			if (!issues[i].IsOpen)
			{
				continue;
			}
			CharacterObject speaker = issues[i].IsCompanionIssue ? null : CharacterObject.Find(issues[i].Subject);
			AnimusForgeQuickInfo.Show(FieldCampText.IssueOpening(issues[i]), speaker);
		}
	}

	private void SpawnEveryone()
	{
		Hero main = Hero.MainHero;
		uint c1 = main?.MapFaction?.Color ?? 4278190335u;
		uint c2 = main?.MapFaction?.Color2 ?? 4278190208u;
		_team = Mission.Teams.Add(BattleSideEnum.Attacker, c1, c2, main?.Clan?.Banner, true, false);
		Mission.PlayerTeam = _team;

		SpawnPlayer();
		// Companions at their posts, army lords and the prisoner pen arrive in step 5.
		SpawnTroops();
	}

	private void SpawnPlayer()
	{
		CharacterObject hero = Hero.MainHero?.CharacterObject;
		if (hero == null)
		{
			return;
		}
		Vec3 pos = ToWorld(_layout.PlayerStart);
		Vec2 dir = ToWorldDirection(new Vec2(0f, -1f));
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

	private void SpawnTroops()
	{
		int fire = 0;
		int sentry = 0;
		int wander = 0;
		int wounded = 0;
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
					Track(agent, Activity.Wounded, s.Position, s.Facing, troopClass);
				}
			}
			for (int i = 0; i < group.SpawnHealthy; i++)
			{
				FieldCampLayout.Slot s;
				Activity activity;
				if (sentry < _layout.SentrySlots.Count && (i % 6 == 5 || _layout.Groups.Count == 1 && i == 0))
				{
					s = _layout.SentrySlots[sentry++];
					activity = Activity.Sentry;
				}
				else if (fire < _layout.FireSlots.Count && (i % 3 != 2 || _layout.WanderSlots.Count == 0))
				{
					s = _layout.FireSlots[fire++];
					activity = Activity.Fire;
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
				Agent agent = SpawnCharacter(group.Character, group.Party.Party, s.Position, s.Facing, civilian: activity != Activity.Sentry);
				if (agent != null)
				{
					Track(agent, activity, s.Position, s.Facing, troopClass);
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
			Vec3 pos = ToWorld(local);
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
			}
			return agent;
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "spawn failed troop=" + character.StringId + " error=" + ex.Message);
			return null;
		}
	}

	private void Track(Agent agent, Activity activity, Vec2 local, Vec2 facing, FormationClass troopClass)
	{
		Vec3 home = ToWorld(local);
		var entry = new CampAgent
		{
			Agent = agent,
			Activity = activity,
			Home = home,
			HomeFacing = ToWorldDirection(facing.LengthSquared < 0.01f ? new Vec2(0f, 1f) : facing),
			Target = home,
			TroopClass = troopClass,
			NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(4f, 14f)
		};
		entry.TargetFacing = entry.HomeFacing;
		_agents.Add(entry);
		HoldAt(entry);
	}
}
