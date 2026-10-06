using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI;

public class BoardGameAIKonane : BoardGameAIBase
{
	private readonly BoardGameKonane _board;

	public BoardGameAIKonane(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
		: base(difficulty, boardGameHandler)
	{
		_board = base.BoardGameHandler.Board as BoardGameKonane;
	}

	protected override void InitializeDifficulty()
	{
		switch (base.Difficulty)
		{
		case BoardGameHelper.AIDifficulty.Easy:
			MaxDepth = 2;
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			MaxDepth = 5;
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			MaxDepth = 8;
			break;
		}
	}

	public override Move CalculateMovementStageMove()
	{
		Move result = default(Move);
		result.GoalTile = null;
		result.Unit = null;
		if (_board.IsReady)
		{
			List<List<Move>> moves = _board.CalculateAllValidMoves(BoardGameSide.AI);
			BoardGameKonane.BoardInformation board = _board.TakeBoardSnapshot();
			if (_board.HasMovesAvailable(ref moves))
			{
				int num = int.MinValue;
				foreach (List<Move> item in moves)
				{
					if (base.AbortRequested)
					{
						break;
					}
					foreach (Move item2 in item)
					{
						if (!base.AbortRequested)
						{
							_board.AIMakeMove(item2);
							int num2 = -NegaMax(MaxDepth, -1, -2147483647, int.MaxValue);
							_board.UndoMove(ref board);
							if (num2 > num)
							{
								result = item2;
								num = num2;
							}
							continue;
						}
						break;
					}
				}
			}
		}
		if (!base.AbortRequested)
		{
			_ = result.IsValid;
		}
		return result;
	}

	public override Move CalculatePreMovementStageMove()
	{
		Move invalid = Move.Invalid;
		int maxValue = _board.CheckForRemovablePawns(playerOne: false);
		int index = MBRandom.RandomInt(0, maxValue);
		invalid.Unit = _board.RemovablePawns[index];
		return invalid;
	}

	private int NegaMax(int depth, int color, int alpha, int beta)
	{
		if (depth == 0)
		{
			return color * Evaluation();
		}
		List<List<Move>> moves = _board.CalculateAllValidMoves((color != 1) ? BoardGameSide.Player : BoardGameSide.AI);
		if (!_board.HasMovesAvailable(ref moves))
		{
			return color * Evaluation();
		}
		BoardGameKonane.BoardInformation board = _board.TakeBoardSnapshot();
		foreach (List<Move> item in moves)
		{
			foreach (Move item2 in item)
			{
				_board.AIMakeMove(item2);
				int num = -NegaMax(depth - 1, -color, -beta, -alpha);
				_board.UndoMove(ref board);
				if (num >= beta)
				{
					return num;
				}
				alpha = MathF.Max(num, alpha);
			}
		}
		return alpha;
	}

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
		List<List<Move>> moves = _board.CalculateAllValidMoves(BoardGameSide.Player);
		List<List<Move>> moves2 = _board.CalculateAllValidMoves(BoardGameSide.AI);
		int totalMovesAvailable = _board.GetTotalMovesAvailable(ref moves);
		int totalMovesAvailable2 = _board.GetTotalMovesAvailable(ref moves2);
		int num2 = MathF.Min(totalMovesAvailable, 1);
		int num3 = MathF.Min(totalMovesAvailable2, 1);
		return (int)((float)(100 * (num3 - num2) + 20 * (_board.GetPlayerTwoUnitsAlive() - _board.GetPlayerOneUnitsAlive()) + 5 * (totalMovesAvailable2 - totalMovesAvailable)) * num);
	}
}
