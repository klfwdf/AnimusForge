using System;
using TaleWorlds.Core;
using TaleWorlds.Library.EventSystem;

namespace SandBox.ViewModelCollection.MapSiege
{
	// Token: 0x02000055 RID: 85
	public class PlayerStartEngineConstructionEvent : EventBase
	{
		// Token: 0x17000193 RID: 403
		// (get) Token: 0x0600054C RID: 1356 RVA: 0x00013FA9 File Offset: 0x000121A9
		// (set) Token: 0x0600054D RID: 1357 RVA: 0x00013FB1 File Offset: 0x000121B1
		public SiegeEngineType Engine { get; private set; }

		// Token: 0x0600054E RID: 1358 RVA: 0x00013FBA File Offset: 0x000121BA
		public PlayerStartEngineConstructionEvent(SiegeEngineType engine)
		{
			this.Engine = engine;
		}
	}
}
