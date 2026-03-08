using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000087 RID: 135
	public interface IGameStateManagerListener
	{
		// Token: 0x06000899 RID: 2201
		void OnCreateState(GameState gameState);

		// Token: 0x0600089A RID: 2202
		void OnPushState(GameState gameState, bool isTopGameState);

		// Token: 0x0600089B RID: 2203
		void OnPopState(GameState gameState);

		// Token: 0x0600089C RID: 2204
		void OnCleanStates();

		// Token: 0x0600089D RID: 2205
		void OnSavedGameLoadFinished();
	}
}
