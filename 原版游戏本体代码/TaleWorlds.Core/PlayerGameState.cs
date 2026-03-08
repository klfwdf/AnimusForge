using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000C4 RID: 196
	public abstract class PlayerGameState : GameState
	{
		// Token: 0x170003B9 RID: 953
		// (get) Token: 0x06000ACA RID: 2762 RVA: 0x00022B4B File Offset: 0x00020D4B
		// (set) Token: 0x06000ACB RID: 2763 RVA: 0x00022B53 File Offset: 0x00020D53
		public VirtualPlayer Peer
		{
			get
			{
				return this._peer;
			}
			private set
			{
				this._peer = value;
			}
		}

		// Token: 0x04000606 RID: 1542
		private VirtualPlayer _peer;
	}
}
