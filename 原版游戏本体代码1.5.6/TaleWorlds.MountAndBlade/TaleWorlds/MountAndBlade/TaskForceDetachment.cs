using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TaskForceDetachment : IDetachment
{
	private readonly MBList<Agent> _agents;

	private readonly MBList<Agent> _tempAgentList;

	private readonly MBList<Formation> _userFormations;

	private Agent _attackedAgent;

	public MBReadOnlyList<Formation> UserFormations => _userFormations;

	public bool IsLoose => true;

	public int CountOfAgents { get; private set; }

	public Agent TargetAgent { get; }

	public TaskForceDetachment(Agent attackedAgent, Agent targetAgent)
	{
		_attackedAgent = attackedAgent;
		TargetAgent = targetAgent;
		_userFormations = new MBList<Formation>();
		_agents = new MBList<Agent>();
		_tempAgentList = new MBList<Agent>();
		Mission.Current.GetNearbyAllyAgents(attackedAgent.Position.AsVec2, 5f, attackedAgent.Team, _tempAgentList);
		int num = 0;
		foreach (Agent tempAgent in _tempAgentList)
		{
			if (tempAgent.IsDetachableFromFormation && !tempAgent.IsDetachedFromFormation && tempAgent.Formation == attackedAgent.Formation)
			{
				if (num > 4)
				{
					break;
				}
				AddAgentAtSlotIndex(tempAgent, num);
				num++;
			}
		}
	}

	public void AddAgent(Agent agent, int slotIndex, Agent.AIScriptedFrameFlags customFlags = Agent.AIScriptedFrameFlags.None)
	{
		_agents[slotIndex] = agent;
		CountOfAgents++;
	}

	public void AddAgentAtSlotIndex(Agent agent, int slotIndex)
	{
		if (_agents.Count <= slotIndex)
		{
			_agents.Add(agent);
		}
		else
		{
			_agents[slotIndex] = agent;
		}
		CountOfAgents++;
		agent.Formation?.DetachUnit(agent, isLoose: true);
		agent.Detachment = this;
		agent.SetDetachmentWeight(1f);
		agent.SetDetachmentIndex(slotIndex);
		agent.SetFormationFrameDisabled();
		agent.SetAutomaticTargetSelection(enable: false);
		agent.SetTargetAgent(TargetAgent);
	}

	public void AddReinforcementAgent(Agent agent)
	{
		bool flag = false;
		int i;
		for (i = 0; i < _agents.Count; i++)
		{
			if (_agents[i] == null)
			{
				AddAgentAtSlotIndex(agent, i);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			AddAgentAtSlotIndex(agent, i);
		}
	}

	void IDetachment.FormationStartUsing(Formation formation)
	{
		_userFormations.Add(formation);
	}

	void IDetachment.FormationStopUsing(Formation formation)
	{
		_userFormations.Remove(formation);
	}

	public bool IsUsedByFormation(Formation formation)
	{
		return _userFormations.Contains(formation);
	}

	Agent IDetachment.GetMovingAgentAtSlotIndex(int slotIndex)
	{
		if (slotIndex >= _agents.Count)
		{
			return null;
		}
		return _agents[slotIndex];
	}

	void IDetachment.GetSlotIndexWeightTuples(List<(int, float)> slotIndexWeightTuples)
	{
	}

	bool IDetachment.IsSlotAtIndexAvailableForAgent(int slotIndex, Agent agent)
	{
		return false;
	}

	bool IDetachment.IsAgentEligible(Agent agent)
	{
		return agent.Detachment == this;
	}

	void IDetachment.UnmarkDetachment()
	{
	}

	bool IDetachment.IsDetachmentRecentlyEvaluated()
	{
		return true;
	}

	void IDetachment.MarkSlotAtIndex(int slotIndex)
	{
	}

	bool IDetachment.IsAgentUsingOrInterested(Agent agent)
	{
		if (agent.DetachmentIndex >= 0 && agent.DetachmentIndex < _agents.Count)
		{
			return _agents[agent.DetachmentIndex] == agent;
		}
		return false;
	}

	void IDetachment.OnFormationLeave(Formation formation)
	{
		for (int num = _agents.Count - 1; num >= 0; num--)
		{
			Agent agent = _agents[num];
			if (agent != null && agent.Formation == formation && !agent.IsPlayerControlled)
			{
				_agents[num] = null;
				CountOfAgents--;
				agent.DisableScriptedMovement();
				agent.DisableScriptedCombatMovement();
				formation.AttachUnit(agent);
			}
		}
	}

	public bool IsStandingPointAvailableForAgent(Agent agent)
	{
		return false;
	}

	public List<float> GetTemplateCostsOfAgent(Agent candidate, List<float> oldValue)
	{
		return oldValue;
	}

	float IDetachment.GetExactCostOfAgentAtSlot(Agent candidate, int slotIndex)
	{
		return float.MaxValue;
	}

	public float GetTemplateWeightOfAgent(Agent candidate)
	{
		return float.MaxValue;
	}

	public float? GetWeightOfAgentAtNextSlot(List<Agent> newAgents, out Agent match)
	{
		match = null;
		return null;
	}

	public float? GetWeightOfAgentAtNextSlot(List<(Agent, float)> agentTemplateScores, out Agent match)
	{
		match = null;
		return null;
	}

	public float? GetWeightOfAgentAtOccupiedSlot(Agent detachedAgent, List<Agent> newAgents, out Agent match)
	{
		match = null;
		return float.MaxValue;
	}

	public void RemoveAgent(Agent agent)
	{
		if (agent == _attackedAgent)
		{
			foreach (Agent agent2 in _agents)
			{
				if (agent2 != _attackedAgent)
				{
					_attackedAgent = agent2;
					break;
				}
			}
		}
		_agents[agent.DetachmentIndex] = null;
		CountOfAgents--;
		agent.DisableScriptedMovement();
		agent.DisableScriptedCombatMovement();
		agent.SetAutomaticTargetSelection(enable: true);
	}

	public int GetNumberOfUsableSlots()
	{
		return 0;
	}

	public bool CalculateShouldBeDisbanded()
	{
		if (!TargetAgent.IsActive())
		{
			return true;
		}
		int num = 0;
		foreach (Agent agent in _agents)
		{
			if (agent != null && agent.IsActive())
			{
				num++;
			}
		}
		if (num <= 0 || _attackedAgent?.Team == null || TargetAgent?.Formation?.Team == null || _tempAgentList == null)
		{
			return true;
		}
		float num2 = TargetAgent.Position.DistanceSquared(_attackedAgent.Position);
		if (TargetAgent.Formation != null && (TargetAgent.Formation.CountOfUnits > CountOfAgents || (float)TargetAgent.Formation.CountOfUnits > (float)_userFormations[0].CountOfUnits * 0.5f) && num2 > TargetAgent.Position.AsVec2.DistanceSquared(TargetAgent.Formation.CachedAveragePosition) * 0.36f)
		{
			return true;
		}
		if (num2 > TargetAgent.Position.AsVec2.DistanceSquared(TargetAgent.Team.QuerySystem.AveragePosition) * 0.36f)
		{
			return true;
		}
		Mission.Current.GetNearbyEnemyAgents(TargetAgent.Position.AsVec2, 20f, _attackedAgent.Team, _tempAgentList);
		return _tempAgentList.Count > num;
	}

	public WorldFrame? GetAgentFrame(Agent agent)
	{
		return null;
	}

	public float? GetWeightOfNextSlot(BattleSideEnum side)
	{
		return null;
	}

	public float GetWeightOfOccupiedSlot(Agent agent)
	{
		return float.MinValue;
	}

	float IDetachment.GetDetachmentWeight(BattleSideEnum side)
	{
		return float.MinValue;
	}

	void IDetachment.ResetEvaluation()
	{
	}

	bool IDetachment.IsEvaluated()
	{
		return true;
	}

	void IDetachment.SetAsEvaluated()
	{
	}

	float IDetachment.GetDetachmentWeightFromCache()
	{
		return float.MinValue;
	}

	float IDetachment.ComputeAndCacheDetachmentWeight(BattleSideEnum side)
	{
		return float.MinValue;
	}
}
