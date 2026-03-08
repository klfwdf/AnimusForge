using System;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000AF RID: 175
	public class WalkingBehavior : AgentBehavior
	{
		// Token: 0x17000098 RID: 152
		// (get) Token: 0x0600074D RID: 1869 RVA: 0x000323CB File Offset: 0x000305CB
		private bool CanWander
		{
			get
			{
				return (this._isIndoor && this._indoorWanderingIsActive) || (!this._isIndoor && this._outdoorWanderingIsActive);
			}
		}

		// Token: 0x0600074E RID: 1870 RVA: 0x000323F0 File Offset: 0x000305F0
		public WalkingBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			this._missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
			this._wanderTarget = null;
			this._isIndoor = CampaignMission.Current.Location.IsIndoor;
			this._indoorWanderingIsActive = true;
			this._outdoorWanderingIsActive = true;
			this._wasSimulation = false;
		}

		// Token: 0x0600074F RID: 1871 RVA: 0x00032446 File Offset: 0x00030646
		public void SetIndoorWandering(bool isActive)
		{
			this._indoorWanderingIsActive = isActive;
		}

		// Token: 0x06000750 RID: 1872 RVA: 0x0003244F File Offset: 0x0003064F
		public void SetOutdoorWandering(bool isActive)
		{
			this._outdoorWanderingIsActive = isActive;
		}

		// Token: 0x06000751 RID: 1873 RVA: 0x00032458 File Offset: 0x00030658
		public override void Tick(float dt, bool isSimulation)
		{
			if (this._wanderTarget == null || base.Navigator.TargetUsableMachine == null || this._wanderTarget.IsDisabled || !this._wanderTarget.IsStandingPointAvailableForAgent(base.OwnerAgent))
			{
				this._wanderTarget = this.FindTarget();
				this._lastTarget = this._wanderTarget;
			}
			else if (base.Navigator.GetDistanceToTarget(this._wanderTarget) < 5f)
			{
				bool flag = this._wasSimulation && !isSimulation && this._wanderTarget != null && this._waitTimer != null && MBRandom.RandomFloat < (this._isIndoor ? 0f : (Settlement.CurrentSettlement.IsVillage ? 0.6f : 0.1f));
				if (this._waitTimer == null)
				{
					if (!this._wanderTarget.GameEntity.HasTag("npc_idle"))
					{
						this.SetTimerForTheAgent(isSimulation);
					}
				}
				else if (this._waitTimer.Check(base.Mission.CurrentTime) || flag)
				{
					if (this.CanWander)
					{
						this._waitTimer = null;
						UsableMachine usableMachine = this.FindTarget();
						if (usableMachine == null || this.IsChildrenOfSameParent(usableMachine, this._wanderTarget))
						{
							this.SetTimerForTheAgent(isSimulation);
						}
						else
						{
							this._lastTarget = this._wanderTarget;
							this._wanderTarget = usableMachine;
						}
					}
					else
					{
						this._waitTimer.Reset(100f);
					}
				}
			}
			if (base.OwnerAgent.CurrentlyUsedGameObject != null && base.Navigator.GetDistanceToTarget(this._lastTarget) > 1f)
			{
				base.Navigator.SetTarget(this._lastTarget, this._lastTarget == this._wanderTarget, Agent.AIScriptedFrameFlags.None);
			}
			base.Navigator.SetTarget(this._wanderTarget, false, Agent.AIScriptedFrameFlags.None);
			this._wasSimulation = isSimulation;
		}

		// Token: 0x06000752 RID: 1874 RVA: 0x0003261C File Offset: 0x0003081C
		private void SetTimerForTheAgent(bool isSimulation)
		{
			AnimationPoint animationPoint;
			float num = (((animationPoint = base.OwnerAgent.CurrentlyUsedGameObject as AnimationPoint) != null) ? animationPoint.GetRandomWaitInSeconds() : 10f);
			if (isSimulation && MBRandom.RandomFloat < 0.33f)
			{
				num /= 10f + MBRandom.RandomFloat * 10f;
			}
			this._waitTimer = new Timer(base.Mission.CurrentTime, (num < 0f) ? 2.1474836E+09f : num, true);
		}

		// Token: 0x06000753 RID: 1875 RVA: 0x00032698 File Offset: 0x00030898
		private bool IsChildrenOfSameParent(UsableMachine machine, UsableMachine otherMachine)
		{
			WeakGameEntity weakGameEntity = machine.GameEntity;
			while (weakGameEntity.Parent.IsValid)
			{
				weakGameEntity = weakGameEntity.Parent;
			}
			WeakGameEntity weakGameEntity2 = otherMachine.GameEntity;
			while (weakGameEntity2.Parent.IsValid)
			{
				weakGameEntity2 = weakGameEntity2.Parent;
			}
			return weakGameEntity == weakGameEntity2;
		}

		// Token: 0x06000754 RID: 1876 RVA: 0x000326F0 File Offset: 0x000308F0
		public override void ConversationTick()
		{
			if (this._waitTimer != null)
			{
				this._waitTimer.Reset(base.Mission.CurrentTime);
			}
		}

		// Token: 0x06000755 RID: 1877 RVA: 0x00032710 File Offset: 0x00030910
		public override float GetAvailability(bool isSimulation)
		{
			if (this.FindTarget() == null)
			{
				return 0f;
			}
			return 1f;
		}

		// Token: 0x06000756 RID: 1878 RVA: 0x00032725 File Offset: 0x00030925
		public override void SetCustomWanderTarget(UsableMachine customUsableMachine)
		{
			this._wanderTarget = customUsableMachine;
			if (this._waitTimer != null)
			{
				this._waitTimer = null;
			}
		}

		// Token: 0x06000757 RID: 1879 RVA: 0x00032740 File Offset: 0x00030940
		private UsableMachine FindRandomWalkingTarget(bool forWaiting)
		{
			if (forWaiting && (this._wanderTarget ?? base.Navigator.TargetUsableMachine) != null)
			{
				return null;
			}
			string text = base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag;
			if (text == null)
			{
				text = "npc_common";
			}
			else if (!this._missionAgentHandler.HasUsablePointWithTag(text))
			{
				text = "npc_common_limited";
			}
			return this._missionAgentHandler.FindUnusedPointWithTagForAgent(base.OwnerAgent, text);
		}

		// Token: 0x06000758 RID: 1880 RVA: 0x000327B0 File Offset: 0x000309B0
		private UsableMachine FindTarget()
		{
			return this.FindRandomWalkingTarget(this._isIndoor && !this._indoorWanderingIsActive);
		}

		// Token: 0x06000759 RID: 1881 RVA: 0x000327CC File Offset: 0x000309CC
		private float GetTargetScore(UsableMachine usableMachine)
		{
			if (base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag != null && !usableMachine.GameEntity.HasTag(base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag))
			{
				return 0f;
			}
			StandingPoint vacantStandingPointForAI = usableMachine.GetVacantStandingPointForAI(base.OwnerAgent);
			if (vacantStandingPointForAI == null || vacantStandingPointForAI.IsDisabledForAgent(base.OwnerAgent))
			{
				return 0f;
			}
			float num = 1f;
			Vec3 vec = vacantStandingPointForAI.GetUserFrameForAgent(base.OwnerAgent).Origin.GetGroundVec3() - base.OwnerAgent.Position;
			if (vec.Length < 2f)
			{
				num *= vec.Length / 2f;
			}
			return num * (0.8f + MBRandom.RandomFloat * 0.2f);
		}

		// Token: 0x0600075A RID: 1882 RVA: 0x000328A4 File Offset: 0x00030AA4
		public override void OnSpecialTargetChanged()
		{
			if (this._wanderTarget == null)
			{
				return;
			}
			if (!base.Navigator.SpecialTargetTag.IsEmpty<char>() && !this._wanderTarget.GameEntity.HasTag(base.Navigator.SpecialTargetTag))
			{
				this._wanderTarget = null;
				base.Navigator.SetTarget(this._wanderTarget, false, Agent.AIScriptedFrameFlags.None);
				return;
			}
			if (base.Navigator.SpecialTargetTag.IsEmpty<char>() && !this._wanderTarget.GameEntity.HasTag("npc_common"))
			{
				this._wanderTarget = null;
				base.Navigator.SetTarget(this._wanderTarget, false, Agent.AIScriptedFrameFlags.None);
			}
		}

		// Token: 0x0600075B RID: 1883 RVA: 0x00032950 File Offset: 0x00030B50
		public override string GetDebugInfo()
		{
			string text = "Walk ";
			if (this._waitTimer != null)
			{
				text = string.Concat(new object[]
				{
					text,
					"(Wait ",
					(int)this._waitTimer.ElapsedTime(),
					"/",
					this._waitTimer.Duration,
					")"
				});
			}
			else if (this._wanderTarget == null)
			{
				text += "(search for target!)";
			}
			return text;
		}

		// Token: 0x0600075C RID: 1884 RVA: 0x000329D1 File Offset: 0x00030BD1
		protected override void OnDeactivate()
		{
			base.Navigator.ClearTarget();
			this._wanderTarget = null;
			this._waitTimer = null;
		}

		// Token: 0x040003EE RID: 1006
		private readonly MissionAgentHandler _missionAgentHandler;

		// Token: 0x040003EF RID: 1007
		private readonly bool _isIndoor;

		// Token: 0x040003F0 RID: 1008
		private UsableMachine _wanderTarget;

		// Token: 0x040003F1 RID: 1009
		private UsableMachine _lastTarget;

		// Token: 0x040003F2 RID: 1010
		private Timer _waitTimer;

		// Token: 0x040003F3 RID: 1011
		private bool _indoorWanderingIsActive;

		// Token: 0x040003F4 RID: 1012
		private bool _outdoorWanderingIsActive;

		// Token: 0x040003F5 RID: 1013
		private bool _wasSimulation;
	}
}
