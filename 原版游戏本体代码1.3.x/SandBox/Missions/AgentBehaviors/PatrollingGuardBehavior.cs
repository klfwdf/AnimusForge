using System;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000AB RID: 171
	public class PatrollingGuardBehavior : AgentBehavior
	{
		// Token: 0x0600072F RID: 1839 RVA: 0x000318A3 File Offset: 0x0002FAA3
		public PatrollingGuardBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			this._missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		}

		// Token: 0x06000730 RID: 1840 RVA: 0x000318C0 File Offset: 0x0002FAC0
		public override void Tick(float dt, bool isSimulation)
		{
			if (this._target == null)
			{
				UsableMachine usableMachine = ((base.Navigator.SpecialTargetTag == null || base.Navigator.SpecialTargetTag.IsEmpty<char>()) ? this._missionAgentHandler.FindUnusedPointWithTagForAgent(base.OwnerAgent, "npc_common") : this._missionAgentHandler.FindUnusedPointWithTagForAgent(base.OwnerAgent, base.Navigator.SpecialTargetTag));
				if (usableMachine != null)
				{
					this._target = usableMachine;
					base.Navigator.SetTarget(this._target, false, Agent.AIScriptedFrameFlags.None);
					return;
				}
			}
			else if (base.Navigator.TargetUsableMachine == null)
			{
				base.Navigator.SetTarget(this._target, false, Agent.AIScriptedFrameFlags.None);
			}
		}

		// Token: 0x06000731 RID: 1841 RVA: 0x00031967 File Offset: 0x0002FB67
		public override float GetAvailability(bool isSimulation)
		{
			if (this._missionAgentHandler.GetAllUsablePointsWithTag(base.Navigator.SpecialTargetTag).Count <= 0)
			{
				return 0f;
			}
			return 1f;
		}

		// Token: 0x06000732 RID: 1842 RVA: 0x00031992 File Offset: 0x0002FB92
		protected override void OnDeactivate()
		{
			this._target = null;
			base.Navigator.ClearTarget();
		}

		// Token: 0x06000733 RID: 1843 RVA: 0x000319A6 File Offset: 0x0002FBA6
		public override string GetDebugInfo()
		{
			return "Guard patrol";
		}

		// Token: 0x040003D8 RID: 984
		private readonly MissionAgentHandler _missionAgentHandler;

		// Token: 0x040003D9 RID: 985
		private UsableMachine _target;
	}
}
