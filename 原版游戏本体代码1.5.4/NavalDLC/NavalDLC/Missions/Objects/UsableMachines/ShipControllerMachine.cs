using System.Collections.Generic;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Missions.AI.UsableMachineAIs;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipControllerMachine : UsableMachine
{
	public const float CaptureTime = 3f;

	private const string ControllerEntityName = "controller";

	private const string HandTargetEntityName = "hand_position";

	private const string CameraTargetEntityName = "camera_target";

	private const string ShoulderCameraTargetEntityName = "shoulder_camera_target";

	private const string FrontCameraTargetEntityName = "front_camera_target";

	private const string RudderRotationEntityTag = "rudder_rotation_entity";

	private GameEntity _cameraTargetEntity;

	public GameEntity _rudderRotationEntity;

	private MatrixFrame _rudderRotationEntityInitialLocalFrame;

	private GameEntity _shoulderCameraTargetEntity;

	private GameEntity _frontCameraTargetEntity;

	private ActionIndexCache _shipControlActionPushLeftIndex = ActionIndexCache.act_none;

	private ActionIndexCache _shipControlActionPullRightIndex = ActionIndexCache.act_none;

	private ActionIndexCache _shipControlActionRelaxedIndex = ActionIndexCache.act_none;

	private ActionIndexCache _shipCaptureActionIndex = ActionIndexCache.act_none;

	private TextObject _overridenDescriptionForActiveEnemyShipControllerMachine;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	[EditableScriptComponentVariable(true, "")]
	private Vec3 _cameraOffset = new Vec3(0f, -20f, 5f);

	[EditableScriptComponentVariable(true, "")]
	private string _shipCaptureAction = "act_ship_capture";

	[EditableScriptComponentVariable(true, "")]
	private string _shipControlActionTurnLeft = "act_rudder_backward_push_idle";

	[EditableScriptComponentVariable(true, "")]
	private string _shipControlActionTurnRight = "act_rudder_backward_pull_idle";

	[EditableScriptComponentVariable(true, "")]
	private string _shipControlActionRelaxed = "act_rudder_backward_stand_idle";

	[EditableScriptComponentVariable(true, "")]
	private bool _isRightHandOnly;

	[EditableScriptComponentVariable(true, "")]
	private Vec3 _shoulderCameraOffset = new Vec3(0f, 0f, 0f, -1f);

	[EditableScriptComponentVariable(true, "")]
	private bool _isLeftHandOnly;

	[EditableScriptComponentVariable(true, "")]
	private Vec3 _frontCameraOffset = new Vec3(0f, -10f, 2f);

	[EditableScriptComponentVariable(true, "")]
	private float _shoulderCameraDistance = 2f;

	[EditableScriptComponentVariable(true, "")]
	private float _frontCameraDistance = 10f;

	[EditableScriptComponentVariable(true, "")]
	private float _cameraFovMultiplier = 1f;

	[EditableScriptComponentVariable(true, "")]
	private float _shoulderCameraFovMultiplier = 1f;

	[EditableScriptComponentVariable(true, "")]
	private float _frontCameraFovMultiplier = 1f;

	private float _captureTimer = -1f;

	public GameEntity ControllerEntity { get; private set; }

	public MissionShip AttachedShip { get; private set; }

	public GameEntity HandTargetEntity { get; private set; }

	public Vec3 BackCameraOffset => _cameraOffset;

	public float CaptureTimer => _captureTimer;

	public Vec3 ShoulderCameraOffset => _shoulderCameraOffset;

	public Vec3 FrontCameraOffset => _frontCameraOffset;

	public float ShoulderCameraDistance => _shoulderCameraDistance;

	public float FrontCameraDistance => _frontCameraDistance;

	public float BackCameraFovMultiplier => _cameraFovMultiplier;

	public float ShoulderCameraFovMultiplier => _shoulderCameraFovMultiplier;

	public float FrontCameraFovMultiplier => _frontCameraFovMultiplier;

	public Vec3 BackCameraTargetLocalPosition => _cameraTargetEntity?.GetFrame().origin ?? Vec3.Zero;

	public Vec3 ShoulderCameraTargetLocalPosition => _shoulderCameraTargetEntity?.GetFrame().origin ?? Vec3.Zero;

	public Vec3 FrontCameraTargetLocalPosition => _frontCameraTargetEntity?.GetFrame().origin ?? Vec3.Zero;

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnEditorTick(float dt)
	{
		if (!base.GameEntity.IsGhostObject())
		{
			UpdateVisualizer();
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		AttachedShip = base.GameEntity.GetFirstScriptOfTypeInFamily<MissionShip>();
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (child.Name == "controller")
			{
				ControllerEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
				_rudderRotationEntity = ControllerEntity;
				_rudderRotationEntityInitialLocalFrame = _rudderRotationEntity.GetFrame();
				foreach (WeakGameEntity child2 in child.GetChildren())
				{
					if (child2.Name == "hand_position")
					{
						HandTargetEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child2);
					}
				}
			}
			else if (child.Name == "hand_position")
			{
				HandTargetEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
			else if (child.Name == "camera_target")
			{
				_cameraTargetEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
			else if (child.Name == "shoulder_camera_target")
			{
				_shoulderCameraTargetEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
			else if (child.Name == "front_camera_target")
			{
				_frontCameraTargetEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
		}
		if (_rudderRotationEntity == null)
		{
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			base.GameEntity.Root.GetChildrenWithTagRecursive(list, "rudder_rotation_entity");
			foreach (WeakGameEntity item in list)
			{
				_rudderRotationEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
				_rudderRotationEntityInitialLocalFrame = _rudderRotationEntity.GetFrame();
			}
		}
		_shipControlActionPushLeftIndex = ActionIndexCache.Create(_shipControlActionTurnLeft);
		_shipControlActionPullRightIndex = ActionIndexCache.Create(_shipControlActionTurnRight);
		_shipControlActionRelaxedIndex = ActionIndexCache.Create(_shipControlActionRelaxed);
		_shipCaptureActionIndex = ActionIndexCache.Create(_shipCaptureAction);
		SetScriptComponentToTick(GetTickRequirement());
		EnemyRangeToStopUsing = 5f;
	}

	public bool CheckControllerMachineFlags(bool editMode)
	{
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenRecursive(ref children);
		bool flag = false;
		children.Add(base.GameEntity);
		foreach (WeakGameEntity item in children)
		{
			if (!item.EntityFlags.HasAnyFlag(EntityFlags.DontSaveToScene) && !item.EntityFlags.HasAnyFlag(EntityFlags.DoesNotAffectParentsLocalBb))
			{
				flag = true;
			}
		}
		if (flag)
		{
			string msg = $"In Root Entity {base.GameEntity.Root.Name}, {base.GameEntity.Name}'s every descendant including itself must have Does not Affect Parent's Local Bounding Box flag.";
			if (editMode)
			{
				MBEditor.AddEntityWarning(base.GameEntity, msg);
			}
		}
		return flag;
	}

	public override void OnDeploymentFinished()
	{
		EnsureStandingPointComponents();
		if (AttachedShip.BattleSide != Mission.Current.PlayerTeam.Side)
		{
			base.PilotStandingPoint.SetUsableByAIOnly();
		}
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = Mission.Current.GetMissionBehavior<NavalAgentsLogic>();
	}

	private void EnsureStandingPointComponents()
	{
		if (base.PilotStandingPoint.GetComponent<ResetAnimationOnStopUsageComponent>() == null)
		{
			base.PilotStandingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
			base.PilotStandingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
			base.PilotStandingPoint.AddComponent((NavalDLCManager.Instance.NavalPerks != null) ? new UserDamageCalculateComponent(NavalPerks.Shipmaster.TheHelmsmansShield, isPrimaryBonus: true, -0.6f) : new UserDamageCalculateComponent(null, isPrimaryBonus: false, -0.6f));
		}
	}

	public override void OnPilotAssignedDuringSpawn()
	{
		EnsureStandingPointComponents();
		bool flag = MBAnimation.GetAnimationBlendsWithActionIndex(MBActionSet.GetAnimationIndexOfAction(base.PilotAgent.ActionSet, in _shipControlActionRelaxedIndex)).Index >= 0;
		base.PilotAgent.SetActionChannel(1, in _shipControlActionRelaxedIndex, ignorePriority: false, AnimFlags.amf_priority_equip | AnimFlags.amf_priority_continue, flag ? 0.5f : 0f);
		MatrixFrame globalFrame = base.PilotStandingPoint.GameEntity.GetGlobalFrame();
		base.PilotAgent.TeleportToPosition(globalFrame.origin);
		base.PilotAgent.DisableScriptedMovement();
		base.PilotAgent.SetMovementDirection(globalFrame.rotation.f.AsVec2.Normalized());
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_rudderRotationEntity != null)
		{
			MatrixFrame frame = _rudderRotationEntityInitialLocalFrame;
			frame.rotation.RotateAboutUp(AttachedShip.VisualRudderRotation);
			_rudderRotationEntity.SetLocalFrame(ref frame, isTeleportation: false);
		}
		if (_navalShipsLogic != null && Agent.Main?.Formation?.Team != null && AttachedShip.BattleSide != Agent.Main.Formation.Team.Side)
		{
			base.PilotStandingPoint.IsDisabledForPlayers = !AttachedShip.CanBeTakenOver || !IsAttachedShipVacant() || !MissionShip.AreShipsConnected(_navalShipsLogic.GetShipAssignment(Agent.Main.Formation.Team.TeamSide, Agent.Main.Formation.FormationIndex).MissionShip, AttachedShip);
		}
		if (base.PilotAgent == null)
		{
			_captureTimer = -1f;
		}
		if (base.PilotAgent == null)
		{
			return;
		}
		if (base.PilotAgent.IsMainAgent && IsAttachedShipVacant() && base.PilotAgent.Formation != null)
		{
			MissionShip missionShip = _navalShipsLogic.GetShipAssignment(base.PilotAgent.Formation.Team.TeamSide, base.PilotAgent.Formation.FormationIndex).MissionShip;
			if (MissionShip.AreShipsConnected(missionShip, AttachedShip))
			{
				if (!base.PilotAgent.SetActionChannel(0, in _shipCaptureActionIndex, ignorePriority: false, (AnimFlags)0uL))
				{
					return;
				}
				if (_captureTimer > 0f)
				{
					_captureTimer -= dt;
					if (_captureTimer <= 0f)
					{
						Agent pilotAgent = base.PilotAgent;
						base.PilotAgent.StopUsingGameObject();
						OnShipCapturedByAgent(pilotAgent);
						missionShip.InvalidateActiveFormationTroopOnShipCache();
						AttachedShip.InvalidateActiveFormationTroopOnShipCache();
					}
				}
				else
				{
					_captureTimer = 3f;
				}
			}
			else
			{
				_captureTimer = -1f;
				base.PilotAgent.StopUsingGameObject();
			}
			return;
		}
		float input = AttachedShip.VisualRudderRotationPercentage * (float)MathF.Sign(base.GameEntity.GetGlobalScale().x);
		input = MBMath.Map(input, -1f, 1f, 0.95f, 0.05f);
		ActionIndexCache actionIndexCache = ((AttachedShip.VisualRudderPullDirection == 0f) ? _shipControlActionRelaxedIndex : ((!(AttachedShip.VisualRudderPullDirection > 0f)) ? _shipControlActionPushLeftIndex : _shipControlActionPullRightIndex));
		int animationIndexOfAction = MBActionSet.GetAnimationIndexOfAction(base.PilotAgent.ActionSet, in actionIndexCache);
		bool flag = MBAnimation.GetAnimationBlendsWithActionIndex(animationIndexOfAction) != ActionIndexCache.act_none;
		AnimFlags additionalFlags = AnimFlags.amf_priority_equip | AnimFlags.amf_priority_continue | AnimFlags.anf_ignore_all_collisions | AnimFlags.anf_lock_movement | AnimFlags.anf_align_with_ground;
		if (base.PilotAgent.SetActionChannel(1, in actionIndexCache, ignorePriority: false, additionalFlags, flag ? input : 0f))
		{
			if (HandTargetEntity != null)
			{
				Vec3 origin = HandTargetEntity.GetGlobalFrame().origin;
				float currentActionProgress = base.PilotAgent.GetCurrentActionProgress(1);
				MatrixFrame frame2 = base.PilotAgent.Frame;
				MBAgentVisuals agentVisuals = base.PilotAgent.AgentVisuals;
				MatrixFrame boneEntitialFrame = agentVisuals.GetBoneEntitialFrame(base.PilotAgent.Monster.MainHandBoneIndex, useBoneMapping: false);
				MatrixFrame boneEntitialFrame2 = agentVisuals.GetBoneEntitialFrame(base.PilotAgent.Monster.OffHandBoneIndex, useBoneMapping: false);
				MatrixFrame boneEntitialFrameAtAnimationProgress = base.PilotAgent.GetBoneEntitialFrameAtAnimationProgress(base.PilotAgent.Monster.MainHandBoneIndex, animationIndexOfAction, currentActionProgress);
				MatrixFrame boneEntitialFrameAtAnimationProgress2 = base.PilotAgent.GetBoneEntitialFrameAtAnimationProgress(base.PilotAgent.Monster.OffHandBoneIndex, animationIndexOfAction, currentActionProgress);
				Vec3 vec = frame2.TransformToParent(in boneEntitialFrameAtAnimationProgress.origin);
				Vec3 vec2 = frame2.TransformToParent(in boneEntitialFrameAtAnimationProgress2.origin);
				float alpha = MathF.Clamp(dt * 15f, 0f, 1f);
				MatrixFrame m = default(MatrixFrame);
				m.origin = boneEntitialFrameAtAnimationProgress.origin;
				m.rotation = Mat3.SlerpFPSIndependent(in boneEntitialFrame.rotation, in boneEntitialFrameAtAnimationProgress.rotation, alpha);
				MatrixFrame m2 = default(MatrixFrame);
				m2.origin = boneEntitialFrameAtAnimationProgress2.origin;
				m2.rotation = Mat3.SlerpFPSIndependent(in boneEntitialFrame2.rotation, in boneEntitialFrameAtAnimationProgress2.rotation, alpha);
				MatrixFrame rightGlobalFrame = frame2.TransformToParent(in m);
				MatrixFrame leftGlobalFrame = frame2.TransformToParent(in m2);
				if (_isLeftHandOnly)
				{
					leftGlobalFrame.origin = origin;
					base.PilotAgent.SetHandInverseKinematicsFrame(in leftGlobalFrame, MatrixFrame.Identity);
					return;
				}
				if (_isRightHandOnly)
				{
					rightGlobalFrame.origin = origin;
					base.PilotAgent.SetHandInverseKinematicsFrame(MatrixFrame.Identity, in rightGlobalFrame);
					return;
				}
				Vec3 vec3 = ((ControllerEntity != null) ? ControllerEntity.GetGlobalFrame().rotation.s.NormalizedCopy() : base.PilotStandingPoint.GameEntity.GetGlobalFrame().rotation.s.NormalizedCopy());
				float num = Vec3.DotProduct(vec3, vec - vec2);
				rightGlobalFrame.origin = origin + 0.5f * num * vec3;
				leftGlobalFrame.origin = origin - 0.5f * num * vec3;
				base.PilotAgent.SetHandInverseKinematicsFrame(in leftGlobalFrame, in rightGlobalFrame);
			}
		}
		else if (base.PilotAgent.IsInBeingStruckAction)
		{
			base.PilotAgent.ClearHandInverseKinematics();
		}
		else
		{
			base.PilotAgent.StopUsingGameObject();
		}
	}

	private void OnShipCapturedByAgent(Agent captorAgent)
	{
		_navalShipsLogic?.OnShipCaptured(AttachedShip, captorAgent.Formation);
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=!}{KEY}");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		return float.MinValue;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if (AttachedShip.BattleSide == Mission.Current.PlayerTeam.Side)
		{
			return new TextObject("{=OGY9BKOM}Control the Ship");
		}
		if (AttachedShip.CanBeTakenOver)
		{
			if (IsAttachedShipVacant())
			{
				MissionShip missionShip = null;
				if (_navalShipsLogic != null && Agent.Main.Formation?.Team != null)
				{
					missionShip = _navalShipsLogic.GetShipAssignment(Agent.Main.Formation.Team.TeamSide, Agent.Main.Formation.FormationIndex)?.MissionShip;
				}
				if (missionShip != null && MissionShip.AreShipsConnected(missionShip, AttachedShip))
				{
					return new TextObject("{=fOX1aVDv}Capture the ship");
				}
				if (!(_overridenDescriptionForActiveEnemyShipControllerMachine != null))
				{
					return new TextObject("{=lS53LgyN}You need to be boarded to capture the ship");
				}
				return _overridenDescriptionForActiveEnemyShipControllerMachine;
			}
			return new TextObject("{=UrBktTYi}Clear the crew to capture the ship");
		}
		return null;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new ShipControllerMachineAI(this);
	}

	private void UpdateVisualizer()
	{
		WeakGameEntity gameEntity = base.GameEntity.GetFirstChildEntityWithTag("visualizer");
		StandingPoint firstScriptOfTypeRecursive = base.GameEntity.GetFirstScriptOfTypeRecursive<StandingPoint>();
		bool flag = false;
		if (_shipControlActionRelaxedIndex == ActionIndexCache.act_none || _shipControlActionRelaxedIndex.GetName() != _shipControlActionRelaxed)
		{
			_shipControlActionRelaxedIndex = ActionIndexCache.Create(_shipControlActionRelaxed);
			if (_shipControlActionRelaxedIndex != ActionIndexCache.act_none)
			{
				flag = MBAnimation.GetAnimationBlendsWithActionIndex(MBActionSet.GetAnimationIndexOfAction(MBActionSet.GetActionSetWithIndex(0), in _shipControlActionRelaxedIndex)) != ActionIndexCache.act_none;
			}
		}
		if (_shipControlActionRelaxedIndex != ActionIndexCache.act_none && firstScriptOfTypeRecursive != null)
		{
			_ = firstScriptOfTypeRecursive.GameEntity;
			if (!gameEntity.IsValid)
			{
				GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.CreateEmpty(base.GameEntity.Scene, isModifiableFromEditor: false);
				gameEntity = gameEntity2.WeakEntity;
				gameEntity.SetEntityFlags(gameEntity.EntityFlags | EntityFlags.DontSaveToScene);
				gameEntity.SetName("visualizer");
				gameEntity.AddTag("visualizer");
				MBActionSet actionSetWithIndex = MBActionSet.GetActionSetWithIndex(0);
				gameEntity.CreateAgentSkeleton("human_skeleton", isHumanoid: true, actionSetWithIndex, "human", MBObjectManager.Instance.GetObject<Monster>("human"));
				gameEntity.Skeleton.SetAgentActionChannel(0, in _shipControlActionRelaxedIndex, 0f, 0f, forceFaceMorphRestart: true, flag ? 0.5f : 0f);
				gameEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("roman_cloth_tunic_a"));
				gameEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("casual_02_boots"));
				gameEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("hands_male_a"));
				gameEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("head_male_a"));
				base.GameEntity.AddChild(gameEntity2.WeakEntity);
			}
		}
		if (gameEntity.IsValid)
		{
			gameEntity.SetGlobalFrame(firstScriptOfTypeRecursive.GameEntity.GetGlobalFrame());
			if (gameEntity.Skeleton.GetActionAtChannel(0) != _shipControlActionRelaxedIndex)
			{
				gameEntity.Skeleton.SetAgentActionChannel(0, in _shipControlActionRelaxedIndex, 0f, 0f, forceFaceMorphRestart: true, flag ? 0.5f : 0f);
			}
		}
	}

	public override bool ShouldAutoLeaveDetachmentWhenDisabled(BattleSideEnum sideEnum)
	{
		return false;
	}

	public bool IsAttachedShipVacant()
	{
		if (AttachedShip.Formation != null)
		{
			if (!AttachedShip.AnyActiveFormationTroopOnShip)
			{
				NavalAgentsLogic navalAgentsLogic = _navalAgentsLogic;
				if (navalAgentsLogic == null)
				{
					return false;
				}
				return navalAgentsLogic.GetReservedTroopsCountOfShip(AttachedShip) <= 0;
			}
			return false;
		}
		return true;
	}

	public override void OnMissionEnded()
	{
	}

	public void SetOverridenDescriptionForActiveEnemyShipControllerMachine(TextObject description)
	{
		_overridenDescriptionForActiveEnemyShipControllerMachine = description;
	}
}
