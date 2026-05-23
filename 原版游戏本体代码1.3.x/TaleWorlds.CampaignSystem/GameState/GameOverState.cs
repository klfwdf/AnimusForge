using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000390 RID: 912
	public class GameOverState : GameState
	{
		// Token: 0x17000C6D RID: 3181
		// (get) Token: 0x0600346F RID: 13423 RVA: 0x000D6086 File Offset: 0x000D4286
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C6E RID: 3182
		// (get) Token: 0x06003470 RID: 13424 RVA: 0x000D6089 File Offset: 0x000D4289
		// (set) Token: 0x06003471 RID: 13425 RVA: 0x000D6091 File Offset: 0x000D4291
		public IGameOverStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		// Token: 0x17000C6F RID: 3183
		// (get) Token: 0x06003472 RID: 13426 RVA: 0x000D609A File Offset: 0x000D429A
		// (set) Token: 0x06003473 RID: 13427 RVA: 0x000D60A2 File Offset: 0x000D42A2
		public GameOverState.GameOverReason Reason { get; private set; }

		// Token: 0x06003474 RID: 13428 RVA: 0x000D60AB File Offset: 0x000D42AB
		public GameOverState()
		{
		}

		// Token: 0x06003475 RID: 13429 RVA: 0x000D60B3 File Offset: 0x000D42B3
		public GameOverState(GameOverState.GameOverReason reason)
		{
			this.Reason = reason;
		}

		// Token: 0x06003476 RID: 13430 RVA: 0x000D60C2 File Offset: 0x000D42C2
		public static GameOverState CreateForVictory()
		{
			Game game = Game.Current;
			if (game == null)
			{
				return null;
			}
			return game.GameStateManager.CreateState<GameOverState>(new object[] { GameOverState.GameOverReason.Victory });
		}

		// Token: 0x06003477 RID: 13431 RVA: 0x000D60E8 File Offset: 0x000D42E8
		public static GameOverState CreateForRetirement()
		{
			Game game = Game.Current;
			if (game == null)
			{
				return null;
			}
			return game.GameStateManager.CreateState<GameOverState>(new object[] { GameOverState.GameOverReason.Retirement });
		}

		// Token: 0x06003478 RID: 13432 RVA: 0x000D610E File Offset: 0x000D430E
		public static GameOverState CreateForClanDestroyed()
		{
			Game game = Game.Current;
			if (game == null)
			{
				return null;
			}
			return game.GameStateManager.CreateState<GameOverState>(new object[] { GameOverState.GameOverReason.ClanDestroyed });
		}

		// Token: 0x04000EF2 RID: 3826
		private IGameOverStateHandler _handler;

		// Token: 0x02000764 RID: 1892
		public enum GameOverReason
		{
			// Token: 0x04001E25 RID: 7717
			Retirement,
			// Token: 0x04001E26 RID: 7718
			ClanDestroyed,
			// Token: 0x04001E27 RID: 7719
			Victory
		}
	}
}
