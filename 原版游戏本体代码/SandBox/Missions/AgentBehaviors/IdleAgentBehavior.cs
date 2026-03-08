using System;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000A7 RID: 167
	public class IdleAgentBehavior : AgentBehavior
	{
		// Token: 0x06000712 RID: 1810 RVA: 0x00030C3D File Offset: 0x0002EE3D
		public IdleAgentBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
		}

		// Token: 0x06000713 RID: 1811 RVA: 0x00030C46 File Offset: 0x0002EE46
		public override float GetAvailability(bool isSimulation)
		{
			return 1f;
		}

		// Token: 0x06000714 RID: 1812 RVA: 0x00030C50 File Offset: 0x0002EE50
		protected override void OnActivate()
		{
			base.OwnerAgent.SetIsAIPaused(true);
			base.OwnerAgent.SetTargetPosition(base.OwnerAgent.GetWorldPosition().AsVec2);
		}

		// Token: 0x06000715 RID: 1813 RVA: 0x00030C87 File Offset: 0x0002EE87
		protected override void OnDeactivate()
		{
			base.OwnerAgent.SetIsAIPaused(false);
			base.OwnerAgent.ClearTargetFrame();
		}

		// Token: 0x06000716 RID: 1814 RVA: 0x00030CA0 File Offset: 0x0002EEA0
		public override string GetDebugInfo()
		{
			return "Idle Behavior";
		}
	}
}
