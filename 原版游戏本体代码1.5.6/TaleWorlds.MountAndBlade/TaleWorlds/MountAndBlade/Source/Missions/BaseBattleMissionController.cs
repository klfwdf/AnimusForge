using System.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public abstract class BaseBattleMissionController : MissionLogic
{
	protected readonly Game game;

	protected bool IsPlayerAttacker { get; private set; }

	protected int DeployedAttackerTroopCount { get; private set; }

	protected int DeployedDefenderTroopCount { get; private set; }

	protected BaseBattleMissionController(bool isPlayerAttacker)
	{
		IsPlayerAttacker = isPlayerAttacker;
		game = Game.Current;
	}

	public override void EarlyStart()
	{
		EarlyStart();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		CreateTeams();
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
	}

	protected virtual void SetupTeam(Team team)
	{
		if (team.Side == BattleSideEnum.Attacker)
		{
			CreateAttackerTroops();
		}
		else
		{
			CreateDefenderTroops();
		}
		if (team == base.Mission.PlayerTeam)
		{
			CreatePlayer();
		}
	}

	protected abstract void CreateDefenderTroops();

	protected abstract void CreateAttackerTroops();

	public virtual TeamAIComponent GetTeamAI(Team team, float thinkTimerTime = 5f, float applyTimerTime = 1f)
	{
		return new TeamAIGeneral(base.Mission, team, thinkTimerTime, applyTimerTime);
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
	}

	[Conditional("DEBUG")]
	private void DebugTick()
	{
		if (Input.DebugInput.IsHotKeyPressed("SwapToEnemy"))
		{
			BecomeEnemy();
		}
		if (Input.DebugInput.IsHotKeyDown("BaseBattleMissionControllerHotkeyBecomePlayer"))
		{
			BecomePlayer();
		}
	}

	protected bool IsPlayerDead()
	{
		if (base.Mission.MainAgent != null)
		{
			return !base.Mission.MainAgent.IsActive();
		}
		return true;
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		if (!base.Mission.IsDeploymentFinished)
		{
			return false;
		}
		if (IsPlayerDead())
		{
			missionResult = MissionResult.CreateDefeated(base.Mission);
			return true;
		}
		if (base.Mission.GetMemberCountOfSide(BattleSideEnum.Attacker) == 0)
		{
			missionResult = ((base.Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateDefeated(base.Mission) : MissionResult.CreateSuccessful(base.Mission));
			return true;
		}
		if (base.Mission.GetMemberCountOfSide(BattleSideEnum.Defender) == 0)
		{
			missionResult = ((base.Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateSuccessful(base.Mission) : MissionResult.CreateDefeated(base.Mission));
			return true;
		}
		return false;
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (!IsPlayerDead() && base.Mission.IsPlayerCloseToAnEnemy())
		{
			canPlayerLeave = false;
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat"));
		}
		else
		{
			MissionResult missionResult = null;
			if (!IsPlayerDead() && !MissionEnded(ref missionResult))
			{
				return new InquiryData("", GameTexts.FindText("str_retreat_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), base.Mission.OnEndMissionResult, null);
			}
		}
		return null;
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
	}

	private void CreateTeams()
	{
		if (!base.Mission.Teams.IsEmpty())
		{
			throw new MBIllegalValueException("Number of teams is not 0.");
		}
		base.Mission.Teams.Add(BattleSideEnum.Defender, 4278190335u, 4278190335u);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, 4278255360u, 4278255360u);
		if (IsPlayerAttacker)
		{
			base.Mission.PlayerTeam = base.Mission.AttackerTeam;
		}
		else
		{
			base.Mission.PlayerTeam = base.Mission.DefenderTeam;
		}
		TeamAIComponent teamAI = GetTeamAI(base.Mission.DefenderTeam);
		base.Mission.DefenderTeam.AddTeamAI(teamAI);
		TeamAIComponent teamAI2 = GetTeamAI(base.Mission.AttackerTeam);
		base.Mission.AttackerTeam.AddTeamAI(teamAI2);
	}

	protected void IncrementDeploymedTroops(BattleSideEnum side)
	{
		if (side == BattleSideEnum.Attacker)
		{
			DeployedAttackerTroopCount++;
		}
		else
		{
			DeployedDefenderTroopCount++;
		}
	}

	protected virtual void CreatePlayer()
	{
		game.PlayerTroop = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("main_hero");
		FormationClass formationClass = base.Mission.GetFormationSpawnClass(base.Mission.PlayerTeam, FormationClass.NumberOfRegularFormations);
		if (formationClass != FormationClass.NumberOfRegularFormations)
		{
			formationClass = game.PlayerTroop.DefaultFormationClass;
		}
		base.Mission.GetFormationSpawnFrame(base.Mission.PlayerTeam, formationClass, isReinforcement: false, out var spawnPosition, out var spawnDirection);
		Agent agent = base.Mission.SpawnAgent(new AgentBuildData(game.PlayerTroop).Team(base.Mission.PlayerTeam).InitialPosition(spawnPosition.GetGroundVec3()).InitialDirection(in spawnDirection)
			.Controller(AgentControllerType.Player));
		agent.WieldInitialWeapons();
		base.Mission.MainAgent = agent;
	}

	protected void BecomeEnemy()
	{
		base.Mission.MainAgent.Controller = AgentControllerType.AI;
		base.Mission.PlayerEnemyTeam.Leader.Controller = AgentControllerType.Player;
		SwapTeams();
	}

	protected void BecomePlayer()
	{
		base.Mission.MainAgent.Controller = AgentControllerType.Player;
		base.Mission.PlayerEnemyTeam.Leader.Controller = AgentControllerType.AI;
		SwapTeams();
	}

	protected void SwapTeams()
	{
		base.Mission.PlayerTeam = base.Mission.PlayerEnemyTeam;
		IsPlayerAttacker = !IsPlayerAttacker;
	}
}
