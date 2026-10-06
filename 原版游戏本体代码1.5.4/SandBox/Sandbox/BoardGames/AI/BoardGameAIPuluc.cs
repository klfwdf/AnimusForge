using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;

namespace SandBox.BoardGames.AI;

public class BoardGameAIPuluc : BoardGameAIBase
{
	private readonly BoardGamePuluc _board;

	private readonly float[] _diceProbabilities = new float[5] { 0.0625f, 0.25f, 0.375f, 0.25f, 0.0625f };

	public BoardGameAIPuluc(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
		: base(difficulty, boardGameHandler)
	{
		_board = base.BoardGameHandler.Board as BoardGamePuluc;
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
		Move bestMove = default(Move);
		bestMove.GoalTile = null;
		bestMove.Unit = null;
		if (_board.IsReady)
		{
			ExpectiMax(MaxDepth, BoardGameSide.AI, chanceNode: false, ref bestMove);
		}
		if (!base.AbortRequested)
		{
			_ = bestMove.IsValid;
		}
		return bestMove;
	}

	private float ExpectiMax(int depth, BoardGameSide side, bool chanceNode, ref Move bestMove)
	{
		float num;
		if (depth == 0)
		{
			num = Evaluation();
			if (side == BoardGameSide.Player)
			{
				num = 0f - num;
			}
		}
		else if (chanceNode)
		{
			num = 0f;
			for (int i = 0; i < 5; i++)
			{
				int lastDice = _board.LastDice;
				_board.ForceDice((i == 0) ? 5 : i);
				num += _diceProbabilities[i] * ExpectiMax(depth - 1, side, chanceNode: false, ref bestMove);
				_board.ForceDice(lastDice);
			}
		}
		else
		{
			BoardGamePuluc.BoardInformation board = _board.TakeBoardSnapshot();
			List<List<Move>> moves = _board.CalculateAllValidMoves(side);
			if (_board.HasMovesAvailable(ref moves))
			{
				num = float.MinValue;
				foreach (List<Move> item in moves)
				{
					if (item == null)
					{
						continue;
					}
					foreach (Move item2 in item)
					{
						_board.AIMakeMove(item2);
						BoardGameSide side2 = ((side == BoardGameSide.AI) ? BoardGameSide.Player : BoardGameSide.AI);
						float num2 = 0f - ExpectiMax(depth - 1, side2, chanceNode: true, ref bestMove);
						_board.UndoMove(ref board);
						if (num < num2)
						{
							num = num2;
							if (depth == MaxDepth)
							{
								bestMove = item2;
							}
						}
					}
				}
			}
			else
			{
				num = Evaluation();
				if (side == BoardGameSide.Player)
				{
					num = 0f - num;
				}
			}
		}
		return num;
	}

	private int Evaluation()
	{
		return 20 * (_board.GetPlayerTwoUnitsAlive() - _board.GetPlayerOneUnitsAlive()) + 5 * (GetUnitsBeingCaptured(playerOne: true) - GetUnitsBeingCaptured(playerOne: false)) + (GetUnitsInPlay(playerOne: false) - GetUnitsInPlay(playerOne: true));
	}

	private int GetUnitsInSpawn(bool playerOne)
	{
		int num = 0;
		foreach (PawnPuluc item in playerOne ? _board.PlayerOneUnits : _board.PlayerTwoUnits)
		{
			if (item.IsInSpawn)
			{
				num++;
			}
		}
		return num;
	}

	private int GetUnitsBeingCaptured(bool playerOne)
	{
		int num = 0;
		foreach (PawnPuluc item in playerOne ? _board.PlayerOneUnits : _board.PlayerTwoUnits)
		{
			if (!item.IsTopPawn)
			{
				num++;
			}
		}
		return num;
	}

	private int GetUnitsInPlay(bool playerOne)
	{
		int num = 0;
		foreach (PawnPuluc item in playerOne ? _board.PlayerOneUnits : _board.PlayerTwoUnits)
		{
			if (item.InPlay && item.IsTopPawn)
			{
				num++;
			}
		}
		return num;
	}
}
