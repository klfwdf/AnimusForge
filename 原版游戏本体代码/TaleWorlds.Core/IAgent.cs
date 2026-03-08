using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200007B RID: 123
	public interface IAgent
	{
		// Token: 0x170002CE RID: 718
		// (get) Token: 0x0600084F RID: 2127
		BasicCharacterObject Character { get; }

		// Token: 0x06000850 RID: 2128
		bool IsEnemyOf(IAgent agent);

		// Token: 0x06000851 RID: 2129
		bool IsFriendOf(IAgent agent);

		// Token: 0x170002CF RID: 719
		// (get) Token: 0x06000852 RID: 2130
		AgentState State { get; }

		// Token: 0x170002D0 RID: 720
		// (get) Token: 0x06000853 RID: 2131
		IMissionTeam Team { get; }

		// Token: 0x170002D1 RID: 721
		// (get) Token: 0x06000854 RID: 2132
		IAgentOriginBase Origin { get; }

		// Token: 0x170002D2 RID: 722
		// (get) Token: 0x06000855 RID: 2133
		float Age { get; }

		// Token: 0x06000856 RID: 2134
		bool IsActive();

		// Token: 0x06000857 RID: 2135
		void SetAsConversationAgent(bool set);
	}
}
