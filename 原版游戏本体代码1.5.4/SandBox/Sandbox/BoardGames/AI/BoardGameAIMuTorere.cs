using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI;

public class BoardGameAIMuTorere : BoardGameAIBase
{
	private readonly BoardGameMuTorere _board;

	public BoardGameAIMuTorere(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
		: base(difficulty, boardGameHandler)
	{
		_board = base.BoardGameHandler.Board as BoardGameMuTorere;
	}

	protected override void InitializeDifficulty()
	{
		switch (base.Difficulty)
		{
		case BoardGameHelper.AIDifficulty.Easy:
			MaxDepth = 3;
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			MaxDepth = 5;
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			MaxDepth = 7;
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
			BoardGameMuTorere.BoardInformation board = _board.TakePawnsSnapshot();
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
							int num2 = -NegaMax(MaxDepth, -1);
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

	private int NegaMax(int depth, int color)
	{
		int num = int.MinValue;
		if (depth == 0)
		{
			return color * Evaluation() * ((_board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? 1 : (-1));
		}
		BoardGameMuTorere.BoardInformation board = _board.TakePawnsSnapshot();
		List<List<Move>> moves = _board.CalculateAllValidMoves((color != 1) ? BoardGameSide.Player : BoardGameSide.AI);
		if (!_board.HasMovesAvailable(ref moves))
		{
			return color * Evaluation();
		}
		foreach (List<Move> item in moves)
		{
			foreach (Move item2 in item)
			{
				_board.AIMakeMove(item2);
				num = MathF.Max(num, -NegaMax(depth - 1, -color));
				_board.UndoMove(ref board);
			}
		}
		return num;
	}

	private int Evaluation()
	{
		float num = MBRandom.RandomFloat;
		switch (base.Difficulty)
		{
		case BoardGameHelper.AIDifficulty.Easy:
			num = num * 2f - 1f;
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			num = num * 1.7f - 0.7f;
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			num = num * 1.4f - 0.4f;
			break;
		}
		return (int)(num * 100f * (float)(CanMove(playerOne: false) - CanMove(playerOne: true)));
	}

	private int CanMove(bool playerOne)
	{
		List<List<Move>> moves = _board.CalculateAllValidMoves(playerOne ? BoardGameSide.Player : BoardGameSide.AI);
		if (!_board.HasMovesAvailable(ref moves))
		{
			return 0;
		}
		return 1;
	}
}
