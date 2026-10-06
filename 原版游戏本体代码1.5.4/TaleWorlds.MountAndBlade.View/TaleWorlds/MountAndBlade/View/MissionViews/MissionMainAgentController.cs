using System;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

[DefaultView]
public class MissionMainAgentController : MissionView
{
	public enum OverrideMainAgentControlFlag
	{
		None = 0,
		Walk = 1,
		Run = 2,
		Crouch = 4,
		Stand = 8
	}

	public delegate void OnLockedAgentChangedDelegate(Agent newAgent);

	public delegate void OnPotentialLockedAgentChangedDelegate(Agent newPotentialAgent);

	private const float MinValueForAimStart = 0.2f;

	private const float MaxValueForAttackEnd = 0.6f;

	private float _lastForwardKeyPressTime;

	private float _lastBackwardKeyPressTime;

	private float _lastLeftKeyPressTime;

	private float _lastRightKeyPressTime;

	private float _lastWieldNextPrimaryWeaponTriggerTime;

	private float _lastWieldNextOffhandWeaponTriggerTime;

	private bool _activated = true;

	private bool _strafeModeActive;

	private bool _autoDismountModeActive;

	private bool _isPlayerAgentAdded;

	private bool _isPlayerAiming;

	private bool _isPlayerOrderOpen;

	private bool _isTargetLockEnabled;

	private Agent.MovementControlFlag _lastMovementKeyPressed = Agent.MovementControlFlag.Forward;

	private Agent _lockedAgent;

	private Agent _potentialLockTargetAgent;

	private OverrideMainAgentControlFlag _overrideControlsThisFrame;

	private float _lastLockKeyPressTime;

	private float _lastLockedAgentHeightDifference;

	public readonly MissionMainAgentInteractionComponent InteractionComponent;

	public bool IsChatOpen;

	private bool _weaponUsageToggleRequested;

	public bool IsDisabled { get; set; }

	public Vec3 CustomLookDir { get; set; }

	public bool IsPlayerAiming
	{
		get
		{
			if (_isPlayerAiming)
			{
				return true;
			}
			if (base.Mission.MainAgent == null)
			{
				return false;
			}
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			if (base.Input != null)
			{
				flag2 = base.Input.IsGameKeyDown(9);
			}
			if (base.Mission.MainAgent != null)
			{
				if (base.Mission.MainAgent.WieldedWeapon.CurrentUsageItem != null)
				{
					flag = base.Mission.MainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon || base.Mission.MainAgent.WieldedWeapon.CurrentUsageItem.IsAmmo;
				}
				flag3 = base.Mission.MainAgent.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.AttackMask);
			}
			return flag && flag2 && flag3;
		}
	}

	public Agent LockedAgent
	{
		get
		{
			return _lockedAgent;
		}
		private set
		{
			if (_lockedAgent != value)
			{
				_lockedAgent = value;
				this.OnLockedAgentChanged?.Invoke(value);
			}
		}
	}

	public Agent PotentialLockTargetAgent
	{
		get
		{
			return _potentialLockTargetAgent;
		}
		private set
		{
			if (_potentialLockTargetAgent != value)
			{
				_potentialLockTargetAgent = value;
				this.OnPotentialLockedAgentChanged?.Invoke(value);
			}
		}
	}

	public event OnLockedAgentChangedDelegate OnLockedAgentChanged;

	public event OnPotentialLockedAgentChangedDelegate OnPotentialLockedAgentChanged;

	public MissionMainAgentController()
	{
		InteractionComponent = new MissionMainAgentInteractionComponent(this);
		CustomLookDir = Vec3.Zero;
		IsChatOpen = false;
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnPlayerToggleOrder);
		base.Mission.OnMainAgentChanged += Mission_OnMainAgentChanged;
		MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		if (missionBehavior?.RoundComponent != null)
		{
			missionBehavior.RoundComponent.OnRoundStarted += Disable;
			missionBehavior.RoundComponent.OnPreparationEnded += Enable;
		}
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		UpdateLockTargetOption();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.Mission.OnMainAgentChanged -= Mission_OnMainAgentChanged;
		Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnPlayerToggleOrder);
		MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		if (missionBehavior?.RoundComponent != null)
		{
			missionBehavior.RoundComponent.OnRoundStarted -= Disable;
			missionBehavior.RoundComponent.OnPreparationEnded -= Enable;
		}
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	public override bool IsReady()
	{
		bool result = true;
		if (base.Mission.MainAgent != null)
		{
			result = base.Mission.MainAgent.AgentVisuals.CheckResources(addToQueue: true);
		}
		return result;
	}

	private void Mission_OnMainAgentChanged(Agent oldAgent)
	{
		if (base.Mission.MainAgent != null)
		{
			_isPlayerAgentAdded = true;
			_strafeModeActive = false;
			_autoDismountModeActive = false;
		}
	}

	public override void OnPreMissionTick(float dt)
	{
		base.OnPreMissionTick(dt);
		if (base.MissionScreen == null)
		{
			return;
		}
		if (base.Mission.MainAgent == null && GameNetwork.MyPeer != null)
		{
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component != null)
			{
				if (component.HasSpawnedAgentVisuals)
				{
					AgentVisualsMovementCheck();
				}
				else if (component.FollowedAgent != null)
				{
					RequestToSpawnAsBotCheck();
				}
			}
		}
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent != null && mainAgent.State == AgentState.Active && !base.MissionScreen.IsCheatGhostMode && !base.Mission.MainAgent.IsAIControlled && !base.MissionScreen.IsPhotoModeEnabled && !IsDisabled && _activated)
		{
			InteractionComponent.FocusTick();
			InteractionComponent.FocusedItemHealthTick();
			ControlTick();
			InteractionComponent.FocusStateCheckTick();
			LookTick(dt);
		}
		else
		{
			InteractionComponent.ClearFocus();
			LockedAgent = null;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (InteractionComponent.CurrentFocusedObject == affectedAgent || affectedAgent == base.Mission.MainAgent)
		{
			InteractionComponent.ClearFocus();
		}
	}

	public override void OnAgentDeleted(Agent affectedAgent)
	{
		if (InteractionComponent.CurrentFocusedObject == affectedAgent)
		{
			InteractionComponent.ClearFocus();
		}
	}

	public override void OnClearScene()
	{
		InteractionComponent.OnClearScene();
	}

	private void LookTick(float dt)
	{
		if (IsDisabled)
		{
			return;
		}
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent == null)
		{
			return;
		}
		if (_isPlayerAgentAdded)
		{
			_isPlayerAgentAdded = false;
			mainAgent.LookDirectionAsAngle = mainAgent.MovementDirectionAsAngle;
		}
		if (!(base.Mission.ClearSceneTimerElapsedTime >= 0f))
		{
			return;
		}
		Vec3 lookDirection;
		if (LockedAgent != null)
		{
			float num = 0f;
			float agentScale = LockedAgent.AgentScale;
			float agentScale2 = mainAgent.AgentScale;
			num = ((!LockedAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid)) ? (num + LockedAgent.Monster.BodyCapsulePoint1.z * agentScale) : (LockedAgent.HasMount ? (num + ((LockedAgent.MountAgent.Monster.RiderCameraHeightAdder + LockedAgent.MountAgent.Monster.BodyCapsulePoint1.z + LockedAgent.MountAgent.Monster.BodyCapsuleRadius) * LockedAgent.MountAgent.AgentScale + LockedAgent.Monster.CrouchEyeHeight * agentScale)) : ((!LockedAgent.CrouchMode && !LockedAgent.IsSitting()) ? (num + (LockedAgent.Monster.StandingEyeHeight + 0.2f) * agentScale) : (num + (LockedAgent.Monster.CrouchEyeHeight + 0.2f) * agentScale))));
			num = ((!mainAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid)) ? (num - LockedAgent.Monster.BodyCapsulePoint1.z * agentScale2) : (mainAgent.HasMount ? (num - ((mainAgent.MountAgent.Monster.RiderCameraHeightAdder + mainAgent.MountAgent.Monster.BodyCapsulePoint1.z + mainAgent.MountAgent.Monster.BodyCapsuleRadius) * mainAgent.MountAgent.AgentScale + mainAgent.Monster.CrouchEyeHeight * agentScale2)) : ((!mainAgent.CrouchMode && !mainAgent.IsSitting()) ? (num - (mainAgent.Monster.StandingEyeHeight + 0.2f) * agentScale2) : (num - (mainAgent.Monster.CrouchEyeHeight + 0.2f) * agentScale2))));
			if (LockedAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid))
			{
				num -= 0.3f * agentScale;
			}
			num = (_lastLockedAgentHeightDifference = MBMath.Lerp(_lastLockedAgentHeightDifference, num, TaleWorlds.Library.MathF.Min(8f * dt, 1f)));
			lookDirection = (LockedAgent.VisualPosition + ((LockedAgent.MountAgent != null) ? (LockedAgent.MountAgent.GetMovementDirection().ToVec3() * LockedAgent.MountAgent.Monster.RiderBodyCapsuleForwardAdder) : Vec3.Zero) + new Vec3(0f, 0f, num) - (mainAgent.VisualPosition + ((mainAgent.MountAgent != null) ? (mainAgent.MountAgent.GetMovementDirection().ToVec3() * mainAgent.MountAgent.Monster.RiderBodyCapsuleForwardAdder) : Vec3.Zero))).NormalizedCopy();
		}
		else if (CustomLookDir.IsNonZero)
		{
			lookDirection = CustomLookDir;
		}
		else
		{
			Mat3 identity = Mat3.Identity;
			identity.RotateAboutUp(base.MissionScreen.CameraBearing);
			identity.RotateAboutSide(base.MissionScreen.CameraElevation);
			lookDirection = identity.f;
		}
		if (!base.MissionScreen.IsViewingCharacter() && !mainAgent.IsLookDirectionLocked && mainAgent.MovementLockedState != AgentMovementLockedState.FrameLocked)
		{
			mainAgent.LookDirection = lookDirection;
		}
		mainAgent.HeadCameraMode = base.Mission.CameraIsFirstPerson;
	}

	private void AgentVisualsMovementCheck()
	{
		if (base.Input.IsGameKeyReleased(13))
		{
			BreakAgentVisualsInvulnerability();
		}
	}

	public void BreakAgentVisualsInvulnerability()
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new AgentVisualsBreakInvulnerability());
			GameNetwork.EndModuleEventAsClient();
		}
		else
		{
			Mission.Current.GetMissionBehavior<SpawnComponent>().SetEarlyAgentVisualsDespawning(GameNetwork.MyPeer.GetComponent<MissionPeer>());
		}
	}

	private void RequestToSpawnAsBotCheck()
	{
		if (base.Input.IsGameKeyPressed(13))
		{
			if (GameNetwork.IsClient)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new RequestToSpawnAsBot());
				GameNetwork.EndModuleEventAsClient();
			}
			else if (GameNetwork.MyPeer.GetComponent<MissionPeer>().HasSpawnTimerExpired)
			{
				GameNetwork.MyPeer.GetComponent<MissionPeer>().WantsToSpawnAsBot = true;
			}
		}
	}

	private Agent FindTargetedLockableAgent(Agent player)
	{
		Vec3 direction = base.MissionScreen.CombatCamera.Direction;
		Vec3 vec = direction;
		Vec3 position = base.MissionScreen.CombatCamera.Position;
		Vec3 visualPosition = player.VisualPosition;
		float num = new Vec3(position.x, position.y).Distance(new Vec3(visualPosition.x, visualPosition.y));
		Vec3 vec2 = position * (1f - num) + (position + direction) * num;
		float num2 = 0f;
		Agent agent = null;
		foreach (Agent agent2 in base.Mission.Agents)
		{
			if ((!agent2.IsMount || agent2.RiderAgent == null || !agent2.RiderAgent.IsEnemyOf(player)) && (agent2.IsMount || !agent2.IsEnemyOf(player)))
			{
				continue;
			}
			Vec3 vec3 = agent2.GetChestGlobalPosition() - vec2;
			float num3 = vec3.Normalize();
			if (!(num3 < 20f))
			{
				continue;
			}
			float num4 = Vec2.DotProduct(vec.AsVec2.Normalized(), vec3.AsVec2.Normalized());
			float num5 = Vec2.DotProduct(new Vec2(vec.AsVec2.Length, vec.z), new Vec2(vec3.AsVec2.Length, vec3.z));
			if (num4 > 0.95f && num5 > 0.95f)
			{
				float num6 = num4 * num4 * num4 / TaleWorlds.Library.MathF.Pow(num3, 0.15f);
				if (num6 > num2)
				{
					num2 = num6;
					agent = agent2;
				}
			}
		}
		if (agent != null && agent.IsMount && agent.RiderAgent != null)
		{
			return agent.RiderAgent;
		}
		return agent;
	}

	private void ControlTick()
	{
		if ((base.MissionScreen != null && base.MissionScreen.IsPhotoModeEnabled) || IsChatOpen)
		{
			return;
		}
		Agent mainAgent = base.Mission.MainAgent;
		bool flag = false;
		if (LockedAgent != null && (!base.Mission.Agents.ContainsQ(LockedAgent) || !LockedAgent.IsActive() || LockedAgent.Position.DistanceSquared(mainAgent.Position) > 625f || base.Input.IsGameKeyReleased(26) || base.Input.IsGameKeyDown(25) || (base.Mission.Mode != MissionMode.Battle && base.Mission.Mode != MissionMode.Stealth) || (!mainAgent.WieldedWeapon.IsEmpty && mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) || base.MissionScreen == null || base.MissionScreen.GetSpectatingData(base.MissionScreen.CombatCamera.Frame.origin).CameraType != SpectatorCameraTypes.LockToMainPlayer || IsThereAnyCustomCameraAddition()))
		{
			LockedAgent = null;
			flag = true;
		}
		int num;
		if (base.Mission.Mode == MissionMode.Conversation)
		{
			mainAgent.MovementFlags = Agent.MovementControlFlag.None;
			mainAgent.MovementInputVector = Vec2.Zero;
		}
		else if (base.Mission.ClearSceneTimerElapsedTime >= 0f && mainAgent.State == AgentState.Active)
		{
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			Vec2 movementInputVector = new Vec2(base.Input.GetGameKeyAxis("MovementAxisX"), base.Input.GetGameKeyAxis("MovementAxisY"));
			if (_autoDismountModeActive)
			{
				if (!base.Input.IsGameKeyDown(0) && mainAgent.MountAgent != null)
				{
					if (mainAgent.GetCurrentVelocity().y > 0f)
					{
						movementInputVector.y = -1f;
					}
				}
				else
				{
					_autoDismountModeActive = false;
				}
			}
			if (TaleWorlds.Library.MathF.Abs(movementInputVector.x) < 0.2f)
			{
				movementInputVector.x = 0f;
			}
			if (TaleWorlds.Library.MathF.Abs(movementInputVector.y) < 0.2f)
			{
				movementInputVector.y = 0f;
			}
			if (movementInputVector.IsNonZero())
			{
				float rotationInRadians = movementInputVector.RotationInRadians;
				if (rotationInRadians > -System.MathF.PI / 4f && rotationInRadians < System.MathF.PI / 4f)
				{
					flag3 = true;
				}
				else if (rotationInRadians < System.MathF.PI * -3f / 4f || rotationInRadians > System.MathF.PI * 3f / 4f)
				{
					flag5 = true;
				}
				else if (rotationInRadians < 0f)
				{
					flag2 = true;
				}
				else
				{
					flag4 = true;
				}
			}
			mainAgent.EventControlFlags = Agent.EventControlFlag.None;
			mainAgent.MovementFlags = Agent.MovementControlFlag.None;
			mainAgent.MovementInputVector = Vec2.Zero;
			foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
			{
				if (missionBehavior is IPlayerInputEffector playerInputEffector)
				{
					mainAgent.EventControlFlags |= playerInputEffector.OnCollectPlayerEventControlFlags();
				}
			}
			if (!base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen)
			{
				if (base.Input.IsGameKeyPressed(14))
				{
					if (mainAgent.MountAgent == null || mainAgent.MovementVelocity.LengthSquared > 0.09f)
					{
						mainAgent.EventControlFlags |= Agent.EventControlFlag.Jump;
					}
					else
					{
						mainAgent.EventControlFlags |= Agent.EventControlFlag.Rear;
					}
				}
				if (base.Input.IsGameKeyPressed(13))
				{
					mainAgent.MovementFlags |= Agent.MovementControlFlag.Action;
				}
			}
			if (mainAgent.MountAgent != null && mainAgent.GetCurrentVelocity().y < 0.5f && (base.Input.IsGameKeyDown(3) || base.Input.IsGameKeyDown(2)))
			{
				if (base.Input.IsGameKeyPressed(16))
				{
					_strafeModeActive = true;
				}
			}
			else
			{
				_strafeModeActive = false;
			}
			Agent.MovementControlFlag movementControlFlag = _lastMovementKeyPressed;
			if (base.Input.IsGameKeyPressed(0))
			{
				movementControlFlag = Agent.MovementControlFlag.Forward;
			}
			else if (base.Input.IsGameKeyPressed(1))
			{
				movementControlFlag = Agent.MovementControlFlag.Backward;
			}
			else if (base.Input.IsGameKeyPressed(2))
			{
				movementControlFlag = Agent.MovementControlFlag.StrafeLeft;
			}
			else if (base.Input.IsGameKeyPressed(3))
			{
				movementControlFlag = Agent.MovementControlFlag.StrafeRight;
			}
			if (movementControlFlag != _lastMovementKeyPressed)
			{
				_lastMovementKeyPressed = movementControlFlag;
				Game.Current?.EventManager.TriggerEvent(new MissionPlayerMovementFlagsChangeEvent(_lastMovementKeyPressed));
			}
			if (!base.Input.GetIsMouseActive())
			{
				bool flag6 = true;
				if (flag3)
				{
					movementControlFlag = Agent.MovementControlFlag.Forward;
				}
				else if (flag5)
				{
					movementControlFlag = Agent.MovementControlFlag.Backward;
				}
				else if (flag4)
				{
					movementControlFlag = Agent.MovementControlFlag.StrafeLeft;
				}
				else if (flag2)
				{
					movementControlFlag = Agent.MovementControlFlag.StrafeRight;
				}
				else
				{
					flag6 = false;
				}
				if (flag6)
				{
					base.Mission.SetLastMovementKeyPressed(movementControlFlag);
				}
			}
			else
			{
				base.Mission.SetLastMovementKeyPressed(_lastMovementKeyPressed);
			}
			if (base.Input.IsGameKeyPressed(0))
			{
				if (_lastForwardKeyPressTime + 0.3f > Time.ApplicationTime)
				{
					mainAgent.EventControlFlags &= ~Agent.EventControlFlag.DoubleTapToDirectionMask;
					mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionUp;
				}
				_lastForwardKeyPressTime = Time.ApplicationTime;
			}
			if (base.Input.IsGameKeyPressed(1))
			{
				if (_lastBackwardKeyPressTime + 0.3f > Time.ApplicationTime)
				{
					mainAgent.EventControlFlags &= ~Agent.EventControlFlag.DoubleTapToDirectionMask;
					mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionDown;
				}
				_lastBackwardKeyPressTime = Time.ApplicationTime;
			}
			if (base.Input.IsGameKeyPressed(2))
			{
				if (_lastLeftKeyPressTime + 0.3f > Time.ApplicationTime)
				{
					mainAgent.EventControlFlags &= ~Agent.EventControlFlag.DoubleTapToDirectionMask;
					mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionLeft;
				}
				_lastLeftKeyPressTime = Time.ApplicationTime;
			}
			if (base.Input.IsGameKeyPressed(3))
			{
				if (_lastRightKeyPressTime + 0.3f > Time.ApplicationTime)
				{
					mainAgent.EventControlFlags &= ~Agent.EventControlFlag.DoubleTapToDirectionMask;
					mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionRight;
				}
				_lastRightKeyPressTime = Time.ApplicationTime;
			}
			if (_isTargetLockEnabled && !IsThereAnyCustomCameraAddition())
			{
				if (base.Input.IsGameKeyDown(26) && LockedAgent == null && !base.Input.IsGameKeyDown(25) && (base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Stealth) && (mainAgent.WieldedWeapon.IsEmpty || !mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) && !GameNetwork.IsMultiplayer)
				{
					float applicationTime = Time.ApplicationTime;
					if (_lastLockKeyPressTime <= 0f)
					{
						_lastLockKeyPressTime = applicationTime;
					}
					if (applicationTime > _lastLockKeyPressTime + 0.3f)
					{
						PotentialLockTargetAgent = FindTargetedLockableAgent(mainAgent);
					}
				}
				else
				{
					PotentialLockTargetAgent = null;
				}
				if (LockedAgent == null && !flag && base.Input.IsGameKeyReleased(26) && !GameNetwork.IsMultiplayer)
				{
					_lastLockKeyPressTime = 0f;
					if (!base.Input.IsGameKeyDown(25) && (base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Stealth) && (mainAgent.WieldedWeapon.IsEmpty || !mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) && base.MissionScreen != null && base.MissionScreen.GetSpectatingData(base.MissionScreen.CombatCamera.Frame.origin).CameraType == SpectatorCameraTypes.LockToMainPlayer)
					{
						LockedAgent = FindTargetedLockableAgent(mainAgent);
					}
				}
			}
			if (mainAgent.MountAgent != null && !_strafeModeActive)
			{
				if (flag2 || movementInputVector.x > 0f)
				{
					mainAgent.MovementFlags |= Agent.MovementControlFlag.TurnRight;
				}
				else if (flag4 || movementInputVector.x < 0f)
				{
					mainAgent.MovementFlags |= Agent.MovementControlFlag.TurnLeft;
				}
			}
			mainAgent.MovementInputVector = movementInputVector;
			if (!base.MissionScreen.MouseVisible && !base.MissionScreen.IsRadialMenuActive && !_isPlayerOrderOpen && mainAgent.CombatActionsEnabled)
			{
				if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableAlternateAiming) != 0f && TaleWorlds.InputSystem.Input.IsGamepadActive)
				{
					WeaponComponentData currentUsageItem = mainAgent.WieldedWeapon.CurrentUsageItem;
					if (currentUsageItem != null && currentUsageItem.WeaponFlags.HasAllFlags(WeaponFlags.StringHeldByHand))
					{
						num = 1;
						goto IL_08b1;
					}
					WeaponComponentData currentUsageItem2 = mainAgent.WieldedWeapon.CurrentUsageItem;
					if (currentUsageItem2 != null && currentUsageItem2.IsRangedWeapon && !mainAgent.WieldedWeapon.CurrentUsageItem.IsConsumable)
					{
						num = ((!mainAgent.WieldedWeapon.CurrentUsageItem.WeaponFlags.HasAllFlags(WeaponFlags.StringHeldByHand)) ? 1 : 0);
						if (num != 0)
						{
							goto IL_08b1;
						}
					}
					else
					{
						num = 0;
					}
				}
				else
				{
					num = 0;
				}
				if (base.Input.IsGameKeyDown(9) && mainAgent.GetAgentFlags().HasAnyFlag(AgentFlag.CanAttack))
				{
					mainAgent.MovementFlags |= mainAgent.AttackDirectionToMovementFlag(mainAgent.GetAttackDirection());
				}
				goto IL_08fe;
			}
			goto IL_09be;
		}
		goto IL_0dad;
		IL_0dad:
		_overrideControlsThisFrame = OverrideMainAgentControlFlag.None;
		return;
		IL_08b1:
		if (mainAgent.GetAgentFlags().HasAnyFlag(AgentFlag.CanAttack))
		{
			HandleRangedWeaponAttackAlternativeAiming(mainAgent);
		}
		goto IL_08fe;
		IL_09be:
		if (!base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen)
		{
			if (base.Input.IsGameKeyPressed(16) && (mainAgent.KickClear() || mainAgent.MountAgent != null))
			{
				mainAgent.EventControlFlags |= Agent.EventControlFlag.Kick;
			}
			if (base.Input.IsGameKeyPressed(18))
			{
				mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.WeaponItemBeginSlot, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
			}
			else if (base.Input.IsGameKeyPressed(19))
			{
				mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon1, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
			}
			else if (base.Input.IsGameKeyPressed(20))
			{
				mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon2, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
			}
			else if (base.Input.IsGameKeyPressed(21))
			{
				mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon3, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
			}
			else if (base.Input.IsGameKeyPressed(11) && _lastWieldNextPrimaryWeaponTriggerTime + 0.2f < Time.ApplicationTime)
			{
				_lastWieldNextPrimaryWeaponTriggerTime = Time.ApplicationTime;
				mainAgent.WieldNextWeapon(Agent.HandIndex.MainHand);
			}
			else if (base.Input.IsGameKeyPressed(12) && _lastWieldNextOffhandWeaponTriggerTime + 0.2f < Time.ApplicationTime)
			{
				_lastWieldNextOffhandWeaponTriggerTime = Time.ApplicationTime;
				mainAgent.WieldNextWeapon(Agent.HandIndex.OffHand);
			}
			else if (base.Input.IsGameKeyPressed(23))
			{
				mainAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
			}
			if (base.Input.IsGameKeyPressed(17) || _weaponUsageToggleRequested)
			{
				mainAgent.EventControlFlags |= Agent.EventControlFlag.ToggleAlternativeWeapon;
				_weaponUsageToggleRequested = false;
			}
			if (_overrideControlsThisFrame.HasAnyFlag((!mainAgent.WalkMode) ? OverrideMainAgentControlFlag.Walk : OverrideMainAgentControlFlag.Run) || base.Input.IsGameKeyPressed(30))
			{
				mainAgent.EventControlFlags |= (Agent.EventControlFlag)(mainAgent.WalkMode ? 4096 : 2048);
			}
			if (mainAgent.IsInWater())
			{
				if (base.Input.IsGameKeyDown(14))
				{
					mainAgent.EventControlFlags |= Agent.EventControlFlag.Jump;
				}
				if (base.Input.IsGameKeyDown(15))
				{
					mainAgent.EventControlFlags |= Agent.EventControlFlag.Crouch;
				}
			}
			if (mainAgent.MountAgent != null)
			{
				if (base.Input.IsGameKeyPressed(15) || _autoDismountModeActive)
				{
					if (mainAgent.GetCurrentVelocity().y < 0.5f && mainAgent.MountAgent.GetCurrentActionType(0) != Agent.ActionCodeType.Rear)
					{
						mainAgent.EventControlFlags |= Agent.EventControlFlag.Dismount;
					}
					else if (base.Input.IsGameKeyPressed(15))
					{
						_autoDismountModeActive = true;
						mainAgent.EventControlFlags &= ~Agent.EventControlFlag.DoubleTapToDirectionMask;
						mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionDown;
					}
				}
			}
			else if (_overrideControlsThisFrame.HasAnyFlag(mainAgent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.Crouch) ? OverrideMainAgentControlFlag.Stand : OverrideMainAgentControlFlag.Crouch) || (!TaleWorlds.InputSystem.Input.IsGamepadActive && base.Input.IsGameKeyPressed(15)) || (mainAgent.EventControlFlags.HasAnyFlag(Agent.EventControlFlag.Crouch) && !mainAgent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.Crouch)) || (mainAgent.EventControlFlags.HasAnyFlag(Agent.EventControlFlag.Stand) && mainAgent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.Crouch)))
			{
				if (mainAgent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.Crouch))
				{
					mainAgent.SetScriptedFlags(mainAgent.GetScriptedFlags() & ~Agent.AIScriptedFrameFlags.Crouch);
				}
				else if (mainAgent.IsCrouchingAllowed())
				{
					mainAgent.SetScriptedFlags(mainAgent.GetScriptedFlags() | Agent.AIScriptedFrameFlags.Crouch);
				}
			}
			if (mainAgent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.Crouch) && (mainAgent.EventControlFlags.HasAnyFlag(Agent.EventControlFlag.Dismount | Agent.EventControlFlag.Mount | Agent.EventControlFlag.Jump | Agent.EventControlFlag.Stand | Agent.EventControlFlag.Kick) || mainAgent.HasMount || mainAgent.IsInWater()))
			{
				mainAgent.SetScriptedFlags(mainAgent.GetScriptedFlags() & ~Agent.AIScriptedFrameFlags.Crouch);
			}
			if (mainAgent.CrouchMode != mainAgent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.Crouch))
			{
				mainAgent.EventControlFlags |= (Agent.EventControlFlag)(mainAgent.GetScriptedFlags().HasAnyFlag(Agent.AIScriptedFrameFlags.Crouch) ? 8192 : 16384);
			}
		}
		goto IL_0dad;
		IL_08fe:
		if (num == 0 && base.Input.IsGameKeyDown(10))
		{
			if (ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.ControlBlockDirection) == 2f && MissionGameModels.Current.AutoBlockModel != null)
			{
				switch (MissionGameModels.Current.AutoBlockModel.GetBlockDirection(base.Mission))
				{
				case Agent.UsageDirection.AttackLeft:
					mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendRight;
					break;
				case Agent.UsageDirection.AttackRight:
					mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendLeft;
					break;
				case Agent.UsageDirection.AttackUp:
					mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendUp;
					break;
				case Agent.UsageDirection.AttackDown:
					mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendDown;
					break;
				}
			}
			else
			{
				mainAgent.MovementFlags |= mainAgent.GetDefendMovementFlag();
			}
		}
		goto IL_09be;
	}

	private void HandleRangedWeaponAttackAlternativeAiming(Agent player)
	{
		if (base.Input.GetKeyState(InputKey.ControllerLTrigger).x > 0.2f)
		{
			if (base.Input.GetKeyState(InputKey.ControllerRTrigger).x < 0.6f)
			{
				player.MovementFlags |= player.AttackDirectionToMovementFlag(player.GetAttackDirection());
			}
			_isPlayerAiming = true;
		}
		else if (_isPlayerAiming)
		{
			player.MovementFlags |= Agent.MovementControlFlag.DefendUp;
			_isPlayerAiming = false;
		}
	}

	public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		if (otherAgent.IsMount)
		{
			return otherAgent.IsActive();
		}
		return false;
	}

	public void Disable()
	{
		_activated = false;
	}

	public void Enable()
	{
		_activated = true;
	}

	private void OnPlayerToggleOrder(MissionPlayerToggledOrderViewEvent obj)
	{
		_isPlayerOrderOpen = obj.IsOrderEnabled;
	}

	public void OnWeaponUsageToggleRequested()
	{
		_weaponUsageToggleRequested = true;
	}

	public void AddOverrideControlsForFrame(OverrideMainAgentControlFlag overrideFlag)
	{
		_overrideControlsThisFrame |= overrideFlag;
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType optionType)
	{
		if (optionType == ManagedOptions.ManagedOptionsType.LockTarget)
		{
			UpdateLockTargetOption();
		}
	}

	private void UpdateLockTargetOption()
	{
		_isTargetLockEnabled = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.LockTarget) == 1f;
		LockedAgent = null;
		PotentialLockTargetAgent = null;
		_lastLockKeyPressTime = 0f;
		_lastLockedAgentHeightDifference = 0f;
	}

	private bool IsThereAnyCustomCameraAddition()
	{
		if (!base.Mission.CustomCameraTargetLocalOffset.IsNonZero && !base.Mission.CustomCameraLocalOffset.IsNonZero && !base.Mission.CustomCameraLocalOffset2.IsNonZero && !base.Mission.CustomCameraGlobalOffset.IsNonZero && !base.Mission.CustomCameraLocalRotationalOffset.IsNonZero)
		{
			return base.Mission.CustomCameraFixedDistance != float.MinValue;
		}
		return true;
	}
}
