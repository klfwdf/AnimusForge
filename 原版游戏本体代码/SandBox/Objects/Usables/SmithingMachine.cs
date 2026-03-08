using System;
using System.Linq;
using SandBox.AI;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables
{
	// Token: 0x0200004F RID: 79
	public class SmithingMachine : UsableMachine
	{
		// Token: 0x060002E3 RID: 739 RVA: 0x00010368 File Offset: 0x0000E568
		protected override void OnInit()
		{
			base.OnInit();
			this._machineUsePoint = (AnimationPoint)base.PilotStandingPoint;
			if (this._machineUsePoint == null)
			{
				"Entity(" + base.GameEntity.Name + ") with script(SmithingMachine) does not have a valid 'PilotStandingPoint'.";
			}
			this._machineUsePoint.IsDeactivated = false;
			this._machineUsePoint.IsDisabledForPlayers = true;
			this._machineUsePoint.KeepOldVisibility = true;
			this._anvilUsePoint = (AnimationPoint)base.StandingPoints.First((StandingPoint x) => x != this._machineUsePoint);
			this._anvilUsePoint.IsDeactivated = true;
			this._anvilUsePoint.KeepOldVisibility = true;
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				standingPoint.IsDisabledForPlayers = true;
			}
			base.SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle", 0, 1f);
			base.SetScriptComponentToTick(this.GetTickRequirement());
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x00010474 File Offset: 0x0000E674
		public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			return new TextObject("{=OCRafO5h}Bellows", null);
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x00010481 File Offset: 0x0000E681
		public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
		{
			TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use", null);
			textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13), 1f));
			return textObject;
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x000104B0 File Offset: 0x0000E6B0
		public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			return ScriptComponentBehavior.TickRequirement.Tick | base.GetTickRequirement();
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x000104BC File Offset: 0x0000E6BC
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			switch (this._state)
			{
			case SmithingMachine.State.Stable:
				if (this._machineUsePoint.HasUser && this._machineUsePoint.UserAgent.GetCurrentVelocity().LengthSquared < 0.0001f)
				{
					this._machineUsePoint.UserAgent.SetActionChannel(0, ActionIndexCache.act_use_smithing_machine_ready, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					this._state = SmithingMachine.State.Preparation;
				}
				if (this._anvilUsePoint.HasUser)
				{
					this._state = SmithingMachine.State.UseAnvilPoint;
					return;
				}
				break;
			case SmithingMachine.State.Preparation:
				if (!this._machineUsePoint.HasUser)
				{
					base.SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0, 1f);
					this._state = SmithingMachine.State.Stable;
					return;
				}
				if (this._machineUsePoint.UserAgent.GetCurrentAction(0) == ActionIndexCache.act_use_smithing_machine_ready && this._machineUsePoint.UserAgent.GetCurrentActionProgress(0) > 0.99f)
				{
					base.SetAnimationAtChannelSynched("anim_merchant_smithing_machine_loop", 0, 1f);
					this._machineUsePoint.UserAgent.SetActionChannel(0, ActionIndexCache.act_use_smithing_machine_loop, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					this._state = SmithingMachine.State.Working;
					return;
				}
				break;
			case SmithingMachine.State.Working:
				if (!this._machineUsePoint.HasUser)
				{
					base.SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0, 1f);
					this._state = SmithingMachine.State.Stable;
					this._disableTimer = null;
					this._anvilUsePoint.IsDeactivated = false;
					return;
				}
				if (this._machineUsePoint.UserAgent.GetCurrentAction(0) != ActionIndexCache.act_use_smithing_machine_loop)
				{
					base.SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0, 1f);
					this._state = SmithingMachine.State.Paused;
					this._remainingTimeToReset = this._disableTimer.Duration - this._disableTimer.ElapsedTime();
					return;
				}
				if (this._disableTimer == null)
				{
					this._disableTimer = new Timer(Mission.Current.CurrentTime, 9.8f, true);
					return;
				}
				if (this._disableTimer.Check(Mission.Current.CurrentTime))
				{
					base.SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0, 1f);
					this._disableTimer = null;
					this._machineUsePoint.IsDeactivated = true;
					this._anvilUsePoint.IsDeactivated = false;
					this._state = SmithingMachine.State.Stable;
					return;
				}
				break;
			case SmithingMachine.State.Paused:
				if (this._machineUsePoint.IsRotationCorrectDuringUsage())
				{
					this._machineUsePoint.UserAgent.SetActionChannel(0, ActionIndexCache.act_use_smithing_machine_ready, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				}
				if (this._machineUsePoint.UserAgent.GetCurrentAction(0) == ActionIndexCache.act_use_smithing_machine_ready)
				{
					this._state = SmithingMachine.State.Preparation;
					this._disableTimer.Reset(Mission.Current.CurrentTime, this._remainingTimeToReset);
					this._remainingTimeToReset = 0f;
					return;
				}
				break;
			case SmithingMachine.State.UseAnvilPoint:
			{
				if (!this._anvilUsePoint.HasUser)
				{
					this._state = SmithingMachine.State.Stable;
					this._disableTimer = null;
					this._machineUsePoint.IsDeactivated = false;
					return;
				}
				if (this._disableTimer == null)
				{
					this._disableTimer = new Timer(Mission.Current.CurrentTime, 96f, true);
					this._leftItemIsVisible = true;
					return;
				}
				if (this._disableTimer.Check(Mission.Current.CurrentTime))
				{
					this._disableTimer = null;
					this._anvilUsePoint.IsDeactivated = true;
					this._machineUsePoint.IsDeactivated = false;
					this._state = SmithingMachine.State.Stable;
					return;
				}
				ActionIndexCache currentAction = this._anvilUsePoint.UserAgent.GetCurrentAction(0);
				if (this._leftItemIsVisible && SmithingMachine._actionsWithoutLeftHandItem.Contains(currentAction))
				{
					this._anvilUsePoint.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(this._anvilUsePoint.UserAgent.Monster.OffHandItemBoneIndex, this._anvilUsePoint.LeftHandItem, false);
					this._leftItemIsVisible = false;
					return;
				}
				if (!this._leftItemIsVisible && !SmithingMachine._actionsWithoutLeftHandItem.Contains(currentAction))
				{
					this._anvilUsePoint.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(this._anvilUsePoint.UserAgent.Monster.OffHandItemBoneIndex, this._anvilUsePoint.LeftHandItem, true);
					this._leftItemIsVisible = true;
				}
				break;
			}
			default:
				return;
			}
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x00010917 File Offset: 0x0000EB17
		public override UsableMachineAIBase CreateAIBehaviorObject()
		{
			return new UsablePlaceAI(this);
		}

		// Token: 0x0400013F RID: 319
		private const string MachineIdleAnimationName = "anim_merchant_smithing_machine_idle";

		// Token: 0x04000140 RID: 320
		private const string MachineIdleWithBlendInAnimationName = "anim_merchant_smithing_machine_idle_with_blend_in";

		// Token: 0x04000141 RID: 321
		private const string MachineUseAnimationName = "anim_merchant_smithing_machine_loop";

		// Token: 0x04000142 RID: 322
		private static readonly ActionIndexCache[] _actionsWithoutLeftHandItem = new ActionIndexCache[]
		{
			ActionIndexCache.act_smithing_machine_anvil_start,
			ActionIndexCache.act_smithing_machine_anvil_part_2,
			ActionIndexCache.act_smithing_machine_anvil_part_4,
			ActionIndexCache.act_smithing_machine_anvil_part_5
		};

		// Token: 0x04000143 RID: 323
		private AnimationPoint _anvilUsePoint;

		// Token: 0x04000144 RID: 324
		private AnimationPoint _machineUsePoint;

		// Token: 0x04000145 RID: 325
		private SmithingMachine.State _state;

		// Token: 0x04000146 RID: 326
		private Timer _disableTimer;

		// Token: 0x04000147 RID: 327
		private float _remainingTimeToReset;

		// Token: 0x04000148 RID: 328
		private bool _leftItemIsVisible;

		// Token: 0x0200014E RID: 334
		private enum State
		{
			// Token: 0x04000693 RID: 1683
			Stable,
			// Token: 0x04000694 RID: 1684
			Preparation,
			// Token: 0x04000695 RID: 1685
			Working,
			// Token: 0x04000696 RID: 1686
			Paused,
			// Token: 0x04000697 RID: 1687
			UseAnvilPoint
		}
	}
}
