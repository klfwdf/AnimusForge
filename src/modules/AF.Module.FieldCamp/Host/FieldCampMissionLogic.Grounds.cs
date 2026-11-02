using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Duel ring and execution ground (field camp v3).
/// - Duels started in the camp stay in place (DuelBehavior routes them here) and both
///   duelists are moved into the fenced ring first; camp life leaves them alone until
///   the duel's own logic gives them back.
/// - Camp layout reserves no apparatus. The player chooses and confirms the execution site
///   through the existing B-key builder; camp entry never selects a saved preset.
///   Frequency: duel work runs once per duel; no automatic execution prop work.
/// </summary>
public sealed partial class FieldCampMissionLogic
{

	private readonly HashSet<Agent> _duelists = new HashSet<Agent>();
	private float _duelReleaseCheck;

	/// <summary>
	/// Called by the duel start, before the duel teams are set: both fighters are
	/// teleported to opposite sides of the ring facing each other, and the enemy team the
	/// duel needs is created (the camp has only the player's team).
	/// </summary>
	internal void PlaceDuelists(Agent player, Agent opponent)
	{
		if (player == null || opponent == null || !player.IsActive() || !opponent.IsActive())
		{
			return;
		}
		try
		{
			// Captive combat isolates only the player. Rejoin the camp before DuelBehavior
			// snapshots teams, so it moves just the opponent to its separate enemy team.
			// Camp upkeep resumes captive isolation after the duel releases both actors.
			if (player == Mission.MainAgent && _prisonerPlayerTeam != null && player.Team == _prisonerPlayerTeam)
			{
				player.SetTeam(_team, true);
			}
			EnsureDuelEnemyTeam();
			Vec2 inward = new Vec2(1f, 0f);
			float r = _layout.RingRadius * 0.55f;
			Vec2 playerLocal = _layout.RingCenter + inward * r;
			Vec2 opponentLocal = _layout.RingCenter - inward * r;
			HoldDuelist(opponent);
			Teleport(player, playerLocal, -inward);
			Teleport(opponent, opponentLocal, inward);
			_duelists.Add(player);
			_duelists.Add(opponent);
			Logger.Log(LogSource, "duel ring placed opponent=" + opponent.Index);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "[WARN] duel ring placement failed: " + ex.Message);
		}
	}

	/// <summary>The duel needs a hostile team; create the defender team once and keep it peaceful until the duel arms it.</summary>
	private void EnsureDuelEnemyTeam()
	{
		if (Mission.PlayerEnemyTeam != null && Mission.PlayerEnemyTeam != _team)
		{
			return;
		}
		Team enemy = Mission.DefenderTeam;
		if (enemy == null || enemy == _team)
		{
			enemy = Mission.Teams.Add(BattleSideEnum.Defender, 0xFF7A2020u, 0xFF2A0808u, null, false, false);
		}
		// A PlayerEnemy team only exists while the two sides are hostile.
		_team.SetIsEnemyOf(enemy, true);
		enemy.SetIsEnemyOf(_team, true);
	}

	/// <summary>Takes a camp agent out of camp life: no pose, no scripted hold.</summary>
	private void HoldDuelist(Agent agent)
	{
		for (int i = 0; i < _agents.Count; i++)
		{
			if (_agents[i].Agent == agent)
			{
				ReleasePose(_agents[i]);
				break;
			}
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
	}

	private void Teleport(Agent agent, Vec2 local, Vec2 facingLocal)
	{
		Vec3 pos = ToWorld(local);
		agent.TeleportToPosition(pos);
		Vec2 dir = ToWorldDirection(facingLocal);
		agent.LookDirection = new Vec3(dir.x, dir.y, 0f);
		agent.Controller = agent == Agent.Main ? AgentControllerType.Player : agent.Controller;
	}

	private bool IsDuelist(Agent agent) => _duelists.Count > 0 && agent != null && _duelists.Contains(agent);

	/// <summary>Once a second: when the duel is over, survivors go back to their camp posts and the defender team calms down.</summary>
	private void TickDuelRelease(float dt)
	{
		if (_duelists.Count == 0)
		{
			return;
		}
		_duelReleaseCheck -= dt;
		if (_duelReleaseCheck > 0f)
		{
			return;
		}
		_duelReleaseCheck = 1f;
		if (DuelBehavior.IsFormalDuelActive || DuelBehavior.IsFormalDuelPreFightActive)
		{
			return;
		}
		foreach (Agent agent in _duelists)
		{
			if (agent == null || !agent.IsActive() || agent == Agent.Main)
			{
				continue;
			}
			for (int i = 0; i < _agents.Count; i++)
			{
				if (_agents[i].Agent == agent)
				{
					_agents[i].Target = _agents[i].Home;
					_agents[i].TargetFacing = _agents[i].HomeFacing;
					MoveTo(_agents[i], _agents[i].Home, _agents[i].HomeFacing, run: false);
					break;
				}
			}
		}
		_duelists.Clear();
		CalmTeams();
		Logger.Log(LogSource, "duel ring released");
	}

	private void CalmTeams()
	{
		try
		{
			foreach (Team team in Mission.Teams)
			{
				if (team != null && team != _team && _team.IsEnemyOf(team))
				{
					_team.SetIsEnemyOf(team, false);
					team.SetIsEnemyOf(_team, false);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "[WARN] team calm failed: " + ex.Message);
		}
	}

	// ---------- execution ground ----------

	/// <summary>Where a prisoner kneels for an execution order (camp local).</summary>
	internal bool TryGetExecutionSpot(out Vec3 world, out Vec2 facing)
	{
		world = Vec3.Zero;
		facing = Vec2.Forward;
		if (_layout == null || !_layout.HasExecutionGround)
		{
			return false;
		}
		world = ToWorld(_layout.ExecutionCenter + _layout.ExecutionFacing * 0.9f);
		facing = ToWorldDirection(_layout.ExecutionFacing);
		return true;
	}

}
