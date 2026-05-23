using System;
using Helpers;
using SandBox.BoardGames.MissionLogics;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI
{
	// Token: 0x020000FF RID: 255
	public abstract class BoardGameAIBase
	{
		// Token: 0x1700010F RID: 271
		// (get) Token: 0x06000CDC RID: 3292 RVA: 0x0005E4E8 File Offset: 0x0005C6E8
		public BoardGameAIBase.AIState State
		{
			get
			{
				return this._state;
			}
		}

		// Token: 0x17000110 RID: 272
		// (get) Token: 0x06000CDD RID: 3293 RVA: 0x0005E4F2 File Offset: 0x0005C6F2
		// (set) Token: 0x06000CDE RID: 3294 RVA: 0x0005E4FA File Offset: 0x0005C6FA
		public Move RecentMoveCalculated { get; private set; }

		// Token: 0x17000111 RID: 273
		// (get) Token: 0x06000CDF RID: 3295 RVA: 0x0005E503 File Offset: 0x0005C703
		public bool AbortRequested
		{
			get
			{
				return this.State == BoardGameAIBase.AIState.AbortRequested;
			}
		}

		// Token: 0x17000112 RID: 274
		// (get) Token: 0x06000CE0 RID: 3296 RVA: 0x0005E50E File Offset: 0x0005C70E
		// (set) Token: 0x06000CE1 RID: 3297 RVA: 0x0005E516 File Offset: 0x0005C716
		private protected BoardGameHelper.AIDifficulty Difficulty { protected get; private set; }

		// Token: 0x17000113 RID: 275
		// (get) Token: 0x06000CE2 RID: 3298 RVA: 0x0005E51F File Offset: 0x0005C71F
		// (set) Token: 0x06000CE3 RID: 3299 RVA: 0x0005E527 File Offset: 0x0005C727
		private protected MissionBoardGameLogic BoardGameHandler { protected get; private set; }

		// Token: 0x06000CE4 RID: 3300 RVA: 0x0005E530 File Offset: 0x0005C730
		protected BoardGameAIBase(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
		{
			this._stateLock = new object();
			this.Difficulty = difficulty;
			this.BoardGameHandler = boardGameHandler;
			this.Initialize();
			this._aiTask = AsyncTask.CreateWithDelegate(new ManagedDelegate
			{
				Instance = new ManagedDelegate.DelegateDefinition(this.UpdateThinkingAboutMoveOnSeparateThread)
			}, true);
		}

		// Token: 0x06000CE5 RID: 3301 RVA: 0x0005E587 File Offset: 0x0005C787
		public virtual Move CalculatePreMovementStageMove()
		{
			Debug.FailedAssert("CalculatePreMovementStageMove is not implemented for " + this.BoardGameHandler.CurrentBoardGame, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\AI\\BoardGameAIBase.cs", "CalculatePreMovementStageMove", 64);
			return Move.Invalid;
		}

		// Token: 0x06000CE6 RID: 3302
		public abstract Move CalculateMovementStageMove();

		// Token: 0x06000CE7 RID: 3303
		protected abstract void InitializeDifficulty();

		// Token: 0x06000CE8 RID: 3304 RVA: 0x0005E5B9 File Offset: 0x0005C7B9
		public virtual bool WantsToForfeit()
		{
			return false;
		}

		// Token: 0x06000CE9 RID: 3305 RVA: 0x0005E5BC File Offset: 0x0005C7BC
		public virtual void OnSetGameOver()
		{
			object stateLock = this._stateLock;
			lock (stateLock)
			{
				BoardGameAIBase.AIState state = this.State;
				if (state != BoardGameAIBase.AIState.ReadyToRun)
				{
					if (state == BoardGameAIBase.AIState.Running)
					{
						this._state = BoardGameAIBase.AIState.AbortRequested;
					}
				}
				else
				{
					this._state = BoardGameAIBase.AIState.AbortRequested;
				}
			}
			this._aiTask.Wait();
			this.Reset();
		}

		// Token: 0x06000CEA RID: 3306 RVA: 0x0005E62C File Offset: 0x0005C82C
		public virtual void Initialize()
		{
			this.Reset();
			this.InitializeDifficulty();
		}

		// Token: 0x06000CEB RID: 3307 RVA: 0x0005E63A File Offset: 0x0005C83A
		public void SetDifficulty(BoardGameHelper.AIDifficulty difficulty)
		{
			this.Difficulty = difficulty;
			this.InitializeDifficulty();
		}

		// Token: 0x06000CEC RID: 3308 RVA: 0x0005E649 File Offset: 0x0005C849
		public float HowLongDidAIThinkAboutMove()
		{
			return this._aiDecisionTimer;
		}

		// Token: 0x06000CED RID: 3309 RVA: 0x0005E654 File Offset: 0x0005C854
		public void UpdateThinkingAboutMove(float dt)
		{
			this._aiDecisionTimer += dt;
			object stateLock = this._stateLock;
			lock (stateLock)
			{
				if (this.State == BoardGameAIBase.AIState.NeedsToRun)
				{
					this._state = BoardGameAIBase.AIState.ReadyToRun;
					this._aiTask.Invoke();
				}
			}
		}

		// Token: 0x06000CEE RID: 3310 RVA: 0x0005E6B8 File Offset: 0x0005C8B8
		private void UpdateThinkingAboutMoveOnSeparateThread()
		{
			if (this.BoardGameHandler.Board.InPreMovementStage)
			{
				this.CalculatePreMovementStageOnSeparateThread();
				return;
			}
			this.CalculateMovementStageMoveOnSeparateThread();
		}

		// Token: 0x06000CEF RID: 3311 RVA: 0x0005E6D9 File Offset: 0x0005C8D9
		public void ResetThinking()
		{
			this._aiDecisionTimer = 0f;
			this._state = BoardGameAIBase.AIState.NeedsToRun;
		}

		// Token: 0x06000CF0 RID: 3312 RVA: 0x0005E6EF File Offset: 0x0005C8EF
		public bool CanMakeMove()
		{
			return this.State == BoardGameAIBase.AIState.Done && this._aiDecisionTimer >= 1.5f;
		}

		// Token: 0x06000CF1 RID: 3313 RVA: 0x0005E70C File Offset: 0x0005C90C
		private void Reset()
		{
			this.RecentMoveCalculated = Move.Invalid;
			this.MayForfeit = true;
			this.ResetThinking();
			this.MaxDepth = 0;
		}

		// Token: 0x06000CF2 RID: 3314 RVA: 0x0005E730 File Offset: 0x0005C930
		private void CalculatePreMovementStageOnSeparateThread()
		{
			if (this.OnBeginSeparateThread())
			{
				Move calculatedMove = this.CalculatePreMovementStageMove();
				this.OnExitSeparateThread(calculatedMove);
			}
		}

		// Token: 0x06000CF3 RID: 3315 RVA: 0x0005E754 File Offset: 0x0005C954
		private void CalculateMovementStageMoveOnSeparateThread()
		{
			if (this.OnBeginSeparateThread())
			{
				Move calculatedMove = this.CalculateMovementStageMove();
				this.OnExitSeparateThread(calculatedMove);
			}
		}

		// Token: 0x06000CF4 RID: 3316 RVA: 0x0005E778 File Offset: 0x0005C978
		private bool OnBeginSeparateThread()
		{
			bool flag = false;
			object stateLock = this._stateLock;
			lock (stateLock)
			{
				if (this.AbortRequested)
				{
					this._state = BoardGameAIBase.AIState.Aborted;
					flag = true;
				}
				else
				{
					this._state = BoardGameAIBase.AIState.Running;
				}
			}
			return !flag;
		}

		// Token: 0x06000CF5 RID: 3317 RVA: 0x0005E7D8 File Offset: 0x0005C9D8
		private void OnExitSeparateThread(Move calculatedMove)
		{
			object stateLock = this._stateLock;
			lock (stateLock)
			{
				if (this.AbortRequested)
				{
					this._state = BoardGameAIBase.AIState.Aborted;
					this.RecentMoveCalculated = Move.Invalid;
				}
				else
				{
					this._state = BoardGameAIBase.AIState.Done;
					this.RecentMoveCalculated = calculatedMove;
				}
			}
		}

		// Token: 0x0400058F RID: 1423
		private const float AIDecisionDuration = 1.5f;

		// Token: 0x04000590 RID: 1424
		protected bool MayForfeit;

		// Token: 0x04000591 RID: 1425
		protected int MaxDepth;

		// Token: 0x04000592 RID: 1426
		private float _aiDecisionTimer;

		// Token: 0x04000593 RID: 1427
		private readonly ITask _aiTask;

		// Token: 0x04000594 RID: 1428
		private readonly object _stateLock;

		// Token: 0x04000595 RID: 1429
		private volatile BoardGameAIBase.AIState _state;

		// Token: 0x02000225 RID: 549
		public enum AIState
		{
			// Token: 0x040009AF RID: 2479
			NeedsToRun,
			// Token: 0x040009B0 RID: 2480
			ReadyToRun,
			// Token: 0x040009B1 RID: 2481
			Running,
			// Token: 0x040009B2 RID: 2482
			AbortRequested,
			// Token: 0x040009B3 RID: 2483
			Aborted,
			// Token: 0x040009B4 RID: 2484
			Done
		}
	}
}
