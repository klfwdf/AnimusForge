using System;
using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.BoardGames.AI
{
	// Token: 0x02000103 RID: 259
	public class BoardGameAISeega : BoardGameAIBase
	{
		// Token: 0x06000D0A RID: 3338 RVA: 0x0005F344 File Offset: 0x0005D544
		public BoardGameAISeega(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
			: base(difficulty, boardGameHandler)
		{
			this._board = base.BoardGameHandler.Board as BoardGameSeega;
		}

		// Token: 0x06000D0B RID: 3339 RVA: 0x0005F37C File Offset: 0x0005D57C
		protected override void InitializeDifficulty()
		{
			switch (base.Difficulty)
			{
			case BoardGameHelper.AIDifficulty.Easy:
				this.MaxDepth = 2;
				return;
			case BoardGameHelper.AIDifficulty.Normal:
				this.MaxDepth = 3;
				return;
			case BoardGameHelper.AIDifficulty.Hard:
				this.MaxDepth = 4;
				return;
			default:
				return;
			}
		}

		// Token: 0x06000D0C RID: 3340 RVA: 0x0005F3BC File Offset: 0x0005D5BC
		public override Move CalculateMovementStageMove()
		{
			Move result = Move.Invalid;
			if (this._board.IsReady)
			{
				List<List<Move>> list = this._board.CalculateAllValidMoves(BoardGameSide.AI);
				if (!this._board.HasMovesAvailable(ref list))
				{
					IEnumerable<KeyValuePair<PawnBase, int>> blockingPawns = this._board.GetBlockingPawns(false);
					InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=1bzdDYoO}All AI pawns blocked. Removing one of the player's pawns to make a move", null).ToString()));
					PawnBase key = blockingPawns.MaxBy((KeyValuePair<PawnBase, int> x) => x.Value).Key;
					this._board.SetPawnCaptured(key, false);
					list = this._board.CalculateAllValidMoves(BoardGameSide.AI);
				}
				BoardGameSeega.BoardInformation boardInformation = this._board.TakeBoardSnapshot();
				if (this._board.HasMovesAvailable(ref list))
				{
					int num = int.MinValue;
					foreach (List<Move> list2 in list)
					{
						if (base.AbortRequested)
						{
							break;
						}
						foreach (Move move in list2)
						{
							if (base.AbortRequested)
							{
								break;
							}
							this._board.AIMakeMove(move);
							int num2 = -this.NegaMax(this.MaxDepth, -1, -2147483647, int.MaxValue);
							this._board.UndoMove(ref boardInformation);
							if (num2 > num)
							{
								result = move;
								num = num2;
							}
						}
					}
				}
			}
			if (!base.AbortRequested)
			{
				bool isValid = result.IsValid;
			}
			return result;
		}

		// Token: 0x06000D0D RID: 3341 RVA: 0x0005F570 File Offset: 0x0005D770
		public override bool WantsToForfeit()
		{
			if (!this.MayForfeit)
			{
				return false;
			}
			int playerOneUnitsAlive = this._board.GetPlayerOneUnitsAlive();
			int playerTwoUnitsAlive = this._board.GetPlayerTwoUnitsAlive();
			int num = ((base.Difficulty == BoardGameHelper.AIDifficulty.Hard) ? 2 : 1);
			if (playerTwoUnitsAlive <= 7 && playerOneUnitsAlive >= playerTwoUnitsAlive + (num + playerTwoUnitsAlive / 2))
			{
				this.MayForfeit = false;
				return true;
			}
			return false;
		}

		// Token: 0x06000D0E RID: 3342 RVA: 0x0005F5C8 File Offset: 0x0005D7C8
		public override Move CalculatePreMovementStageMove()
		{
			Move invalid = Move.Invalid;
			foreach (PawnBase pawnBase in this._board.PlayerTwoUnits)
			{
				PawnSeega pawnSeega = (PawnSeega)pawnBase;
				if (!pawnSeega.IsPlaced && !pawnSeega.Moving)
				{
					while (!invalid.IsValid)
					{
						if (base.AbortRequested)
						{
							break;
						}
						int x = MBRandom.RandomInt(0, 5);
						int y = MBRandom.RandomInt(0, 5);
						if (this._board.GetTile(x, y).PawnOnTile == null && !this._board.GetTile(x, y).Entity.HasTag("obstructed_at_start"))
						{
							invalid.Unit = pawnSeega;
							invalid.GoalTile = this._board.GetTile(x, y);
						}
					}
					break;
				}
			}
			return invalid;
		}

		// Token: 0x06000D0F RID: 3343 RVA: 0x0005F6B8 File Offset: 0x0005D8B8
		private int NegaMax(int depth, int color, int alpha, int beta)
		{
			int num = int.MinValue;
			if (depth == 0)
			{
				return color * this.Evaluation();
			}
			foreach (PawnBase pawnBase in ((color == 1) ? this._board.PlayerTwoUnits : this._board.PlayerOneUnits))
			{
				((PawnSeega)pawnBase).UpdateMoveBackAvailable();
			}
			List<List<Move>> list = this._board.CalculateAllValidMoves((color == 1) ? BoardGameSide.AI : BoardGameSide.Player);
			if (!this._board.HasMovesAvailable(ref list))
			{
				return color * this.Evaluation();
			}
			BoardGameSeega.BoardInformation boardInformation = this._board.TakeBoardSnapshot();
			foreach (List<Move> list2 in list)
			{
				if (list2 != null)
				{
					foreach (Move move in list2)
					{
						this._board.AIMakeMove(move);
						num = MathF.Max(-this.NegaMax(depth - 1, -color, -beta, -alpha), num);
						alpha = MathF.Max(alpha, num);
						this._board.UndoMove(ref boardInformation);
						if (alpha >= beta && color == 1)
						{
							return alpha;
						}
					}
				}
			}
			return num;
		}

		// Token: 0x06000D10 RID: 3344 RVA: 0x0005F838 File Offset: 0x0005DA38
		private int Evaluation()
		{
			float num = MBRandom.RandomFloat;
			switch (base.Difficulty)
			{
			case BoardGameHelper.AIDifficulty.Easy:
				num = num * 0.7f + 0.5f;
				break;
			case BoardGameHelper.AIDifficulty.Normal:
				num = num * 0.5f + 0.65f;
				break;
			case BoardGameHelper.AIDifficulty.Hard:
				num = num * 0.35f + 0.75f;
				break;
			}
			return (int)((float)(20 * (this._board.GetPlayerTwoUnitsAlive() - this._board.GetPlayerOneUnitsAlive()) + (this.GetPlacementScore(false) - this.GetPlacementScore(true)) + 2 * (this.GetSurroundedScore(false) - this.GetSurroundedScore(true))) * num);
		}

		// Token: 0x06000D11 RID: 3345 RVA: 0x0005F8D4 File Offset: 0x0005DAD4
		private int GetPlacementScore(bool player)
		{
			int num = 0;
			foreach (PawnBase pawnBase in (player ? this._board.PlayerOneUnits : this._board.PlayerTwoUnits))
			{
				PawnSeega pawnSeega = (PawnSeega)pawnBase;
				if (pawnSeega.IsPlaced)
				{
					num += this._boardValues[pawnSeega.X, pawnSeega.Y];
				}
			}
			return num;
		}

		// Token: 0x06000D12 RID: 3346 RVA: 0x0005F960 File Offset: 0x0005DB60
		private int GetSurroundedScore(bool player)
		{
			int num = 0;
			foreach (PawnBase pawnBase in (player ? this._board.PlayerOneUnits : this._board.PlayerTwoUnits))
			{
				PawnSeega pawnSeega = (PawnSeega)pawnBase;
				if (pawnSeega.IsPlaced)
				{
					num += this.GetAmountSurroundingThisPawn(pawnSeega);
				}
			}
			return num;
		}

		// Token: 0x06000D13 RID: 3347 RVA: 0x0005F9DC File Offset: 0x0005DBDC
		private int GetAmountSurroundingThisPawn(PawnSeega pawn)
		{
			int num = 0;
			int x = pawn.X;
			int y = pawn.Y;
			if (x > 0 && this._board.GetTile(x - 1, y).PawnOnTile != null)
			{
				num++;
			}
			if (y > 0 && this._board.GetTile(x, y - 1).PawnOnTile != null)
			{
				num++;
			}
			if (x < BoardGameSeega.BoardWidth - 1 && this._board.GetTile(x + 1, y).PawnOnTile != null)
			{
				num++;
			}
			if (y < BoardGameSeega.BoardHeight - 1 && this._board.GetTile(x, y + 1).PawnOnTile != null)
			{
				num++;
			}
			return num;
		}

		// Token: 0x0400059D RID: 1437
		private readonly BoardGameSeega _board;

		// Token: 0x0400059E RID: 1438
		private readonly int[,] _boardValues = new int[,]
		{
			{ 3, 2, 2, 2, 3 },
			{ 2, 1, 1, 1, 2 },
			{ 2, 1, 3, 1, 2 },
			{ 2, 1, 1, 1, 2 },
			{ 3, 2, 2, 2, 3 }
		};
	}
}
