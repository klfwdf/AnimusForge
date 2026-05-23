using System.Collections.Generic;
using System.Linq;
using SandBox.BoardGames.Pawns;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI;

public class TreeNodeTablut
{
	private struct SimulationResult
	{
		public readonly BoardGameTablut.State EndState;

		public readonly int TurnsNeededToReachEndState;

		public SimulationResult(BoardGameTablut.State s, int turns)
		{
			EndState = s;
			TurnsNeededToReachEndState = turns;
		}
	}

	private enum ExpandResult
	{
		NeedsToBeSimulated,
		AIWon,
		PlayerWon,
		Aborted
	}

	private const float UCTConstant = 1.5f;

	private static int MaxDepth;

	private readonly int _depth;

	private BoardGameTablut.BoardInformation _boardState;

	private TreeNodeTablut _parent;

	private List<TreeNodeTablut> _children;

	private BoardGameSide _lastTurnIsPlayedBy;

	private int _visits;

	private int _wins;

	public Move OpeningMove { get; private set; }

	private bool IsLeaf => _children == null;

	public TreeNodeTablut(BoardGameSide lastTurnIsPlayedBy, int depth)
	{
		_lastTurnIsPlayedBy = lastTurnIsPlayedBy;
		_depth = depth;
	}

	public static TreeNodeTablut CreateTreeAndReturnRootNode(BoardGameTablut.BoardInformation initialBoardState, int maxDepth)
	{
		MaxDepth = maxDepth;
		return new TreeNodeTablut(BoardGameSide.Player, 0)
		{
			_boardState = initialBoardState
		};
	}

	public TreeNodeTablut GetChildWithBestScore()
	{
		TreeNodeTablut result = null;
		if (!IsLeaf)
		{
			float num = float.MinValue;
			foreach (TreeNodeTablut child in _children)
			{
				if (child._visits <= 0)
				{
					continue;
				}
				float num2 = (float)child._wins / (float)child._visits;
				if (!child.IsLeaf)
				{
					float num3 = 0f;
					foreach (TreeNodeTablut child2 in child._children)
					{
						if (child2._visits > 0)
						{
							float num4 = (float)child2._wins / (float)child2._visits;
							if (num4 > num3)
							{
								num3 = num4;
							}
						}
					}
					num2 *= 1f - num3;
				}
				if (num2 > num)
				{
					result = child;
					num = num2;
				}
			}
		}
		return result;
	}

	public void SelectAction()
	{
		TreeNodeTablut treeNodeTablut = this;
		while (!treeNodeTablut.IsLeaf)
		{
			treeNodeTablut = treeNodeTablut.Select();
		}
		ExpandResult expandResult = treeNodeTablut.Expand();
		BoardGameSide winner = BoardGameSide.None;
		bool flag = false;
		if (expandResult == ExpandResult.NeedsToBeSimulated)
		{
			if (!treeNodeTablut.IsLeaf)
			{
				treeNodeTablut = treeNodeTablut.Select();
			}
			SimulationResult simulationResult = treeNodeTablut.Simulate();
			if (simulationResult.EndState != BoardGameTablut.State.Aborted)
			{
				winner = ((simulationResult.EndState != BoardGameTablut.State.AIWon) ? BoardGameSide.Player : BoardGameSide.AI);
				treeNodeTablut.BackPropagate(winner);
				flag = simulationResult.TurnsNeededToReachEndState <= 1;
			}
		}
		else if (expandResult != ExpandResult.Aborted)
		{
			winner = ((expandResult != ExpandResult.AIWon) ? BoardGameSide.Player : BoardGameSide.AI);
			treeNodeTablut.BackPropagate(winner);
			flag = true;
		}
		if (flag)
		{
			PruneSiblings(treeNodeTablut, winner);
		}
	}

	private void PruneSiblings(TreeNodeTablut node, BoardGameSide winner)
	{
		if (node._parent == null || winner != node._lastTurnIsPlayedBy)
		{
			return;
		}
		int count = node._parent._children.Count;
		if (count <= 1)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		for (int num3 = count - 1; num3 >= 0; num3--)
		{
			if (node._parent._children[num3] != node)
			{
				num += node._parent._children[num3]._wins;
				num2 += node._parent._children[num3]._visits;
				node._parent._children.RemoveAt(num3);
			}
		}
		int num4 = num2 - num;
		for (TreeNodeTablut parent = node._parent; parent != null; parent = parent._parent)
		{
			if (parent._lastTurnIsPlayedBy == winner)
			{
				parent._wins -= num;
			}
			else
			{
				parent._wins -= num4;
			}
			parent._visits -= num2;
		}
	}

	private TreeNodeTablut Select()
	{
		double num = double.MinValue;
		TreeNodeTablut treeNodeTablut = null;
		foreach (TreeNodeTablut child in _children)
		{
			if (child._visits == 0)
			{
				treeNodeTablut = child;
				break;
			}
			double num2 = (double)child._wins / (double)child._visits + (double)(1.5f * MathF.Sqrt(MathF.Log(_visits) / (float)child._visits));
			if (num2 > num)
			{
				treeNodeTablut = child;
				num = num2;
			}
		}
		if (treeNodeTablut._boardState.PawnInformation == null)
		{
			BoardGameAITablut.Board.UndoMove(ref treeNodeTablut._parent._boardState);
			BoardGameAITablut.Board.AIMakeMove(treeNodeTablut.OpeningMove);
			treeNodeTablut._boardState = BoardGameAITablut.Board.TakeBoardSnapshot();
		}
		return treeNodeTablut;
	}

	private ExpandResult Expand()
	{
		ExpandResult result = ExpandResult.NeedsToBeSimulated;
		if (_depth < MaxDepth)
		{
			BoardGameAITablut.Board.UndoMove(ref _boardState);
			switch (BoardGameAITablut.Board.CheckGameState())
			{
			case BoardGameTablut.State.InProgress:
			{
				BoardGameSide boardGameSide = ((_lastTurnIsPlayedBy != BoardGameSide.Player) ? BoardGameSide.Player : BoardGameSide.AI);
				Move winningMoveIfPresent = BoardGameAITablut.Board.GetWinningMoveIfPresent(boardGameSide);
				if (winningMoveIfPresent.IsValid)
				{
					TreeNodeTablut treeNodeTablut = new TreeNodeTablut(boardGameSide, _depth + 1);
					treeNodeTablut.OpeningMove = winningMoveIfPresent;
					treeNodeTablut._parent = this;
					_children = new List<TreeNodeTablut>(1);
					_children.Add(treeNodeTablut);
					break;
				}
				List<List<Move>> moves = BoardGameAITablut.Board.CalculateAllValidMoves(boardGameSide);
				int totalMovesAvailable = BoardGameAITablut.Board.GetTotalMovesAvailable(ref moves);
				if (totalMovesAvailable > 0)
				{
					_children = new List<TreeNodeTablut>(totalMovesAvailable);
					foreach (List<Move> item in moves)
					{
						foreach (Move item2 in item)
						{
							TreeNodeTablut treeNodeTablut2 = new TreeNodeTablut(boardGameSide, _depth + 1);
							treeNodeTablut2.OpeningMove = item2;
							treeNodeTablut2._parent = this;
							_children.Add(treeNodeTablut2);
						}
					}
				}
				else
				{
					Debug.FailedAssert("No available moves left but the game is in progress", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\AI\\TreeNodeTablut.cs", "Expand", 396);
				}
				break;
			}
			case BoardGameTablut.State.Aborted:
				result = ExpandResult.Aborted;
				break;
			case BoardGameTablut.State.AIWon:
				result = ExpandResult.AIWon;
				break;
			default:
				result = ExpandResult.PlayerWon;
				break;
			}
		}
		return result;
	}

	private SimulationResult Simulate()
	{
		BoardGameAITablut.Board.UndoMove(ref _boardState);
		BoardGameTablut.State state = BoardGameAITablut.Board.CheckGameState();
		BoardGameSide boardGameSide = ((_lastTurnIsPlayedBy != BoardGameSide.Player) ? BoardGameSide.Player : BoardGameSide.AI);
		int num = 0;
		while (state == BoardGameTablut.State.InProgress)
		{
			Move move = BoardGameAITablut.Board.GetWinningMoveIfPresent(boardGameSide);
			if (!move.IsValid)
			{
				List<PawnBase> list = ((boardGameSide == BoardGameSide.Player) ? BoardGameAITablut.Board.PlayerOneUnits : BoardGameAITablut.Board.PlayerTwoUnits);
				int count = list.Count;
				PawnBase pawnBase = null;
				bool flag = false;
				int num2 = 3;
				do
				{
					pawnBase = list[MBRandom.RandomInt(count)];
					flag = BoardGameAITablut.Board.HasAvailableMoves(pawnBase as PawnTablut);
					num2--;
				}
				while (!flag && num2 > 0);
				if (!flag)
				{
					pawnBase = list.OrderBy((PawnBase x) => MBRandom.RandomInt()).FirstOrDefault((PawnBase x) => BoardGameAITablut.Board.HasAvailableMoves(x as PawnTablut));
					flag = pawnBase != null;
				}
				if (flag)
				{
					move = BoardGameAITablut.Board.GetRandomAvailableMove(pawnBase as PawnTablut);
				}
			}
			if (move.IsValid)
			{
				BoardGameAITablut.Board.AIMakeMove(move);
				state = BoardGameAITablut.Board.CheckGameState();
			}
			else
			{
				state = ((boardGameSide != BoardGameSide.Player) ? BoardGameTablut.State.PlayerWon : BoardGameTablut.State.AIWon);
			}
			boardGameSide = ((boardGameSide != BoardGameSide.Player) ? BoardGameSide.Player : BoardGameSide.AI);
			num++;
		}
		return new SimulationResult(state, num);
	}

	private void BackPropagate(BoardGameSide winner)
	{
		for (TreeNodeTablut treeNodeTablut = this; treeNodeTablut != null; treeNodeTablut = treeNodeTablut._parent)
		{
			treeNodeTablut._visits++;
			if (winner == treeNodeTablut._lastTurnIsPlayedBy)
			{
				treeNodeTablut._wins++;
			}
		}
	}
}
