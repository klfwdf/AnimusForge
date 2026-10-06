using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class MissionAgentLookHandler : MissionLogic
{
	private class PointOfInterest
	{
		public const int MaxSelectDistanceForAgent = 5;

		public const int MaxSelectDistanceForFrame = 4;

		private readonly int _selectDistance;

		private readonly int _releaseDistanceSquare;

		private readonly Agent _agent;

		private readonly MatrixFrame _frame;

		private readonly bool _ignoreDirection;

		private readonly int _priority;

		public bool IsActive
		{
			get
			{
				if (_agent != null)
				{
					return _agent.IsActive();
				}
				return true;
			}
		}

		public PointOfInterest(Agent agent)
		{
			_agent = agent;
			_selectDistance = 5;
			_releaseDistanceSquare = 36;
			_ignoreDirection = false;
			CharacterObject characterObject = (CharacterObject)agent.Character;
			if (!agent.IsHuman)
			{
				_priority = 1;
			}
			else if (characterObject.IsHero)
			{
				_priority = 5;
			}
			else if (characterObject.Occupation == Occupation.HorseTrader || characterObject.Occupation == Occupation.Weaponsmith || characterObject.Occupation == Occupation.GoodsTrader || characterObject.Occupation == Occupation.Armorer || characterObject.Occupation == Occupation.Blacksmith)
			{
				_priority = 3;
			}
			else
			{
				_priority = 1;
			}
		}

		public PointOfInterest(MatrixFrame frame)
		{
			_frame = frame;
			_selectDistance = 4;
			_releaseDistanceSquare = 25;
			_ignoreDirection = true;
			_priority = 2;
		}

		public float GetScore(Agent agent)
		{
			if (agent == _agent || GetBasicPosition().DistanceSquared(agent.Position) > (float)(_selectDistance * _selectDistance))
			{
				return -1f;
			}
			Vec3 vec = GetTargetPosition() - agent.GetEyeGlobalPosition();
			float num = vec.Normalize();
			if (Vec2.DotProduct(vec.AsVec2, agent.GetMovementDirection()) < 0.7f)
			{
				return -1f;
			}
			float num2 = (float)(_priority * _selectDistance) / num;
			if (IsMoving())
			{
				num2 *= 5f;
			}
			if (!_ignoreDirection)
			{
				float num3 = Vec2.DotProduct(GetTargetFrame().rotation.f.AsVec2, agent.Frame.rotation.f.AsVec2);
				if (num3 < -0.7f)
				{
					num2 *= 2f;
				}
				else if (MathF.Abs(num3) < 0.1f)
				{
					num2 *= 2f;
				}
			}
			return num2;
		}

		public Vec3 GetTargetPosition()
		{
			return _agent?.GetEyeGlobalPosition() ?? _frame.origin;
		}

		public Vec3 GetBasicPosition()
		{
			if (_agent == null)
			{
				return _frame.origin;
			}
			return _agent.Position;
		}

		private bool IsMoving()
		{
			if (_agent != null)
			{
				return _agent.GetCurrentVelocity().LengthSquared > 0.040000003f;
			}
			return true;
		}

		private MatrixFrame GetTargetFrame()
		{
			if (_agent == null)
			{
				return _frame;
			}
			return _agent.Frame;
		}

		public bool IsVisibleFor(Agent agent)
		{
			Vec3 basicPosition = GetBasicPosition();
			Vec3 position = agent.Position;
			if (agent == _agent || position.DistanceSquared(basicPosition) > (float)_releaseDistanceSquare)
			{
				return false;
			}
			Vec3 vec = basicPosition - position;
			vec.Normalize();
			return Vec2.DotProduct(vec.AsVec2, agent.GetMovementDirection()) > 0.4f;
		}

		public bool IsRelevant(Agent agent)
		{
			return agent == _agent;
		}
	}

	private class LookInfo
	{
		public readonly Agent Agent;

		public PointOfInterest PointOfInterest;

		public readonly Timer CheckTimer;

		public LookInfo(Agent agent, float checkTime)
		{
			Agent = agent;
			CheckTimer = new Timer(Mission.Current.CurrentTime, checkTime);
		}

		public void Reset(PointOfInterest pointOfInterest, float duration)
		{
			if (PointOfInterest != pointOfInterest)
			{
				PointOfInterest = pointOfInterest;
				if (PointOfInterest != null)
				{
					Agent.SetLookToPointOfInterest(PointOfInterest.GetTargetPosition());
				}
				else if (Agent.IsActive())
				{
					Agent.DisableLookToPointOfInterest();
				}
			}
			CheckTimer.Reset(Mission.Current.CurrentTime, duration);
		}
	}

	private delegate PointOfInterest SelectionDelegate(Agent agent);

	private readonly List<PointOfInterest> _staticPointList;

	private readonly List<LookInfo> _checklist;

	private SelectionDelegate _selectionDelegate;

	public MissionAgentLookHandler()
	{
		_staticPointList = new List<PointOfInterest>();
		_checklist = new List<LookInfo>();
		_selectionDelegate = SelectRandomAccordingToScore;
	}

	public override void AfterStart()
	{
		AddStablePointsOfInterest();
	}

	private void AddStablePointsOfInterest()
	{
		foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag("point_of_interest"))
		{
			_staticPointList.Add(new PointOfInterest(item.GetGlobalFrame()));
		}
	}

	private void DebugTick()
	{
	}

	public override void OnMissionTick(float dt)
	{
		if (Game.Current.IsDevelopmentMode)
		{
			DebugTick();
		}
		float currentTime = base.Mission.CurrentTime;
		foreach (LookInfo item in _checklist)
		{
			if (!item.Agent.IsActive() || ConversationMission.ConversationAgents.Contains(item.Agent) || (ConversationMission.ConversationAgents.Any() && item.Agent.IsPlayerControlled))
			{
				continue;
			}
			if (item.CheckTimer.Check(currentTime))
			{
				PointOfInterest pointOfInterest = _selectionDelegate(item.Agent);
				if (pointOfInterest != null)
				{
					item.Reset(pointOfInterest, 5f);
				}
				else
				{
					item.Reset(null, 1f + MBRandom.RandomFloat);
				}
			}
			else if (item.PointOfInterest != null && (!item.PointOfInterest.IsActive || !item.PointOfInterest.IsVisibleFor(item.Agent)))
			{
				PointOfInterest pointOfInterest2 = _selectionDelegate(item.Agent);
				if (pointOfInterest2 != null)
				{
					item.Reset(pointOfInterest2, 5f + MBRandom.RandomFloat);
				}
				else
				{
					item.Reset(null, MBRandom.RandomFloat * 5f + 5f);
				}
			}
			else if (item.PointOfInterest != null)
			{
				Vec3 targetPosition = item.PointOfInterest.GetTargetPosition();
				item.Agent.SetLookToPointOfInterest(targetPosition);
			}
		}
	}

	private PointOfInterest SelectFirstNonAgent(Agent agent)
	{
		if (agent.IsAIControlled)
		{
			int num = MBRandom.RandomInt(_staticPointList.Count);
			int num2 = num;
			do
			{
				PointOfInterest pointOfInterest = _staticPointList[num2];
				if (pointOfInterest.GetScore(agent) > 0f)
				{
					return pointOfInterest;
				}
				num2 = ((num2 + 1 != _staticPointList.Count) ? (num2 + 1) : 0);
			}
			while (num2 != num);
		}
		return null;
	}

	private PointOfInterest SelectBestOfLimitedNonAgent(Agent agent)
	{
		int num = 3;
		PointOfInterest result = null;
		float num2 = -1f;
		if (agent.IsAIControlled)
		{
			int num3 = MBRandom.RandomInt(_staticPointList.Count);
			int num4 = num3;
			do
			{
				PointOfInterest pointOfInterest = _staticPointList[num4];
				float score = pointOfInterest.GetScore(agent);
				if (score > 0f)
				{
					if (score > num2)
					{
						num2 = score;
						result = pointOfInterest;
					}
					num--;
				}
				num4 = ((num4 + 1 != _staticPointList.Count) ? (num4 + 1) : 0);
			}
			while (num4 != num3 && num > 0);
		}
		return result;
	}

	private PointOfInterest SelectBest(Agent agent)
	{
		PointOfInterest result = null;
		float num = -1f;
		if (agent.IsAIControlled)
		{
			foreach (PointOfInterest staticPoint in _staticPointList)
			{
				float score = staticPoint.GetScore(agent);
				if (score > 0f && score > num)
				{
					num = score;
					result = staticPoint;
				}
			}
			AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(base.Mission, agent.Position.AsVec2, 5f);
			while (searchStruct.LastFoundAgent != null)
			{
				PointOfInterest pointOfInterest = new PointOfInterest(searchStruct.LastFoundAgent);
				float score2 = pointOfInterest.GetScore(agent);
				if (score2 > 0f && score2 > num)
				{
					num = score2;
					result = pointOfInterest;
				}
				AgentProximityMap.FindNext(base.Mission, ref searchStruct);
			}
		}
		return result;
	}

	private PointOfInterest SelectRandomAccordingToScore(Agent agent)
	{
		float num = 0f;
		List<KeyValuePair<float, PointOfInterest>> list = new List<KeyValuePair<float, PointOfInterest>>();
		if (agent.IsAIControlled)
		{
			foreach (PointOfInterest staticPoint in _staticPointList)
			{
				float score = staticPoint.GetScore(agent);
				if (score > 0f)
				{
					list.Add(new KeyValuePair<float, PointOfInterest>(score, staticPoint));
					num += score;
				}
			}
			AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, agent.Position.AsVec2, 5f);
			while (searchStruct.LastFoundAgent != null)
			{
				PointOfInterest pointOfInterest = new PointOfInterest(searchStruct.LastFoundAgent);
				float score2 = pointOfInterest.GetScore(agent);
				if (score2 > 0f)
				{
					list.Add(new KeyValuePair<float, PointOfInterest>(score2, pointOfInterest));
					num += score2;
				}
				AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		float num2 = MBRandom.RandomFloat * num;
		PointOfInterest value = list[list.Count - 1].Value;
		foreach (KeyValuePair<float, PointOfInterest> item in list)
		{
			num2 -= item.Key;
			if (num2 <= 0f)
			{
				value = item.Value;
				break;
			}
		}
		return value;
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (agent.IsHuman)
		{
			_checklist.Add(new LookInfo(agent, MBRandom.RandomFloat));
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		for (int i = 0; i < _checklist.Count; i++)
		{
			LookInfo lookInfo = _checklist[i];
			if (lookInfo.Agent == affectedAgent)
			{
				_checklist.RemoveAt(i);
				i--;
			}
			else if (lookInfo.PointOfInterest != null && lookInfo.PointOfInterest.IsRelevant(affectedAgent))
			{
				lookInfo.Reset(null, MBRandom.RandomFloat * 2f + 2f);
			}
		}
	}
}
