using System;
using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;

namespace SandBox.BoardGames.AI
{
	// Token: 0x02000102 RID: 258
	public class BoardGameAIPuluc : BoardGameAIBase
	{
		// Token: 0x06000D02 RID: 3330 RVA: 0x0005EF3E File Offset: 0x0005D13E
		public BoardGameAIPuluc(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
			: base(difficulty, boardGameHandler)
		{
			this._board = base.BoardGameHandler.Board as BoardGamePuluc;
		}

		// Token: 0x06000D03 RID: 3331 RVA: 0x0005EF78 File Offset: 0x0005D178
		protected override void InitializeDifficulty()
		{
			switch (base.Difficulty)
			{
			case BoardGameHelper.AIDifficulty.Easy:
				this.MaxDepth = 3;
				return;
			case BoardGameHelper.AIDifficulty.Normal:
				this.MaxDepth = 5;
				return;
			case BoardGameHelper.AIDifficulty.Hard:
				this.MaxDepth = 7;
				return;
			default:
				return;
			}
		}

		// Token: 0x06000D04 RID: 3332 RVA: 0x0005EFB8 File Offset: 0x0005D1B8
		public override Move CalculateMovementStageMove()
		{
			Move result;
			result.GoalTile = null;
			result.Unit = null;
			if (this._board.IsReady)
			{
				this.ExpectiMax(this.MaxDepth, BoardGameSide.AI, false, ref result);
			}
			if (!base.AbortRequested)
			{
				bool isValid = result.IsValid;
			}
			return result;
		}

		// Token: 0x06000D05 RID: 3333 RVA: 0x0005F004 File Offset: 0x0005D204
		private float ExpectiMax(int depth, BoardGameSide side, bool chanceNode, ref Move bestMove)
		{
			float num;
			if (depth == 0)
			{
				num = (float)this.Evaluation();
				if (side == BoardGameSide.Player)
				{
					num = -num;
				}
			}
			else if (chanceNode)
			{
				num = 0f;
				for (int i = 0; i < 5; i++)
				{
					int lastDice = this._board.LastDice;
					this._board.ForceDice((i == 0) ? 5 : i);
					num += this._diceProbabilities[i] * this.ExpectiMax(depth - 1, side, false, ref bestMove);
					this._board.ForceDice(lastDice);
				}
			}
			else
			{
				BoardGamePuluc.BoardInformation boardInformation = this._board.TakeBoardSnapshot();
				List<List<Move>> list = this._board.CalculateAllValidMoves(side);
				if (this._board.HasMovesAvailable(ref list))
				{
					num = float.MinValue;
					using (List<List<Move>>.Enumerator enumerator = list.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							List<Move> list2 = enumerator.Current;
							if (list2 != null)
							{
								foreach (Move move in list2)
								{
									this._board.AIMakeMove(move);
									BoardGameSide side2 = ((side == BoardGameSide.AI) ? BoardGameSide.Player : BoardGameSide.AI);
									float num2 = -this.ExpectiMax(depth - 1, side2, true, ref bestMove);
									this._board.UndoMove(ref boardInformation);
									if (num < num2)
									{
										num = num2;
										if (depth == this.MaxDepth)
										{
											bestMove = move;
										}
									}
								}
							}
						}
						return num;
					}
				}
				num = (float)this.Evaluation();
				if (side == BoardGameSide.Player)
				{
					num = -num;
				}
			}
			return num;
		}

		// Token: 0x06000D06 RID: 3334 RVA: 0x0005F19C File Offset: 0x0005D39C
		private int Evaluation()
		{
			return 20 * (this._board.GetPlayerTwoUnitsAlive() - this._board.GetPlayerOneUnitsAlive()) + 5 * (this.GetUnitsBeingCaptured(true) - this.GetUnitsBeingCaptured(false)) + (this.GetUnitsInPlay(false) - this.GetUnitsInPlay(true));
		}

		// Token: 0x06000D07 RID: 3335 RVA: 0x0005F1DC File Offset: 0x0005D3DC
		private int GetUnitsInSpawn(bool playerOne)
		{
			int num = 0;
			using (List<PawnBase>.Enumerator enumerator = (playerOne ? this._board.PlayerOneUnits : this._board.PlayerTwoUnits).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (((PawnPuluc)enumerator.Current).IsInSpawn)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000D08 RID: 3336 RVA: 0x0005F250 File Offset: 0x0005D450
		private int GetUnitsBeingCaptured(bool playerOne)
		{
			int num = 0;
			using (List<PawnBase>.Enumerator enumerator = (playerOne ? this._board.PlayerOneUnits : this._board.PlayerTwoUnits).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!((PawnPuluc)enumerator.Current).IsTopPawn)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000D09 RID: 3337 RVA: 0x0005F2C4 File Offset: 0x0005D4C4
		private int GetUnitsInPlay(bool playerOne)
		{
			int num = 0;
			foreach (PawnBase pawnBase in (playerOne ? this._board.PlayerOneUnits : this._board.PlayerTwoUnits))
			{
				PawnPuluc pawnPuluc = (PawnPuluc)pawnBase;
				if (pawnPuluc.InPlay && pawnPuluc.IsTopPawn)
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x0400059B RID: 1435
		private readonly BoardGamePuluc _board;

		// Token: 0x0400059C RID: 1436
		private readonly float[] _diceProbabilities = new float[] { 0.0625f, 0.25f, 0.375f, 0.25f, 0.0625f };
	}
}
