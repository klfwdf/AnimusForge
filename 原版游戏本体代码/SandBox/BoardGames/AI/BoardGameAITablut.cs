using System;
using Helpers;
using SandBox.BoardGames.MissionLogics;

namespace SandBox.BoardGames.AI
{
	// Token: 0x02000104 RID: 260
	public class BoardGameAITablut : BoardGameAIBase
	{
		// Token: 0x06000D14 RID: 3348 RVA: 0x0005FA7E File Offset: 0x0005DC7E
		public BoardGameAITablut(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
			: base(difficulty, boardGameHandler)
		{
		}

		// Token: 0x06000D15 RID: 3349 RVA: 0x0005FA88 File Offset: 0x0005DC88
		public override void Initialize()
		{
			base.Initialize();
			BoardGameAITablut.Board = base.BoardGameHandler.Board as BoardGameTablut;
		}

		// Token: 0x06000D16 RID: 3350 RVA: 0x0005FAA5 File Offset: 0x0005DCA5
		public override void OnSetGameOver()
		{
			base.OnSetGameOver();
			BoardGameAITablut.Board = null;
		}

		// Token: 0x06000D17 RID: 3351 RVA: 0x0005FAB4 File Offset: 0x0005DCB4
		public override Move CalculateMovementStageMove()
		{
			Move openingMove;
			openingMove.GoalTile = null;
			openingMove.Unit = null;
			if (BoardGameAITablut.Board.IsReady)
			{
				BoardGameTablut.BoardInformation initialBoardState = BoardGameAITablut.Board.TakeBoardSnapshot();
				TreeNodeTablut treeNodeTablut = TreeNodeTablut.CreateTreeAndReturnRootNode(initialBoardState, this.MaxDepth);
				int num = 0;
				while (num < this._sampleCount && !base.AbortRequested)
				{
					treeNodeTablut.SelectAction();
					num++;
				}
				if (!base.AbortRequested)
				{
					BoardGameAITablut.Board.UndoMove(ref initialBoardState);
					TreeNodeTablut childWithBestScore = treeNodeTablut.GetChildWithBestScore();
					if (childWithBestScore != null)
					{
						openingMove = childWithBestScore.OpeningMove;
					}
				}
			}
			if (!base.AbortRequested)
			{
				bool isValid = openingMove.IsValid;
			}
			return openingMove;
		}

		// Token: 0x06000D18 RID: 3352 RVA: 0x0005FB50 File Offset: 0x0005DD50
		protected override void InitializeDifficulty()
		{
			switch (base.Difficulty)
			{
			case BoardGameHelper.AIDifficulty.Easy:
				this.MaxDepth = 3;
				this._sampleCount = 30000;
				return;
			case BoardGameHelper.AIDifficulty.Normal:
				this.MaxDepth = 4;
				this._sampleCount = 47000;
				return;
			case BoardGameHelper.AIDifficulty.Hard:
				this.MaxDepth = 5;
				this._sampleCount = 64000;
				return;
			default:
				return;
			}
		}

		// Token: 0x0400059F RID: 1439
		public static BoardGameTablut Board;

		// Token: 0x040005A0 RID: 1440
		private int _sampleCount;
	}
}
