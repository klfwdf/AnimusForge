using System;
using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;
using SandBox.BoardGames.Tiles;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI
{
	// Token: 0x020000FE RID: 254
	public class BoardGameAIBaghChal : BoardGameAIBase
	{
		// Token: 0x06000CD3 RID: 3283 RVA: 0x0005DEAC File Offset: 0x0005C0AC
		public BoardGameAIBaghChal(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
			: base(difficulty, boardGameHandler)
		{
			this._board = base.BoardGameHandler.Board as BoardGameBaghChal;
		}

		// Token: 0x06000CD4 RID: 3284 RVA: 0x0005DECC File Offset: 0x0005C0CC
		protected override void InitializeDifficulty()
		{
			switch (base.Difficulty)
			{
			case BoardGameHelper.AIDifficulty.Easy:
				this.MaxDepth = 3;
				return;
			case BoardGameHelper.AIDifficulty.Normal:
				this.MaxDepth = 4;
				return;
			case BoardGameHelper.AIDifficulty.Hard:
				this.MaxDepth = 5;
				return;
			default:
				return;
			}
		}

		// Token: 0x06000CD5 RID: 3285 RVA: 0x0005DF0C File Offset: 0x0005C10C
		public override Move CalculateMovementStageMove()
		{
			Move result;
			result.GoalTile = null;
			result.Unit = null;
			if (this._board.IsReady)
			{
				List<List<Move>> list = this._board.CalculateAllValidMoves(BoardGameSide.AI);
				BoardGameBaghChal.BoardInformation boardInformation = this._board.TakeBoardSnapshot();
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

		// Token: 0x06000CD6 RID: 3286 RVA: 0x0005E048 File Offset: 0x0005C248
		public override Move CalculatePreMovementStageMove()
		{
			return this.CalculateMovementStageMove();
		}

		// Token: 0x06000CD7 RID: 3287 RVA: 0x0005E050 File Offset: 0x0005C250
		private int NegaMax(int depth, int color, int alpha, int beta)
		{
			if (depth == 0)
			{
				return color * this.Evaluation() * ((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? 1 : (-1));
			}
			BoardGameBaghChal.BoardInformation boardInformation = this._board.TakeBoardSnapshot();
			if (color == ((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? (-1) : 1) && this._board.GetANonePlacedGoat() != null)
			{
				for (int i = 0; i < this._board.TileCount; i++)
				{
					TileBase tileBase = this._board.Tiles[i];
					if (tileBase.PawnOnTile == null)
					{
						Move move = new Move(this._board.GetANonePlacedGoat(), tileBase);
						this._board.AIMakeMove(move);
						int num = -this.NegaMax(depth - 1, -color, -beta, -alpha);
						this._board.UndoMove(ref boardInformation);
						if (num >= beta)
						{
							return num;
						}
						alpha = MathF.Max(num, alpha);
					}
				}
			}
			else
			{
				List<List<Move>> list = this._board.CalculateAllValidMoves((color == 1) ? BoardGameSide.AI : BoardGameSide.Player);
				if (!this._board.HasMovesAvailable(ref list))
				{
					return color * this.Evaluation() * ((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? 1 : (-1));
				}
				foreach (List<Move> list2 in list)
				{
					foreach (Move move2 in list2)
					{
						this._board.AIMakeMove(move2);
						int num2 = -this.NegaMax(depth - 1, -color, -beta, -alpha);
						this._board.UndoMove(ref boardInformation);
						if (num2 >= beta)
						{
							return num2;
						}
						alpha = MathF.Max(num2, alpha);
					}
				}
				return alpha;
			}
			return alpha;
		}

		// Token: 0x06000CD8 RID: 3288 RVA: 0x0005E22C File Offset: 0x0005C42C
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
			List<List<Move>> list = this._board.CalculateAllValidMoves((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? BoardGameSide.AI : BoardGameSide.Player);
			int totalMovesAvailable = this._board.GetTotalMovesAvailable(ref list);
			return (int)((float)(100 * -(float)this.GetTigersStuck() + 50 * this.GetGoatsCaptured() + totalMovesAvailable + this.GetCombinedDistanceBetweenTigers()) * num);
		}

		// Token: 0x06000CD9 RID: 3289 RVA: 0x0005E2D4 File Offset: 0x0005C4D4
		private int GetTigersStuck()
		{
			int num = 0;
			foreach (PawnBase pawnBase in ((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? this._board.PlayerTwoUnits : this._board.PlayerOneUnits))
			{
				PawnBaghChal pawn = (PawnBaghChal)pawnBase;
				if (this._board.CalculateValidMoves(pawn).Count == 0)
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000CDA RID: 3290 RVA: 0x0005E360 File Offset: 0x0005C560
		private int GetGoatsCaptured()
		{
			int num = 0;
			using (List<PawnBase>.Enumerator enumerator = ((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? this._board.PlayerOneUnits : this._board.PlayerTwoUnits).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (((PawnBaghChal)enumerator.Current).Captured)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000CDB RID: 3291 RVA: 0x0005E3E0 File Offset: 0x0005C5E0
		private int GetCombinedDistanceBetweenTigers()
		{
			int num = 0;
			foreach (PawnBase pawnBase in ((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? this._board.PlayerTwoUnits : this._board.PlayerOneUnits))
			{
				PawnBaghChal pawnBaghChal = (PawnBaghChal)pawnBase;
				foreach (PawnBase pawnBase2 in ((this._board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? this._board.PlayerTwoUnits : this._board.PlayerOneUnits))
				{
					PawnBaghChal pawnBaghChal2 = (PawnBaghChal)pawnBase2;
					if (pawnBaghChal != pawnBaghChal2)
					{
						num += MathF.Abs(pawnBaghChal.X - pawnBaghChal2.X) + MathF.Abs(pawnBaghChal.Y + pawnBaghChal2.Y);
					}
				}
			}
			return num;
		}

		// Token: 0x0400058E RID: 1422
		private readonly BoardGameBaghChal _board;
	}
}
