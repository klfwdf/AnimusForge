using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects;

public class StealthZone
{
	public delegate void StealthZoneEvent();

	public const string VolumeBoxId = "stealth_zone_volume";

	public bool IsZoneUsable;

	public StealthZoneEvent OnTargetFlees;

	public StealthZoneEvent OnTargetEliminated;

	public StealthZoneEvent OnTargetInZone;

	public Agent TargetAgent;

	public bool AreAgentsActive { get; private set; }

	public bool UseVolumeBox { get; private set; }

	public int EliminatedAgents { get; private set; }

	private Timer ActivationTimer { get; set; }

	private Timer DeactivationTimer { get; set; }

	private Timer ForceTargetTimer { get; set; }

	public List<Agent> Agents { get; private set; }

	public VolumeBox VolumeBox { get; private set; }

	public event Action OnActivated;

	public event Action OnDisactivated;

	public StealthZone(Agent targetAgent, bool useVolumeBox)
	{
		TargetAgent = targetAgent;
		EliminatedAgents = 0;
		AreAgentsActive = false;
		UseVolumeBox = useVolumeBox;
		if (UseVolumeBox)
		{
			VolumeBox = Mission.Current.Scene.FindEntityWithTag("stealth_zone_volume")?.GetFirstScriptOfType<VolumeBox>();
		}
		IsZoneUsable = false;
	}

	public void SetStealthAgents(List<Agent> agents)
	{
		Agents = agents;
	}

	public void Tick()
	{
		bool flag = TargetAgent != null && TargetAgent.IsActive() && (!UseVolumeBox || VolumeBox.IsPointIn(TargetAgent.Position));
		if (IsZoneUsable)
		{
			if (!AreAgentsActive && flag && ActivationTimer == null)
			{
				ActivationTimer = new Timer(Mission.Current.CurrentTime, 2f);
			}
			if (AreAgentsActive && !flag && DeactivationTimer == null)
			{
				DeactivationTimer = new Timer(Mission.Current.CurrentTime, 2f);
			}
			if (!flag)
			{
				ActivationTimer = null;
			}
			else
			{
				DeactivationTimer = null;
			}
			if (AreAgentsActive && !flag && DeactivationTimer.Check(Mission.Current.CurrentTime))
			{
				DeactivateStealthZone();
			}
			if (flag && !AreAgentsActive && ActivationTimer.Check(Mission.Current.CurrentTime) && Agents != null && Agents.Count > 0)
			{
				ActivateStealthZone();
			}
		}
		else
		{
			if (flag && ForceTargetTimer == null)
			{
				ForceTargetTimer = new Timer(Mission.Current.CurrentTime, 5f);
			}
			else if (!flag)
			{
				ForceTargetTimer = null;
			}
			if (flag && ForceTargetTimer.Check(Mission.Current.CurrentTime))
			{
				OnTargetInZone?.Invoke();
				ForceTargetTimer.Reset(Mission.Current.CurrentTime);
			}
		}
	}

	private void ActivateStealthZone()
	{
		AreAgentsActive = true;
		ActivationTimer = null;
		DeactivationTimer = null;
		ActivateStealthZoneInternal();
		this.OnActivated();
	}

	private void DeactivateStealthZone()
	{
		AreAgentsActive = false;
		ActivationTimer = null;
		DeactivationTimer = null;
		DeactivateStealthZoneInternal();
		this.OnDisactivated();
	}

	private void ActivateStealthZoneInternal()
	{
		Mission.Current.SetMissionMode(MissionMode.Stealth, atStart: false);
		Mission.Current.IsInventoryAccessible = false;
		Mission.Current.IsQuestScreenAccessible = false;
		EliminatedAgents = 0;
		foreach (Agent agent in Agents)
		{
			if (agent.IsActive())
			{
				agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanGetAlarmed);
				SetStealthMode(agent, isActive: true);
				agent.SetTeam(Mission.Current.PlayerEnemyTeam, sync: true);
			}
		}
	}

	private void DeactivateStealthZoneInternal()
	{
		Mission.Current.SetMissionMode(MissionMode.StartUp, atStart: false);
		Mission.Current.IsInventoryAccessible = true;
		Mission.Current.IsQuestScreenAccessible = true;
		foreach (Agent agent in Agents)
		{
			if (agent.IsActive())
			{
				agent.SetAgentFlags((AgentFlag)((uint)agent.GetAgentFlags() & 0xFFFEFFFFu));
				SetStealthMode(agent, isActive: false);
				agent.Health = agent.HealthLimit;
				agent.SetTeam(Team.Invalid, sync: true);
			}
		}
		EliminatedAgents = 0;
	}

	public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent)
	{
		if (affectedAgent.IsMainAgent && Agents != null && Agents.Contains(affectorAgent))
		{
			OnTargetEliminated?.Invoke();
		}
		else if (affectedAgent.IsAIControlled && affectedAgent.Team == Mission.Current.PlayerEnemyTeam && Agents.Contains(affectedAgent))
		{
			EliminatedAgents++;
			Agents.Remove(affectedAgent);
		}
	}

	public bool IsAgentInside(Agent agent)
	{
		return VolumeBox?.IsPointIn(agent.Position) ?? false;
	}

	public void OnPlayerFlees()
	{
		if (IsAnyoneAlarmed() || IsAgentInside(TargetAgent))
		{
			OnTargetFlees?.Invoke();
		}
	}

	private void SetStealthMode(Agent agent, bool isActive)
	{
		AlarmedBehaviorGroup alarmedBehaviorGroup = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		if (alarmedBehaviorGroup != null)
		{
			alarmedBehaviorGroup.DoNotCheckForAlarmFactorIncrease = !isActive;
			alarmedBehaviorGroup.ResetAlarmFactor();
			if (isActive)
			{
				alarmedBehaviorGroup.DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy = false;
			}
			alarmedBehaviorGroup.IsActive = isActive;
			agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().IsActive = !isActive;
		}
	}

	public void ResetEvents()
	{
		OnTargetInZone = null;
		OnTargetFlees = null;
		OnTargetEliminated = null;
	}

	public void DisableAll()
	{
		SetStealthAgents(null);
		ResetEvents();
		IsZoneUsable = false;
		AreAgentsActive = false;
		TargetAgent = null;
	}

	private bool IsAnyoneAlarmed()
	{
		if (Agents != null)
		{
			return Agents.Any((Agent x) => x.IsAlarmed());
		}
		return false;
	}
}
