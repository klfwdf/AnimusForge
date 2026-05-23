using System;
using TaleWorlds.MountAndBlade;

namespace SandBox.AI
{
	// Token: 0x02000108 RID: 264
	public class UsablePlaceAI : UsableMachineAIBase
	{
		// Token: 0x06000D37 RID: 3383 RVA: 0x000605F0 File Offset: 0x0005E7F0
		public UsablePlaceAI(UsableMachine usableMachine)
			: base(usableMachine)
		{
		}

		// Token: 0x06000D38 RID: 3384 RVA: 0x000605FC File Offset: 0x0005E7FC
		protected override Agent.AIScriptedFrameFlags GetScriptedFrameFlags(Agent agent)
		{
			if (!this.UsableMachine.GameEntity.HasTag("quest_wanderer_target"))
			{
				return Agent.AIScriptedFrameFlags.DoNotRun;
			}
			return Agent.AIScriptedFrameFlags.None;
		}
	}
}
