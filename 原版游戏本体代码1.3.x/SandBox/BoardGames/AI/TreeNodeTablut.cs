using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.BoardGames.Pawns;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI
{
	// Token: 0x02000105 RID: 261
	public class TreeNodeTablut
	{
		// Token: 0x17000114 RID: 276
		// (get) Token: 0x06000D19 RID: 3353 RVA: 0x0005FBAF File Offset: 0x0005DDAF
		// (set) Token: 0x06000D1A RID: 3354 RVA: 0x0005FBB7 File Offset: 0x0005DDB7
		public Move OpeningMove { get; private set; }

		// Token: 0x17000115 RID: 277
		// (get) Token: 0x06000D1B RID: 3355 RVA: 0x0005FBC0 File Offset: 0x0005DDC0
		private bool IsLeaf
		{
			get
			{
				return this._children == null;
			}
		}

		// Token: 0x06000D1C RID: 3356 RVA: 0x0005FBCB File Offset: 0x0005DDCB
		public TreeNodeTablut(BoardGameSide lastTurnIsPlayedBy, int depth)
		{
			this._lastTurnIsPlayedBy = lastTurnIsPlayedBy;
			this._depth = depth;
		}

		// Token: 0x06000D1D RID: 3357 RVA: 0x0005FBE1 File Offset: 0x0005DDE1
		public static TreeNodeTablut CreateTreeAndReturnRootNode(BoardGameTablut.BoardInformation initialBoardState, int maxDepth)
		{
			TreeNodeTablut.MaxDepth = maxDepth;
			return new TreeNodeTablut(BoardGameSide.Player, 0)
			{
				_boardState = initialBoardState
			};
		}

		// Token: 0x06000D1E RID: 3358 RVA: 0x0005FBF8 File Offset: 0x0005DDF8
		public TreeNodeTablut GetChildWithBestScore()
		{
			TreeNodeTablut result = null;
			if (!this.IsLeaf)
			{
				float num = float.MinValue;
				foreach (TreeNodeTablut treeNodeTablut in this._children)
				{
					if (treeNodeTablut._visits > 0)
					{
						float num2 = (float)treeNodeTablut._wins / (float)treeNodeTablut._visits;
						if (!treeNodeTablut.IsLeaf)
						{
							float num3 = 0f;
							foreach (TreeNodeTablut treeNodeTablut2 in treeNodeTablut._children)
							{
								if (treeNodeTablut2._visits > 0)
								{
									float num4 = (float)treeNodeTablut2._wins / (float)treeNodeTablut2._visits;
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
							result = treeNodeTablut;
							num = num2;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000D1F RID: 3359 RVA: 0x0005FD08 File Offset: 0x0005DF08
		public void SelectAction()
		{
			TreeNodeTablut treeNodeTablut = this;
			while (!treeNodeTablut.IsLeaf)
			{
				treeNodeTablut = treeNodeTablut.Select();
			}
			TreeNodeTablut.ExpandResult expandResult = treeNodeTablut.Expand();
			BoardGameSide winner = BoardGameSide.None;
			bool flag = false;
			if (expandResult == TreeNodeTablut.ExpandResult.NeedsToBeSimulated)
			{
				if (!treeNodeTablut.IsLeaf)
				{
					treeNodeTablut = treeNodeTablut.Select();
				}
				TreeNodeTablut.SimulationResult simulationResult = treeNodeTablut.Simulate();
				if (simulationResult.EndState != BoardGameTablut.State.Aborted)
				{
					winner = ((simulationResult.EndState == BoardGameTablut.State.AIWon) ? BoardGameSide.AI : BoardGameSide.Player);
					treeNodeTablut.BackPropagate(winner);
					flag = simulationResult.TurnsNeededToReachEndState <= 1;
				}
			}
			else if (expandResult != TreeNodeTablut.ExpandResult.Aborted)
			{
				winner = ((expandResult == TreeNodeTablut.ExpandResult.AIWon) ? BoardGameSide.AI : BoardGameSide.Player);
				treeNodeTablut.BackPropagate(winner);
				flag = true;
			}
			if (flag)
			{
				this.PruneSiblings(treeNodeTablut, winner);
			}
		}

		// Token: 0x06000D20 RID: 3360 RVA: 0x0005FDA0 File Offset: 0x0005DFA0
		private void PruneSiblings(TreeNodeTablut node, BoardGameSide winner)
		{
			if (node._parent != null && winner == node._lastTurnIsPlayedBy)
			{
				int count = node._parent._children.Count;
				if (count > 1)
				{
					int num = 0;
					int num2 = 0;
					for (int i = count - 1; i >= 0; i--)
					{
						if (node._parent._children[i] != node)
						{
							num += node._parent._children[i]._wins;
							num2 += node._parent._children[i]._visits;
							node._parent._children.RemoveAt(i);
						}
					}
					int num3 = num2 - num;
					for (TreeNodeTablut parent = node._parent; parent != null; parent = parent._parent)
					{
						if (parent._lastTurnIsPlayedBy == winner)
						{
							parent._wins -= num;
						}
						else
						{
							parent._wins -= num3;
						}
						parent._visits -= num2;
					}
				}
			}
		}

		// Token: 0x06000D21 RID: 3361 RVA: 0x0005FEA4 File Offset: 0x0005E0A4
		private TreeNodeTablut Select()
		{
			double num = double.MinValue;
			TreeNodeTablut treeNodeTablut = null;
			foreach (TreeNodeTablut treeNodeTablut2 in this._children)
			{
				if (treeNodeTablut2._visits == 0)
				{
					treeNodeTablut = treeNodeTablut2;
					break;
				}
				double num2 = (double)treeNodeTablut2._wins / (double)treeNodeTablut2._visits + (double)(1.5f * MathF.Sqrt(MathF.Log((float)this._visits) / (float)treeNodeTablut2._visits));
				if (num2 > num)
				{
					treeNodeTablut = treeNodeTablut2;
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

		// Token: 0x06000D22 RID: 3362 RVA: 0x0005FF88 File Offset: 0x0005E188
		private TreeNodeTablut.ExpandResult Expand()
		{
			TreeNodeTablut.ExpandResult result = TreeNodeTablut.ExpandResult.NeedsToBeSimulated;
			if (this._depth < TreeNodeTablut.MaxDepth)
			{
				BoardGameAITablut.Board.UndoMove(ref this._boardState);
				BoardGameTablut.State state = BoardGameAITablut.Board.CheckGameState();
				if (state == BoardGameTablut.State.InProgress)
				{
					BoardGameSide boardGameSide = ((this._lastTurnIsPlayedBy == BoardGameSide.Player) ? BoardGameSide.AI : BoardGameSide.Player);
					Move winningMoveIfPresent = BoardGameAITablut.Board.GetWinningMoveIfPresent(boardGameSide);
					if (winningMoveIfPresent.IsValid)
					{
						TreeNodeTablut treeNodeTablut = new TreeNodeTablut(boardGameSide, this._depth + 1);
						treeNodeTablut.OpeningMove = winningMoveIfPresent;
						treeNodeTablut._parent = this;
						this._children = new List<TreeNodeTablut>(1);
						this._children.Add(treeNodeTablut);
					}
					else
					{
						List<List<Move>> list = BoardGameAITablut.Board.CalculateAllValidMoves(boardGameSide);
						int totalMovesAvailable = BoardGameAITablut.Board.GetTotalMovesAvailable(ref list);
						if (totalMovesAvailable > 0)
						{
							this._children = new List<TreeNodeTablut>(totalMovesAvailable);
							using (List<List<Move>>.Enumerator enumerator = list.GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									List<Move> list2 = enumerator.Current;
									foreach (Move openingMove in list2)
									{
										TreeNodeTablut treeNodeTablut2 = new TreeNodeTablut(boardGameSide, this._depth + 1);
										treeNodeTablut2.OpeningMove = openingMove;
										treeNodeTablut2._parent = this;
										this._children.Add(treeNodeTablut2);
									}
								}
								return result;
							}
						}
						Debug.FailedAssert("No available moves left but the game is in progress", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\AI\\TreeNodeTablut.cs", "Expand", 396);
					}
				}
				else if (state == BoardGameTablut.State.Aborted)
				{
					result = TreeNodeTablut.ExpandResult.Aborted;
				}
				else if (state == BoardGameTablut.State.AIWon)
				{
					result = TreeNodeTablut.ExpandResult.AIWon;
				}
				else
				{
					result = TreeNodeTablut.ExpandResult.PlayerWon;
				}
			}
			return result;
		}

		// Token: 0x06000D23 RID: 3363 RVA: 0x00060128 File Offset: 0x0005E328
		private TreeNodeTablut.SimulationResult Simulate()
		{
			BoardGameAITablut.Board.UndoMove(ref this._boardState);
			BoardGameTablut.State state = BoardGameAITablut.Board.CheckGameState();
			BoardGameSide boardGameSide = ((this._lastTurnIsPlayedBy == BoardGameSide.Player) ? BoardGameSide.AI : BoardGameSide.Player);
			int num = 0;
			while (state == BoardGameTablut.State.InProgress)
			{
				Move move = BoardGameAITablut.Board.GetWinningMoveIfPresent(boardGameSide);
				if (!move.IsValid)
				{
					List<PawnBase> list = ((boardGameSide == BoardGameSide.Player) ? BoardGameAITablut.Board.PlayerOneUnits : BoardGameAITablut.Board.PlayerTwoUnits);
					int count = list.Count;
					int num2 = 3;
					PawnBase pawnBase;
					bool flag;
					do
					{
						pawnBase = list[MBRandom.RandomInt(count)];
						flag = BoardGameAITablut.Board.HasAvailableMoves(pawnBase as PawnTablut);
						num2--;
					}
					while (!flag && num2 > 0);
					if (!flag)
					{
						pawnBase = (from x in list
							orderby MBRandom.RandomInt()
							select x).FirstOrDefault((PawnBase x) => BoardGameAITablut.Board.HasAvailableMoves(x as PawnTablut));
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
				else if (boardGameSide == BoardGameSide.Player)
				{
					state = BoardGameTablut.State.AIWon;
				}
				else
				{
					state = BoardGameTablut.State.PlayerWon;
				}
				boardGameSide = ((boardGameSide == BoardGameSide.Player) ? BoardGameSide.AI : BoardGameSide.Player);
				num++;
			}
			return new TreeNodeTablut.SimulationResult(state, num);
		}

		// Token: 0x06000D24 RID: 3364 RVA: 0x00060290 File Offset: 0x0005E490
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

		// Token: 0x040005A1 RID: 1441
		private const float UCTConstant = 1.5f;

		// Token: 0x040005A2 RID: 1442
		private static int MaxDepth;

		// Token: 0x040005A3 RID: 1443
		private readonly int _depth;

		// Token: 0x040005A4 RID: 1444
		private BoardGameTablut.BoardInformation _boardState;

		// Token: 0x040005A5 RID: 1445
		private TreeNodeTablut _parent;

		// Token: 0x040005A6 RID: 1446
		private List<TreeNodeTablut> _children;

		// Token: 0x040005A7 RID: 1447
		private BoardGameSide _lastTurnIsPlayedBy;

		// Token: 0x040005A8 RID: 1448
		private int _visits;

		// Token: 0x040005A9 RID: 1449
		private int _wins;

		// Token: 0x02000227 RID: 551
		private struct SimulationResult
		{
			// Token: 0x060013F9 RID: 5113 RVA: 0x000784A1 File Offset: 0x000766A1
			public SimulationResult(BoardGameTablut.State s, int turns)
			{
				this.EndState = s;
				this.TurnsNeededToReachEndState = turns;
			}

			// Token: 0x040009B7 RID: 2487
			public readonly BoardGameTablut.State EndState;

			// Token: 0x040009B8 RID: 2488
			public readonly int TurnsNeededToReachEndState;
		}

		// Token: 0x02000228 RID: 552
		private enum ExpandResult
		{
			// Token: 0x040009BA RID: 2490
			NeedsToBeSimulated,
			// Token: 0x040009BB RID: 2491
			AIWon,
			// Token: 0x040009BC RID: 2492
			PlayerWon,
			// Token: 0x040009BD RID: 2493
			Aborted
		}
	}
}
