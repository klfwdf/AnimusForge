using System;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000AD RID: 173
	public class StandGuardBehavior : AgentBehavior
	{
		// Token: 0x06000741 RID: 1857 RVA: 0x0003204B File Offset: 0x0003024B
		public StandGuardBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			this._missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		}

		// Token: 0x06000742 RID: 1858 RVA: 0x00032068 File Offset: 0x00030268
		public override void Tick(float dt, bool isSimulation)
		{
			if (base.OwnerAgent.CurrentWatchState == Agent.WatchState.Patrolling)
			{
				if (this._standPoint == null || isSimulation)
				{
					UsableMachine usableMachine = this._oldStandPoint ?? this._missionAgentHandler.FindUnusedPointWithTagForAgent(base.OwnerAgent, base.Navigator.SpecialTargetTag);
					if (usableMachine != null)
					{
						this._oldStandPoint = null;
						this._standPoint = usableMachine;
						base.Navigator.SetTarget(this._standPoint, false, Agent.AIScriptedFrameFlags.None);
						return;
					}
				}
			}
			else if (this._standPoint != null)
			{
				this._oldStandPoint = this._standPoint;
				base.Navigator.SetTarget(null, false, Agent.AIScriptedFrameFlags.None);
				this._standPoint = null;
			}
		}

		// Token: 0x06000743 RID: 1859 RVA: 0x00032104 File Offset: 0x00030304
		protected override void OnDeactivate()
		{
			base.Navigator.ClearTarget();
			this._standPoint = null;
		}

		// Token: 0x06000744 RID: 1860 RVA: 0x00032118 File Offset: 0x00030318
		public override float GetAvailability(bool isSimulation)
		{
			return 1f;
		}

		// Token: 0x06000745 RID: 1861 RVA: 0x0003211F File Offset: 0x0003031F
		public override string GetDebugInfo()
		{
			return "Guard stand";
		}

		// Token: 0x040003E9 RID: 1001
		private UsableMachine _oldStandPoint;

		// Token: 0x040003EA RID: 1002
		private UsableMachine _standPoint;

		// Token: 0x040003EB RID: 1003
		private readonly MissionAgentHandler _missionAgentHandler;

		// Token: 0x020001B5 RID: 437
		private enum GuardState
		{
			// Token: 0x040007EF RID: 2031
			StandIdle,
			// Token: 0x040007F0 RID: 2032
			StandAttention,
			// Token: 0x040007F1 RID: 2033
			StandCautious,
			// Token: 0x040007F2 RID: 2034
			GotToStandPoint
		}
	}
}
