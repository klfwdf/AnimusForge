using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Arena
{
	// Token: 0x02000095 RID: 149
	public class ArenaAgentStateDeciderLogic : MissionLogic, IAgentStateDecider, IMissionBehavior
	{
		// Token: 0x06000635 RID: 1589 RVA: 0x0002A835 File Offset: 0x00028A35
		public AgentState GetAgentState(Agent effectedAgent, float deathProbability, out bool usedSurgery)
		{
			usedSurgery = false;
			return AgentState.Unconscious;
		}
	}
}
