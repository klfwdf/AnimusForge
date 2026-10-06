using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class CasualtyHandler : MissionLogic
{
	private readonly Dictionary<Formation, int> _casualtyCounts = new Dictionary<Formation, int>();

	private readonly Dictionary<Formation, float> _powerLoss = new Dictionary<Formation, float>();

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		RegisterCasualty(affectedAgent);
	}

	public override void OnAgentFleeing(Agent affectedAgent)
	{
		RegisterCasualty(affectedAgent);
	}

	public int GetCasualtyCountOfFormation(Formation formation)
	{
		if (!_casualtyCounts.TryGetValue(formation, out var value))
		{
			value = 0;
			_casualtyCounts[formation] = 0;
		}
		return value;
	}

	public float GetCasualtyPowerLossOfFormation(Formation formation)
	{
		if (!_powerLoss.TryGetValue(formation, out var value))
		{
			value = 0f;
			_powerLoss[formation] = 0f;
		}
		return value;
	}

	private void RegisterCasualty(Agent agent)
	{
		Formation formation = agent.Formation;
		if (formation != null)
		{
			if (_casualtyCounts.ContainsKey(formation))
			{
				_casualtyCounts[formation]++;
			}
			else
			{
				_casualtyCounts[formation] = 1;
			}
			if (_powerLoss.ContainsKey(formation))
			{
				_powerLoss[formation] += agent.Character.GetPower();
			}
			else
			{
				_powerLoss[formation] = agent.Character.GetPower();
			}
		}
	}
}
