using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public class MissionAgentPanicHandler : MissionLogic
{
	private readonly List<Agent> _panickedAgents;

	private readonly List<Formation> _panickedFormations;

	private readonly List<Team> _panickedTeams;

	public MissionAgentPanicHandler()
	{
		_panickedAgents = new List<Agent>(256);
		_panickedFormations = new List<Formation>(24);
		_panickedTeams = new List<Team>(2);
	}

	public override void OnAgentPanicked(Agent agent)
	{
		_panickedAgents.Add(agent);
		if (agent.Formation != null && agent.Team != null)
		{
			if (!_panickedFormations.Contains(agent.Formation))
			{
				_panickedFormations.Add(agent.Formation);
			}
			if (!_panickedTeams.Contains(agent.Team))
			{
				_panickedTeams.Add(agent.Team);
			}
		}
	}

	public override void OnPreMissionTick(float dt)
	{
		if (_panickedAgents.Count <= 0)
		{
			return;
		}
		foreach (Team panickedTeam in _panickedTeams)
		{
			panickedTeam.UpdateCachedEnemyDataForFleeing();
		}
		foreach (Formation panickedFormation in _panickedFormations)
		{
			panickedFormation.OnBatchUnitRemovalStart();
		}
		foreach (Agent panickedAgent in _panickedAgents)
		{
			panickedAgent.CommonAIComponent?.Retreat();
			Mission.Current.OnAgentFleeing(panickedAgent);
		}
		foreach (Formation panickedFormation2 in _panickedFormations)
		{
			panickedFormation2.OnBatchUnitRemovalEnd();
		}
		_panickedAgents.Clear();
		_panickedFormations.Clear();
		_panickedTeams.Clear();
	}

	public override void OnRemoveBehavior()
	{
		_panickedAgents.Clear();
		_panickedFormations.Clear();
		_panickedTeams.Clear();
		base.OnRemoveBehavior();
	}
}
