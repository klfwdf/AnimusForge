using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class StealthAreaMissionLogic : MissionLogic
{
	public delegate MBList<Agent> SpawnReinforcementAllyTroopsDelegate(StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker);

	public class StealthAreaData
	{
		internal bool IsStealthAreaTriggered;

		internal bool IsReinforcementCalled;

		internal readonly StealthAreaUsePoint StealthAreaUsePoint;

		internal readonly Dictionary<StealthAreaMarker, List<Agent>> StealthAreaMarkers;

		internal StealthAreaData(StealthAreaUsePoint stealthAreaUsePoint)
		{
			StealthAreaUsePoint = stealthAreaUsePoint;
			StealthAreaMarkers = new Dictionary<StealthAreaMarker, List<Agent>>();
			foreach (WeakGameEntity child in stealthAreaUsePoint.GameEntity.GetChildren())
			{
				if (child.HasScriptOfType<StealthAreaMarker>())
				{
					StealthAreaMarkers.Add(child.GetFirstScriptOfType<StealthAreaMarker>(), new List<Agent>());
				}
			}
		}

		internal void AddAgentToStealthAreaMarker(StealthAreaMarker stealthAreaMarker, Agent agent)
		{
			StealthAreaMarkers[stealthAreaMarker].Add(agent);
		}

		internal void RemoveAgentFromStealthAreaMarker(StealthAreaMarker stealthAreaMarker, Agent agent)
		{
			StealthAreaMarkers[stealthAreaMarker].Remove(agent);
			if (StealthAreaMarkers.All((KeyValuePair<StealthAreaMarker, List<Agent>> x) => x.Value.IsEmpty()))
			{
				StealthAreaUsePoint.EnableStealthAreaUsePoint();
				IsStealthAreaTriggered = true;
			}
		}
	}

	private readonly MBList<StealthAreaData> _stealthAreaData = new MBList<StealthAreaData>();

	private readonly Dictionary<string, Dictionary<string, int>> _agentSpawnTypes = new Dictionary<string, Dictionary<string, int>>();

	private readonly MBList<Agent> _allyTroops = new MBList<Agent>();

	public SpawnReinforcementAllyTroopsDelegate SpawnReinforcementAllyTroopsEvent;

	public MBReadOnlyList<Agent> AllyTroops => _allyTroops;

	public bool AllReinforcementsCalled { get; private set; }

	public bool IsSentry(Agent agent)
	{
		foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				if (stealthAreaMarker.Value.Contains(agent))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		foreach (StealthAreaUsePoint item in base.Mission.MissionObjects.FindAllWithType<StealthAreaUsePoint>())
		{
			_stealthAreaData.Add(new StealthAreaData(item));
		}
	}

	private MBList<Agent> SpawnReinforcementAllyGroupTroops(StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker)
	{
		return SpawnReinforcementAllyTroopsEvent?.Invoke(triggeredStealthAreaData, stealthAreaMarker) ?? new MBList<Agent>();
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		base.OnAgentBuild(agent, banner);
		CheckStealthAreaMarkerForAgent(agent);
	}

	public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
		base.OnAgentTeamChanged(prevTeam, newTeam, agent);
		CheckStealthAreaMarkerForAgent(agent);
	}

	private void CheckStealthAreaMarkerForAgent(Agent agent)
	{
		if (!agent.IsHuman || agent.Team != Mission.Current.PlayerEnemyTeam)
		{
			return;
		}
		foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				if (stealthAreaMarker.Key.IsPositionInRange(agent.Position))
				{
					stealthAreaDatum.AddAgentToStealthAreaMarker(stealthAreaMarker.Key, agent);
					break;
				}
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectorAgent == null || !affectorAgent.IsMainAgent)
		{
			return;
		}
		foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				if (stealthAreaMarker.Value.Contains(affectedAgent))
				{
					stealthAreaDatum.RemoveAgentFromStealthAreaMarker(stealthAreaMarker.Key, affectedAgent);
				}
			}
		}
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (usedObject is StealthAreaUsePoint)
		{
			if (IsInCombat())
			{
				return;
			}
			StealthAreaData stealthAreaData = null;
			foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
			{
				if (stealthAreaDatum.StealthAreaUsePoint == usedObject)
				{
					stealthAreaData = stealthAreaDatum;
					break;
				}
			}
			if (stealthAreaData != null)
			{
				stealthAreaData.IsReinforcementCalled = true;
				foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaData.StealthAreaMarkers)
				{
					MBList<Agent> collection = SpawnReinforcementAllyGroupTroops(stealthAreaData, stealthAreaMarker.Key);
					_allyTroops.AddRange(collection);
				}
			}
		}
		AllReinforcementsCalled = _stealthAreaData.All((StealthAreaData x) => x.IsReinforcementCalled);
	}

	private bool IsInCombat()
	{
		bool result = false;
		foreach (Agent allAgent in Mission.Current.AllAgents)
		{
			if (allAgent.IsActive())
			{
				Agent.AIStateFlag aIStateFlag = Agent.AIStateFlag.Alarmed;
				if ((allAgent.AIStateFlags & aIStateFlag) == aIStateFlag)
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}

	public bool CheckIfAllStealthAreasAreTriggered()
	{
		return _stealthAreaData.All((StealthAreaData x) => x.IsStealthAreaTriggered);
	}

	public bool CheckIfAllStealthAreasReinforcementsAreCalled()
	{
		return AllReinforcementsCalled;
	}
}
