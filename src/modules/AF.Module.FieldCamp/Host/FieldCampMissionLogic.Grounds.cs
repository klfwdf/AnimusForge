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
/// - The execution centre is logical only; the B-key builder owns confirmed facilities.
///   Frequency: everything here runs once per duel / once per camp mission.
/// </summary>
public sealed partial class FieldCampMissionLogic
{

	private readonly HashSet<Agent> _duelists = new HashSet<Agent>();
	private float _duelReleaseCheck;
    private Team _campDuelPlayerTeam;
    private Team _campDuelOpponentTeam;

    internal void GetDuelTeams(out Team playerTeam, out Team opponentTeam)
    {
        if (_campDuelPlayerTeam == null)
            _campDuelPlayerTeam = Mission.Teams.Add(BattleSideEnum.Attacker, _team.Color, _team.Color2, _team.Banner, true, false);
        if (_campDuelOpponentTeam == null)
            _campDuelOpponentTeam = Mission.Teams.Add(BattleSideEnum.Defender, _team.Color, _team.Color2, _team.Banner, false, false);
        playerTeam = _campDuelPlayerTeam;
        opponentTeam = _campDuelOpponentTeam;
    }

	/// <summary>
	/// Called by the duel start, before the duel teams are set: both fighters are
	/// placed on validated opposite sides of the ring. DuelBehavior owns team isolation.
	/// </summary>
	internal bool PlaceDuelists(Agent player, Agent opponent)
	{
		if (player == null || opponent == null || !player.IsActive() || !opponent.IsActive())
		{
			return false;
		}
		try
		{
            // Prove both start positions and their connecting route before moving either actor.
            Vec3 playerPosition = Vec3.Invalid, opponentPosition = Vec3.Invalid;
            Vec2 inward = Vec2.Forward;
            bool placed = false;
            for (int trial = 0; trial < 8; trial++)
            {
                float angle = trial * MathF.PI / 4f;
                inward = new Vec2(MathF.Cos(angle), MathF.Sin(angle));
                float radius = _layout.RingRadius * 0.55f;
                if (!FieldCampSite.TryResolveGroundedPosition(Mission.Scene, ToWorld(_layout.RingCenter + inward * radius), 0.4f, out playerPosition)
                    || !FieldCampSite.TryResolveGroundedPosition(Mission.Scene, ToWorld(_layout.RingCenter - inward * radius), 0.4f, out opponentPosition)
                    || !IsCampPositionAvailable(player, playerPosition)
                    || !IsCampPositionAvailable(opponent, opponentPosition)
                    || !TryCampRoute(playerPosition, opponentPosition, out _)) continue;
                placed = true;
                break;
            }
            if (!placed) return false;
			// Captive combat isolates only the player. Rejoin the camp before DuelBehavior
			// snapshots the original teams, then isolates both duelists.
			// Camp upkeep resumes captive isolation after the duel releases both actors.
			if (player == Mission.MainAgent && _prisonerPlayerTeam != null && player.Team == _prisonerPlayerTeam)
			{
				player.SetTeam(_team, true);
			}
            HoldDuelist(opponent);
            player.TeleportToPosition(playerPosition);
            opponent.TeleportToPosition(opponentPosition);
            Vec2 facing = ToWorldDirection(inward);
            player.LookDirection = new Vec3(-facing.x, -facing.y, 0f);
            opponent.LookDirection = new Vec3(facing.x, facing.y, 0f);
			_duelists.Add(player);
			_duelists.Add(opponent);
			Logger.Log(LogSource, "duel ring placed opponent=" + opponent.Index);
            return true;
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "[WARN] duel ring placement failed: " + ex.Message);
            return false;
		}
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
            StopCampNavigation(agent);
            agent.SetIsAIPaused(false);
		}
		catch
		{
		}
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
