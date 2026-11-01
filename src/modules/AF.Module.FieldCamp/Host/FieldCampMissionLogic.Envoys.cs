using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Camp issues are raised in person through scene-shout ambient chat instead of a
/// top-of-screen announcement: one envoy at a time (a soldier of the issue's troop type,
/// or the companion concerned) walks to the player, opens with an LLM line on the issue and
/// the player answers with a normal shout, whose postprocess resolves the issue through the
/// camp issue tags. Runs in the 0.5 s upkeep tick. Choosing an envoy scans the camp agents
/// once; while he walks, only his distance to the player is checked and his move target is
/// re-issued only after the player has moved more than a metre.
/// </summary>
public sealed partial class FieldCampMissionLogic
{
	private const float EnvoyFirstDelaySeconds = 6f;
	private const float EnvoyGapSeconds = 25f;
	private const float EnvoyWalkTimeoutSeconds = 35f;
	private const float EnvoyArriveDistance = 2.4f;
	private const float EnvoyStandOff = 1.6f;
	private const float EnvoyReleaseGraceSeconds = 4f;
	private const int EnvoyMaxVisits = 2;

	private enum EnvoyStage { None, Walking, Talking }

	private readonly List<int> _envoyQueue = new List<int>();
	private readonly Dictionary<int, int> _envoyVisits = new Dictionary<int, int>();
	private CampAgent _envoy;
	private int _envoyIssue = -1;
	private EnvoyStage _envoyStage;
	private float _envoyDeadline;
	private float _envoyIdleSince = -1f;
	private float _nextEnvoyTime;
	private Vec2 _envoyGoal;

	/// <summary>Once at camp set-up: every open issue gets a visit, in issue order.</summary>
	private void QueueEnvoys()
	{
		_envoyQueue.Clear();
		_envoyVisits.Clear();
		FieldCampBehavior behavior = FieldCampBehavior.Instance;
		if (behavior == null || behavior.State != FieldCampBehavior.CampState.Camped)
		{
			return;
		}
		IReadOnlyList<FieldCampIssues.Issue> issues = behavior.Issues;
		for (int i = 0; i < issues.Count; i++)
		{
			if (issues[i].IsOpen)
			{
				_envoyQueue.Add(i);
			}
		}
		_nextEnvoyTime = _elapsed + EnvoyFirstDelaySeconds;
	}

	private bool IsEnvoy(CampAgent entry) => _envoy != null && ReferenceEquals(entry, _envoy);

	private void TickEnvoys()
	{
		if (_envoy != null)
		{
			TickEnvoy();
			return;
		}
		if (_envoyQueue.Count == 0 || _elapsed < _nextEnvoyTime || _assembled || _duelists.Count > 0
			|| Agent.Main == null || !Agent.Main.IsActive() || ShoutBehavior.IsSceneSpeechBusyForExternal())
		{
			return;
		}
		int index = _envoyQueue[0];
		_envoyQueue.RemoveAt(0);
		FieldCampIssues.Issue issue = OpenIssue(index);
		CampAgent envoy = issue != null ? PickEnvoy(issue) : null;
		if (envoy == null)
		{
			_nextEnvoyTime = _elapsed + 1f;
			return;
		}
		_envoyVisits[index] = (_envoyVisits.TryGetValue(index, out int visits) ? visits : 0) + 1;
		_envoy = envoy;
		_envoyIssue = index;
		_envoyStage = EnvoyStage.Walking;
		_envoyDeadline = _elapsed + EnvoyWalkTimeoutSeconds;
		_envoyGoal = new Vec2(float.MaxValue, float.MaxValue);
		envoy.Returning = false;
		ReleasePose(envoy);
		WalkEnvoy();
		Logger.Log(LogSource, "envoy issue=" + (index + 1) + " kind=" + issue.Kind + " agent=" + envoy.Agent.Index);
	}

	private void TickEnvoy()
	{
		Agent agent = _envoy.Agent;
		if (agent == null || !agent.IsActive() || Agent.Main == null || !Agent.Main.IsActive())
		{
			DropEnvoy(sendHome: false);
			return;
		}
		if (_assembled)
		{
			// The assemble order already gave him a place in the ranks.
			DropEnvoy(sendHome: false);
			return;
		}
		if (_envoyStage == EnvoyStage.Walking)
		{
			if (OpenIssue(_envoyIssue) == null)
			{
				DropEnvoy(sendHome: true);
				return;
			}
			float distance = agent.Position.AsVec2.Distance(Agent.Main.Position.AsVec2);
			if (distance <= EnvoyArriveDistance)
			{
				Vec2 toPlayer = Agent.Main.Position.AsVec2 - agent.Position.AsVec2;
				MoveTo(_envoy, agent.Position, toPlayer.IsNonZero() ? toPlayer.Normalized() : _envoy.HomeFacing, run: false);
				if (ShoutBehavior.TryStartFieldCampEnvoyForExternal(agent, FieldCampText.IssuePromptText(OpenIssue(_envoyIssue))))
				{
					_envoyStage = EnvoyStage.Talking;
					_envoyIdleSince = -1f;
					return;
				}
			}
			else
			{
				WalkEnvoy();
			}
			if (_elapsed >= _envoyDeadline)
			{
				Requeue(_envoyIssue);
				DropEnvoy(sendHome: true);
			}
			return;
		}
		// Talking: wait until the opening line, the player's answer and any reply are over.
		if (ShoutBehavior.IsFieldCampEnvoyActiveForExternal() || ShoutBehavior.IsSceneSpeechBusyForExternal())
		{
			_envoyIdleSince = -1f;
			return;
		}
		if (_envoyIdleSince < 0f)
		{
			_envoyIdleSince = _elapsed;
			return;
		}
		if (_elapsed - _envoyIdleSince < EnvoyReleaseGraceSeconds)
		{
			return;
		}
		// Not settled (the player ignored him or answered something else): ask again later.
		Requeue(_envoyIssue);
		DropEnvoy(sendHome: true);
	}

	/// <summary>Walks toward a spot just short of the player; re-issued only when the player has moved.</summary>
	private void WalkEnvoy()
	{
		Vec2 player = Agent.Main.Position.AsVec2;
		Vec2 from = _envoy.Agent.Position.AsVec2 - player;
		Vec2 goal = player + (from.IsNonZero() ? from.Normalized() : Vec2.Forward) * EnvoyStandOff;
		if (goal.DistanceSquared(_envoyGoal) < 1f)
		{
			return;
		}
		_envoyGoal = goal;
		Vec3 world = new Vec3(goal.x, goal.y, Agent.Main.Position.z);
		world.z = GroundHeight(Mission.Scene, world);
		Vec2 facing = from.IsNonZero() ? -from.Normalized() : Vec2.Forward;
		MoveTo(_envoy, world, facing, run: from.Length > 15f);
	}

	private void DropEnvoy(bool sendHome)
	{
		CampAgent entry = _envoy;
		_envoy = null;
		_envoyIssue = -1;
		_envoyStage = EnvoyStage.None;
		_nextEnvoyTime = _elapsed + EnvoyGapSeconds;
		if (!sendHome || entry?.Agent == null || !entry.Agent.IsActive())
		{
			return;
		}
		entry.Target = entry.Home;
		entry.TargetFacing = entry.HomeFacing;
		entry.Returning = false;
		MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
	}

	private void Requeue(int index)
	{
		if (OpenIssue(index) != null && (!_envoyVisits.TryGetValue(index, out int visits) || visits < EnvoyMaxVisits))
		{
			_envoyQueue.Add(index);
		}
	}

	private static FieldCampIssues.Issue OpenIssue(int index)
	{
		FieldCampBehavior behavior = FieldCampBehavior.Instance;
		if (behavior == null || behavior.State != FieldCampBehavior.CampState.Camped || index < 0 || index >= behavior.Issues.Count)
		{
			return null;
		}
		FieldCampIssues.Issue issue = behavior.Issues[index];
		return issue.IsOpen ? issue : null;
	}

	/// <summary>Nearest main-party agent the issue is about: the companion, or a soldier of the troop type.</summary>
	private CampAgent PickEnvoy(FieldCampIssues.Issue issue)
	{
		CampAgent best = PickEnvoy(issue, issue.Subject);
		if (best == null && !issue.IsCompanionIssue && issue.Kind == FieldCampIssues.Kind.CultureFriction)
		{
			best = PickEnvoy(issue, issue.Subject2);
		}
		return best;
	}

	private CampAgent PickEnvoy(FieldCampIssues.Issue issue, string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		Vec2 player = Agent.Main.Position.AsVec2;
		CampAgent best = null;
		float bestDistance = float.MaxValue;
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
			Agent agent = entry.Agent;
			if (agent == null || agent == Agent.Main || !agent.IsActive() || entry.Activity == Activity.Wounded || IsDuelist(agent)
				|| !(agent.Character is CharacterObject character)
				|| !ReferenceEquals(agent.Origin?.BattleCombatant as PartyBase, PartyBase.MainParty))
			{
				continue;
			}
			bool match = issue.IsCompanionIssue
				? character.HeroObject != null && character.HeroObject.StringId == id
				: !character.IsHero && character.StringId == id;
			if (!match)
			{
				continue;
			}
			float distance = agent.Position.AsVec2.DistanceSquared(player);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = entry;
			}
		}
		return best;
	}
}
