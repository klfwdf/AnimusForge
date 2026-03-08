using System;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.AgentControllers
{
	// Token: 0x02000032 RID: 50
	public class JoustingAgentController : AgentController
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060001D3 RID: 467 RVA: 0x0000BD6C File Offset: 0x00009F6C
		// (set) Token: 0x060001D4 RID: 468 RVA: 0x0000BD74 File Offset: 0x00009F74
		public JoustingAgentController.JoustingAgentState State
		{
			get
			{
				return this._state;
			}
			set
			{
				if (value != this._state)
				{
					this._state = value;
					this.JoustingMissionController.OnJoustingAgentStateChanged(base.Owner, value);
				}
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x0000BD98 File Offset: 0x00009F98
		// (set) Token: 0x060001D6 RID: 470 RVA: 0x0000BDA0 File Offset: 0x00009FA0
		public TournamentJoustingMissionController JoustingMissionController { get; private set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060001D7 RID: 471 RVA: 0x0000BDAC File Offset: 0x00009FAC
		public Agent Opponent
		{
			get
			{
				if (this._opponentAgent == null)
				{
					foreach (Agent agent in base.Mission.Agents)
					{
						if (agent.IsHuman && agent != base.Owner)
						{
							this._opponentAgent = agent;
						}
					}
				}
				return this._opponentAgent;
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060001D8 RID: 472 RVA: 0x0000BE24 File Offset: 0x0000A024
		// (set) Token: 0x060001D9 RID: 473 RVA: 0x0000BE2C File Offset: 0x0000A02C
		public bool PrepareEquipmentsAfterDismount { get; private set; }

		// Token: 0x060001DA RID: 474 RVA: 0x0000BE35 File Offset: 0x0000A035
		public override void OnInitialize()
		{
			this.JoustingMissionController = base.Mission.GetMissionBehavior<TournamentJoustingMissionController>();
			this._state = JoustingAgentController.JoustingAgentState.WaitingOpponent;
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0000BE4F File Offset: 0x0000A04F
		public void UpdateState()
		{
			if (base.Owner.Character.IsPlayerCharacter)
			{
				this.UpdateMainAgentState();
				return;
			}
			this.UpdateAIAgentState();
		}

		// Token: 0x060001DC RID: 476 RVA: 0x0000BE70 File Offset: 0x0000A070
		private void UpdateMainAgentState()
		{
			JoustingAgentController controller = this.Opponent.GetController<JoustingAgentController>();
			bool flag = this.JoustingMissionController.CornerStartList[this.CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position) && !this.JoustingMissionController.RegionBoxList[this.CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position);
			switch (this.State)
			{
			case JoustingAgentController.JoustingAgentState.GoToStartPosition:
				if (flag)
				{
					this.State = JoustingAgentController.JoustingAgentState.WaitInStartPosition;
					return;
				}
				break;
			case JoustingAgentController.JoustingAgentState.WaitInStartPosition:
				if (!flag)
				{
					this.State = JoustingAgentController.JoustingAgentState.GoToStartPosition;
					return;
				}
				if (base.Owner.GetCurrentVelocity().LengthSquared < 0.0025000002f)
				{
					this.State = JoustingAgentController.JoustingAgentState.WaitingOpponent;
					return;
				}
				break;
			case JoustingAgentController.JoustingAgentState.WaitingOpponent:
				if (!flag)
				{
					this.State = JoustingAgentController.JoustingAgentState.GoToStartPosition;
					return;
				}
				if (controller.State == JoustingAgentController.JoustingAgentState.WaitingOpponent || controller.State == JoustingAgentController.JoustingAgentState.Ready)
				{
					this.State = JoustingAgentController.JoustingAgentState.Ready;
					return;
				}
				break;
			case JoustingAgentController.JoustingAgentState.Ready:
				if (this.JoustingMissionController.IsAgentInTheTrack(base.Owner, true) && base.Owner.GetCurrentVelocity().LengthSquared > 0.0025000002f)
				{
					this.State = JoustingAgentController.JoustingAgentState.Riding;
					return;
				}
				if (controller.State == JoustingAgentController.JoustingAgentState.GoToStartPosition)
				{
					this.State = JoustingAgentController.JoustingAgentState.WaitingOpponent;
					return;
				}
				if (!this.JoustingMissionController.CornerStartList[this.CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position))
				{
					this.State = JoustingAgentController.JoustingAgentState.GoToStartPosition;
					return;
				}
				break;
			case JoustingAgentController.JoustingAgentState.StartRiding:
				break;
			case JoustingAgentController.JoustingAgentState.Riding:
				if (this.JoustingMissionController.IsAgentInTheTrack(base.Owner, false))
				{
					this.State = JoustingAgentController.JoustingAgentState.RidingAtWrongSide;
				}
				if (this.JoustingMissionController.RegionExitBoxList[this.CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position))
				{
					this.State = JoustingAgentController.JoustingAgentState.GoToStartPosition;
					this.CurrentCornerIndex = 1 - this.CurrentCornerIndex;
					return;
				}
				break;
			case JoustingAgentController.JoustingAgentState.RidingAtWrongSide:
				if (this.JoustingMissionController.IsAgentInTheTrack(base.Owner, true))
				{
					this.State = JoustingAgentController.JoustingAgentState.Riding;
					return;
				}
				if (this.JoustingMissionController.CornerStartList[1 - this.CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position))
				{
					this.State = JoustingAgentController.JoustingAgentState.GoToStartPosition;
					this.CurrentCornerIndex = 1 - this.CurrentCornerIndex;
				}
				break;
			default:
				return;
			}
		}

		// Token: 0x060001DD RID: 477 RVA: 0x0000C0A0 File Offset: 0x0000A2A0
		private void UpdateAIAgentState()
		{
			if (this.Opponent != null && this.Opponent.IsActive())
			{
				JoustingAgentController controller = this.Opponent.GetController<JoustingAgentController>();
				switch (this.State)
				{
				case JoustingAgentController.JoustingAgentState.GoingToBackStart:
					if (base.Owner.Position.Distance(this.JoustingMissionController.CornerBackStartList[this.CurrentCornerIndex].origin) < 3f && base.Owner.GetCurrentVelocity().LengthSquared < 0.0025000002f)
					{
						this.CurrentCornerIndex = 1 - this.CurrentCornerIndex;
						MatrixFrame globalFrame = this.JoustingMissionController.CornerStartList[this.CurrentCornerIndex].GetGlobalFrame();
						WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, globalFrame.origin, false);
						base.Owner.SetScriptedPositionAndDirection(ref worldPosition, globalFrame.rotation.f.AsVec2.RotationInRadians, false, Agent.AIScriptedFrameFlags.None);
						this.State = JoustingAgentController.JoustingAgentState.GoToStartPosition;
						return;
					}
					break;
				case JoustingAgentController.JoustingAgentState.GoToStartPosition:
					if (this.JoustingMissionController.CornerStartList[this.CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position) && base.Owner.GetCurrentVelocity().LengthSquared < 0.0025000002f)
					{
						this.State = JoustingAgentController.JoustingAgentState.WaitingOpponent;
						return;
					}
					break;
				case JoustingAgentController.JoustingAgentState.WaitInStartPosition:
					break;
				case JoustingAgentController.JoustingAgentState.WaitingOpponent:
					if (controller.State == JoustingAgentController.JoustingAgentState.WaitingOpponent || controller.State == JoustingAgentController.JoustingAgentState.Ready)
					{
						this.State = JoustingAgentController.JoustingAgentState.Ready;
						return;
					}
					break;
				case JoustingAgentController.JoustingAgentState.Ready:
					if (controller.State == JoustingAgentController.JoustingAgentState.Riding)
					{
						this.State = JoustingAgentController.JoustingAgentState.StartRiding;
						WorldPosition worldPosition2 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, this.JoustingMissionController.CornerMiddleList[this.CurrentCornerIndex].origin, false);
						base.Owner.SetScriptedPosition(ref worldPosition2, false, Agent.AIScriptedFrameFlags.NeverSlowDown);
						return;
					}
					if (controller.State == JoustingAgentController.JoustingAgentState.Ready)
					{
						WorldPosition worldPosition3 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, this.JoustingMissionController.CornerStartList[this.CurrentCornerIndex].GetGlobalFrame().origin, false);
						base.Owner.SetScriptedPosition(ref worldPosition3, false, Agent.AIScriptedFrameFlags.NeverSlowDown);
						return;
					}
					this.State = JoustingAgentController.JoustingAgentState.WaitingOpponent;
					return;
				case JoustingAgentController.JoustingAgentState.StartRiding:
					if (base.Owner.Position.Distance(this.JoustingMissionController.CornerMiddleList[this.CurrentCornerIndex].origin) < 3f)
					{
						WorldPosition worldPosition4 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, this.JoustingMissionController.CornerFinishList[this.CurrentCornerIndex].origin, false);
						base.Owner.SetScriptedPosition(ref worldPosition4, false, Agent.AIScriptedFrameFlags.NeverSlowDown);
						this.State = JoustingAgentController.JoustingAgentState.Riding;
						return;
					}
					break;
				case JoustingAgentController.JoustingAgentState.Riding:
					if (base.Owner.Position.Distance(this.JoustingMissionController.CornerFinishList[this.CurrentCornerIndex].origin) < 3f)
					{
						WorldPosition worldPosition5 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, this.JoustingMissionController.CornerBackStartList[this.CurrentCornerIndex].origin, false);
						base.Owner.SetScriptedPosition(ref worldPosition5, false, Agent.AIScriptedFrameFlags.None);
						this.State = JoustingAgentController.JoustingAgentState.GoingToBackStart;
					}
					break;
				default:
					return;
				}
			}
		}

		// Token: 0x060001DE RID: 478 RVA: 0x0000C3E4 File Offset: 0x0000A5E4
		public void PrepareAgentToSwordDuel()
		{
			if (base.Owner.MountAgent != null)
			{
				base.Owner.Controller = AgentControllerType.AI;
				WorldPosition worldPosition = this.Opponent.GetWorldPosition();
				base.Owner.SetScriptedPosition(ref worldPosition, false, Agent.AIScriptedFrameFlags.GoWithoutMount);
				this.PrepareEquipmentsAfterDismount = true;
				return;
			}
			this.PrepareEquipmentsForSwordDuel();
			base.Owner.DisableScriptedMovement();
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0000C43F File Offset: 0x0000A63F
		public void PrepareEquipmentsForSwordDuel()
		{
			this.AddEquipmentsForSwordDuel();
			base.Owner.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
			this.PrepareEquipmentsAfterDismount = false;
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x0000C45C File Offset: 0x0000A65C
		private void AddEquipmentsForSwordDuel()
		{
			base.Owner.DropItem(EquipmentIndex.WeaponItemBeginSlot, WeaponClass.Undefined);
			ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>("wooden_sword_t1");
			ItemModifier itemModifier = null;
			IAgentOriginBase origin = base.Owner.Origin;
			MissionWeapon missionWeapon = new MissionWeapon(@object, itemModifier, (origin != null) ? origin.Banner : null);
			base.Owner.EquipWeaponWithNewEntity(EquipmentIndex.Weapon2, ref missionWeapon);
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x0000C4B7 File Offset: 0x0000A6B7
		public bool IsRiding()
		{
			return this.State == JoustingAgentController.JoustingAgentState.StartRiding || this.State == JoustingAgentController.JoustingAgentState.Riding;
		}

		// Token: 0x040000A8 RID: 168
		private JoustingAgentController.JoustingAgentState _state;

		// Token: 0x040000AA RID: 170
		public int CurrentCornerIndex;

		// Token: 0x040000AB RID: 171
		private const float MaxDistance = 3f;

		// Token: 0x040000AC RID: 172
		public int Score;

		// Token: 0x040000AD RID: 173
		private Agent _opponentAgent;

		// Token: 0x02000142 RID: 322
		public enum JoustingAgentState
		{
			// Token: 0x04000658 RID: 1624
			GoingToBackStart,
			// Token: 0x04000659 RID: 1625
			GoToStartPosition,
			// Token: 0x0400065A RID: 1626
			WaitInStartPosition,
			// Token: 0x0400065B RID: 1627
			WaitingOpponent,
			// Token: 0x0400065C RID: 1628
			Ready,
			// Token: 0x0400065D RID: 1629
			StartRiding,
			// Token: 0x0400065E RID: 1630
			Riding,
			// Token: 0x0400065F RID: 1631
			RidingAtWrongSide,
			// Token: 0x04000660 RID: 1632
			SwordDuel
		}
	}
}
