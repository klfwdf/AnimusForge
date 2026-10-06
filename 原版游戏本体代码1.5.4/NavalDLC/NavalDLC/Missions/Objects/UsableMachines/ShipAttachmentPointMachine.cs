using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NavalDLC.Missions.AI.UsableMachineAIs;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipAttachmentPointMachine : UsableMachine
{
	[EditableScriptComponentVariable(true, "")]
	public int RelatedShipNavmeshOffset;

	private GameEntity _focusObject;

	private MBList<GameEntity> _rampPhysicsList;

	private ActionIndexCache _actionForJumpingOff = ActionIndexCache.act_escape_jump;

	private GameEntity _destructablePart;

	private GameEntity _destructablePartCleanCollider;

	private DestructableComponent _shipAttachmentPointMachineDestructableComponent;

	private PhysicsMaterial _destructablePartPhysicsMaterialCached;

	private Vec3[] _destructablePartQuadsCached = new Vec3[4];

	private UIntPtr _destructablePartQuadPinnedPointer = UIntPtr.Zero;

	private GCHandle _destructablePartQuadPinnedGCHandler;

	private UIntPtr _destructablePartIndicesPinnedPointer = UIntPtr.Zero;

	private GCHandle _destructablePartIndicesPinnedGCHandler;

	private int[] _destructablePartQuadsIndicesCached = new int[6];

	private bool _destructablePartReset = true;

	private Vec3 _lastHitImpactDirection;

	private Vec3 _lastHitImpactPosition;

	private float _lastHitDamage;

	public MissionShip OwnerShip { get; private set; }

	public ShipAttachmentMachine.ShipAttachment CurrentAttachment { get; private set; }

	public Vec3 HookAttachLocalPosition { get; private set; }

	public GameEntity ConnectionClipPlaneEntity { get; private set; }

	public GameEntity RampBarrier { get; private set; }

	internal MBReadOnlyList<GameEntity> RampPhysicsList => _rampPhysicsList;

	public GameEntity RampVisualEntity { get; private set; }

	public ShipAttachmentMachine LinkedAttachmentMachine { get; private set; }

	protected override void OnInit()
	{
		base.OnInit();
		WeakGameEntity parent = base.GameEntity.Parent;
		while (OwnerShip == null && parent.IsValid)
		{
			OwnerShip = parent.GetFirstScriptOfType<MissionShip>();
			parent = parent.Parent;
		}
		if (base.GameEntity.Parent.GetScriptCountOfTypeRecursive<ShipAttachmentMachine>() == 1)
		{
			LinkedAttachmentMachine = base.GameEntity.Parent.GetFirstScriptOfTypeRecursive<ShipAttachmentMachine>();
		}
		int childCount = base.GameEntity.ChildCount;
		WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
		for (int i = 0; i < childCount; i++)
		{
			WeakGameEntity child = base.GameEntity.GetChild(i);
			if (child.Name == "hook_attach_point")
			{
				HookAttachLocalPosition = child.GetFrame().origin + 0.5f * child.GetFrame().rotation.u.NormalizedCopy();
				weakGameEntity = child;
			}
			else if (child.Name == "focus_object")
			{
				_focusObject = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
		}
		if (weakGameEntity != WeakGameEntity.Invalid)
		{
			weakGameEntity.Remove(78);
		}
		ConnectionClipPlaneEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTagRecursive("connection_point"));
		RampBarrier = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("connection_barrier"));
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenWithTagRecursive(list, "step_capsule");
		_rampPhysicsList = new MBList<GameEntity>();
		foreach (WeakGameEntity item in list)
		{
			if (item.GetVisibilityExcludeParents())
			{
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
				gameEntity.SetVisibilityExcludeParents(visible: false);
				_rampPhysicsList.Add(gameEntity);
			}
		}
		RampVisualEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTagRecursive("bridge_target"));
		RampVisualEntity.SetVisibilityExcludeParents(visible: false);
		SetScriptComponentToTick(GetTickRequirement());
		EnemyRangeToStopUsing = 5f;
		_destructablePartPhysicsMaterialCached = PhysicsMaterial.GetFromName("fabric");
		_destructablePart = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, "ship_attachment_point_destructable", MatrixFrame.Identity);
		_destructablePartCleanCollider = _destructablePart.GetFirstChildEntityWithTagRecursive("cleanCollider");
		_ = MatrixFrame.Identity;
		_shipAttachmentPointMachineDestructableComponent = _destructablePart.GetFirstScriptOfType<DestructableComponent>();
		_shipAttachmentPointMachineDestructableComponent.OnHitTakenWithImpact += OnDestructableHitTakenWithImpact;
		_destructablePart.SetVisibilityExcludeParents(visible: false);
		_destructablePartQuadPinnedGCHandler = GCHandle.Alloc(_destructablePartQuadsCached, GCHandleType.Pinned);
		_destructablePartQuadPinnedPointer = (UIntPtr)(ulong)(long)_destructablePartQuadPinnedGCHandler.AddrOfPinnedObject();
		_destructablePartIndicesPinnedGCHandler = GCHandle.Alloc(_destructablePartQuadsIndicesCached, GCHandleType.Pinned);
		_destructablePartIndicesPinnedPointer = (UIntPtr)(ulong)(long)_destructablePartIndicesPinnedGCHandler.AddrOfPinnedObject();
		_destructablePartQuadsIndicesCached[0] = 2;
		_destructablePartQuadsIndicesCached[1] = 1;
		_destructablePartQuadsIndicesCached[2] = 0;
		_destructablePartQuadsIndicesCached[3] = 3;
		_destructablePartQuadsIndicesCached[4] = 2;
		_destructablePartQuadsIndicesCached[5] = 0;
		IsDisabledForAttackerAIDueToEnemyInRange = new QueryData<bool>(() => OwnerShip != null && OwnerShip.ShipOrder != null && OwnerShip.ShipOrder.IsEnemyOnShip, 1f);
		IsDisabledForDefenderAIDueToEnemyInRange = new QueryData<bool>(() => OwnerShip != null && OwnerShip.ShipOrder != null && OwnerShip.ShipOrder.IsEnemyOnShip, 1f);
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	public override void OnDeploymentFinished()
	{
		base.PilotStandingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
	}

	public void SetPhysicsEntitiesVisibility(bool isEnabled)
	{
		_shipAttachmentPointMachineDestructableComponent.Reset();
		_destructablePart.SetVisibilityExcludeParents(isEnabled);
		_destructablePartReset = true;
	}

	protected override void OnTick(float dt)
	{
		bool flag = !OwnerShip.BeingAbandoned && (LinkedAttachmentMachine?.CurrentAttachment != null || CurrentAttachment == null || (base.PilotAgent == null && ((CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected && CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.RopesPulling) || OwnerShip.IsDisconnectionBlocked())));
		base.PilotStandingPoint.SetIsDeactivatedSynched(flag);
		if (CurrentAttachment != null)
		{
			if ((CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected || CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval || CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown) && !_destructablePartReset)
			{
				_shipAttachmentPointMachineDestructableComponent.Reset();
				_destructablePart.SetVisibilityExcludeParents(visible: false);
				_destructablePartReset = true;
			}
			else if (CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.RopesPulling)
			{
				if (_destructablePartReset)
				{
					_destructablePart.SetVisibilityExcludeParents(visible: true);
					_destructablePartReset = false;
				}
				MatrixFrame globalFrame = ConnectionClipPlaneEntity.GetGlobalFrame();
				MatrixFrame globalFrame2 = CurrentAttachment.AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrame();
				Vec3 v = globalFrame2.origin - globalFrame2.rotation.s * 0.125f;
				Vec3 v2 = globalFrame2.origin + globalFrame2.rotation.s * 0.125f;
				Vec3 v3 = globalFrame.origin + globalFrame.rotation.s * 0.125f;
				Vec3 v4 = globalFrame.origin - globalFrame.rotation.s * 0.125f;
				MatrixFrame frame = MatrixFrame.Identity;
				frame.origin = (v + v2 + v3 + v4) * 0.25f;
				frame.rotation = Mat3.Identity;
				_destructablePartQuadsCached[0] = frame.TransformToLocal(in v);
				_destructablePartQuadsCached[1] = frame.TransformToLocal(in v4);
				_destructablePartQuadsCached[2] = frame.TransformToLocal(in v3);
				_destructablePartQuadsCached[3] = frame.TransformToLocal(in v2);
				_destructablePartCleanCollider.ReplacePhysicsBodyWithQuadPhysicsBody(_destructablePartQuadPinnedPointer, 4, _destructablePartPhysicsMaterialCached, BodyFlags.TwoSided | BodyFlags.Moveable, _destructablePartIndicesPinnedPointer, 6, replaceTrianglemeshDescriptions: true);
				_destructablePart.SetGlobalFrame(in frame);
				if (_shipAttachmentPointMachineDestructableComponent.IsDestroyed)
				{
					if (base.PilotAgent != null && base.PilotAgent.IsAIControlled)
					{
						base.PilotAgent.DisableScriptedCombatMovement();
						base.PilotAgent.StopUsingGameObject();
					}
					if (base.PilotStandingPoint.IsDisabledForPlayers)
					{
						base.PilotStandingPoint.SetIsDisabledForPlayersSynched(value: false);
					}
					_shipAttachmentPointMachineDestructableComponent.Reset();
					_destructablePart.SetVisibilityExcludeParents(visible: false);
					_destructablePartReset = true;
				}
			}
		}
		if (_focusObject.GetVisibilityExcludeParents() == flag)
		{
			_focusObject.SetVisibilityExcludeParents(!flag);
		}
		if (base.PilotAgent == null)
		{
			return;
		}
		if (OwnerShip.BeingAbandoned)
		{
			WorldFrame userFrameForAgent = base.PilotStandingPoint.GetUserFrameForAgent(base.PilotAgent);
			Vec3 targetDirection = userFrameForAgent.Rotation.f;
			targetDirection.Normalize();
			if (base.PilotAgent.GetCurrentAction(0) != _actionForJumpingOff && base.PilotAgent.Frame.origin.AsVec2.DistanceSquared(userFrameForAgent.Origin.AsVec2) <= 0.3f && Vec3.DotProduct(base.PilotAgent.Frame.rotation.f.NormalizedCopy(), targetDirection) > 0.95f)
			{
				Agent pilotAgent = base.PilotAgent;
				if (pilotAgent.Formation != null)
				{
					((IDetachment)this).RemoveAgent(pilotAgent);
					pilotAgent.Formation.AttachUnit(pilotAgent);
				}
				else
				{
					base.PilotAgent.StopUsingGameObject();
				}
				Vec3 vec = pilotAgent.Position + targetDirection * 10f;
				pilotAgent.GetComponent<AgentNavalComponent>().SetupAgentToAbandonShip();
				pilotAgent.SetActionChannel(0, in _actionForJumpingOff, ignorePriority: false, (AnimFlags)0uL);
				pilotAgent.SetTargetPositionAndDirection(vec.AsVec2, in targetDirection);
				pilotAgent.ClearTargetFrame();
			}
		}
		else
		{
			if (CurrentAttachment == null || CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				return;
			}
			if (base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_ship_connection_break, ignorePriority: false, (AnimFlags)0uL))
			{
				if (base.PilotAgent.GetCurrentActionProgress(1) > 0.99f)
				{
					CurrentAttachment.AttachmentSource.DisconnectAttachment();
					base.PilotAgent.StopUsingGameObject();
				}
			}
			else
			{
				base.PilotAgent.StopUsingGameObject();
			}
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		if (_shipAttachmentPointMachineDestructableComponent != null)
		{
			_shipAttachmentPointMachineDestructableComponent.OnHitTakenWithImpact -= OnDestructableHitTakenWithImpact;
		}
		if (_destructablePartQuadPinnedGCHandler.IsAllocated)
		{
			_destructablePartQuadPinnedGCHandler.Free();
			_destructablePartQuadPinnedPointer = UIntPtr.Zero;
		}
		if (_destructablePartIndicesPinnedGCHandler.IsAllocated)
		{
			_destructablePartIndicesPinnedGCHandler.Free();
			_destructablePartIndicesPinnedPointer = UIntPtr.Zero;
		}
	}

	private void OnDestructableHitTakenWithImpact(DestructableComponent target, Agent attackerAgent, Vec3 impactPosition, Vec3 impactDirection, int inflictedDamage)
	{
		if (CurrentAttachment == null)
		{
			return;
		}
		RopePileBaked ropePileBaked = CurrentAttachment.AttachmentSource?.RopeVisual;
		if (ropePileBaked == null)
		{
			return;
		}
		Vec3 origin = CurrentAttachment.AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrame().origin;
		Vec3 vec = ConnectionClipPlaneEntity.GetGlobalFrame().origin - origin;
		float lengthSquared = vec.LengthSquared;
		if (!(lengthSquared >= 0.0001f))
		{
			return;
		}
		Vec3 vec2 = vec * (1f / TaleWorlds.Library.MathF.Sqrt(lengthSquared));
		Vec3 globalHitVector = impactDirection - vec2 * Vec3.DotProduct(impactDirection, vec2);
		if (globalHitVector.LengthSquared >= 1E-06f)
		{
			float num = MBMath.ClampFloat(Vec3.DotProduct(impactPosition - origin, vec) / lengthSquared, 0f, 1f);
			_lastHitImpactDirection = impactDirection;
			_lastHitImpactPosition = impactPosition;
			_lastHitDamage = inflictedDamage;
			if (target.IsDestroyed)
			{
				float num2 = TaleWorlds.Library.MathF.Min((float)inflictedDamage * 0.45f, 16f);
				Vec3 impulseAtBreakPoint = ((impactDirection.LengthSquared > 0.0001f && num2 > 0f) ? (impactDirection.NormalizedCopy() * num2) : Vec3.Zero);
				CurrentAttachment.BreakWithCutRope(num, impulseAtBreakPoint, CurrentAttachment.ShipAttachmentJoint.TensionRatio);
				MissionCombatMechanicsHelper.NextBlowCollisionReactionOverride = MeleeCollisionReaction.SlicedThrough;
			}
			else
			{
				float intensity = MBMath.ClampFloat((float)inflictedDamage * 1.5f, 15f, 60f);
				ropePileBaked.ApplyWobble(globalHitVector, intensity, 1f, num);
			}
		}
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		return float.MinValue;
	}

	public override bool ShouldAutoLeaveDetachmentWhenDisabled(BattleSideEnum sideEnum)
	{
		return false;
	}

	public void AssignConnection(ShipAttachmentMachine.ShipAttachment shipAttachment)
	{
		CurrentAttachment = shipAttachment;
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=PUbT3s7W}{KEY} Cut Loose");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if ((CurrentAttachment != null && CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected) || (LinkedAttachmentMachine?.CurrentAttachment != null && LinkedAttachmentMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected))
		{
			return new TextObject("{=kCMGJl1W}Bridge");
		}
		return new TextObject("{=7zCPG8TR}Hook");
	}

	public bool IsShipAttachmentMachinePointBridgeWithEnemy()
	{
		if (CurrentAttachment != null)
		{
			Team team = CurrentAttachment?.AttachmentSource?.OwnerShip?.Team;
			Team team2 = CurrentAttachment?.AttachmentTarget?.OwnerShip?.Team;
			if (team != null && team2 != null && team.IsEnemyOf(team2))
			{
				return CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected;
			}
			return false;
		}
		return false;
	}

	public bool IsShipAttachmentPointBridged()
	{
		if (CurrentAttachment != null)
		{
			if (CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				return CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown;
			}
			return true;
		}
		return false;
	}

	public bool IsShipAttachmentPointConnectedToEnemy()
	{
		if (CurrentAttachment != null && (CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.RopesPulling || CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown || CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected) && CurrentAttachment.AttachmentSource.OwnerShip.Team != null && CurrentAttachment.AttachmentTarget.OwnerShip.Team != null && CurrentAttachment.AttachmentSource.OwnerShip.Team.IsEnemyOf(CurrentAttachment.AttachmentTarget.OwnerShip.Team))
		{
			Formation formation = CurrentAttachment.AttachmentSource.OwnerShip.Formation;
			if (formation == null)
			{
				return false;
			}
			return formation.CountOfUnits > 0;
		}
		return false;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new ShipAttachmentPointAI(this);
	}

	protected override bool OnCheckForProblems()
	{
		return true;
	}

	public void SetJumpOffAction(ActionIndexCache action)
	{
		_actionForJumpingOff = action;
	}
}
