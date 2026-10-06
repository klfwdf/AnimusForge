using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

[DefaultView]
public class MissionGamepadEffectsView : MissionView
{
	private enum TriggerState
	{
		Off,
		SoftTriggerFeedbackLeft,
		SoftTriggerFeedbackRight,
		HardTriggerFeedbackLeft,
		HardTriggerFeedbackRight,
		WeaponEffect,
		Vibration
	}

	private TriggerState _triggerState;

	private readonly byte[] _triggerFeedback = new byte[4];

	private bool _isAdaptiveTriggerEnabled;

	private bool _usingAlternativeAiming;

	private RangedSiegeWeapon _currentlyUsedSiegeWeapon;

	private UsableMissionObject _currentlyUsedMissionObject;

	public override void OnMissionStateActivated()
	{
		base.OnMissionStateActivated();
		ResetTriggerFeedback();
		ResetTriggerVibration();
		_isAdaptiveTriggerEnabled = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableVibration) != 0f;
		_usingAlternativeAiming = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableAlternateAiming) != 0f;
		NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Combine(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	private void OnGamepadActiveStateChanged()
	{
		if (!TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			ResetTriggerFeedback();
			ResetTriggerVibration();
		}
	}

	public override void OnMissionStateDeactivated()
	{
		base.OnMissionStateDeactivated();
		ResetTriggerFeedback();
		ResetTriggerVibration();
		NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Remove(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	public override void OnPreMissionTick(float dt)
	{
		base.OnPreMissionTick(dt);
		Agent mainAgent = base.Mission.MainAgent;
		if (!_isAdaptiveTriggerEnabled)
		{
			return;
		}
		if (mainAgent != null && mainAgent.State == AgentState.Active && mainAgent.CombatActionsEnabled && !mainAgent.IsCheering && !base.Mission.IsOrderMenuOpen && IsMissionModeApplicableForAdaptiveTrigger(base.Mission.Mode))
		{
			MissionWeapon wieldedWeapon = mainAgent.WieldedWeapon;
			bool num = wieldedWeapon.CurrentUsageItem?.WeaponFlags.HasAllFlags(WeaponFlags.StringHeldByHand) ?? false;
			WeaponComponentData currentUsageItem = wieldedWeapon.CurrentUsageItem;
			bool flag = currentUsageItem != null && currentUsageItem.WeaponFlags.HasAllFlags(WeaponFlags.HasString) && !wieldedWeapon.CurrentUsageItem.WeaponFlags.HasAllFlags(WeaponFlags.StringHeldByHand);
			WeaponComponentData currentUsageItem2 = wieldedWeapon.CurrentUsageItem;
			bool flag2 = currentUsageItem2 != null && currentUsageItem2.IsRangedWeapon && wieldedWeapon.CurrentUsageItem.IsConsumable;
			WeaponComponentData currentUsageItem3 = wieldedWeapon.CurrentUsageItem;
			bool flag3 = (currentUsageItem3 != null && currentUsageItem3.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon)) || mainAgent.WieldedOffhandWeapon.IsShield();
			if (num)
			{
				HandleBowAdaptiveTriggers();
			}
			else if (flag)
			{
				HandleCrossbowAdaptiveTriggers();
			}
			else if (flag2)
			{
				HandleThrowableAdaptiveTriggers();
			}
			else if (flag3)
			{
				HandleMeleeAdaptiveTriggers();
			}
			else if (mainAgent.CurrentlyUsedGameObject != null)
			{
				if (mainAgent.CurrentlyUsedGameObject != _currentlyUsedMissionObject)
				{
					_currentlyUsedMissionObject = mainAgent.CurrentlyUsedGameObject;
					UsableMachine usableMachineFromUsableMissionObject = GetUsableMachineFromUsableMissionObject(_currentlyUsedMissionObject);
					_currentlyUsedSiegeWeapon = ((usableMachineFromUsableMissionObject is RangedSiegeWeapon rangedSiegeWeapon) ? rangedSiegeWeapon : null);
				}
				HandleRangedSiegeEngineAdaptiveTriggers(_currentlyUsedSiegeWeapon);
			}
			else
			{
				_currentlyUsedSiegeWeapon = null;
				_currentlyUsedMissionObject = null;
				ResetTriggerFeedback();
				ResetTriggerVibration();
			}
		}
		else
		{
			_currentlyUsedSiegeWeapon = null;
			_currentlyUsedMissionObject = null;
			ResetTriggerFeedback();
			ResetTriggerVibration();
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
		if (affectedAgent == Agent.Main)
		{
			if (attackCollisionData.CollisionResult == CombatCollisionResult.Blocked || attackCollisionData.CollisionResult == CombatCollisionResult.ChamberBlocked || attackCollisionData.CollisionResult == CombatCollisionResult.Parried)
			{
				float[] leftTriggerAmplitudes = new float[1] { 0.5f };
				float[] leftTriggerFrequencies = new float[1] { 0.3f };
				float[] array = new float[1] { 0.3f };
				SetTriggerVibration(leftTriggerAmplitudes, leftTriggerFrequencies, array, array.Length, null, null, null, 0);
				SetTriggerState(TriggerState.Off);
			}
			if (affectedAgent.WieldedOffhandWeapon.IsEmpty && attackCollisionData.AttackBlockedWithShield)
			{
				SetTriggerState(TriggerState.Off);
			}
		}
		else if (affectorAgent == Agent.Main && (attackCollisionData.CollisionResult == CombatCollisionResult.StrikeAgent || attackCollisionData.CollisionResult == CombatCollisionResult.Blocked) && !affectorWeapon.IsEmpty && affectorWeapon.IsShield())
		{
			float[] leftTriggerAmplitudes2 = new float[1] { 1f };
			float[] leftTriggerFrequencies2 = new float[1] { 0.1f };
			float[] array2 = new float[1] { 0.35f };
			SetTriggerVibration(leftTriggerAmplitudes2, leftTriggerFrequencies2, array2, array2.Length, null, null, null, 0);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent.IsMainAgent)
		{
			ResetTriggerFeedback();
			ResetTriggerVibration();
		}
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		SetTriggerState(TriggerState.Off);
	}

	private void OnNativeOptionChanged(NativeOptions.NativeOptionsType changedNativeOptionsType)
	{
		if (changedNativeOptionsType == NativeOptions.NativeOptionsType.EnableVibration)
		{
			bool isAdaptiveTriggerEnabled = _isAdaptiveTriggerEnabled;
			_isAdaptiveTriggerEnabled = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableVibration) != 0f;
			_usingAlternativeAiming = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableAlternateAiming) != 0f;
			if (isAdaptiveTriggerEnabled && !_isAdaptiveTriggerEnabled)
			{
				_currentlyUsedSiegeWeapon = null;
				_currentlyUsedMissionObject = null;
				ResetTriggerFeedback();
				ResetTriggerVibration();
			}
		}
	}

	private bool IsMissionModeApplicableForAdaptiveTrigger(MissionMode mode)
	{
		switch (mode)
		{
		case MissionMode.StartUp:
		case MissionMode.Battle:
		case MissionMode.Duel:
		case MissionMode.Stealth:
		case MissionMode.Tournament:
			return true;
		default:
			return false;
		}
	}

	private void HandleBowAdaptiveTriggers()
	{
		Agent mainAgent = base.Mission.MainAgent;
		switch (mainAgent?.GetCurrentActionStage(1) ?? Agent.ActionStage.None)
		{
		case Agent.ActionStage.None:
		case Agent.ActionStage.ReloadMidPhase:
		case Agent.ActionStage.ReloadLastPhase:
			SetTriggerState(_usingAlternativeAiming ? TriggerState.SoftTriggerFeedbackLeft : TriggerState.SoftTriggerFeedbackRight);
			break;
		case Agent.ActionStage.AttackReady:
		{
			float num = mainAgent.GetAimingTimer() - mainAgent.AgentDrivenProperties.WeaponUnsteadyBeginTime;
			if (num > 0f)
			{
				float num2 = mainAgent.AgentDrivenProperties.WeaponUnsteadyEndTime - mainAgent.AgentDrivenProperties.WeaponUnsteadyBeginTime;
				float amount = MBMath.ClampFloat(num / num2, 0f, 1f);
				float num3 = MBMath.Lerp(0f, 1f, amount);
				float[] array = new float[1] { num3 };
				float num4 = MBMath.ClampFloat(1f - num3, 0.1f, 1f);
				float[] array2 = new float[1] { num4 };
				float[] array3 = new float[1] { 0.05f };
				if (_usingAlternativeAiming)
				{
					SetTriggerVibration(array, array2, array3, array3.Length, null, null, null, 0);
				}
				else
				{
					SetTriggerVibration(null, null, null, 0, array, array2, array3, array3.Length);
				}
				_triggerState = TriggerState.Vibration;
			}
			else
			{
				SetTriggerState(_usingAlternativeAiming ? TriggerState.SoftTriggerFeedbackLeft : TriggerState.SoftTriggerFeedbackRight);
				float[] array4 = new float[1] { 0.07f };
				float[] array5 = new float[1] { 0.5f };
				float[] array6 = new float[1] { 0.5f };
				if (_usingAlternativeAiming)
				{
					SetTriggerVibration(array4, array5, array6, array6.Length, null, null, null, 0);
				}
				else
				{
					SetTriggerVibration(null, null, null, 0, array4, array5, array6, array6.Length);
				}
			}
			if (_usingAlternativeAiming)
			{
				SetTriggerWeaponEffect(0, 0, 0, 3, 7, 8);
			}
			else
			{
				SetTriggerWeaponEffect(0, 0, 0, 0, 0, 0);
			}
			break;
		}
		case Agent.ActionStage.AttackRelease:
			SetTriggerState(TriggerState.Off);
			break;
		default:
			SetTriggerState(TriggerState.Off);
			break;
		}
	}

	private void HandleCrossbowAdaptiveTriggers()
	{
		switch (base.Mission.MainAgent?.GetCurrentActionStage(1) ?? Agent.ActionStage.None)
		{
		case Agent.ActionStage.ReloadMidPhase:
			SetTriggerState(TriggerState.Off);
			break;
		case Agent.ActionStage.AttackRelease:
		{
			float[] rightTriggerAmplitudes = new float[1] { 0.01f };
			float[] rightTriggerFrequencies = new float[1] { 0.08f };
			float[] array = new float[1] { 0.05f };
			SetTriggerVibration(null, null, null, 0, rightTriggerAmplitudes, rightTriggerFrequencies, array, array.Length);
			SetTriggerState(TriggerState.Off);
			break;
		}
		case Agent.ActionStage.AttackReady:
			if (_usingAlternativeAiming)
			{
				SetTriggerWeaponEffect(0, 0, 0, 3, 7, 8);
			}
			break;
		default:
			if (!_usingAlternativeAiming)
			{
				SetTriggerWeaponEffect(0, 0, 0, 3, 7, 8);
			}
			break;
		}
	}

	private void HandleThrowableAdaptiveTriggers()
	{
		bool num = base.Mission.MainAgent.WieldedOffhandWeapon.CurrentUsageItem?.WeaponFlags.HasAnyFlag(WeaponFlags.CanBlockRanged) ?? false;
		_triggerFeedback[2] = 0;
		_triggerFeedback[3] = 3;
		if (num)
		{
			_triggerFeedback[0] = 4;
			_triggerFeedback[1] = 2;
		}
		else
		{
			_triggerFeedback[0] = 0;
			_triggerFeedback[1] = 0;
		}
		SetTriggerFeedback(_triggerFeedback[0], _triggerFeedback[1], _triggerFeedback[2], _triggerFeedback[3]);
	}

	private void HandleMeleeAdaptiveTriggers()
	{
		Agent mainAgent = base.Mission.MainAgent;
		MissionWeapon wieldedWeapon = mainAgent.WieldedWeapon;
		bool flag = wieldedWeapon.CurrentUsageItem?.WeaponFlags.HasAllFlags(WeaponFlags.NotUsableWithOneHand) ?? false;
		bool num = mainAgent.WieldedOffhandWeapon.CurrentUsageItem?.WeaponFlags.HasAnyFlag(WeaponFlags.CanBlockRanged) ?? false;
		if (flag)
		{
			_triggerFeedback[2] = 3;
			_triggerFeedback[3] = 0;
		}
		else if (wieldedWeapon.CurrentUsageItem == null)
		{
			_triggerFeedback[2] = 0;
			_triggerFeedback[3] = 0;
		}
		else
		{
			_triggerFeedback[2] = 4;
			_triggerFeedback[3] = 1;
		}
		if (num || flag || wieldedWeapon.CurrentUsageItem != null)
		{
			_triggerFeedback[0] = 4;
			_triggerFeedback[1] = 2;
		}
		else
		{
			_triggerFeedback[0] = 0;
			_triggerFeedback[1] = 0;
		}
		SetTriggerFeedback(_triggerFeedback[0], _triggerFeedback[1], _triggerFeedback[2], _triggerFeedback[3]);
	}

	private void HandleRangedSiegeEngineAdaptiveTriggers(RangedSiegeWeapon rangedSiegeWeapon)
	{
		if (rangedSiegeWeapon is Ballista || rangedSiegeWeapon is FireBallista)
		{
			if (rangedSiegeWeapon.State == RangedSiegeWeapon.WeaponState.Idle)
			{
				SetTriggerWeaponEffect(0, 0, 0, 4, 6, 10);
			}
			else if (rangedSiegeWeapon.State == RangedSiegeWeapon.WeaponState.Shooting || rangedSiegeWeapon.State == RangedSiegeWeapon.WeaponState.WaitingBeforeProjectileLeaving)
			{
				SetTriggerWeaponEffect(0, 0, 0, 0, 0, 0);
				float[] rightTriggerAmplitudes = new float[3] { 0.2f, 0.4f, 0.2f };
				float[] rightTriggerFrequencies = new float[3] { 0.2f, 0.4f, 0.2f };
				float[] array = new float[3] { 0.2f, 0.3f, 0.2f };
				SetTriggerVibration(null, null, null, 0, rightTriggerAmplitudes, rightTriggerFrequencies, array, array.Length);
			}
			else
			{
				ResetTriggerFeedback();
				ResetTriggerVibration();
			}
		}
		else
		{
			ResetTriggerFeedback();
			ResetTriggerVibration();
		}
	}

	private UsableMachine GetUsableMachineFromUsableMissionObject(UsableMissionObject usableMissionObject)
	{
		if (usableMissionObject is StandingPoint { GameEntity: var weakGameEntity })
		{
			while (weakGameEntity.IsValid && !weakGameEntity.HasScriptOfType<UsableMachine>())
			{
				weakGameEntity = weakGameEntity.Parent;
			}
			if (weakGameEntity.IsValid)
			{
				UsableMachine firstScriptOfType = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
				if (firstScriptOfType != null)
				{
					return firstScriptOfType;
				}
			}
		}
		return null;
	}

	private void SetTriggerState(TriggerState triggerState)
	{
		if (_triggerState != triggerState)
		{
			switch (triggerState)
			{
			case TriggerState.Off:
				ResetTriggerFeedback();
				ResetTriggerVibration();
				break;
			case TriggerState.SoftTriggerFeedbackRight:
				SetTriggerFeedback(0, 0, 0, 2);
				SetTriggerWeaponEffect(0, 0, 0, 0, 0, 0);
				break;
			case TriggerState.HardTriggerFeedbackRight:
				SetTriggerFeedback(0, 0, 0, 4);
				SetTriggerWeaponEffect(0, 0, 0, 0, 0, 0);
				break;
			case TriggerState.SoftTriggerFeedbackLeft:
				SetTriggerFeedback(0, 2, 0, 0);
				SetTriggerWeaponEffect(0, 0, 0, 0, 0, 0);
				break;
			case TriggerState.HardTriggerFeedbackLeft:
				SetTriggerFeedback(0, 4, 0, 0);
				SetTriggerWeaponEffect(0, 0, 0, 0, 0, 0);
				break;
			case TriggerState.WeaponEffect:
				SetTriggerWeaponEffect(0, 0, 0, 4, 7, 7);
				break;
			default:
				Debug.FailedAssert("Unexpected trigger state:" + triggerState, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\MissionViews\\MissionGamepadEffectsView.cs", "SetTriggerState", 495);
				break;
			}
			_triggerState = triggerState;
		}
	}

	private void ResetTriggerFeedback()
	{
		_triggerFeedback[0] = 0;
		_triggerFeedback[1] = 0;
		_triggerFeedback[2] = 0;
		_triggerFeedback[3] = 0;
		SetTriggerFeedback(0, 0, 0, 0);
		SetTriggerWeaponEffect(0, 0, 0, 0, 0, 0);
		_triggerState = TriggerState.Off;
	}

	private void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
	{
		TaleWorlds.InputSystem.Input.SetTriggerFeedback(leftTriggerPosition, leftTriggerStrength, rightTriggerPosition, rightTriggerStrength);
	}

	private void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
	{
		TaleWorlds.InputSystem.Input.SetTriggerWeaponEffect(leftStartPosition, leftEnd_position, leftStrength, rightStartPosition, rightEndPosition, rightStrength);
	}

	private void ResetTriggerVibration()
	{
		float[] array = new float[1];
		SetTriggerVibration(array, array, array, 0, array, array, array, 0);
	}

	private void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
	{
		TaleWorlds.InputSystem.Input.SetTriggerVibration(leftTriggerAmplitudes, leftTriggerFrequencies, leftTriggerDurations, numLeftTriggerElements, rightTriggerAmplitudes, rightTriggerFrequencies, rightTriggerDurations, numRightTriggerElements);
	}

	private static void SetLightbarColor(float red, float green, float blue)
	{
		TaleWorlds.InputSystem.Input.SetLightbarColor(red, green, blue);
	}
}
