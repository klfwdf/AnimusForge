using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;
using SandBox.BoardGames.Tiles;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI;

public class BoardGameAIBaghChal : BoardGameAIBase
{
	private readonly BoardGameBaghChal _board;

	public BoardGameAIBaghChal(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
		: base(difficulty, boardGameHandler)
	{
		_board = base.BoardGameHandler.Board as BoardGameBaghChal;
	}

	protected override void InitializeDifficulty()
	{
		switch (base.Difficulty)
		{
		case BoardGameHelper.AIDifficulty.Easy:
			MaxDepth = 3;
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			MaxDepth = 4;
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			MaxDepth = 5;
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
			BoardGameBaghChal.BoardInformation board = _board.TakeBoardSnapshot();
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
		return CalculateMovementStageMove();
	}

	private int NegaMax(int depth, int color, int alpha, int beta)
	{
		if (depth == 0)
		{
			return color * Evaluation() * ((_board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? 1 : (-1));
		}
		BoardGameBaghChal.BoardInformation board = _board.TakeBoardSnapshot();
		if (color == ((_board.PlayerWhoStarted != PlayerTurn.PlayerOne) ? 1 : (-1)) && _board.GetANonePlacedGoat() != null)
		{
			for (int i = 0; i < _board.TileCount; i++)
			{
				TileBase tileBase = _board.Tiles[i];
				if (tileBase.PawnOnTile == null)
				{
					Move move = new Move(_board.GetANonePlacedGoat(), tileBase);
					_board.AIMakeMove(move);
					int num = -NegaMax(depth - 1, -color, -beta, -alpha);
					_board.UndoMove(ref board);
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
			List<List<Move>> moves = _board.CalculateAllValidMoves((color != 1) ? BoardGameSide.Player : BoardGameSide.AI);
			if (!_board.HasMovesAvailable(ref moves))
			{
				return color * Evaluation() * ((_board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? 1 : (-1));
			}
			foreach (List<Move> item in moves)
			{
				foreach (Move item2 in item)
				{
					_board.AIMakeMove(item2);
					int num2 = -NegaMax(depth - 1, -color, -beta, -alpha);
					_board.UndoMove(ref board);
					if (num2 >= beta)
					{
						return num2;
					}
					alpha = MathF.Max(num2, alpha);
				}
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
		List<List<Move>> moves = _board.CalculateAllValidMoves((_board.PlayerWhoStarted != PlayerTurn.PlayerOne) ? BoardGameSide.Player : BoardGameSide.AI);
		int totalMovesAvailable = _board.GetTotalMovesAvailable(ref moves);
		return (int)((float)(100 * -GetTigersStuck() + 50 * GetGoatsCaptured() + totalMovesAvailable + GetCombinedDistanceBetweenTigers()) * num);
	}

	private int GetTigersStuck()
	{
		int num = 0;
		foreach (PawnBaghChal item in (_board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? _board.PlayerTwoUnits : _board.PlayerOneUnits)
		{
			if (_board.CalculateValidMoves(item).Count == 0)
			{
				num++;
			}
		}
		return num;
	}

	private int GetGoatsCaptured()
	{
		int num = 0;
		foreach (PawnBaghChal item in (_board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? _board.PlayerOneUnits : _board.PlayerTwoUnits)
		{
			if (item.Captured)
			{
				num++;
			}
		}
		return num;
	}

	private int GetCombinedDistanceBetweenTigers()
	{
		int num = 0;
		foreach (PawnBaghChal item in (_board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? _board.PlayerTwoUnits : _board.PlayerOneUnits)
		{
			foreach (PawnBaghChal item2 in (_board.PlayerWhoStarted == PlayerTurn.PlayerOne) ? _board.PlayerTwoUnits : _board.PlayerOneUnits)
			{
				if (item != item2)
				{
					num += MathF.Abs(item.X - item2.X) + MathF.Abs(item.Y + item2.Y);
				}
			}
		}
		return num;
	}
}
