using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x02000070 RID: 112
	public abstract class GameModelsManager
	{
		// Token: 0x060007E3 RID: 2019 RVA: 0x0001A2AD File Offset: 0x000184AD
		protected GameModelsManager(IEnumerable<GameModel> inputComponents)
		{
			this._gameModels = inputComponents.ToMBList<GameModel>();
		}

		// Token: 0x060007E4 RID: 2020 RVA: 0x0001A2C4 File Offset: 0x000184C4
		protected T GetGameModel<T>() where T : GameModel
		{
			for (int i = this._gameModels.Count - 1; i >= 0; i--)
			{
				T result;
				if ((result = this._gameModels[i] as T) != null)
				{
					return result;
				}
			}
			return default(T);
		}

		// Token: 0x060007E5 RID: 2021 RVA: 0x0001A313 File Offset: 0x00018513
		public MBReadOnlyList<GameModel> GetGameModels()
		{
			return this._gameModels;
		}

		// Token: 0x0400040C RID: 1036
		private readonly MBList<GameModel> _gameModels;
	}
}
