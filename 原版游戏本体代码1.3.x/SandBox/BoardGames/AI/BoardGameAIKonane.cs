using System;
using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI
{
	// Token: 0x02000100 RID: 256
	public class BoardGameAIKonane : BoardGameAIBase
	{
		// Token: 0x06000CF6 RID: 3318 RVA: 0x0005E840 File Offset: 0x0005CA40
		public BoardGameAIKonane(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
			: base(difficulty, boardGameHandler)
		{
			this._board = base.BoardGameHandler.Board as BoardGameKonane;
		}

		// Token: 0x06000CF7 RID: 3319 RVA: 0x0005E860 File Offset: 0x0005CA60
		protected override void InitializeDifficulty()
		{
			switch (base.Difficulty)
			{
			case BoardGameHelper.AIDifficulty.Easy:
				this.MaxDepth = 2;
				return;
			case BoardGameHelper.AIDifficulty.Normal:
				this.MaxDepth = 5;
				return;
			case BoardGameHelper.AIDifficulty.Hard:
				this.MaxDepth = 8;
				return;
			default:
				return;
			}
		}

		// Token: 0x06000CF8 RID: 3320 RVA: 0x0005E8A0 File Offset: 0x0005CAA0
		public override Move CalculateMovementStageMove()
		{
			Move result;
			result.GoalTile = null;
			result.Unit = null;
			if (this._board.IsReady)
			{
				List<List<Move>> list = this._board.CalculateAllValidMoves(BoardGameSide.AI);
				BoardGameKonane.BoardInformation boardInformation = this._board.TakeBoardSnapshot();
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

		// Token: 0x06000CF9 RID: 3321 RVA: 0x0005E9DC File Offset: 0x0005CBDC
		public override Move CalculatePreMovementStageMove()
		{
			Move invalid = Move.Invalid;
			int maxValue = this._board.CheckForRemovablePawns(false);
			int index = MBRandom.RandomInt(0, maxValue);
			invalid.Unit = this._board.RemovablePawns[index];
			return invalid;
		}

		// Token: 0x06000CFA RID: 3322 RVA: 0x0005EA20 File Offset: 0x0005CC20
		private int NegaMax(int depth, int color, int alpha, int beta)
		{
			if (depth == 0)
			{
				return color * this.Evaluation();
			}
			List<List<Move>> list = this._board.CalculateAllValidMoves((color == 1) ? BoardGameSide.AI : BoardGameSide.Player);
			if (!this._board.HasMovesAvailable(ref list))
			{
				return color * this.Evaluation();
			}
			BoardGameKonane.BoardInformation boardInformation = this._board.TakeBoardSnapshot();
			foreach (List<Move> list2 in list)
			{
				foreach (Move move in list2)
				{
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
			return alpha;
		}

		// Token: 0x06000CFB RID: 3323 RVA: 0x0005EB28 File Offset: 0x0005CD28
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
			List<List<Move>> list = this._board.CalculateAllValidMoves(BoardGameSide.Player);
			List<List<Move>> list2 = this._board.CalculateAllValidMoves(BoardGameSide.AI);
			int totalMovesAvailable = this._board.GetTotalMovesAvailable(ref list);
			int totalMovesAvailable2 = this._board.GetTotalMovesAvailable(ref list2);
			int num2 = MathF.Min(totalMovesAvailable, 1);
			int num3 = MathF.Min(totalMovesAvailable2, 1);
			return (int)((float)(100 * (num3 - num2) + 20 * (this._board.GetPlayerTwoUnitsAlive() - this._board.GetPlayerOneUnitsAlive()) + 5 * (totalMovesAvailable2 - totalMovesAvailable)) * num);
		}

		// Token: 0x04000599 RID: 1433
		private readonly BoardGameKonane _board;
	}
}
