using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class CorpseDraggingMissionLogic : MissionLogic, IPlayerInputEffector, IMissionBehavior
{
	private enum CrouchStandEvent
	{
		None,
		Crouch,
		Stand
	}

	private enum GroundMaterialCorpseDrag
	{
		Default,
		Fabric,
		Grass,
		Mud,
		Sand,
		Snow,
		Stone,
		Water,
		Wood
	}

	private static readonly Dictionary<string, GroundMaterialCorpseDrag> _corpseDragMateriels = new Dictionary<string, GroundMaterialCorpseDrag>
	{
		{
			"",
			GroundMaterialCorpseDrag.Default
		},
		{
			"default",
			GroundMaterialCorpseDrag.Default
		},
		{
			"fabric",
			GroundMaterialCorpseDrag.Fabric
		},
		{
			"grass",
			GroundMaterialCorpseDrag.Grass
		},
		{
			"mud",
			GroundMaterialCorpseDrag.Mud
		},
		{
			"sand",
			GroundMaterialCorpseDrag.Sand
		},
		{
			"snow",
			GroundMaterialCorpseDrag.Snow
		},
		{
			"stone",
			GroundMaterialCorpseDrag.Stone
		},
		{
			"water",
			GroundMaterialCorpseDrag.Water
		},
		{
			"wood",
			GroundMaterialCorpseDrag.Wood
		}
	};

	private static int _bodyDragSoundEventId = SoundManager.GetEventGlobalIndex("event:/physics/bodydrag/human/drag/default");

	private Agent _draggedCorpse;

	private bool _startedPickingUpDraggedCorpse;

	private bool _triggerBodyGrabSound;

	private Vec3 _draggedCorpseAverageVelocity = Vec3.Zero;

	private Vec3 _draggedCorpseBoneLastGlobalPosition = Vec3.Zero;

	private sbyte _draggedCorpseBoneIndex = -1;

	private float _draggedCorpseUnbindDistanceSquared = 100f;

	private EquipmentIndex _draggedCorpseCarrierLastWieldedPrimaryWeaponIndex = EquipmentIndex.None;

	private bool _draggedCorpseCarrierLastCrouchState;

	private bool _previousActionKeyPressed;

	private CrouchStandEvent _crouchStandEvent;

	private SoundEvent _bodyDragSoundEvent;

	public override void AfterStart()
	{
		base.AfterStart();
		base.Mission.FocusableObjectInformationProvider.AddInfoCallback(GetFocusableObjectInteractionInfoTexts);
		base.Mission.Scene.SetFixedTickCallbackActive(isActive: true);
	}

	private void GetFocusableObjectInteractionInfoTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (requesterAgent.IsMainAgent && focusableObject is Agent agent && !agent.IsActive())
		{
			focusableObjectInformation.PrimaryInteractionText = agent.Character?.Name ?? agent.Monster.GetName();
			if (isInteractable)
			{
				MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
				focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action");
				focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
				focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", GameTexts.FindText("str_ui_drag"));
			}
			else
			{
				focusableObjectInformation.SecondaryInteractionText = null;
			}
			focusableObjectInformation.IsActive = true;
		}
		else
		{
			focusableObjectInformation.IsActive = false;
		}
	}

	private void SetDraggedCorpse(Agent draggedCorpse, sbyte draggedCorpseBoneIndex)
	{
		if (_draggedCorpse != null)
		{
			_draggedCorpse.SetVelocityLimitsOnRagdoll(-1f, -1f);
			_draggedCorpse.EndRagdollAsCorpse();
		}
		else if (draggedCorpse != null)
		{
			EquipmentIndex offhandWieldedItemIndex = draggedCorpse.GetOffhandWieldedItemIndex();
			EquipmentIndex primaryWieldedItemIndex = draggedCorpse.GetPrimaryWieldedItemIndex();
			MissionEquipment equipment = draggedCorpse.Equipment;
			if (primaryWieldedItemIndex != EquipmentIndex.None && !equipment[primaryWieldedItemIndex].IsEmpty)
			{
				draggedCorpse.DropItem(primaryWieldedItemIndex);
			}
			if (offhandWieldedItemIndex != EquipmentIndex.None && !equipment[offhandWieldedItemIndex].IsEmpty)
			{
				draggedCorpse.DropItem(offhandWieldedItemIndex);
			}
		}
		_draggedCorpse = draggedCorpse;
		_draggedCorpseBoneIndex = draggedCorpseBoneIndex;
		_draggedCorpseUnbindDistanceSquared = 100f;
		_draggedCorpseAverageVelocity = Vec3.Zero;
		_draggedCorpseBoneLastGlobalPosition = Vec3.Zero;
		if (_bodyDragSoundEvent != null)
		{
			_bodyDragSoundEvent.Stop();
		}
		_bodyDragSoundEvent = null;
		if (_draggedCorpse != null)
		{
			if (!_draggedCorpse.IsAddedAsCorpse())
			{
				_draggedCorpse.AddAsCorpse();
			}
			MatrixFrame globalFrame = _draggedCorpse.AgentVisuals.GetGlobalFrame();
			MatrixFrame boneEntitialFrameWithIndex = _draggedCorpse.AgentVisuals.GetEntity().GetBoneEntitialFrameWithIndex(Math.Max(0, _draggedCorpseBoneIndex));
			_draggedCorpseBoneLastGlobalPosition = globalFrame.TransformToParent(in boneEntitialFrameWithIndex.origin);
			_draggedCorpse.StartRagdollAsCorpse();
			Agent mainAgent = base.Mission.MainAgent;
			mainAgent.SetMaximumSpeedLimit(1f, isMultiplier: false);
			mainAgent.SetDraggingMode(set: true);
			_crouchStandEvent = CrouchStandEvent.Crouch;
			_draggedCorpseCarrierLastWieldedPrimaryWeaponIndex = mainAgent.GetPrimaryWieldedItemIndex();
			_draggedCorpseCarrierLastCrouchState = mainAgent.CrouchMode;
			_startedPickingUpDraggedCorpse = true;
			_triggerBodyGrabSound = true;
		}
		else if (base.Mission.MainAgent != null)
		{
			Agent mainAgent2 = base.Mission.MainAgent;
			mainAgent2.SetMaximumSpeedLimit(-1f, isMultiplier: false);
			mainAgent2.SetDraggingMode(set: false);
			_crouchStandEvent = CrouchStandEvent.Stand;
			_startedPickingUpDraggedCorpse = false;
		}
	}

	public override void OnFixedMissionTick(float fixedDt)
	{
		Agent mainAgent = base.Mission.MainAgent;
		if (_draggedCorpse == null || _crouchStandEvent != CrouchStandEvent.None || (mainAgent.GetCurrentActionType(1) == Agent.ActionCodeType.EquipUnequip && _startedPickingUpDraggedCorpse) || _draggedCorpse.AgentVisuals.GetCurrentRagdollState() != RagdollState.Active)
		{
			return;
		}
		_draggedCorpse.SetVelocityLimitsOnRagdoll(10f, 50f);
		GameEntity entity = mainAgent.AgentVisuals.GetEntity();
		entity.GetQuickBoneEntitialFrame(mainAgent.Monster.MainHandItemBoneIndex, out var frame);
		Vec3 vec = entity.GetGlobalFrameImpreciseForFixedTick().TransformToParent(in frame.origin);
		_draggedCorpse.AgentVisuals.GetEntity().GetQuickBoneEntitialFrame(Math.Max(0, _draggedCorpseBoneIndex), out var frame2);
		Vec3 origin = _draggedCorpse.AgentVisuals.GetEntity().GetGlobalFrameImpreciseForFixedTick().TransformToParent(in frame2)
			.origin;
		Vec3 vec2 = vec - origin;
		Vec3 v = (origin - _draggedCorpseBoneLastGlobalPosition) / fixedDt;
		_draggedCorpseAverageVelocity = Vec3.Lerp(_draggedCorpseAverageVelocity, v, 10f * fixedDt);
		_draggedCorpseBoneLastGlobalPosition = origin;
		if (_triggerBodyGrabSound)
		{
			EquipmentElement equipmentElement = _draggedCorpse.SpawnEquipment[EquipmentIndex.Body];
			float soundParameterForArmorType = Agent.GetSoundParameterForArmorType((!equipmentElement.IsInvalid() && equipmentElement.Item.HasArmorComponent) ? equipmentElement.Item.ArmorComponent.MaterialType : ArmorComponent.ArmorMaterialTypes.None);
			_bodyDragSoundEvent = SoundEvent.CreateEvent(_bodyDragSoundEventId, Mission.Current.Scene);
			_bodyDragSoundEvent.SetParameter("Armor Type", soundParameterForArmorType);
			GroundMaterialCorpseDrag value = GroundMaterialCorpseDrag.Default;
			_corpseDragMateriels.TryGetValue(PhysicsMaterial.GetNameAtIndex(_draggedCorpse.GetGroundMaterialForCollisionEffect()), out value);
			_bodyDragSoundEvent.SetParameter("BodydragMaterial", (float)value);
			_bodyDragSoundEvent.SetPosition(_draggedCorpse.Position);
			_bodyDragSoundEvent.Play();
			SoundManager.StartOneShotEvent("event:/physics/bodydrag/human/grab/body_grab", _draggedCorpse.Position, "Armor Type", soundParameterForArmorType);
			_triggerBodyGrabSound = false;
		}
		if (vec2.LengthSquared > _draggedCorpseUnbindDistanceSquared)
		{
			SetDraggedCorpse(null, -1);
			return;
		}
		Vec3 vec3 = vec2 + new Vec3(mainAgent.Velocity.AsVec2 - _draggedCorpseAverageVelocity.AsVec2 * 0.5f) * 0.5f;
		float num = 150000f * fixedDt;
		Vec3 force = vec3 * num;
		if (force.LengthSquared > num * num)
		{
			force.Normalize();
			force *= num;
		}
		_draggedCorpse.ApplyForceOnRagdoll(_draggedCorpseBoneIndex, in force);
		_draggedCorpseUnbindDistanceSquared = Math.Max(2.25f, Math.Min(vec2.LengthSquared * 1.3f * 1.3f, _draggedCorpseUnbindDistanceSquared));
		GroundMaterialCorpseDrag value2 = GroundMaterialCorpseDrag.Default;
		_corpseDragMateriels.TryGetValue(PhysicsMaterial.GetNameAtIndex(_draggedCorpse.GetGroundMaterialForCollisionEffect()), out value2);
		_bodyDragSoundEvent.SetParameter("BodydragMaterial", (float)value2);
		_bodyDragSoundEvent.SetPosition(_draggedCorpse.Position);
		_bodyDragSoundEvent.SetParameter("BodydragSpeed", _draggedCorpseAverageVelocity.AsVec2.Length);
	}

	public override void OnMissionTick(float dt)
	{
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			SetDraggedCorpse(null, -1);
		}
		bool flag = mainAgent?.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.Action) ?? false;
		if (_draggedCorpse != null && _crouchStandEvent == CrouchStandEvent.None && (mainAgent.GetCurrentActionType(1) != Agent.ActionCodeType.EquipUnequip || !_startedPickingUpDraggedCorpse) && _draggedCorpse.AgentVisuals.GetCurrentRagdollState() == RagdollState.Active && dt > 0f)
		{
			_startedPickingUpDraggedCorpse = false;
			Agent.ActionCodeType currentActionType = mainAgent.GetCurrentActionType(0);
			if ((flag && !_previousActionKeyPressed) || (mainAgent.GetCurrentAction(0) != ActionIndexCache.act_none && currentActionType != Agent.ActionCodeType.Jump && currentActionType != Agent.ActionCodeType.JumpEnd) || mainAgent.GetCurrentAction(1) != ActionIndexCache.act_none || !mainAgent.CrouchMode || mainAgent.GetPrimaryWieldedItemIndex() >= EquipmentIndex.WeaponItemBeginSlot)
			{
				SetDraggedCorpse(null, -1);
			}
		}
		_previousActionKeyPressed = flag;
	}

	public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		if (_draggedCorpse == null && userAgent.IsPlayerControlled)
		{
			if (otherAgent.State != AgentState.Killed)
			{
				return otherAgent.State == AgentState.Unconscious;
			}
			return true;
		}
		return false;
	}

	public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		if (IsThereAgentAction(userAgent, agent))
		{
			MBAgentVisuals agentVisuals = userAgent.AgentVisuals;
			Skeleton skeleton = agentVisuals.GetSkeleton();
			List<int> list = new List<int>(21)
			{
				agentVisuals.GetRealBoneIndex(HumanBone.ItemL),
				agentVisuals.GetRealBoneIndex(HumanBone.ItemR),
				agentVisuals.GetRealBoneIndex(HumanBone.HandL),
				agentVisuals.GetRealBoneIndex(HumanBone.HandR),
				agentVisuals.GetRealBoneIndex(HumanBone.Forearm1L),
				agentVisuals.GetRealBoneIndex(HumanBone.Forearm1R),
				agentVisuals.GetRealBoneIndex(HumanBone.ForearmL),
				agentVisuals.GetRealBoneIndex(HumanBone.ForearmR),
				agentVisuals.GetRealBoneIndex(HumanBone.UpperarmTwist1L),
				agentVisuals.GetRealBoneIndex(HumanBone.UpperarmTwist1R),
				agentVisuals.GetRealBoneIndex(HumanBone.UpperarmL),
				agentVisuals.GetRealBoneIndex(HumanBone.UpperarmR),
				agentVisuals.GetRealBoneIndex(HumanBone.ToeL),
				agentVisuals.GetRealBoneIndex(HumanBone.ToeR),
				agentVisuals.GetRealBoneIndex(HumanBone.FootL),
				agentVisuals.GetRealBoneIndex(HumanBone.FootR),
				agentVisuals.GetRealBoneIndex(HumanBone.CalfL),
				agentVisuals.GetRealBoneIndex(HumanBone.CalfR),
				agentVisuals.GetRealBoneIndex(HumanBone.ThighL),
				agentVisuals.GetRealBoneIndex(HumanBone.ThighR),
				agentVisuals.GetRealBoneIndex(HumanBone.Head)
			};
			while (list.IndexOf(agentBoneIndex) >= 0)
			{
				agentBoneIndex = skeleton.GetParentBoneIndex(agentBoneIndex);
			}
			SetDraggedCorpse(agent, agentBoneIndex);
			_previousActionKeyPressed = true;
		}
	}

	public Agent.EventControlFlag OnCollectPlayerEventControlFlags()
	{
		if (_crouchStandEvent == CrouchStandEvent.Crouch)
		{
			_crouchStandEvent = CrouchStandEvent.None;
			return Agent.EventControlFlag.Sheath0 | Agent.EventControlFlag.Crouch;
		}
		if (_crouchStandEvent == CrouchStandEvent.Stand)
		{
			_crouchStandEvent = CrouchStandEvent.None;
			Agent.EventControlFlag eventControlFlag = ((!_draggedCorpseCarrierLastCrouchState) ? Agent.EventControlFlag.Stand : Agent.EventControlFlag.None);
			if (base.Mission.MainAgent.GetCurrentActionType(1) != Agent.ActionCodeType.EquipUnequip)
			{
				switch (_draggedCorpseCarrierLastWieldedPrimaryWeaponIndex)
				{
				case EquipmentIndex.WeaponItemBeginSlot:
					eventControlFlag |= Agent.EventControlFlag.Wield0;
					break;
				case EquipmentIndex.Weapon1:
					eventControlFlag |= Agent.EventControlFlag.Wield1;
					break;
				case EquipmentIndex.Weapon2:
					eventControlFlag |= Agent.EventControlFlag.Wield2;
					break;
				case EquipmentIndex.Weapon3:
					eventControlFlag |= Agent.EventControlFlag.Wield3;
					break;
				}
			}
			_draggedCorpseCarrierLastCrouchState = false;
			_draggedCorpseCarrierLastWieldedPrimaryWeaponIndex = EquipmentIndex.None;
			return eventControlFlag;
		}
		return Agent.EventControlFlag.None;
	}
}
