using System.Collections.Generic;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.BoardGames.AI;

public class BoardGameAISeega : BoardGameAIBase
{
	private readonly BoardGameSeega _board;

	private readonly int[,] _boardValues = new int[5, 5]
	{
		{ 3, 2, 2, 2, 3 },
		{ 2, 1, 1, 1, 2 },
		{ 2, 1, 3, 1, 2 },
		{ 2, 1, 1, 1, 2 },
		{ 3, 2, 2, 2, 3 }
	};

	public BoardGameAISeega(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
		: base(difficulty, boardGameHandler)
	{
		_board = base.BoardGameHandler.Board as BoardGameSeega;
	}

	protected override void InitializeDifficulty()
	{
		switch (base.Difficulty)
		{
		case BoardGameHelper.AIDifficulty.Easy:
			MaxDepth = 2;
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			MaxDepth = 3;
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			MaxDepth = 4;
			break;
		}
	}

	public override Move CalculateMovementStageMove()
	{
		Move result = Move.Invalid;
		if (_board.IsReady)
		{
			List<List<Move>> moves = _board.CalculateAllValidMoves(BoardGameSide.AI);
			if (!_board.HasMovesAvailable(ref moves))
			{
				Dictionary<PawnBase, int> blockingPawns = _board.GetBlockingPawns(playerOneBlocked: false);
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=1bzdDYoO}All AI pawns blocked. Removing one of the player's pawns to make a move").ToString()));
				PawnBase key = blockingPawns.MaxBy((KeyValuePair<PawnBase, int> x) => x.Value).Key;
				_board.SetPawnCaptured(key);
				moves = _board.CalculateAllValidMoves(BoardGameSide.AI);
			}
			BoardGameSeega.BoardInformation board = _board.TakeBoardSnapshot();
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

	public override bool WantsToForfeit()
	{
		if (!MayForfeit)
		{
			return false;
		}
		int playerOneUnitsAlive = _board.GetPlayerOneUnitsAlive();
		int playerTwoUnitsAlive = _board.GetPlayerTwoUnitsAlive();
		int num = ((base.Difficulty != BoardGameHelper.AIDifficulty.Hard) ? 1 : 2);
		if (playerTwoUnitsAlive <= 7 && playerOneUnitsAlive >= playerTwoUnitsAlive + (num + playerTwoUnitsAlive / 2))
		{
			MayForfeit = false;
			return true;
		}
		return false;
	}

	public override Move CalculatePreMovementStageMove()
	{
		Move invalid = Move.Invalid;
		foreach (PawnSeega playerTwoUnit in _board.PlayerTwoUnits)
		{
			if (playerTwoUnit.IsPlaced || playerTwoUnit.Moving)
			{
				continue;
			}
			while (!invalid.IsValid && !base.AbortRequested)
			{
				int x = MBRandom.RandomInt(0, 5);
				int y = MBRandom.RandomInt(0, 5);
				if (_board.GetTile(x, y).PawnOnTile == null && !_board.GetTile(x, y).Entity.HasTag("obstructed_at_start"))
				{
					invalid.Unit = playerTwoUnit;
					invalid.GoalTile = _board.GetTile(x, y);
				}
			}
			break;
		}
		return invalid;
	}

	private int NegaMax(int depth, int color, int alpha, int beta)
	{
		int num = int.MinValue;
		if (depth == 0)
		{
			return color * Evaluation();
		}
		foreach (PawnSeega item in (color == 1) ? _board.PlayerTwoUnits : _board.PlayerOneUnits)
		{
			item.UpdateMoveBackAvailable();
		}
		List<List<Move>> moves = _board.CalculateAllValidMoves((color != 1) ? BoardGameSide.Player : BoardGameSide.AI);
		if (!_board.HasMovesAvailable(ref moves))
		{
			return color * Evaluation();
		}
		BoardGameSeega.BoardInformation board = _board.TakeBoardSnapshot();
		foreach (List<Move> item2 in moves)
		{
			if (item2 == null)
			{
				continue;
			}
			foreach (Move item3 in item2)
			{
				_board.AIMakeMove(item3);
				num = MathF.Max(-NegaMax(depth - 1, -color, -beta, -alpha), num);
				alpha = MathF.Max(alpha, num);
				_board.UndoMove(ref board);
				if (alpha >= beta && color == 1)
				{
					return alpha;
				}
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
			num = num * 0.7f + 0.5f;
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			num = num * 0.5f + 0.65f;
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			num = num * 0.35f + 0.75f;
			break;
		}
		return (int)((float)(20 * (_board.GetPlayerTwoUnitsAlive() - _board.GetPlayerOneUnitsAlive()) + (GetPlacementScore(player: false) - GetPlacementScore(player: true)) + 2 * (GetSurroundedScore(player: false) - GetSurroundedScore(player: true))) * num);
	}

	private int GetPlacementScore(bool player)
	{
		int num = 0;
		foreach (PawnSeega item in player ? _board.PlayerOneUnits : _board.PlayerTwoUnits)
		{
			if (item.IsPlaced)
			{
				num += _boardValues[item.X, item.Y];
			}
		}
		return num;
	}

	private int GetSurroundedScore(bool player)
	{
		int num = 0;
		foreach (PawnSeega item in player ? _board.PlayerOneUnits : _board.PlayerTwoUnits)
		{
			if (item.IsPlaced)
			{
				num += GetAmountSurroundingThisPawn(item);
			}
		}
		return num;
	}

	private int GetAmountSurroundingThisPawn(PawnSeega pawn)
	{
		int num = 0;
		int x = pawn.X;
		int y = pawn.Y;
		if (x > 0 && _board.GetTile(x - 1, y).PawnOnTile != null)
		{
			num++;
		}
		if (y > 0 && _board.GetTile(x, y - 1).PawnOnTile != null)
		{
			num++;
		}
		if (x < BoardGameSeega.BoardWidth - 1 && _board.GetTile(x + 1, y).PawnOnTile != null)
		{
			num++;
		}
		if (y < BoardGameSeega.BoardHeight - 1 && _board.GetTile(x, y + 1).PawnOnTile != null)
		{
			num++;
		}
		return num;
	}
}
