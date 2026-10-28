using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class ShipPlacementDetachment : IDetachment
{
	private class ShipPlacementPosition
	{
		private bool _isHighPos;

		private Agent _extraAgent;

		public Agent AssignedAgent { get; private set; }

		public MatrixFrame LocalFrame { get; }

		public bool IsOuterPos { get; }

		public bool HasExtraAgent { get; private set; }

		public bool LentToOtherFrame => ExtraFrameIndex >= 0;

		public int ExtraFrameIndex { get; private set; } = -1;

		public ShipPlacementPosition(MatrixFrame frame, bool isOuterPos, bool isHighPos)
		{
			LocalFrame = frame;
			IsOuterPos = isOuterPos;
			_isHighPos = isHighPos;
			HasExtraAgent = false;
			AssignedAgent = null;
			_extraAgent = null;
		}

		public void RemoveAgent()
		{
			AssignedAgent = null;
			_extraAgent = null;
		}

		public void LendToExtraPosition(int extraFrameIndex)
		{
			ExtraFrameIndex = extraFrameIndex;
		}

		public void ResetPlacementPosition()
		{
			AssignedAgent = null;
			ResetExtraPosition();
		}

		public void ResetExtraPosition()
		{
			ExtraFrameIndex = -1;
			HasExtraAgent = false;
			_extraAgent = null;
		}

		public void SetAgent(Agent agent)
		{
			AssignedAgent = agent;
		}

		public void SetExtraAgent(Agent agent)
		{
			HasExtraAgent = agent != null;
			_extraAgent = agent;
		}

		public void CalculateDefaultScore(out float resultScore, out float resultPossibleGain, out PositionCondition outGainCondition)
		{
			float num = 1f * (IsOuterPos ? 10f : 1f) * (_isHighPos ? 50f : 1f);
			resultScore = ((AssignedAgent == null) ? 0f : (AssignedAgent.HasAnyRangedWeaponCached ? num : 1f));
			resultPossibleGain = num - resultScore;
			outGainCondition = PositionCondition.Ranged;
		}

		public void CalculateUnderMissileFireScore(out float resultScore, out float resultPossibleGain, out PositionCondition outGainCondition)
		{
			float num = 1f * (IsOuterPos ? 50f : 1f) * (_isHighPos ? 50f : 1f);
			if (!IsOuterPos && !_isHighPos)
			{
				num = 1f;
				resultScore = ((AssignedAgent != null) ? num : 0f);
				resultPossibleGain = num - resultScore;
				outGainCondition = PositionCondition.Any;
			}
			else if (!_isHighPos)
			{
				num = 50f;
				outGainCondition = PositionCondition.RangedOrShield;
				resultScore = ((AssignedAgent == null) ? 0f : (CheckCondition(outGainCondition, AssignedAgent) ? 50f : 1f));
				resultPossibleGain = num - resultScore;
			}
			else if (!IsOuterPos)
			{
				num = 50f;
				outGainCondition = PositionCondition.Ranged;
				resultScore = ((AssignedAgent == null) ? 0f : (CheckCondition(outGainCondition, AssignedAgent) ? 50f : 1f));
				resultPossibleGain = num - resultScore;
			}
			else
			{
				num = 250f;
				outGainCondition = PositionCondition.Ranged;
				resultScore = ((AssignedAgent == null) ? 0f : (CheckCondition(outGainCondition, AssignedAgent) ? 250f : 1f));
				resultPossibleGain = num - resultScore;
			}
		}

		public void CalculateBoardingScore(Vec2 boardingLocalPosition, out float resultScore, out float resultPossibleGain, out PositionCondition outGainCondition, out bool requestExtraAgent)
		{
			requestExtraAgent = false;
			if (_isHighPos)
			{
				float num = 1f;
				if (boardingLocalPosition.IsNonZero())
				{
					if (boardingLocalPosition.x * LocalFrame.origin.x >= 0f)
					{
						float num2 = boardingLocalPosition.Normalized().DotProduct(LocalFrame.origin.AsVec2.Normalized());
						if (num2 >= 0f)
						{
							num = MathF.Clamp(num2 * 10f, 1f, 10f);
						}
					}
				}
				else
				{
					num = 10f;
				}
				float num3 = 50f * (IsOuterPos ? 10f : 1f) * num;
				outGainCondition = PositionCondition.Ranged;
				resultScore = ((AssignedAgent == null) ? 0f : (CheckCondition(outGainCondition, AssignedAgent) ? num3 : 1f));
				resultPossibleGain = num3 - resultScore;
				return;
			}
			float num4;
			if (boardingLocalPosition.IsNonZero())
			{
				num4 = 0.1f;
				if (boardingLocalPosition.x * LocalFrame.origin.x >= 0f)
				{
					num4 = MathF.Clamp((boardingLocalPosition.Normalized().DotProduct(LocalFrame.origin.AsVec2.Normalized()) + 1f) * 10f, 1f, 15f);
					requestExtraAgent = AssignedAgent != null && _extraAgent == null;
				}
			}
			else
			{
				num4 = 10f;
			}
			float num5 = 100f * (IsOuterPos ? 10f : 1f) * num4;
			outGainCondition = PositionCondition.Any;
			resultScore = ((AssignedAgent == null) ? 0f : ((CheckCondition(outGainCondition, AssignedAgent) ? num5 : (num5 * 0.1f)) + ((_extraAgent == null) ? 0f : (CheckCondition(outGainCondition, _extraAgent) ? num5 : (num5 * 0.1f)))));
			resultPossibleGain = num5 * (requestExtraAgent ? 2f : 1f) - resultScore;
		}
	}

	private enum PositionCondition
	{
		Any,
		RangedOrShield,
		Ranged
	}

	private readonly Agent[] _agents;

	private readonly MBList<Formation> _userFormations;

	private readonly MBList<ShipPlacementPosition> _shipPlacementPositions;

	private readonly MissionShip _ownerShip;

	private bool _isUnderMissileFire;

	private bool _isBoarding;

	private Vec2 _boardingDirection;

	private MissionTimer _placementDetachmentTimer;

	private bool _isTickRequired = true;

	public MBReadOnlyList<Formation> UserFormations => _userFormations;

	public bool IsLoose => true;

	public bool IsActive => true;

	public bool HasAgent => CountOfAgents > 0;

	public int CountOfAgents { get; private set; }

	public bool HasAvailableSlots => _shipPlacementPositions.Count > CountOfAgents;

	public bool IsTickRequired
	{
		get
		{
			if (!_isTickRequired)
			{
				return _placementDetachmentTimer.Check();
			}
			return true;
		}
	}

	public ShipPlacementDetachment(in MissionShip ownerShip)
	{
		_ownerShip = ownerShip;
		_userFormations = new MBList<Formation>();
		_shipPlacementPositions = new MBList<ShipPlacementPosition>();
		float num = 0f;
		foreach (MatrixFrame outerDeckLocalFrame in ownerShip.OuterDeckLocalFrames)
		{
			num += outerDeckLocalFrame.origin.z;
		}
		foreach (MatrixFrame innerDeckLocalFrame in ownerShip.InnerDeckLocalFrames)
		{
			num += innerDeckLocalFrame.origin.z;
		}
		foreach (MatrixFrame crewSpawnLocalFrame in ownerShip.CrewSpawnLocalFrames)
		{
			num += crewSpawnLocalFrame.origin.z;
		}
		int num2 = ownerShip.OuterDeckLocalFrames.Count + ownerShip.InnerDeckLocalFrames.Count + ownerShip.CrewSpawnLocalFrames.Count;
		float num3 = num / (float)((num2 <= 0) ? 1 : num2);
		foreach (MatrixFrame outerDeckLocalFrame2 in ownerShip.OuterDeckLocalFrames)
		{
			_shipPlacementPositions.Add(new ShipPlacementPosition(outerDeckLocalFrame2, isOuterPos: true, outerDeckLocalFrame2.origin.z - num3 >= 1f));
		}
		foreach (MatrixFrame innerDeckLocalFrame2 in ownerShip.InnerDeckLocalFrames)
		{
			_shipPlacementPositions.Add(new ShipPlacementPosition(innerDeckLocalFrame2, isOuterPos: false, innerDeckLocalFrame2.origin.z - num3 >= 1f));
		}
		foreach (MatrixFrame crewSpawnLocalFrame2 in ownerShip.CrewSpawnLocalFrames)
		{
			_shipPlacementPositions.Add(new ShipPlacementPosition(crewSpawnLocalFrame2, isOuterPos: false, crewSpawnLocalFrame2.origin.z - num3 >= 1f));
		}
		_agents = new Agent[_shipPlacementPositions.Count];
		_boardingDirection = Vec2.Zero;
		_placementDetachmentTimer = new MissionTimer(5f);
	}

	public void AddAgent(Agent agent, int slotIndex, Agent.AIScriptedFrameFlags customFlags = Agent.AIScriptedFrameFlags.None)
	{
		_agents[slotIndex] = agent;
		CountOfAgents++;
	}

	public void AddAgentAtSlotIndex(Agent agent, int slotIndex)
	{
		_agents[slotIndex] = agent;
		CountOfAgents++;
		_shipPlacementPositions[slotIndex].SetAgent(agent);
		agent.Formation?.DetachUnit(agent, isLoose: true);
		agent.Detachment = this;
		agent.SetDetachmentWeight(1f);
		agent.SetDetachmentIndex(slotIndex);
	}

	public void AddAgent(Agent agent)
	{
		for (int i = 0; i < _agents.Length; i++)
		{
			if (_agents[i] == null)
			{
				AddAgentAtSlotIndex(agent, i);
				break;
			}
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
		if (slotIndex >= _agents.Length)
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
		if (agent.DetachmentIndex >= 0 && agent.DetachmentIndex < _agents.Length)
		{
			return _agents[agent.DetachmentIndex] == agent;
		}
		return false;
	}

	void IDetachment.OnFormationLeave(Formation formation)
	{
		for (int num = _agents.Length - 1; num >= 0; num--)
		{
			Agent agent = _agents[num];
			if (agent != null && agent.Formation == formation && !agent.IsPlayerControlled)
			{
				_agents[num] = null;
				CountOfAgents--;
				agent.SetCrouchMode(set: false);
				agent.EnforceShieldUsage(Agent.UsageDirection.None);
				agent.DisableScriptedMovement();
				agent.DisableScriptedCombatMovement();
				formation.AttachUnit(agent);
			}
		}
		for (int i = 0; i < _shipPlacementPositions.Count; i++)
		{
			_shipPlacementPositions[i].ResetPlacementPosition();
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
		_agents[agent.DetachmentIndex] = null;
		CountOfAgents--;
		_shipPlacementPositions[agent.DetachmentIndex].RemoveAgent();
		agent.SetCrouchMode(set: false);
		agent.EnforceShieldUsage(Agent.UsageDirection.None);
		agent.DisableScriptedMovement();
		agent.DisableScriptedCombatMovement();
	}

	public int GetNumberOfUsableSlots()
	{
		return _shipPlacementPositions.Count - CountOfAgents;
	}

	public void SetUnderMissileFire(bool isUnderMissileFire)
	{
		if (_isUnderMissileFire != isUnderMissileFire)
		{
			_isUnderMissileFire = isUnderMissileFire;
			_isTickRequired = true;
		}
	}

	public void SetBoarding(bool isBoarding, Vec2 localDir)
	{
		if (_isBoarding == isBoarding && (!_boardingDirection.IsNonZero() || localDir.IsNonZero()) && (_boardingDirection.IsNonZero() || !localDir.IsNonZero()))
		{
			return;
		}
		if (!isBoarding || !localDir.IsNonZero())
		{
			for (int i = 0; i < _shipPlacementPositions.Count; i++)
			{
				_shipPlacementPositions[i].ResetExtraPosition();
			}
		}
		_isBoarding = isBoarding;
		_boardingDirection = localDir;
		_isTickRequired = true;
	}

	public void Tick()
	{
		float num = 0f;
		int num2 = -1;
		float num3 = float.MaxValue;
		int num4 = -1;
		PositionCondition positionCondition = PositionCondition.Any;
		bool flag = false;
		float resultPossibleGain = 0f;
		float resultScore = 0f;
		PositionCondition outGainCondition = PositionCondition.Any;
		bool requestExtraAgent = false;
		for (int i = 0; i < _shipPlacementPositions.Count; i++)
		{
			if (_isBoarding)
			{
				_shipPlacementPositions[i].CalculateBoardingScore(_boardingDirection, out resultScore, out resultPossibleGain, out outGainCondition, out requestExtraAgent);
			}
			else if (_isUnderMissileFire)
			{
				_shipPlacementPositions[i].CalculateUnderMissileFireScore(out resultScore, out resultPossibleGain, out outGainCondition);
			}
			else
			{
				_shipPlacementPositions[i].CalculateDefaultScore(out resultScore, out resultPossibleGain, out outGainCondition);
			}
			if (resultPossibleGain > num)
			{
				num = resultPossibleGain;
				num2 = i;
				positionCondition = outGainCondition;
				flag = requestExtraAgent;
			}
		}
		for (int j = 0; j < _shipPlacementPositions.Count; j++)
		{
			if (_shipPlacementPositions[j].AssignedAgent != null && !_shipPlacementPositions[j].LentToOtherFrame && CheckCondition(positionCondition, _shipPlacementPositions[j].AssignedAgent))
			{
				if (_isBoarding)
				{
					_shipPlacementPositions[j].CalculateBoardingScore(_boardingDirection, out resultScore, out resultPossibleGain, out outGainCondition, out requestExtraAgent);
				}
				else if (_isUnderMissileFire)
				{
					_shipPlacementPositions[j].CalculateUnderMissileFireScore(out resultScore, out resultPossibleGain, out outGainCondition);
				}
				else
				{
					_shipPlacementPositions[j].CalculateDefaultScore(out resultScore, out resultPossibleGain, out outGainCondition);
				}
				if (resultScore < num3)
				{
					num3 = resultScore;
					num4 = j;
				}
			}
		}
		if (num2 != num4 && num2 > -1 && num4 > -1 && num > num3)
		{
			Agent assignedAgent = _shipPlacementPositions[num2].AssignedAgent;
			Agent assignedAgent2 = _shipPlacementPositions[num4].AssignedAgent;
			if (flag)
			{
				_shipPlacementPositions[num4].LendToExtraPosition(num2);
				_shipPlacementPositions[num2].SetExtraAgent(assignedAgent2);
			}
			else
			{
				_shipPlacementPositions[num2].SetAgent(assignedAgent2);
				_agents[num2] = assignedAgent2;
				assignedAgent2.SetDetachmentIndex(num2);
				if (assignedAgent != null)
				{
					_shipPlacementPositions[num4].SetAgent(assignedAgent);
					_agents[num4] = assignedAgent;
					assignedAgent.SetDetachmentIndex(num4);
				}
				else
				{
					_shipPlacementPositions[num4].RemoveAgent();
					_agents[num4] = null;
				}
			}
			_isTickRequired = true;
		}
		else
		{
			_isTickRequired = false;
			_placementDetachmentTimer.Reset();
		}
	}

	public WorldFrame? GetAgentFrame(Agent agent)
	{
		ShipPlacementPosition shipPlacementPosition = _shipPlacementPositions[agent.DetachmentIndex];
		if (shipPlacementPosition.LentToOtherFrame)
		{
			shipPlacementPosition = _shipPlacementPositions[shipPlacementPosition.ExtraFrameIndex];
		}
		agent.EnforceShieldUsage((_isUnderMissileFire && !agent.HasAnyRangedWeaponCached) ? ((shipPlacementPosition.IsOuterPos && agent.HasShieldCached) ? Agent.UsageDirection.DefendDown : Agent.UsageDirection.AttackEnd) : Agent.UsageDirection.None);
		MatrixFrame m = shipPlacementPosition.LocalFrame;
		if (_isBoarding && shipPlacementPosition.HasExtraAgent)
		{
			m = new MatrixFrame(in m.rotation, new Vec3(m.origin.x, m.origin.y + ((agent == shipPlacementPosition.AssignedAgent) ? (-0.5f) : 0.5f), m.origin.z));
		}
		MatrixFrame matrixFrame = _ownerShip.GlobalFrame.TransformToParent(in m);
		Mat3 rotation;
		if ((shipPlacementPosition.IsOuterPos && (agent.HasAnyRangedWeaponCached || _isBoarding)) || (_isUnderMissileFire && agent.HasShieldCached))
		{
			if (m.origin.x > 0f)
			{
				Vec3 s = -_ownerShip.GlobalFrame.rotation.f;
				MatrixFrame globalFrame = _ownerShip.GlobalFrame;
				ref Vec3 s2 = ref globalFrame.rotation.s;
				MatrixFrame globalFrame2 = _ownerShip.GlobalFrame;
				rotation = new Mat3(in s, in s2, in globalFrame2.rotation.u);
			}
			else
			{
				MatrixFrame globalFrame = _ownerShip.GlobalFrame;
				ref Vec3 f = ref globalFrame.rotation.f;
				Vec3 s = -_ownerShip.GlobalFrame.rotation.s;
				MatrixFrame globalFrame2 = _ownerShip.GlobalFrame;
				rotation = new Mat3(in f, in s, in globalFrame2.rotation.u);
			}
		}
		else
		{
			rotation = matrixFrame.rotation;
		}
		agent.SetCrouchMode(_isUnderMissileFire && !agent.HasAnyRangedWeaponCached && !agent.HasShieldCached && agent.Position.DistanceSquared(matrixFrame.origin) <= 1f);
		return new WorldFrame(rotation, matrixFrame.origin.ToWorldPosition());
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

	public Agent PickLastAgent()
	{
		Agent result = null;
		for (int num = _agents.Length - 1; num >= 0; num--)
		{
			if (_agents[num] != null)
			{
				result = _agents[num];
				RemoveAgent(result);
				result.Formation.AttachUnit(result);
				return result;
			}
		}
		return result;
	}

	private static bool CheckCondition(PositionCondition positionCondition, Agent checkedAgent)
	{
		switch (positionCondition)
		{
		case PositionCondition.Any:
			return true;
		case PositionCondition.RangedOrShield:
			if (!checkedAgent.HasShieldCached)
			{
				return checkedAgent.HasAnyRangedWeaponCached;
			}
			return true;
		case PositionCondition.Ranged:
			return checkedAgent.HasAnyRangedWeaponCached;
		default:
			return false;
		}
	}
}
