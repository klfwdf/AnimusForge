using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

public class AgentMoraleInteractionLogic : MissionLogic
{
	private const float DebacleVoiceChance = 0.7f;

	private const float MoraleEffectRadius = 4f;

	private const int MaxNumAgentsToGainMorale = 10;

	private const int MaxNumAgentsToLoseMorale = 10;

	private const float SquaredDistanceForSeparateAffectorQuery = 2.25f;

	private const ushort RandomSelectorCapacity = 1024;

	private readonly HashSet<Agent> _agentsToReceiveMoraleGain = new HashSet<Agent>();

	private readonly HashSet<Agent> _agentsToReceiveMoraleLoss = new HashSet<Agent>();

	private readonly MBFastRandomSelector<Agent> _randomAgentSelector = new MBFastRandomSelector<Agent>(1024);

	private readonly MBFastRandomSelector<IFormationUnit> _randomFormationUnitSelector = new MBFastRandomSelector<IFormationUnit>(1024);

	private readonly MBList<Agent> _nearbyAgentsCache;

	private readonly MBList<Agent> _nearbyAllyAgentsCache;

	public AgentMoraleInteractionLogic()
	{
		_nearbyAgentsCache = new MBList<Agent>();
		_nearbyAllyAgentsCache = new MBList<Agent>();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectedAgent == null || !affectedAgent.IsHuman || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
		{
			return;
		}
		var (num, num2) = MissionGameModels.Current.BattleMoraleModel.CalculateMaxMoraleChangeDueToAgentIncapacitated(affectedAgent, agentState, affectorAgent, in killingBlow);
		if (num > 0f || num2 > 0f)
		{
			if (affectorAgent != null)
			{
				affectorAgent = (affectorAgent.IsHuman ? affectorAgent : (affectorAgent.IsMount ? affectorAgent.RiderAgent : null));
			}
			ApplyMoraleEffectOnAgentIncapacitated(affectedAgent, affectorAgent, num, num2, 4f);
		}
	}

	public override void OnAgentFleeing(Agent affectedAgent)
	{
		if (affectedAgent != null && affectedAgent.IsHuman)
		{
			var (num, num2) = MissionGameModels.Current.BattleMoraleModel.CalculateMaxMoraleChangeDueToAgentPanicked(affectedAgent);
			if (num > 0f || num2 > 0f)
			{
				ApplyMoraleEffectOnAgentIncapacitated(affectedAgent, null, num, num2, 4f);
			}
			if (MBRandom.RandomFloat < 0.7f)
			{
				affectedAgent.MakeVoice(SkinVoiceManager.VoiceType.Debacle, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			}
		}
	}

	private void ApplyMoraleEffectOnAgentIncapacitated(Agent affectedAgent, Agent affectorAgent, float affectedSideMaxMoraleLoss, float affectorSideMoraleMaxGain, float effectRadius)
	{
		_agentsToReceiveMoraleLoss.Clear();
		_agentsToReceiveMoraleGain.Clear();
		if (affectedAgent != null && affectedAgent.IsHuman)
		{
			Vec2 asVec = affectedAgent.GetWorldPosition().AsVec2;
			base.Mission.GetNearbyAgents(asVec, effectRadius, _nearbyAgentsCache);
			SelectRandomAgentsFromListToAgentSet(_nearbyAgentsCache, _agentsToReceiveMoraleLoss, 10, AffectedsAllyCondition);
			if (_agentsToReceiveMoraleLoss.Count < 10 && affectedAgent.Formation != null)
			{
				SelectRandomAgentsFromFormationToAgentSet(affectedAgent.Formation, _agentsToReceiveMoraleLoss, 10, FormationCondition);
			}
			if (affectorAgent != null && affectorAgent.IsActive() && affectorAgent.IsHuman && affectorAgent.IsAIControlled && affectorAgent.IsEnemyOf(affectedAgent))
			{
				_agentsToReceiveMoraleGain.Add(affectorAgent);
			}
			if (_agentsToReceiveMoraleGain.Count < 10)
			{
				SelectRandomAgentsFromListToAgentSet(_nearbyAgentsCache, _agentsToReceiveMoraleGain, 10, AffectedsEnemyCondition);
			}
			if (_agentsToReceiveMoraleGain.Count < 10 && affectorAgent?.Team != null && affectorAgent.IsEnemyOf(affectedAgent))
			{
				Vec2 asVec2 = affectorAgent.GetWorldPosition().AsVec2;
				if (asVec2.DistanceSquared(asVec) > 2.25f)
				{
					base.Mission.GetNearbyAllyAgents(asVec2, effectRadius, affectedAgent.Team, _nearbyAllyAgentsCache);
					SelectRandomAgentsFromListToAgentSet(_nearbyAllyAgentsCache, _agentsToReceiveMoraleGain, 10, AffectorsAllyCondition);
				}
			}
			if (_agentsToReceiveMoraleGain.Count < 10 && affectorAgent?.Formation != null)
			{
				SelectRandomAgentsFromFormationToAgentSet(affectorAgent.Formation, _agentsToReceiveMoraleGain, 10, FormationCondition2);
			}
		}
		foreach (Agent item in _agentsToReceiveMoraleLoss)
		{
			float delta = 0f - MissionGameModels.Current.BattleMoraleModel.CalculateMoraleChangeToCharacter(item, affectedSideMaxMoraleLoss);
			item.ChangeMorale(delta);
		}
		foreach (Agent item2 in _agentsToReceiveMoraleGain)
		{
			float delta2 = MissionGameModels.Current.BattleMoraleModel.CalculateMoraleChangeToCharacter(item2, affectorSideMoraleMaxGain);
			item2.ChangeMorale(delta2);
		}
		bool AffectedsAllyCondition(Agent agent)
		{
			if (agent.IsActive() && agent.IsHuman && agent.IsAIControlled && agent != affectedAgent)
			{
				return agent.IsFriendOf(affectedAgent);
			}
			return false;
		}
		bool AffectedsEnemyCondition(Agent agent)
		{
			if (agent.IsActive() && agent.IsHuman && agent.IsAIControlled && agent != affectorAgent)
			{
				return agent.IsEnemyOf(affectedAgent);
			}
			return false;
		}
		bool AffectorsAllyCondition(Agent agent)
		{
			if (agent.IsActive() && agent.IsHuman && agent.IsAIControlled)
			{
				return agent != affectorAgent;
			}
			return false;
		}
		static bool FormationCondition(IFormationUnit unit)
		{
			if (unit is Agent agent && agent.IsActive() && agent.IsHuman)
			{
				return agent.IsAIControlled;
			}
			return false;
		}
		static bool FormationCondition2(IFormationUnit unit)
		{
			if (unit is Agent agent && agent.IsActive() && agent.IsHuman)
			{
				return agent.IsAIControlled;
			}
			return false;
		}
	}

	private void SelectRandomAgentsFromListToAgentSet(MBReadOnlyList<Agent> agentsList, HashSet<Agent> outputAgentsSet, int maxCountInSet, Predicate<Agent> conditions = null)
	{
		if (outputAgentsSet != null && agentsList != null)
		{
			_randomAgentSelector.Initialize(agentsList);
			Agent selection;
			while (outputAgentsSet.Count < maxCountInSet && _randomAgentSelector.SelectRandom(out selection, conditions))
			{
				outputAgentsSet.Add(selection);
			}
		}
	}

	private void SelectRandomAgentsFromFormationToAgentSet(Formation formation, HashSet<Agent> outputAgentsSet, int maxCountInSet, Predicate<IFormationUnit> conditions = null)
	{
		if (outputAgentsSet == null || formation == null || formation.CountOfUnits <= 0)
		{
			return;
		}
		int num = Math.Max(0, maxCountInSet - outputAgentsSet.Count);
		if (num <= 0)
		{
			return;
		}
		int num2 = (int)((float)formation.CountOfDetachedUnits / (float)formation.CountOfUnits * (float)num);
		if (num2 > 0)
		{
			_randomAgentSelector.Initialize(formation.DetachedUnits);
			int num3 = 0;
			Agent selection;
			while (num3 < num2 && outputAgentsSet.Count < maxCountInSet && _randomAgentSelector.SelectRandom(out selection, conditions))
			{
				if (outputAgentsSet.Add(selection))
				{
					num3++;
				}
			}
		}
		if (outputAgentsSet.Count >= maxCountInSet || !(formation.Arrangement?.GetAllUnits() is MBList<IFormationUnit> { Count: >0 } mBList))
		{
			return;
		}
		_randomFormationUnitSelector.Initialize(mBList);
		IFormationUnit selection2;
		while (outputAgentsSet.Count < maxCountInSet && _randomFormationUnitSelector.SelectRandom(out selection2, conditions))
		{
			if (selection2 is Agent item)
			{
				outputAgentsSet.Add(item);
			}
		}
	}
}
