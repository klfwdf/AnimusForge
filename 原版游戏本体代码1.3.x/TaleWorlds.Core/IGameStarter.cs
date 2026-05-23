using System;
using System.Collections.Generic;

namespace TaleWorlds.Core
{
	// Token: 0x0200006D RID: 109
	public interface IGameStarter
	{
		// Token: 0x060007D2 RID: 2002
		void AddModel(GameModel gameModel);

		// Token: 0x060007D3 RID: 2003
		void AddModel<T>(MBGameModel<T> gameModel) where T : GameModel;

		// Token: 0x170002BB RID: 699
		// (get) Token: 0x060007D4 RID: 2004
		IEnumerable<GameModel> Models { get; }
	}
}
