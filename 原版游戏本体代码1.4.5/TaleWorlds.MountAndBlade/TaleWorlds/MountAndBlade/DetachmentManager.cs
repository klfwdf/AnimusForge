using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DetachmentManager
{
	private readonly MBList<(IDetachment, DetachmentData)> _detachments;

	private readonly Dictionary<IDetachment, DetachmentData> _detachmentDataDictionary;

	private readonly List<(int, float)> _slotIndexWeightTuplesCache;

	private readonly Team _team;

	public MBReadOnlyList<(IDetachment, DetachmentData)> Detachments => _detachments;

	public DetachmentManager(Team team)
	{
		_detachments = new MBList<(IDetachment, DetachmentData)>();
		_detachmentDataDictionary = new Dictionary<IDetachment, DetachmentData>();
		_team = team;
		team.OnFormationsChanged += Team_OnFormationsChanged;
		_slotIndexWeightTuplesCache = new List<(int, float)>();
	}

	private void Team_OnFormationsChanged(Team team, Formation formation)
	{
		float currentTime = Mission.Current.CurrentTime;
		foreach (IDetachment detachment in formation.Detachments)
		{
			DetachmentData detachmentData = _detachmentDataDictionary[detachment];
			detachmentData.agentScores.Clear();
			detachmentData.firstTime = currentTime;
		}
	}

	public void Clear()
	{
		_team.OnFormationsChanged -= Team_OnFormationsChanged;
		_team.OnFormationsChanged += Team_OnFormationsChanged;
		foreach (var item in Detachments.ToList())
		{
			DestroyDetachment(item.Item1);
		}
	}

	public bool ContainsDetachment(IDetachment detachment)
	{
		return _detachmentDataDictionary.ContainsKey(detachment);
	}

	public void MakeDetachment(IDetachment detachment)
	{
		DetachmentData detachmentData = new DetachmentData();
		_detachments.Add((detachment, detachmentData));
		_detachmentDataDictionary[detachment] = detachmentData;
	}

	public void DestroyDetachment(IDetachment destroyedDetachment)
	{
		for (int i = 0; i < _detachments.Count; i++)
		{
			(IDetachment, DetachmentData) tuple = _detachments[i];
			if (tuple.Item1 == destroyedDetachment)
			{
				for (int num = tuple.Item2.joinedFormations.Count - 1; num >= 0; num--)
				{
					tuple.Item2.joinedFormations[num].LeaveDetachment(destroyedDetachment);
				}
				_detachments.RemoveAt(i);
				break;
			}
		}
		_detachmentDataDictionary.Remove(destroyedDetachment);
	}

	public void OnFormationJoinDetachment(Formation formation, IDetachment joinedDetachment)
	{
		DetachmentData detachmentData = _detachmentDataDictionary[joinedDetachment];
		detachmentData.joinedFormations.Add(formation);
		detachmentData.firstTime = MBCommon.GetTotalMissionTime();
		joinedDetachment.FormationStartUsing(formation);
	}

	public void OnFormationLeaveDetachment(Formation formation, IDetachment leftDetachment)
	{
		DetachmentData detachmentData = _detachmentDataDictionary[leftDetachment];
		detachmentData.joinedFormations.Remove(formation);
		detachmentData.agentScores.RemoveAll(((Agent, List<float>) ags) => ags.Item1.Formation == formation);
		detachmentData.firstTime = MBCommon.GetTotalMissionTime();
		leftDetachment.FormationStopUsing(formation);
	}

	public void TickDetachments()
	{
		float totalMissionTime = MBCommon.GetTotalMissionTime();
		if (!Mission.Current.IsLoadingFinished || !Mission.Current.AllowAiTicking)
		{
			foreach (var detachment2 in _detachments)
			{
				detachment2.Item2.firstTime = totalMissionTime;
			}
			return;
		}
		foreach (var detachment3 in _detachments)
		{
			if (detachment3.Item1.ComputeAndCacheDetachmentWeight(_team.Side) > float.MinValue && detachment3.Item2.IsPrecalculated())
			{
				detachment3.Item1.ResetEvaluation();
			}
		}
		bool flag = _detachments.Count == 0;
		while (!flag)
		{
			(IDetachment, DetachmentData) tuple = (null, null);
			float num = float.MinValue;
			foreach (var detachment4 in _detachments)
			{
				if (_team.Mission.Mode != MissionMode.Deployment || !_team.IsAttacker || (!(detachment4.Item1 is SiegeLadder) && !(detachment4.Item1 is StrategicArea)))
				{
					float detachmentWeightFromCache = detachment4.Item1.GetDetachmentWeightFromCache();
					if (num < detachmentWeightFromCache && detachment4.Item2.IsPrecalculated() && !detachment4.Item1.IsEvaluated())
					{
						num = detachmentWeightFromCache;
						tuple = detachment4;
					}
				}
			}
			if (tuple.Item1 != null)
			{
				var (detachment, detachmentData) = tuple;
				if (tuple.Item1.IsDetachmentRecentlyEvaluated())
				{
					foreach (var detachment5 in _detachments)
					{
						if (!detachment5.Item1.IsEvaluated())
						{
							detachment5.Item1.UnmarkDetachment();
						}
					}
				}
				detachment.GetSlotIndexWeightTuples(_slotIndexWeightTuplesCache);
				while (!flag && _slotIndexWeightTuplesCache.Count > 0)
				{
					(int, float) item = (0, 0f);
					float num2 = float.MinValue;
					foreach (var item2 in _slotIndexWeightTuplesCache)
					{
						if (num2 < item2.Item2)
						{
							num2 = item2.Item2;
							item = item2;
						}
					}
					float num3 = float.MaxValue;
					int num4 = -1;
					for (int i = 0; i < detachmentData.agentScores.Count; i++)
					{
						(Agent, List<float>) tuple3 = detachmentData.agentScores[i];
						if (detachment.IsSlotAtIndexAvailableForAgent(item.Item1, tuple3.Item1))
						{
							float num5 = tuple3.Item2[item.Item1];
							if (num3 > num5)
							{
								num3 = num5;
								num4 = i;
							}
						}
					}
					if (num4 != -1)
					{
						Agent movingAgentAtSlotIndex = detachment.GetMovingAgentAtSlotIndex(item.Item1);
						float num6 = float.MaxValue;
						if (movingAgentAtSlotIndex != null)
						{
							int num7 = -1;
							for (int j = 0; j < detachmentData.agentScores.Count; j++)
							{
								if (detachmentData.agentScores[j].Item1 == movingAgentAtSlotIndex)
								{
									num7 = j;
									break;
								}
							}
							float exactCostOfAgentAtSlot = detachment.GetExactCostOfAgentAtSlot(movingAgentAtSlotIndex, item.Item1);
							if (num7 != -1)
							{
								detachmentData.agentScores[num7].Item2[item.Item1] = exactCostOfAgentAtSlot;
							}
							num6 = exactCostOfAgentAtSlot;
						}
						if (movingAgentAtSlotIndex != null)
						{
							detachment.MarkSlotAtIndex(item.Item1);
						}
						(Agent, List<float>) tuple4 = detachmentData.agentScores[num4];
						float num8 = tuple4.Item2[item.Item1];
						if (movingAgentAtSlotIndex != null && num6 <= num8)
						{
							flag = true;
						}
						else if (movingAgentAtSlotIndex != null)
						{
							float exactCostOfAgentAtSlot2 = detachment.GetExactCostOfAgentAtSlot(tuple4.Item1, item.Item1);
							tuple4.Item2[item.Item1] = exactCostOfAgentAtSlot2;
							if (num6 <= exactCostOfAgentAtSlot2)
							{
								flag = true;
							}
						}
						if (!flag)
						{
							detachment.AddAgentAtSlotIndex(tuple4.Item1, item.Item1);
							flag = true;
						}
					}
					else
					{
						_slotIndexWeightTuplesCache.Remove(item);
					}
				}
				_slotIndexWeightTuplesCache.Clear();
				if (!flag)
				{
					tuple.Item1.SetAsEvaluated();
				}
			}
			else
			{
				flag = true;
			}
		}
	}

	public void TickAgent(Agent agent)
	{
		bool isDetachedFromFormation = agent.IsDetachedFromFormation;
		if (!agent.IsDetachableFromFormation)
		{
			return;
		}
		if (isDetachedFromFormation && !agent.Detachment.IsAgentEligible(agent))
		{
			agent.Detachment.RemoveAgent(agent);
			agent.Formation?.AttachUnit(agent);
		}
		if (!agent.IsDetachedFromFormation)
		{
			float totalMissionTime = MBCommon.GetTotalMissionTime();
			bool flag = totalMissionTime - agent.LastDetachmentTickAgentTime > 1.5f;
			{
				foreach (IDetachment detachment in agent.Formation.Detachments)
				{
					float detachmentWeight = detachment.GetDetachmentWeight(agent.Formation.Team.Side);
					DetachmentData detachmentData = _detachmentDataDictionary[detachment];
					if (detachmentWeight > float.MinValue)
					{
						int num = -1;
						for (int i = 0; i < detachmentData.agentScores.Count; i++)
						{
							if (detachmentData.agentScores[i].Item1 == agent)
							{
								num = i;
								break;
							}
						}
						if (num == -1)
						{
							if (detachmentData.agentScores.Count == 0)
							{
								detachmentData.firstTime = totalMissionTime;
							}
							List<float> templateCostsOfAgent = detachment.GetTemplateCostsOfAgent(agent, null);
							detachmentData.agentScores.Add((agent, templateCostsOfAgent));
							agent.SetLastDetachmentTickAgentTime(totalMissionTime);
						}
						else if (flag)
						{
							(Agent, List<float>) tuple = detachmentData.agentScores[num];
							detachmentData.agentScores[num] = (tuple.Item1, detachment.GetTemplateCostsOfAgent(agent, tuple.Item2));
							agent.SetLastDetachmentTickAgentTime(totalMissionTime);
						}
					}
					else
					{
						detachmentData.firstTime = totalMissionTime;
					}
				}
				return;
			}
		}
		if (!agent.AIMoveToGameObjectIsEnabled())
		{
			return;
		}
		float totalMissionTime2 = MBCommon.GetTotalMissionTime();
		DetachmentData detachmentData2 = _detachmentDataDictionary[agent.Detachment];
		int num2 = -1;
		for (int j = 0; j < detachmentData2.agentScores.Count; j++)
		{
			if (detachmentData2.agentScores[j].Item1 == agent)
			{
				num2 = j;
				break;
			}
		}
		if (num2 == -1)
		{
			if (detachmentData2.agentScores.Count == 0)
			{
				detachmentData2.firstTime = totalMissionTime2;
			}
			List<float> templateCostsOfAgent2 = agent.Detachment.GetTemplateCostsOfAgent(agent, null);
			detachmentData2.agentScores.Add((agent, templateCostsOfAgent2));
			agent.SetLastDetachmentTickAgentTime(totalMissionTime2);
		}
		else if (totalMissionTime2 - agent.LastDetachmentTickAgentTime > 1.5f)
		{
			(Agent, List<float>) tuple2 = detachmentData2.agentScores[num2];
			detachmentData2.agentScores[num2] = (tuple2.Item1, agent.Detachment.GetTemplateCostsOfAgent(agent, tuple2.Item2));
			agent.SetLastDetachmentTickAgentTime(totalMissionTime2);
		}
	}

	public void OnAgentRemoved(Agent agent)
	{
		foreach (IDetachment detachment in agent.Formation.Detachments)
		{
			_detachmentDataDictionary[detachment].agentScores.RemoveAll(((Agent, List<float>) ags) => ags.Item1 == agent);
		}
	}

	public void RemoveScoresOfAgentFromDetachments(Agent agent)
	{
		foreach (var detachment in _detachments)
		{
			if (!agent.AIMoveToGameObjectIsEnabled() || agent.Detachment != detachment.Item1)
			{
				detachment.Item2.RemoveScoreOfAgent(agent);
			}
		}
	}

	public void RemoveScoresOfAgentFromDetachment(Agent agent, IDetachment detachmentToBeRemovedFrom)
	{
		foreach (var detachment in _detachments)
		{
			if (detachmentToBeRemovedFrom == detachment.Item1)
			{
				detachment.Item2.RemoveScoreOfAgent(agent);
			}
		}
	}

	public void AddAgentAsMovingToDetachment(Agent agent, IDetachment detachment)
	{
		if (detachment != null)
		{
			_detachmentDataDictionary[detachment].MovingAgentCount++;
		}
	}

	public void RemoveAgentAsMovingToDetachment(Agent agent)
	{
		if (agent.Detachment != null)
		{
			_detachmentDataDictionary[agent.Detachment].MovingAgentCount--;
		}
	}

	public void AddAgentAsDefendingToDetachment(Agent agent, IDetachment detachment)
	{
		_detachmentDataDictionary[detachment].DefendingAgentCount++;
	}

	public void RemoveAgentAsDefendingToDetachment(Agent agent)
	{
		if (!_detachmentDataDictionary.ContainsKey(agent.Detachment))
		{
			TaleWorlds.Library.Debug.Print("DUMP-1671 | " + ((agent.Detachment != null) ? agent.Detachment.ToString() : "null"));
		}
		_detachmentDataDictionary[agent.Detachment].DefendingAgentCount--;
	}

	[Conditional("DEBUG")]
	private void AssertDetachments()
	{
	}

	[Conditional("DEBUG")]
	public void AssertDetachment(Team team, IDetachment detachment)
	{
		_detachmentDataDictionary[detachment].joinedFormations.Where((Formation f) => f.CountOfUnits > 0);
		team.FormationsIncludingSpecialAndEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.Detachments.Contains(detachment));
	}
}
