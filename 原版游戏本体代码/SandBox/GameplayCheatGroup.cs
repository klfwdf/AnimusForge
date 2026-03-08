using System;
using System.Collections.Generic;

namespace SandBox
{
	// Token: 0x0200000A RID: 10
	public abstract class GameplayCheatGroup : GameplayCheatBase
	{
		// Token: 0x06000020 RID: 32
		public abstract IEnumerable<GameplayCheatBase> GetCheats();
	}
}
