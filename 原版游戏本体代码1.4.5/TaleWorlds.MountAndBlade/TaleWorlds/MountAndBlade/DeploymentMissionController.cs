using System;
using System.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class DeploymentMissionController : MissionLogic
{
	protected readonly bool IsPlayerAttacker;

	protected bool IsPlayerControllerSetToNone;

	protected BattleSideEnum PlayerSide;

	protected BattleSideEnum EnemySide;

	protected bool AfterSetupTeamsCalled;

	public bool TeamSetupOver { get; private set; }

	public event Action OnAfterSetupTeams;

	public DeploymentMissionController(bool isPlayerAttacker)
	{
		IsPlayerAttacker = isPlayerAttacker;
		PlayerSide = (IsPlayerAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		EnemySide = ((!IsPlayerAttacker) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
	}

	public override void AfterStart()
	{
		base.Mission.AllowAiTicking = false;
		OnAfterStart();
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		base.Mission.AreOrderGesturesEnabled_AdditionalCondition += AreOrderGesturesEnabled_AdditionalCondition;
	}

	public void FinishDeployment()
	{
		BeforeDeploymentFinished();
		if (IsPlayerAttacker)
		{
			UnhideAgentsOfSide(BattleSideEnum.Defender);
		}
		Mission.Current.OnDeploymentFinished();
		foreach (Team team in base.Mission.Teams)
		{
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits <= 0)
				{
					continue;
				}
				item.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					if (agent.IsAIControlled)
					{
						agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
						agent.SetIsAIPaused(isPaused: false);
						if (agent.GetAgentFlags().HasAnyFlag(AgentFlag.CanWieldWeapon))
						{
							agent.ResetEnemyCaches();
						}
						agent.HumanAIComponent?.SyncBehaviorParamsIfNecessary();
					}
				});
			}
		}
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent != null)
		{
			mainAgent.SetDetachableFromFormation(value: true);
			mainAgent.Controller = AgentControllerType.Player;
		}
		base.Mission.AllowAiTicking = true;
		base.Mission.DisableDying = false;
		base.Mission.SetFallAvoidSystemActive(fallAvoidActive: false);
		Mission.Current.OnAfterDeploymentFinished();
		AfterDeploymentFinished();
		base.Mission.RemoveMissionBehavior(this);
	}

	public override void OnAgentControllerSetToPlayer(Agent agent)
	{
		if (!IsPlayerControllerSetToNone)
		{
			agent.Controller = AgentControllerType.None;
			agent.SetIsAIPaused(isPaused: true);
			agent.SetDetachableFromFormation(value: false);
			IsPlayerControllerSetToNone = true;
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (!TeamSetupOver && base.Mission.Scene != null)
		{
			SetupTeams();
			TeamSetupOver = true;
		}
		if (TeamSetupOver && !AfterSetupTeamsCalled)
		{
			this.OnAfterSetupTeams?.Invoke();
			AfterSetupTeamsCalled = true;
		}
	}

	protected void SetupAgentAIStatesForSide(BattleSideEnum battleSide)
	{
		foreach (Team item in Mission.GetTeamsOfSide(battleSide))
		{
			foreach (Formation item2 in item.FormationsIncludingSpecialAndEmpty)
			{
				if (item2.CountOfUnits <= 0)
				{
					continue;
				}
				item2.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					if (agent.IsAIControlled)
					{
						agent.SetAlarmState(Agent.AIStateFlag.None);
						agent.SetIsAIPaused(isPaused: true);
					}
				});
			}
		}
	}

	protected abstract void OnAfterStart();

	protected abstract void OnSetupTeamsOfSide(BattleSideEnum side);

	protected abstract void OnSetupTeamsFinished();

	protected abstract void BeforeDeploymentFinished();

	protected abstract void AfterDeploymentFinished();

	protected virtual void SetupAIOfEnemySide(BattleSideEnum enemySide)
	{
		Team team = ((enemySide == BattleSideEnum.Attacker) ? base.Mission.AttackerTeam : base.Mission.DefenderTeam);
		SetupAIOfEnemyTeam(team);
		Team team2 = ((enemySide == BattleSideEnum.Attacker) ? base.Mission.AttackerAllyTeam : base.Mission.DefenderAllyTeam);
		if (team2 != null)
		{
			SetupAIOfEnemyTeam(team2);
		}
	}

	protected virtual void SetupAIOfEnemyTeam(Team team)
	{
		foreach (Formation item in team.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.SetControlledByAI(isControlledByAI: true);
			}
		}
		team.QuerySystem.Expire();
		base.Mission.AllowAiTicking = true;
		base.Mission.ForceTickOccasionally = true;
		team.ResetTactic();
		bool isTeleportingAgents = Mission.Current.IsTeleportingAgents;
		base.Mission.IsTeleportingAgents = true;
		team.Tick(0f);
		base.Mission.IsTeleportingAgents = isTeleportingAgents;
		base.Mission.AllowAiTicking = false;
		base.Mission.ForceTickOccasionally = false;
	}

	private void SetupTeams()
	{
		Utilities.SetLoadingScreenPercentage(0.92f);
		base.Mission.DisableDying = true;
		base.Mission.SetFallAvoidSystemActive(fallAvoidActive: true);
		OnSetupTeamsOfSide(EnemySide);
		SetupAIOfEnemySide(EnemySide);
		if (IsPlayerAttacker)
		{
			HideAgentsOfSide(BattleSideEnum.Defender);
		}
		OnSetupTeamsOfSide(PlayerSide);
		OnSetupTeamsFinished();
		base.Mission.AreOrderGesturesEnabled_AdditionalCondition -= AreOrderGesturesEnabled_AdditionalCondition;
		Utilities.SetLoadingScreenPercentage(0.96f);
		if (!MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
		{
			FinishDeployment();
		}
	}

	private void HideAgentsOfSide(BattleSideEnum side)
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsHuman && agent.Team != null && agent.Team.Side == side)
			{
				agent.SetRenderCheckEnabled(value: false);
				agent.AgentVisuals.SetVisible(value: false);
				agent.MountAgent?.SetRenderCheckEnabled(value: false);
				agent.MountAgent?.AgentVisuals.SetVisible(value: false);
			}
		}
	}

	private void UnhideAgentsOfSide(BattleSideEnum side)
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsHuman && agent.Team != null && agent.Team.Side == side)
			{
				agent.SetRenderCheckEnabled(value: true);
				agent.AgentVisuals.SetVisible(value: true);
				agent.MountAgent?.SetRenderCheckEnabled(value: true);
				agent.MountAgent?.AgentVisuals.SetVisible(value: true);
			}
		}
	}

	private bool AreOrderGesturesEnabled_AdditionalCondition()
	{
		return false;
	}

	[Conditional("DEBUG")]
	private void DebugTick()
	{
		if (Input.DebugInput.IsHotKeyPressed("SwapToEnemy"))
		{
			base.Mission.MainAgent.Controller = AgentControllerType.AI;
			base.Mission.PlayerEnemyTeam.Leader.Controller = AgentControllerType.Player;
			SwapTeams();
		}
	}

	private void SwapTeams()
	{
		base.Mission.PlayerTeam = base.Mission.PlayerEnemyTeam;
	}
}
