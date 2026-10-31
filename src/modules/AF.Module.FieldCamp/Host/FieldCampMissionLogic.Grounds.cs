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
/// - With prisoners, an execution ground is laid out; the player's own Vengeance site
///   preset for beheading (if any) replaces the default block and gallows at that spot.
///   Frequency: everything here runs once per duel / once per camp mission.
/// </summary>
public sealed partial class FieldCampMissionLogic
{
	private const string ExecutionPresetMethod = "beheading";

	private readonly HashSet<Agent> _duelists = new HashSet<Agent>();
	private readonly List<GameEntity> _executionPieces = new List<GameEntity>();
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

	/// <summary>
	/// Spawns the pieces of the player's newest saved beheading site preset at the
	/// execution ground (preset local x = right, y = forward, as in Vengeance). Returns
	/// false when there is no usable user preset, so the default block and gallows stay.
	/// </summary>
	private bool TryPlaceExecutionPreset()
	{
		if (!_layout.HasExecutionGround)
		{
			return false;
		}
		RichExecutions.Customization.ExecutionSitePreset preset = null;
		try
		{
			IReadOnlyList<RichExecutions.Customization.ExecutionSitePresetLoadResult> all = RichExecutions.Customization.ExecutionSitePresetStore.LoadAll(ExecutionPresetMethod);
			for (int i = all.Count - 1; i >= 0 && preset == null; i--)
			{
				if (all[i].IsValid && !all[i].Path.StartsWith("<built-in>", StringComparison.Ordinal))
				{
					preset = all[i].Preset;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "[WARN] execution preset load failed: " + ex.Message);
			return false;
		}
		if (preset == null)
		{
			return false;
		}
		Vec2 forward = ToWorldDirection(_layout.ExecutionFacing);
		Vec2 right = new Vec2(forward.y, -forward.x);
		Vec3 origin = ToWorld(_layout.ExecutionCenter);
		int placed = 0;
		foreach (RichExecutions.Customization.ExecutionSitePiece piece in preset.Pieces)
		{
			if (piece == null || piece.ResourceType != RichExecutions.Customization.ExecutionSiteResourceType.Prefab || string.IsNullOrWhiteSpace(piece.ResourceKey)
				|| RichExecutions.Customization.ExecutionSitePresetStore.IsObsoletePiece(piece) || !GameEntity.PrefabExists(piece.ResourceKey))
			{
				continue;
			}
			RichExecutions.Customization.ExecutionPresetTransform tr = piece.Transform;
			Vec2 flat = origin.AsVec2 + right * tr.X + forward * tr.Y;
			Vec3 pos = new Vec3(flat.x, flat.y, FieldCampProps.LowestTerrain(Mission.Scene, flat, 0.4f) + tr.Z);
			MatrixFrame frame = new MatrixFrame(Mat3.CreateMat3WithForward(new Vec3(forward.x, forward.y, 0f)), pos);
			const float deg = MathF.PI / 180f;
			frame.rotation.RotateAboutUp(tr.Yaw * deg);
			frame.rotation.RotateAboutSide(tr.Pitch * deg);
			frame.rotation.RotateAboutForward(tr.Roll * deg);
			if (Math.Abs(tr.Scale - 1f) > 0.001f)
			{
				frame.rotation.ApplyScaleLocal(Math.Max(0.1f, Math.Min(5f, tr.Scale)));
			}
			try
			{
				GameEntity entity = GameEntity.Instantiate(Mission.Scene, piece.ResourceKey, frame, false);
				if (entity != null)
				{
					_executionPieces.Add(entity);
					placed++;
				}
			}
			catch
			{
			}
		}
		Logger.Log(LogSource, "execution preset '" + preset.Name + "' placed pieces=" + placed);
		return placed > 0;
	}

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

	private void CleanupExecution(string reason)
	{
		for (int i = 0; i < _executionPieces.Count; i++)
		{
			try
			{
				_executionPieces[i]?.Remove(81);
			}
			catch
			{
			}
		}
		_executionPieces.Clear();
	}
}
