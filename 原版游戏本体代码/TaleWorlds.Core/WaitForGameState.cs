using System;
using TaleWorlds.Network;

namespace TaleWorlds.Core
{
	// Token: 0x020000DE RID: 222
	public class WaitForGameState : CoroutineState
	{
		// Token: 0x06000B66 RID: 2918 RVA: 0x00024C91 File Offset: 0x00022E91
		public WaitForGameState(Type stateType)
		{
			this._stateType = stateType;
		}

		// Token: 0x170003E2 RID: 994
		// (get) Token: 0x06000B67 RID: 2919 RVA: 0x00024CA0 File Offset: 0x00022EA0
		protected override bool IsFinished
		{
			get
			{
				GameState gameState = ((GameStateManager.Current != null) ? GameStateManager.Current.ActiveState : null);
				return gameState != null && this._stateType.IsInstanceOfType(gameState);
			}
		}

		// Token: 0x04000692 RID: 1682
		private Type _stateType;
	}
}
