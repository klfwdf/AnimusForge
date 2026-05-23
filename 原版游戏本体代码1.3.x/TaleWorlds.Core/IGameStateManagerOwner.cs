using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000088 RID: 136
	public interface IGameStateManagerOwner
	{
		// Token: 0x0600089E RID: 2206
		void OnStateStackEmpty();

		// Token: 0x0600089F RID: 2207
		void OnStateChanged(GameState oldState);
	}
}
