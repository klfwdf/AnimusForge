using Helpers;
using SandBox.BoardGames.MissionLogics;

namespace SandBox.BoardGames.AI;

public class BoardGameAITablut : BoardGameAIBase
{
	public static BoardGameTablut Board;

	private int _sampleCount;

	public BoardGameAITablut(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
		: base(difficulty, boardGameHandler)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		Board = base.BoardGameHandler.Board as BoardGameTablut;
	}

	public override void OnSetGameOver()
	{
		base.OnSetGameOver();
		Board = null;
	}

	public override Move CalculateMovementStageMove()
	{
		Move openingMove = default(Move);
		openingMove.GoalTile = null;
		openingMove.Unit = null;
		if (Board.IsReady)
		{
			BoardGameTablut.BoardInformation board = Board.TakeBoardSnapshot();
			TreeNodeTablut treeNodeTablut = TreeNodeTablut.CreateTreeAndReturnRootNode(board, MaxDepth);
			for (int i = 0; i < _sampleCount; i++)
			{
				if (base.AbortRequested)
				{
					break;
				}
				treeNodeTablut.SelectAction();
			}
			if (!base.AbortRequested)
			{
				Board.UndoMove(ref board);
				TreeNodeTablut childWithBestScore = treeNodeTablut.GetChildWithBestScore();
				if (childWithBestScore != null)
				{
					openingMove = childWithBestScore.OpeningMove;
				}
			}
		}
		if (!base.AbortRequested)
		{
			_ = openingMove.IsValid;
		}
		return openingMove;
	}

	protected override void InitializeDifficulty()
	{
		switch (base.Difficulty)
		{
		case BoardGameHelper.AIDifficulty.Easy:
			MaxDepth = 3;
			_sampleCount = 30000;
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			MaxDepth = 4;
			_sampleCount = 47000;
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			MaxDepth = 5;
			_sampleCount = 64000;
			break;
		}
	}
}
