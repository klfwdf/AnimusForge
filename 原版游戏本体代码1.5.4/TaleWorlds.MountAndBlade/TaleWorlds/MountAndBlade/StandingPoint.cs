using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class StandingPoint : UsableMissionObject
{
	public struct StackArray8StandingPoint
	{
		private StandingPoint _element0;

		private StandingPoint _element1;

		private StandingPoint _element2;

		private StandingPoint _element3;

		private StandingPoint _element4;

		private StandingPoint _element5;

		private StandingPoint _element6;

		private StandingPoint _element7;

		public const int Length = 8;

		public StandingPoint this[int index]
		{
			get
			{
				return index switch
				{
					0 => _element0, 
					1 => _element1, 
					2 => _element2, 
					3 => _element3, 
					4 => _element4, 
					5 => _element5, 
					6 => _element6, 
					7 => _element7, 
					_ => null, 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				case 3:
					_element3 = value;
					break;
				case 4:
					_element4 = value;
					break;
				case 5:
					_element5 = value;
					break;
				case 6:
					_element6 = value;
					break;
				case 7:
					_element7 = value;
					break;
				}
			}
		}
	}

	private struct AgentDistanceCache
	{
		public Vec2 AgentPosition;

		public Vec2 StandingPointPosition;

		public float PathDistance;
	}

	private enum ValidControllerType
	{
		None,
		PlayerOnly,
		AIOnly,
		PlayerOrAI
	}

	public bool AutoSheathWeapons = true;

	public bool AutoEquipWeaponsOnUseStopped;

	private bool _autoAttachOnUsingStopped = true;

	private Action<Agent, bool> _onUsingStoppedAction;

	public bool AutoWieldWeapons;

	public readonly bool TranslateUser = true;

	public bool HasRecentlyBeenRechecked;

	private Dictionary<Agent, AgentDistanceCache> _cachedAgentDistances;

	[EditableScriptComponentVariable(true, "")]
	private bool _useOwnPositionInsteadOfWorldPosition;

	[EditableScriptComponentVariable(true, "")]
	private float _customPlayerInteractionDistance;

	private bool _needsSingleThreadTickOnce;

	private ValidControllerType _validControllerType = ValidControllerType.PlayerOrAI;

	protected BattleSideEnum StandingPointSide = BattleSideEnum.None;

	public virtual Agent.AIScriptedFrameFlags DisableScriptedFrameFlags => Agent.AIScriptedFrameFlags.None;

	public override bool DisableCombatActionsOnUse => false;

	[EditableScriptComponentVariable(false, "")]
	public Agent FavoredUser { get; set; }

	public virtual bool PlayerStopsUsingWhenInteractsWithOther => true;

	public bool UseOwnPositionInsteadOfWorldPosition => _useOwnPositionInsteadOfWorldPosition;

	public float CustomPlayerInteractionDistance => _customPlayerInteractionDistance;

	protected internal override void OnInit()
	{
		base.OnInit();
		_cachedAgentDistances = new Dictionary<Agent, AgentDistanceCache>();
		bool flag = base.GameEntity.HasTag("attacker");
		bool flag2 = base.GameEntity.HasTag("defender");
		if (flag && !flag2)
		{
			StandingPointSide = BattleSideEnum.Attacker;
		}
		else if (!flag && flag2)
		{
			StandingPointSide = BattleSideEnum.Defender;
		}
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void OnParentMachinePhysicsStateChanged()
	{
		base.GameEntityWithWorldPosition.InvalidateWorldPosition();
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (!base.IsDisabledForAgent(agent))
		{
			if (StandingPointSide != BattleSideEnum.None && agent.IsAIControlled && agent.Team != null)
			{
				return agent.Team.Side != StandingPointSide;
			}
			return false;
		}
		return true;
	}

	public override TickRequirement GetTickRequirement()
	{
		if (!GameNetwork.IsClientOrReplay && base.HasUser)
		{
			return base.GetTickRequirement() | TickRequirement.Tick | TickRequirement.TickParallel3;
		}
		return base.GetTickRequirement();
	}

	private void TickAux(bool isParallel)
	{
		if (GameNetwork.IsClientOrReplay || !base.HasUser)
		{
			return;
		}
		if (!base.UserAgent.IsActive() || DoesActionTypeStopUsingGameObject(MBAnimation.GetActionType(base.UserAgent.GetCurrentAction(0))))
		{
			if (isParallel)
			{
				_needsSingleThreadTickOnce = true;
				return;
			}
			Agent userAgent = base.UserAgent;
			Agent.StopUsingGameObjectFlags stopUsingGameObjectFlags = Agent.StopUsingGameObjectFlags.None;
			if (_autoAttachOnUsingStopped)
			{
				stopUsingGameObjectFlags |= Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject;
			}
			userAgent.StopUsingGameObject(isSuccessful: false, stopUsingGameObjectFlags);
			_onUsingStoppedAction?.Invoke(userAgent, arg2: true);
		}
		else if (AutoSheathWeapons)
		{
			if (base.UserAgent.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
			{
				if (isParallel)
				{
					_needsSingleThreadTickOnce = true;
				}
				else
				{
					base.UserAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
				}
			}
			if (base.UserAgent.GetOffhandWieldedItemIndex() != EquipmentIndex.None)
			{
				if (isParallel)
				{
					_needsSingleThreadTickOnce = true;
				}
				else
				{
					base.UserAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
				}
			}
		}
		else if (AutoWieldWeapons && base.UserAgent.Equipment.HasAnyWeapon() && base.UserAgent.GetPrimaryWieldedItemIndex() == EquipmentIndex.None && base.UserAgent.GetOffhandWieldedItemIndex() == EquipmentIndex.None)
		{
			if (isParallel)
			{
				_needsSingleThreadTickOnce = true;
			}
			else
			{
				base.UserAgent.WieldInitialWeapons(Agent.WeaponWieldActionType.Instant);
			}
		}
	}

	protected internal override void OnTickParallel3(float dt)
	{
		TickAux(isParallel: true);
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_needsSingleThreadTickOnce)
		{
			_needsSingleThreadTickOnce = false;
			TickAux(isParallel: false);
		}
	}

	protected virtual bool DoesActionTypeStopUsingGameObject(Agent.ActionCodeType actionType)
	{
		if (actionType != Agent.ActionCodeType.Jump && actionType != Agent.ActionCodeType.Kick)
		{
			return actionType == Agent.ActionCodeType.WeaponBash;
		}
		return true;
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		if (!_autoAttachOnUsingStopped && MovingAgent != null)
		{
			Agent movingAgent = MovingAgent;
			movingAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
			_onUsingStoppedAction?.Invoke(movingAgent, arg2: false);
		}
		base.OnUse(userAgent, agentBoneIndex);
		if (LockUserFrames)
		{
			WorldFrame userFrameForAgent = GetUserFrameForAgent(userAgent);
			userAgent.SetTargetPositionAndDirection(userFrameForAgent.Origin.AsVec2, in userFrameForAgent.Rotation.f);
		}
		else if (LockUserPositions)
		{
			userAgent.SetTargetPosition(GetUserFrameForAgent(userAgent).Origin.AsVec2);
		}
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		if (LockUserFrames || LockUserPositions)
		{
			userAgent.ClearTargetFrame();
		}
	}

	public override WorldFrame GetUserFrameForAgent(Agent agent)
	{
		if (!Mission.Current.IsTeleportingAgents && !TranslateUser)
		{
			return agent.GetWorldFrame();
		}
		if (!Mission.Current.IsTeleportingAgents && (LockUserFrames || LockUserPositions))
		{
			return base.GetUserFrameForAgent(agent);
		}
		WorldFrame userFrameForAgent = base.GetUserFrameForAgent(agent);
		MatrixFrame lookFrame = agent.LookFrame;
		Vec2 vec = (lookFrame.origin.AsVec2 - userFrameForAgent.Origin.AsVec2).Normalized();
		Vec2 vec2 = userFrameForAgent.Origin.AsVec2 + agent.GetInteractionDistanceToUsable(this) * 0.5f * vec;
		Mat3 rotation = lookFrame.rotation;
		userFrameForAgent.Origin.SetVec2(vec2);
		userFrameForAgent.Rotation = rotation;
		return userFrameForAgent;
	}

	public virtual bool HasAlternative()
	{
		return false;
	}

	public virtual float GetUsageScoreForAgent(Agent agent)
	{
		WorldPosition userPosition = GetUserFrameForAgent(agent).Origin;
		WorldPosition agentPosition = agent.GetWorldPosition();
		float pathDistance = GetPathDistance(agent, ref userPosition, ref agentPosition);
		float num = ((pathDistance < 0f) ? float.MinValue : (0f - pathDistance));
		if (agent == FavoredUser)
		{
			num *= 0.5f;
		}
		return num;
	}

	public virtual float GetUsageScoreForAgent((Agent, float) agentPair)
	{
		float item = agentPair.Item2;
		float num = ((item < 0f) ? float.MinValue : (0f - item));
		if (agentPair.Item1 == FavoredUser)
		{
			num *= 0.5f;
		}
		return num;
	}

	public void SetupOnUsingStoppedBehavior(bool autoAttach, Action<Agent, bool> action)
	{
		_autoAttachOnUsingStopped = autoAttach;
		_onUsingStoppedAction = action;
	}

	private float GetPathDistance(Agent agent, ref WorldPosition userPosition, ref WorldPosition agentPosition)
	{
		float pathDistance;
		if (_cachedAgentDistances.TryGetValue(agent, out var value))
		{
			if (value.AgentPosition.DistanceSquared(agentPosition.AsVec2) < 1f && value.StandingPointPosition.DistanceSquared(userPosition.AsVec2) < 1f)
			{
				pathDistance = value.PathDistance;
			}
			else
			{
				if (!Mission.Current.Scene.GetPathDistanceBetweenPositions(ref userPosition, ref agentPosition, agent.Monster.BodyCapsuleRadius, out pathDistance))
				{
					pathDistance = float.MaxValue;
				}
				value = new AgentDistanceCache
				{
					AgentPosition = agentPosition.AsVec2,
					StandingPointPosition = userPosition.AsVec2,
					PathDistance = pathDistance
				};
				_cachedAgentDistances[agent] = value;
			}
		}
		else
		{
			if (!Mission.Current.Scene.GetPathDistanceBetweenPositions(ref userPosition, ref agentPosition, agent.Monster.BodyCapsuleRadius, out pathDistance))
			{
				pathDistance = float.MaxValue;
			}
			value = new AgentDistanceCache
			{
				AgentPosition = agentPosition.AsVec2,
				StandingPointPosition = userPosition.AsVec2,
				PathDistance = pathDistance
			};
			_cachedAgentDistances[agent] = value;
		}
		return pathDistance;
	}

	public override void OnEndMission()
	{
		base.OnEndMission();
		FavoredUser = null;
	}

	protected internal virtual bool IsUsableBySide(BattleSideEnum side)
	{
		if (!base.IsDeactivated && (base.IsInstantUse || !base.HasUser))
		{
			if (StandingPointSide != BattleSideEnum.None)
			{
				return side == StandingPointSide;
			}
			return true;
		}
		return false;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return null;
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		return _validControllerType switch
		{
			ValidControllerType.None => false, 
			ValidControllerType.PlayerOnly => userAgent.IsPlayerControlled, 
			ValidControllerType.AIOnly => userAgent.IsAIControlled, 
			ValidControllerType.PlayerOrAI => true, 
			_ => true, 
		};
	}

	public void SetUsableByAIOnly()
	{
		_validControllerType = ValidControllerType.AIOnly;
	}

	public void SetUsableByPlayerOnly()
	{
		_validControllerType = ValidControllerType.PlayerOnly;
	}

	public void SetUsableByPlayerOrAI()
	{
		_validControllerType = ValidControllerType.PlayerOrAI;
	}
}
