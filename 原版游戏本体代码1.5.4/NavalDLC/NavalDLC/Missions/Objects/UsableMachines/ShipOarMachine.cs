using System;
using System.Collections.Generic;
using NavalDLC.Missions.AI.UsableMachineAIs;
using NavalDLC.Missions.ShipActuators;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipOarMachine : UsableMachine, IShipOarScriptComponent
{
	private GameEntity _oarEntity;

	private MatrixFrame _handTargetLocalFrame;

	private MatrixFrame _oarExtractedEntitialFrame;

	private MatrixFrame _oarRetractedEntitialFrame;

	private MissionOar _oar;

	private float _lastIdleTime;

	private ActionIndexCache _rowIdleActionIndex;

	private ActionIndexCache _rowLoopActionIndex;

	private ActionIndexCache _rowLoopBackwardActionIndex;

	private ActionIndexCache _rowDeathActionIndex;

	private ActionIndexCache _rowSitDownActionIndex;

	private ActionIndexCache _rowStandUpActionIndex;

	private bool _isPilotSitting;

	private Agent _lastPilotAgent;

	private (float, Agent.StopUsingGameObjectFlags) _pilotRemovalTime;

	private readonly List<GameEntity> _disablingAttachmentRampEntities = new List<GameEntity>();

	private BoundingBox _oarMachineBaseBoundingBox;

	[EditableScriptComponentVariable(true, "")]
	private string _rowIdleAction = "act_usage_row_idle_right";

	[EditableScriptComponentVariable(true, "")]
	private string _rowLoopAction = "act_usage_row_loop_right";

	[EditableScriptComponentVariable(true, "")]
	private string _rowLoopBackwardAction = "act_usage_row_loop_right_backward";

	[EditableScriptComponentVariable(true, "")]
	private string _rowDeathAction = "act_row_death_right";

	[EditableScriptComponentVariable(true, "")]
	private string _rowSitDownAction = "act_row_sit_down_right";

	[EditableScriptComponentVariable(true, "")]
	private string _rowStandUpAction = "act_row_stand_up_right";

	public ResetAnimationOnStopUsageComponent ResetAnimationOnStopUsageComponent { get; private set; }

	public override bool IsFocusable => false;

	protected override void OnInit()
	{
		base.OnInit();
		ShipOarDeck.LoadOarScriptEntity(base.GameEntity, out var oarEntity, ref _oarExtractedEntitialFrame, ref _oarRetractedEntitialFrame, out var handTargetEntity);
		_oarEntity = (oarEntity.IsValid ? TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(oarEntity) : null);
		_handTargetLocalFrame = (handTargetEntity.IsValid ? handTargetEntity.GetLocalFrame() : MatrixFrame.Identity);
		_rowIdleActionIndex = ActionIndexCache.Create(_rowIdleAction);
		_rowLoopActionIndex = ActionIndexCache.Create(_rowLoopAction);
		_rowLoopBackwardActionIndex = ActionIndexCache.Create(_rowLoopBackwardAction);
		_rowDeathActionIndex = ActionIndexCache.Create(_rowDeathAction);
		_rowSitDownActionIndex = ActionIndexCache.Create(_rowSitDownAction);
		_rowStandUpActionIndex = ActionIndexCache.Create(_rowStandUpAction);
		SetScriptComponentToTick(GetTickRequirement());
		base.GameEntity.SetHasCustomBoundingBoxValidationSystem(hasCustomBoundingBox: true);
		_oarMachineBaseBoundingBox = base.GameEntity.ComputeBoundingBoxFromLongestHalfDimension(2f);
		base.DestructionComponent.OnDestroyed += OnOarDestroyed;
		ResetAnimationOnStopUsageComponent = new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: true);
		EnemyRangeToStopUsing = 5f;
		base.PilotStandingPoint.SetIsDisabledForPlayersSynched(value: true);
	}

	public void InitializeOar(MissionOar oar)
	{
		_oar = oar;
	}

	public override void OnDeploymentFinished()
	{
		EnsureStandingPointComponents();
	}

	private void EnsureStandingPointComponents()
	{
		if (base.PilotStandingPoint.GetComponent<ResetAnimationOnStopUsageComponent>() == null)
		{
			base.PilotStandingPoint.AddComponent(ResetAnimationOnStopUsageComponent);
			base.PilotStandingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
			base.PilotStandingPoint.AddComponent(new OverrideStrikeAndDeathActionDuringUsageComponent(in ActionIndexCache.act_row_strike, in _rowDeathActionIndex));
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return base.GetTickRequirement() | TickRequirement.TickParallel2;
	}

	public void ArrangeOarBoundingBox()
	{
		base.GameEntity.SetManualLocalBoundingBox(in _oarMachineBaseBoundingBox);
		base.GameEntity.Parent.SetBoundingboxDirty();
	}

	protected override void OnBoundingBoxValidate()
	{
		BoundingBox boundingBox = base.GameEntity.ComputeBoundingBoxIncludeChildren();
		boundingBox.RelaxWithBoundingBox(_oarMachineBaseBoundingBox);
		boundingBox.RecomputeRadius();
		base.GameEntity.RelaxLocalBoundingBox(in boundingBox);
	}

	public bool CheckOarMachineFlags(bool editMode)
	{
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (!child.EntityFlags.HasAnyFlag(EntityFlags.DontSaveToScene) && !child.EntityFlags.HasAnyFlag(EntityFlags.DoesNotAffectParentsLocalBb))
			{
				string msg = "Root Entity: " + base.GameEntity.Root.Name + " " + base.GameEntity.Name + "'s child " + child.Name + " must have Does not Affect Parent's Local Bounding Box flag.";
				if (editMode)
				{
					MBEditor.AddEntityWarning(child, msg);
				}
				return false;
			}
		}
		return true;
	}

	public void SetSlowDownPhaseForDuration(float slowDownMultiplier, float slowDownDuration)
	{
		_oar.SetSlowDownPhaseForDuration(slowDownMultiplier, slowDownDuration);
	}

	public void RegisterRampEntityDisablingOar(GameEntity rampEntity)
	{
		if (_disablingAttachmentRampEntities.Count == 0)
		{
			if (base.PilotStandingPoint.HasUser)
			{
				base.PilotStandingPoint.UserAgent.StopUsingGameObject();
			}
			else if (base.PilotStandingPoint.HasAIMovingTo)
			{
				base.PilotStandingPoint.MovingAgent.StopUsingGameObject();
			}
			base.PilotStandingPoint.SetIsDeactivatedSynched(value: true);
		}
		if (!_disablingAttachmentRampEntities.Contains(rampEntity))
		{
			_disablingAttachmentRampEntities.Add(rampEntity);
		}
	}

	public void DeregisterRampEntityDisablingOar(GameEntity rampEntity)
	{
		if (_disablingAttachmentRampEntities.Remove(rampEntity) && _disablingAttachmentRampEntities.Count == 0)
		{
			base.PilotStandingPoint.SetIsDeactivatedSynched(value: false);
		}
	}

	public override bool ShouldAutoLeaveDetachmentWhenDisabled(BattleSideEnum sideEnum)
	{
		return false;
	}

	public override void OnPilotAssignedDuringSpawn()
	{
		EnsureStandingPointComponents();
		_lastPilotAgent = base.PilotAgent;
		_isPilotSitting = true;
		base.PilotAgent.SetActionChannel(0, in _rowIdleActionIndex, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, 0f, 0f, 1f);
		Vec3 v = MBAnimation.GetAnimationDisplacementAtProgress(MBActionSet.GetAnimationIndexOfAction(base.PilotAgent.ActionSet, in _rowSitDownActionIndex), 1f);
		MatrixFrame globalFrame = base.PilotStandingPoint.GameEntity.GetGlobalFrame();
		globalFrame.rotation.Orthonormalize();
		Vec3 position = globalFrame.TransformToParent(in v);
		Vec2 direction = globalFrame.rotation.f.AsVec2.Normalized();
		base.PilotAgent.TeleportToPosition(position);
		base.PilotAgent.DisableScriptedMovement();
		base.PilotAgent.SetMovementDirection(in direction);
		base.PilotAgent.SetTargetPositionAndDirection(position.AsVec2, direction.ToVec3());
		_oar.SetOarForceMultiplierFromUserAgent(MissionGameModels.Current.MissionShipParametersModel.CalculateOarForceMultiplier(base.PilotAgent, 1f));
		_oar.OnPilotAssignedDuringSpawn();
	}

	public void StartDelayedPilotRemoval(Agent.StopUsingGameObjectFlags flags)
	{
		if (_pilotRemovalTime.Item1 <= 0f)
		{
			_pilotRemovalTime = (Mission.Current.CurrentTime + MBRandom.RandomFloat * 2f, flags);
		}
	}

	protected override void OnTickParallel2(float dt)
	{
		if (_lastPilotAgent != base.PilotAgent)
		{
			base.PilotStandingPoint.SetCustomLocalFrame(MatrixFrame.Identity);
			base.PilotStandingPoint.LockUserFrames = true;
			_isPilotSitting = false;
			if (base.PilotAgent != null)
			{
				WorldFrame userFrameForAgent = base.PilotStandingPoint.GetUserFrameForAgent(base.PilotAgent);
				base.PilotAgent.SetTargetPositionAndDirection(userFrameForAgent.Origin.AsVec2, in userFrameForAgent.Rotation.f);
				base.PilotAgent.SetScriptedFlags(base.PilotAgent.GetScriptedFlags() | Agent.AIScriptedFrameFlags.NoAttack);
				_oar.SetOarForceMultiplierFromUserAgent(MissionGameModels.Current.MissionShipParametersModel.CalculateOarForceMultiplier(base.PilotAgent, 1f));
			}
		}
		_lastPilotAgent = base.PilotAgent;
		bool flag = base.PilotAgent != null;
		bool flag2 = false;
		_oar.SetUsed(flag, flag ? base.PilotAgent.Index : (-1));
		MatrixFrame frame = _oar.ComputeOarEntityFrame(dt, base.GameEntity.GetLocalFrame(), _oarEntity.GetLocalFrame(), in _oarExtractedEntitialFrame, in _oarRetractedEntitialFrame, _lastIdleTime, forUnmanned: false);
		_oarEntity.SetLocalFrame(ref frame, isTeleportation: false);
		if (flag)
		{
			if (_pilotRemovalTime.Item1 > 0f && _pilotRemovalTime.Item1 < Mission.Current.CurrentTime)
			{
				base.PilotAgent.StopUsingGameObjectMT(isSuccessful: true, _pilotRemovalTime.Item2);
				_pilotRemovalTime = (0f, Agent.StopUsingGameObjectFlags.None);
			}
			else if (!_isPilotSitting)
			{
				if (base.PilotAgent.GetCurrentAction(0) != _rowStandUpActionIndex)
				{
					if (base.PilotAgent.MovementLockedState != AgentMovementLockedState.None)
					{
						MatrixFrame globalFrame = base.PilotStandingPoint.GameEntity.GetGlobalFrame();
						base.PilotAgent.SetTargetPositionAndDirection(globalFrame.origin.AsVec2 - _oar.OwnerShip.Physics.LinearVelocity.AsVec2 * dt, in globalFrame.rotation.f);
						base.PilotStandingPoint.LockUserFrames = true;
						if (Vec2.DotProduct(globalFrame.rotation.f.AsVec2.Normalized(), base.PilotAgent.GetMovementDirection()) > 0.99f && base.PilotAgent.GetTargetPosition().DistanceSquared(base.PilotAgent.Position.AsVec2) < 0.01f)
						{
							base.PilotAgent.ClearTargetFrame();
							base.PilotAgent.SetActionChannel(0, in _rowSitDownActionIndex, ignorePriority: false, (AnimFlags)0uL);
							base.PilotStandingPoint.LockUserFrames = false;
						}
					}
					else if (base.PilotAgent.GetCurrentAction(0) == _rowSitDownActionIndex)
					{
						MatrixFrame globalFrame2 = base.PilotStandingPoint.GameEntity.GetGlobalFrame();
						base.PilotAgent.SetTargetPositionAndDirection(globalFrame2.origin.AsVec2, in globalFrame2.rotation.f);
						base.PilotAgent.ClearTargetFrame();
						base.PilotStandingPoint.LockUserFrames = false;
						if (base.PilotAgent.GetCurrentActionProgress(0) > 0.99f)
						{
							_isPilotSitting = true;
							base.PilotAgent.SetActionChannel(0, in ActionIndexCache.act_usage_row_idle_no_hold, ignorePriority: false, (AnimFlags)0uL);
						}
						else if (base.PilotAgent.GetCurrentActionProgress(0) > 0.25f)
						{
							flag2 = true;
						}
					}
					else
					{
						base.PilotAgent.StopUsingGameObjectMT();
					}
				}
			}
			else
			{
				int animationIndexOfAction = MBActionSet.GetAnimationIndexOfAction(base.PilotAgent.ActionSet, in _rowSitDownActionIndex);
				base.PilotStandingPoint.SetCustomLocalFrame(new MatrixFrame(Mat3.Identity, MBAnimation.GetAnimationDisplacementAtProgress(animationIndexOfAction, 1f)));
				base.PilotStandingPoint.LockUserFrames = true;
				if (_oar.IsExtracted)
				{
					bool flag3 = _oar.NeededRevolutionRate < 0f;
					float num = ((_oar.VisualPhase + System.MathF.PI / 2f) / (System.MathF.PI * 2f) + 1f) % 1f;
					if (flag3)
					{
						num = 1f - num;
					}
					bool flag4 = _oar.IsInRowingMotion();
					ActionIndexCache actionIndexCache;
					float startProgress;
					if (flag4)
					{
						actionIndexCache = (flag3 ? _rowLoopBackwardActionIndex : _rowLoopActionIndex);
						startProgress = 0f;
					}
					else
					{
						actionIndexCache = _rowIdleActionIndex;
						startProgress = MBRandom.RandomFloatWithSeed((uint)(base.PilotAgent.Index * Environment.TickCount), (uint)(_oar.OwnerShip.Index * 100));
					}
					if (base.PilotAgent.SetActionChannel(0, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, -0.2f, 0.4f, startProgress) && flag4)
					{
						base.PilotAgent.SetCurrentActionProgress(0, num);
					}
					bool isInBeingStruckAction = base.PilotAgent.IsInBeingStruckAction;
					if (!isInBeingStruckAction && base.PilotAgent.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, -0.2f, 0.4f, startProgress) && flag4)
					{
						base.PilotAgent.SetCurrentActionProgress(1, num);
					}
					ActionIndexCache actionCode = base.PilotAgent.GetCurrentAction(0);
					ActionIndexCache actionCode2 = base.PilotAgent.GetCurrentAction(1);
					if (isInBeingStruckAction || (base.PilotAgent.ActionSet.AreActionsAlternatives(in actionCode, in actionIndexCache) && base.PilotAgent.ActionSet.AreActionsAlternatives(in actionCode2, in actionIndexCache)))
					{
						int animationIndexOfAction2 = MBActionSet.GetAnimationIndexOfAction(base.PilotAgent.ActionSet, (actionCode2 != ActionIndexCache.act_none) ? actionCode2 : actionCode);
						MatrixFrame frame2 = base.PilotAgent.Frame;
						MatrixFrame rightGlobalFrame = frame2.TransformToParent(base.PilotAgent.GetBoneEntitialFrameAtAnimationProgress(base.PilotAgent.Monster.MainHandBoneIndex, animationIndexOfAction2, num));
						MatrixFrame leftGlobalFrame = frame2.TransformToParent(base.PilotAgent.GetBoneEntitialFrameAtAnimationProgress(base.PilotAgent.Monster.OffHandBoneIndex, animationIndexOfAction2, num));
						Vec3 vec = _oarEntity.GetGlobalFrame().rotation.f.NormalizedCopy();
						float num2 = Vec3.DotProduct(vec, rightGlobalFrame.origin - leftGlobalFrame.origin);
						MatrixFrame matrixFrame = _oarEntity.GetGlobalFrame().TransformToParent(in _handTargetLocalFrame);
						rightGlobalFrame.origin = matrixFrame.origin + 0.5f * num2 * vec;
						leftGlobalFrame.origin = matrixFrame.origin - 0.5f * num2 * vec;
						base.PilotAgent.SetHandInverseKinematicsFrame(in leftGlobalFrame, in rightGlobalFrame);
					}
					else
					{
						base.PilotAgent.ClearHandInverseKinematics();
						base.PilotAgent.StopUsingGameObjectMT();
					}
				}
				else
				{
					base.PilotAgent.SetActionChannel(0, in ActionIndexCache.act_usage_row_idle_no_hold, ignorePriority: false, (AnimFlags)0uL);
					if (!base.PilotAgent.IsInBeingStruckAction)
					{
						base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_row_idle_no_hold, ignorePriority: false, (AnimFlags)0uL);
					}
					base.PilotAgent.ClearHandInverseKinematics();
				}
			}
		}
		else
		{
			base.PilotStandingPoint.SetCustomLocalFrame(MatrixFrame.Identity);
			base.PilotStandingPoint.LockUserFrames = true;
			_isPilotSitting = false;
			_pilotRemovalTime = (0f, Agent.StopUsingGameObjectFlags.None);
		}
		ResetAnimationOnStopUsageComponent.UpdateSuccessfulResetAction((base.PilotAgent != null && (_isPilotSitting || flag2) && base.PilotAgent.Mission.Mode != MissionMode.Deployment) ? _rowStandUpActionIndex : ActionIndexCache.act_none);
		if (!flag || !_oar.IsExtracted)
		{
			_lastIdleTime = Mission.Current.CurrentTime;
		}
	}

	private void OnOarDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		_oar.SetUsed(newIsUsed: false, -1);
		target.OnDestroyed -= OnOarDestroyed;
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		return float.MinValue;
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=4b2SXZG8}Oar");
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new ShipOarMachineAI(this);
	}
}
