using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions;

public class AgentNavalAIComponent : AgentComponent
{
	public enum AgentNavalTaunts
	{
		Invite,
		Invite2,
		Point
	}

	private enum AgentJumpOffDecisionType
	{
		None,
		MovingWithoutDetachment,
		MovingWithDetachment
	}

	private const float CheckBridgeAndTargetingAgentCooldown = 3f;

	private const float BarkCooldown = 1.5f;

	private const float MediumMoraleThreshold = 70f;

	private float _tauntTimer;

	private float _barkTimer;

	private float _checkBridgesAndTargetingAgentTimer;

	private float _tauntCooldown = 12f + MBRandom.RandomFloat * 2f;

	private float _tauntDelayTimer;

	private float _barkDelayTimer;

	private float _tauntDelay;

	private float _barkDelay;

	private bool _tauntFired;

	private bool _barkFired;

	private AgentNavalComponent _agentNavalComponent;

	private NavalShipsLogic _navalShipsLogic;

	private ActionIndexCache _currentActionIndexCache;

	private SkinVoiceManager.SkinVoiceType _currentVoiceType;

	private bool _isConnectedToEnemyWithoutBridges;

	private AgentJumpOffDecisionType _jumpOffDecisionType;

	private bool _shouldTrySwimmingToShore;

	private MatrixFrame _targetFrameForSwimmingToShore;

	public AgentNavalAIComponent(Agent agent)
		: base(agent)
	{
		_tauntTimer = 0f;
		_barkTimer = 0f;
		_checkBridgesAndTargetingAgentTimer = 0f;
		_tauntDelay = 0f;
		_barkDelay = 0f;
		_tauntDelayTimer = 0f;
		_barkDelayTimer = 0f;
		_tauntFired = false;
		_barkFired = false;
		_isConnectedToEnemyWithoutBridges = false;
		_currentActionIndexCache = ActionIndexCache.act_none;
		_agentNavalComponent = Agent.GetComponent<AgentNavalComponent>();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
	}

	public bool UnderMeleeAttack(float timeLimit = 1f)
	{
		return Mission.Current.CurrentTime - Agent.LastRecievedMeleeHitTime < timeLimit;
	}

	public bool UnderRangedAttack(float timeLimit = 1f)
	{
		return Mission.Current.CurrentTime - Agent.LastRecievedMeleeHitTime < timeLimit;
	}

	public bool RangeAttacking(float timeLimit = 1f)
	{
		return Mission.Current.CurrentTime - Agent.LastRangedHitTime < timeLimit;
	}

	public bool MeleeAttacking(float timeLimit = 1f)
	{
		return Mission.Current.CurrentTime - Agent.LastMeleeHitTime < timeLimit;
	}

	private bool DecideBoardingTaunts()
	{
		bool result = false;
		float morale = Agent.GetMorale();
		if (!Agent.IsUsingGameObject && morale > 70f && _agentNavalComponent.SteppedShip != null)
		{
			float randomFloat = MBRandom.RandomFloat;
			if (_isConnectedToEnemyWithoutBridges)
			{
				if (randomFloat < 0.33f)
				{
					TryToTriggerTaunt(AgentNavalTaunts.Invite, 0.1f + MBRandom.RandomFloat * 1.5f, 0.1f);
				}
				else if (randomFloat < 0.66f)
				{
					TryToTriggerTaunt(AgentNavalTaunts.Invite2, 0.1f + MBRandom.RandomFloat * 1.5f, 0.1f);
				}
				else
				{
					TryToTriggerTaunt(AgentNavalTaunts.Point, 0.1f + MBRandom.RandomFloat * 1.5f, 0.1f);
				}
				result = true;
			}
		}
		return result;
	}

	private bool DecideTaunt()
	{
		bool result = false;
		if (Agent.IsAIControlled)
		{
			result = DecideBoardingTaunts();
		}
		return result;
	}

	public override void OnTickParallel(float dt)
	{
		_tauntTimer += dt;
		_tauntDelayTimer += dt;
		if (_tauntTimer >= _tauntCooldown)
		{
			DecideTaunt();
			_tauntTimer = 0f;
		}
		ExecuteTaunt();
	}

	public override void OnTick(float dt)
	{
		if (_jumpOffDecisionType != AgentJumpOffDecisionType.None && _agentNavalComponent.SteppedShip == null && (Agent.IsOnLand() || Agent.IsInWater()))
		{
			if (Agent.HumanAIComponent.GetCurrentlyMovingGameObject() != null)
			{
				switch (_jumpOffDecisionType)
				{
				case AgentJumpOffDecisionType.MovingWithoutDetachment:
					Agent.AIMoveToGameObjectDisable();
					break;
				case AgentJumpOffDecisionType.MovingWithDetachment:
					if (Agent.Detachment != null)
					{
						Agent.TryAttachToFormation();
					}
					break;
				default:
					Debug.FailedAssert("Invalid AgentJumpOffDecisionType state while moving to the machine.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\AgentNavalAIComponent.cs", "OnTick", 182);
					break;
				}
			}
			_jumpOffDecisionType = AgentJumpOffDecisionType.None;
		}
		if (Agent.IsAIControlled && !_agentNavalComponent.IsJumpingOffOnCooldown && _agentNavalComponent.SteppedShip != null && _agentNavalComponent.SteppedShip.BeingAbandoned && !Agent.IsUsingGameObject && Agent.HumanAIComponent.GetCurrentlyMovingGameObject() == null && Agent.IsOnLand())
		{
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _agentNavalComponent.SteppedShip.AttachmentPointMachines)
			{
				if (attachmentPointMachine.IsDisabledForAI || attachmentPointMachine.PilotStandingPoint.HasAIMovingTo || attachmentPointMachine.PilotAgent != null || attachmentPointMachine.CurrentAttachment != null)
				{
					continue;
				}
				if (Agent.Formation == null)
				{
					Agent.AIMoveToGameObjectEnable(attachmentPointMachine.PilotStandingPoint, attachmentPointMachine);
					_jumpOffDecisionType = AgentJumpOffDecisionType.MovingWithoutDetachment;
				}
				else if (Agent.Formation == _agentNavalComponent.SteppedShip.Formation)
				{
					if (Agent.Detachment != null)
					{
						Agent.TryAttachToFormation();
					}
					attachmentPointMachine.AddAgentAtSlotIndex(Agent, 0);
					_jumpOffDecisionType = AgentJumpOffDecisionType.MovingWithDetachment;
				}
				break;
			}
		}
		if (_shouldTrySwimmingToShore && _agentNavalComponent.SteppedShip == null)
		{
			if (!Agent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.GoToPosition) && Agent.IsInWater())
			{
				WorldPosition position = (_targetFrameForSwimmingToShore.origin + _targetFrameForSwimmingToShore.rotation.f * 1f).ToWorldPosition();
				Agent.SetScriptedPosition(ref position, addHumanLikeDelay: false);
			}
			else if (Agent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.GoToPosition) && Agent.IsOnLand())
			{
				Agent.DisableScriptedMovement();
			}
		}
		_barkTimer += dt;
		_barkDelayTimer += dt;
		_checkBridgesAndTargetingAgentTimer += dt;
		ExecuteBark();
		if (_checkBridgesAndTargetingAgentTimer >= 3f)
		{
			_isConnectedToEnemyWithoutBridges = _agentNavalComponent.SteppedShip != null && _agentNavalComponent.SteppedShip.GetIsConnectedToEnemyWithoutBridges();
			_checkBridgesAndTargetingAgentTimer = 0f;
		}
	}

	private void ExecuteTaunt()
	{
		if (_tauntFired && _tauntDelayTimer >= _tauntDelay)
		{
			Agent.SetActionChannel(1, in _currentActionIndexCache, ignorePriority: false, (AnimFlags)0uL);
			_tauntDelayTimer = 0f;
			_tauntFired = false;
		}
	}

	private void ExecuteBark()
	{
		if (_barkFired && _barkDelayTimer >= _barkDelay)
		{
			Agent.MakeVoice(_currentVoiceType, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			_barkDelayTimer = 0f;
			_barkFired = false;
		}
	}

	public void TryToTriggerTaunt(AgentNavalTaunts navalTaunt, float delay, float chanceToTrigger = 1f, bool makeTimerZeroIfSuccessful = false)
	{
		if (chanceToTrigger >= MBRandom.RandomFloat && !Agent.IsInBeingStruckAction && Agent.IsOnLand() && (makeTimerZeroIfSuccessful || (_tauntTimer >= _tauntCooldown && !_tauntFired)) && !UnderMeleeAttack() && !UnderRangedAttack() && !RangeAttacking() && !MeleeAttacking())
		{
			_currentActionIndexCache = SelectActionForTaunt(navalTaunt);
			_tauntDelay = delay;
			if (makeTimerZeroIfSuccessful)
			{
				Agent.SetActionChannel(1, in _currentActionIndexCache, ignorePriority: false, (AnimFlags)0uL);
				_tauntFired = false;
			}
			else
			{
				_tauntDelayTimer = 0f;
				_tauntFired = true;
			}
		}
	}

	public void TryToTriggerBark(SkinVoiceManager.SkinVoiceType voiceType, float delay, float chanceToTrigger = 1f, bool makeTimerZeroIfSuccessful = false)
	{
		if (_barkTimer >= 1.5f && chanceToTrigger >= MBRandom.RandomFloat && (Mission.Current.MainAgent == null || Mission.Current.MainAgent.Position.DistanceSquared(Agent.Position) < 625f))
		{
			_barkTimer = 0f;
			_barkDelay = delay;
			_barkDelayTimer = 0f;
			_currentVoiceType = voiceType;
			if (makeTimerZeroIfSuccessful)
			{
				Agent.MakeVoice(_currentVoiceType, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				_barkFired = false;
			}
			else
			{
				_barkFired = true;
			}
		}
	}

	private ActionIndexCache SelectActionForTaunt(AgentNavalTaunts navalTaunt)
	{
		ActionIndexCache result = ActionIndexCache.act_none;
		EquipmentIndex primaryWieldedItemIndex = Agent.GetPrimaryWieldedItemIndex();
		EquipmentIndex offhandWieldedItemIndex = Agent.GetOffhandWieldedItemIndex();
		WeaponComponentData mainHandWeapon = ((primaryWieldedItemIndex != EquipmentIndex.None) ? Agent.Equipment[primaryWieldedItemIndex].CurrentUsageItem : null);
		WeaponComponentData weaponComponentData = ((offhandWieldedItemIndex != EquipmentIndex.None) ? Agent.Equipment[offhandWieldedItemIndex].CurrentUsageItem : null);
		bool hasMount = Agent.HasMount;
		bool isLeftStance = Agent.GetIsLeftStance();
		int num = -1;
		switch (navalTaunt)
		{
		case AgentNavalTaunts.Invite:
			num = ((weaponComponentData == null || !weaponComponentData.IsShield) ? TauntUsageManager.Instance.GetIndexOfAction("taunt_10") : TauntUsageManager.Instance.GetIndexOfAction("taunt_13"));
			break;
		case AgentNavalTaunts.Invite2:
			num = TauntUsageManager.Instance.GetIndexOfAction("taunt_11");
			break;
		case AgentNavalTaunts.Point:
			num = TauntUsageManager.Instance.GetIndexOfAction("taunt_17");
			break;
		}
		if (num != -1)
		{
			result = ActionIndexCache.Create(TauntUsageManager.Instance.GetAction(num, isLeftStance, !hasMount, mainHandWeapon, weaponComponentData));
		}
		return result;
	}

	public void ActivateSwimToShore(MatrixFrame targetFrame)
	{
		_targetFrameForSwimmingToShore = targetFrame;
		_shouldTrySwimmingToShore = true;
	}

	public void DeactivateSwimToShore()
	{
		_shouldTrySwimmingToShore = false;
	}
}
