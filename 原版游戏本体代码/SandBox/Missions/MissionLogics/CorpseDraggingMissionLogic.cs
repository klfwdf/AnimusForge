using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000067 RID: 103
	public class CorpseDraggingMissionLogic : MissionLogic, IPlayerInputEffector, IMissionBehavior
	{
		// Token: 0x06000417 RID: 1047 RVA: 0x00017981 File Offset: 0x00015B81
		public override void AfterStart()
		{
			base.AfterStart();
			base.Mission.FocusableObjectInformationProvider.AddInfoCallback(new GetFocusableObjectInteractionTextsDelegate(this.GetFocusableObjectInteractionInfoTexts));
			base.Mission.Scene.SetFixedTickCallbackActive(true);
		}

		// Token: 0x06000418 RID: 1048 RVA: 0x000179B8 File Offset: 0x00015BB8
		private void GetFocusableObjectInteractionInfoTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
		{
			focusableObjectInformation = default(FocusableObjectInformation);
			Agent agent;
			if (requesterAgent.IsMainAgent && (agent = focusableObject as Agent) != null && !agent.IsActive())
			{
				BasicCharacterObject character = agent.Character;
				focusableObjectInformation.PrimaryInteractionText = ((character != null) ? character.Name : null) ?? agent.Monster.GetName();
				if (isInteractable)
				{
					MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13), 1f), false);
					focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action", null);
					focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use", null));
					focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", GameTexts.FindText("str_ui_drag", null));
				}
				else
				{
					focusableObjectInformation.SecondaryInteractionText = null;
				}
				focusableObjectInformation.IsActive = true;
				return;
			}
			focusableObjectInformation.IsActive = false;
		}

		// Token: 0x06000419 RID: 1049 RVA: 0x00017AA4 File Offset: 0x00015CA4
		private void SetDraggedCorpse(Agent draggedCorpse, sbyte draggedCorpseBoneIndex)
		{
			if (this._draggedCorpse != null)
			{
				this._draggedCorpse.SetVelocityLimitsOnRagdoll(-1f, -1f);
				this._draggedCorpse.EndRagdollAsCorpse();
			}
			this._draggedCorpse = draggedCorpse;
			this._draggedCorpseBoneIndex = draggedCorpseBoneIndex;
			this._draggedCorpseUnbindDistanceSquared = 100f;
			this._draggedCorpseAverageVelocity = Vec3.Zero;
			this._draggedCorpseBoneLastGlobalPosition = Vec3.Zero;
			if (this._bodyDragSoundEvent != null)
			{
				this._bodyDragSoundEvent.Stop();
			}
			this._bodyDragSoundEvent = null;
			if (this._draggedCorpse != null)
			{
				if (!this._draggedCorpse.IsAddedAsCorpse())
				{
					this._draggedCorpse.AddAsCorpse();
				}
				this._draggedCorpseBoneLastGlobalPosition = this._draggedCorpse.AgentVisuals.GetGlobalFrame().TransformToParent(this._draggedCorpse.AgentVisuals.GetEntity().GetBoneEntitialFrameWithIndex(Math.Max(0, this._draggedCorpseBoneIndex)).origin);
				this._draggedCorpse.StartRagdollAsCorpse();
				Agent mainAgent = base.Mission.MainAgent;
				mainAgent.SetMaximumSpeedLimit(1f, false);
				mainAgent.SetDraggingMode(true);
				this._crouchStandEvent = CorpseDraggingMissionLogic.CrouchStandEvent.Crouch;
				this._draggedCorpseCarrierLastWieldedPrimaryWeaponIndex = mainAgent.GetPrimaryWieldedItemIndex();
				this._draggedCorpseCarrierLastCrouchState = mainAgent.CrouchMode;
				this._startedPickingUpDraggedCorpse = true;
				this._triggerBodyGrabSound = true;
				return;
			}
			if (base.Mission.MainAgent != null)
			{
				Agent mainAgent2 = base.Mission.MainAgent;
				mainAgent2.SetMaximumSpeedLimit(-1f, false);
				mainAgent2.SetDraggingMode(false);
				this._crouchStandEvent = CorpseDraggingMissionLogic.CrouchStandEvent.Stand;
				this._startedPickingUpDraggedCorpse = false;
			}
		}

		// Token: 0x0600041A RID: 1050 RVA: 0x00017C1C File Offset: 0x00015E1C
		public override void OnFixedMissionTick(float fixedDt)
		{
			Agent mainAgent = base.Mission.MainAgent;
			if (this._draggedCorpse != null && this._crouchStandEvent == CorpseDraggingMissionLogic.CrouchStandEvent.None && (mainAgent.GetCurrentActionType(1) != Agent.ActionCodeType.EquipUnequip || !this._startedPickingUpDraggedCorpse) && this._draggedCorpse.AgentVisuals.GetCurrentRagdollState() == RagdollState.Active)
			{
				this._draggedCorpse.SetVelocityLimitsOnRagdoll(10f, 50f);
				GameEntity entity = mainAgent.AgentVisuals.GetEntity();
				MatrixFrame matrixFrame;
				entity.GetQuickBoneEntitialFrame(mainAgent.Monster.MainHandItemBoneIndex, out matrixFrame);
				Vec3 v = entity.GetGlobalFrameImpreciseForFixedTick().TransformToParent(matrixFrame.origin);
				MatrixFrame matrixFrame2;
				this._draggedCorpse.AgentVisuals.GetEntity().GetQuickBoneEntitialFrame(Math.Max(0, this._draggedCorpseBoneIndex), out matrixFrame2);
				Vec3 origin = this._draggedCorpse.AgentVisuals.GetEntity().GetGlobalFrameImpreciseForFixedTick().TransformToParent(matrixFrame2)
					.origin;
				Vec3 vec = v - origin;
				Vec3 v2 = (origin - this._draggedCorpseBoneLastGlobalPosition) / fixedDt;
				this._draggedCorpseAverageVelocity = Vec3.Lerp(this._draggedCorpseAverageVelocity, v2, 10f * fixedDt);
				this._draggedCorpseBoneLastGlobalPosition = origin;
				Vec3 vec2;
				if (this._triggerBodyGrabSound)
				{
					EquipmentElement equipmentElement = this._draggedCorpse.SpawnEquipment[EquipmentIndex.Body];
					float soundParameterForArmorType = Agent.GetSoundParameterForArmorType((!equipmentElement.IsInvalid() && equipmentElement.Item.HasArmorComponent) ? equipmentElement.Item.ArmorComponent.MaterialType : ArmorComponent.ArmorMaterialTypes.None);
					this._bodyDragSoundEvent = SoundEvent.CreateEvent(CorpseDraggingMissionLogic._bodyDragSoundEventId, Mission.Current.Scene);
					this._bodyDragSoundEvent.SetParameter("Armor Type", soundParameterForArmorType);
					CorpseDraggingMissionLogic.GroundMaterialCorpseDrag groundMaterialCorpseDrag = CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Default;
					CorpseDraggingMissionLogic._corpseDragMateriels.TryGetValue(PhysicsMaterial.GetNameAtIndex(this._draggedCorpse.GetGroundMaterialForCollisionEffect()), out groundMaterialCorpseDrag);
					this._bodyDragSoundEvent.SetParameter("BodydragMaterial", (float)groundMaterialCorpseDrag);
					this._bodyDragSoundEvent.SetPosition(this._draggedCorpse.Position);
					this._bodyDragSoundEvent.Play();
					string eventFullName = "event:/physics/bodydrag/human/grab/body_grab";
					vec2 = this._draggedCorpse.Position;
					SoundManager.StartOneShotEvent(eventFullName, vec2, "Armor Type", soundParameterForArmorType);
					this._triggerBodyGrabSound = false;
				}
				if (vec.LengthSquared > this._draggedCorpseUnbindDistanceSquared)
				{
					this.SetDraggedCorpse(null, -1);
					return;
				}
				Vec3 v3 = vec;
				vec2 = mainAgent.Velocity;
				Vec3 v4 = v3 + new Vec3(vec2.AsVec2 - this._draggedCorpseAverageVelocity.AsVec2 * 0.5f, 0f, -1f) * 0.5f;
				float num = 150000f * fixedDt;
				Vec3 v5 = v4 * num;
				if (v5.LengthSquared > num * num)
				{
					v5.Normalize();
					v5 *= num;
				}
				this._draggedCorpse.ApplyForceOnRagdoll(this._draggedCorpseBoneIndex, v5);
				this._draggedCorpseUnbindDistanceSquared = Math.Max(2.25f, Math.Min(vec.LengthSquared * 1.3f * 1.3f, this._draggedCorpseUnbindDistanceSquared));
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag groundMaterialCorpseDrag2 = CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Default;
				CorpseDraggingMissionLogic._corpseDragMateriels.TryGetValue(PhysicsMaterial.GetNameAtIndex(this._draggedCorpse.GetGroundMaterialForCollisionEffect()), out groundMaterialCorpseDrag2);
				this._bodyDragSoundEvent.SetParameter("BodydragMaterial", (float)groundMaterialCorpseDrag2);
				this._bodyDragSoundEvent.SetPosition(this._draggedCorpse.Position);
				this._bodyDragSoundEvent.SetParameter("BodydragSpeed", this._draggedCorpseAverageVelocity.AsVec2.Length);
			}
		}

		// Token: 0x0600041B RID: 1051 RVA: 0x00017F80 File Offset: 0x00016180
		public override void OnMissionTick(float dt)
		{
			Agent mainAgent = base.Mission.MainAgent;
			if (mainAgent == null || !mainAgent.IsActive())
			{
				this.SetDraggedCorpse(null, -1);
			}
			bool flag = mainAgent != null && mainAgent.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.Action);
			if (this._draggedCorpse != null && this._crouchStandEvent == CorpseDraggingMissionLogic.CrouchStandEvent.None && (mainAgent.GetCurrentActionType(1) != Agent.ActionCodeType.EquipUnequip || !this._startedPickingUpDraggedCorpse) && this._draggedCorpse.AgentVisuals.GetCurrentRagdollState() == RagdollState.Active && dt > 0f)
			{
				this._startedPickingUpDraggedCorpse = false;
				Agent.ActionCodeType currentActionType = mainAgent.GetCurrentActionType(0);
				if ((flag && !this._previousActionKeyPressed) || (mainAgent.GetCurrentAction(0) != ActionIndexCache.act_none && currentActionType != Agent.ActionCodeType.Jump && currentActionType != Agent.ActionCodeType.JumpEnd) || mainAgent.GetCurrentAction(1) != ActionIndexCache.act_none || !mainAgent.CrouchMode || mainAgent.GetPrimaryWieldedItemIndex() >= EquipmentIndex.WeaponItemBeginSlot)
				{
					this.SetDraggedCorpse(null, -1);
				}
			}
			this._previousActionKeyPressed = flag;
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x00018071 File Offset: 0x00016271
		public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
		{
			return this._draggedCorpse == null && userAgent.IsPlayerControlled && (otherAgent.State == AgentState.Killed || otherAgent.State == AgentState.Unconscious);
		}

		// Token: 0x0600041D RID: 1053 RVA: 0x0001809C File Offset: 0x0001629C
		public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
		{
			if (this.IsThereAgentAction(userAgent, agent))
			{
				MBAgentVisuals agentVisuals = userAgent.AgentVisuals;
				Skeleton skeleton = agentVisuals.GetSkeleton();
				List<int> list = new List<int>(21)
				{
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ItemL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ItemR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.HandL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.HandR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.Forearm1L),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.Forearm1R),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ForearmL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ForearmR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.UpperarmTwist1L),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.UpperarmTwist1R),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.UpperarmL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.UpperarmR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ToeL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ToeR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.FootL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.FootR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.CalfL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.CalfR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ThighL),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.ThighR),
					(int)agentVisuals.GetRealBoneIndex(HumanBone.Head)
				};
				while (list.IndexOf((int)agentBoneIndex) >= 0)
				{
					agentBoneIndex = skeleton.GetParentBoneIndex(agentBoneIndex);
				}
				this.SetDraggedCorpse(agent, agentBoneIndex);
				this._previousActionKeyPressed = true;
			}
		}

		// Token: 0x0600041E RID: 1054 RVA: 0x00018210 File Offset: 0x00016410
		public Agent.EventControlFlag OnCollectPlayerEventControlFlags()
		{
			if (this._crouchStandEvent == CorpseDraggingMissionLogic.CrouchStandEvent.Crouch)
			{
				this._crouchStandEvent = CorpseDraggingMissionLogic.CrouchStandEvent.None;
				return Agent.EventControlFlag.Sheath0 | Agent.EventControlFlag.Crouch;
			}
			if (this._crouchStandEvent == CorpseDraggingMissionLogic.CrouchStandEvent.Stand)
			{
				this._crouchStandEvent = CorpseDraggingMissionLogic.CrouchStandEvent.None;
				Agent.EventControlFlag eventControlFlag = (this._draggedCorpseCarrierLastCrouchState ? Agent.EventControlFlag.None : Agent.EventControlFlag.Stand);
				if (base.Mission.MainAgent.GetCurrentActionType(1) != Agent.ActionCodeType.EquipUnequip)
				{
					switch (this._draggedCorpseCarrierLastWieldedPrimaryWeaponIndex)
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
				this._draggedCorpseCarrierLastCrouchState = false;
				this._draggedCorpseCarrierLastWieldedPrimaryWeaponIndex = EquipmentIndex.None;
				return eventControlFlag;
			}
			return Agent.EventControlFlag.None;
		}

		// Token: 0x04000219 RID: 537
		private static readonly Dictionary<string, CorpseDraggingMissionLogic.GroundMaterialCorpseDrag> _corpseDragMateriels = new Dictionary<string, CorpseDraggingMissionLogic.GroundMaterialCorpseDrag>
		{
			{
				"",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Default
			},
			{
				"default",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Default
			},
			{
				"fabric",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Fabric
			},
			{
				"grass",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Grass
			},
			{
				"mud",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Mud
			},
			{
				"sand",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Sand
			},
			{
				"snow",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Snow
			},
			{
				"stone",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Stone
			},
			{
				"water",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Water
			},
			{
				"wood",
				CorpseDraggingMissionLogic.GroundMaterialCorpseDrag.Wood
			}
		};

		// Token: 0x0400021A RID: 538
		private static int _bodyDragSoundEventId = SoundManager.GetEventGlobalIndex("event:/physics/bodydrag/human/drag/default");

		// Token: 0x0400021B RID: 539
		private Agent _draggedCorpse;

		// Token: 0x0400021C RID: 540
		private bool _startedPickingUpDraggedCorpse;

		// Token: 0x0400021D RID: 541
		private bool _triggerBodyGrabSound;

		// Token: 0x0400021E RID: 542
		private Vec3 _draggedCorpseAverageVelocity = Vec3.Zero;

		// Token: 0x0400021F RID: 543
		private Vec3 _draggedCorpseBoneLastGlobalPosition = Vec3.Zero;

		// Token: 0x04000220 RID: 544
		private sbyte _draggedCorpseBoneIndex = -1;

		// Token: 0x04000221 RID: 545
		private float _draggedCorpseUnbindDistanceSquared = 100f;

		// Token: 0x04000222 RID: 546
		private EquipmentIndex _draggedCorpseCarrierLastWieldedPrimaryWeaponIndex = EquipmentIndex.None;

		// Token: 0x04000223 RID: 547
		private bool _draggedCorpseCarrierLastCrouchState;

		// Token: 0x04000224 RID: 548
		private bool _previousActionKeyPressed;

		// Token: 0x04000225 RID: 549
		private CorpseDraggingMissionLogic.CrouchStandEvent _crouchStandEvent;

		// Token: 0x04000226 RID: 550
		private SoundEvent _bodyDragSoundEvent;

		// Token: 0x0200015C RID: 348
		private enum CrouchStandEvent
		{
			// Token: 0x040006CF RID: 1743
			None,
			// Token: 0x040006D0 RID: 1744
			Crouch,
			// Token: 0x040006D1 RID: 1745
			Stand
		}

		// Token: 0x0200015D RID: 349
		private enum GroundMaterialCorpseDrag
		{
			// Token: 0x040006D3 RID: 1747
			Default,
			// Token: 0x040006D4 RID: 1748
			Fabric,
			// Token: 0x040006D5 RID: 1749
			Grass,
			// Token: 0x040006D6 RID: 1750
			Mud,
			// Token: 0x040006D7 RID: 1751
			Sand,
			// Token: 0x040006D8 RID: 1752
			Snow,
			// Token: 0x040006D9 RID: 1753
			Stone,
			// Token: 0x040006DA RID: 1754
			Water,
			// Token: 0x040006DB RID: 1755
			Wood
		}
	}
}
