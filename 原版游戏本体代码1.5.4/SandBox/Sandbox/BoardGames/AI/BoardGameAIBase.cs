using Helpers;
using SandBox.BoardGames.MissionLogics;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.AI;

public abstract class BoardGameAIBase
{
	public enum AIState
	{
		NeedsToRun,
		ReadyToRun,
		Running,
		AbortRequested,
		Aborted,
		Done
	}

	private const float AIDecisionDuration = 1.5f;

	protected bool MayForfeit;

	protected int MaxDepth;

	private float _aiDecisionTimer;

	private readonly ITask _aiTask;

	private readonly object _stateLock;

	private volatile AIState _state;

	public AIState State => _state;

	public Move RecentMoveCalculated { get; private set; }

	public bool AbortRequested => State == AIState.AbortRequested;

	protected BoardGameHelper.AIDifficulty Difficulty { get; private set; }

	protected MissionBoardGameLogic BoardGameHandler { get; private set; }

	protected BoardGameAIBase(BoardGameHelper.AIDifficulty difficulty, MissionBoardGameLogic boardGameHandler)
	{
		_stateLock = new object();
		Difficulty = difficulty;
		BoardGameHandler = boardGameHandler;
		Initialize();
		_aiTask = AsyncTask.CreateWithDelegate(new ManagedDelegate
		{
			Instance = UpdateThinkingAboutMoveOnSeparateThread
		}, isBackground: true);
	}

	public virtual Move CalculatePreMovementStageMove()
	{
		Debug.FailedAssert("CalculatePreMovementStageMove is not implemented for " + BoardGameHandler.CurrentBoardGame, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\AI\\BoardGameAIBase.cs", "CalculatePreMovementStageMove", 64);
		return Move.Invalid;
	}

	public abstract Move CalculateMovementStageMove();

	protected abstract void InitializeDifficulty();

	public virtual bool WantsToForfeit()
	{
		return false;
	}

	public virtual void OnSetGameOver()
	{
		lock (_stateLock)
		{
			switch (State)
			{
			case AIState.ReadyToRun:
				_state = AIState.AbortRequested;
				break;
			case AIState.Running:
				_state = AIState.AbortRequested;
				break;
			}
		}
		_aiTask.Wait();
		Reset();
	}

	public virtual void Initialize()
	{
		Reset();
		InitializeDifficulty();
	}

	public void SetDifficulty(BoardGameHelper.AIDifficulty difficulty)
	{
		Difficulty = difficulty;
		InitializeDifficulty();
	}

	public float HowLongDidAIThinkAboutMove()
	{
		return _aiDecisionTimer;
	}

	public void UpdateThinkingAboutMove(float dt)
	{
		_aiDecisionTimer += dt;
		lock (_stateLock)
		{
			if (State == AIState.NeedsToRun)
			{
				_state = AIState.ReadyToRun;
				_aiTask.Invoke();
			}
		}
	}

	private void UpdateThinkingAboutMoveOnSeparateThread()
	{
		if (BoardGameHandler.Board.InPreMovementStage)
		{
			CalculatePreMovementStageOnSeparateThread();
		}
		else
		{
			CalculateMovementStageMoveOnSeparateThread();
		}
	}

	public void ResetThinking()
	{
		_aiDecisionTimer = 0f;
		_state = AIState.NeedsToRun;
	}

	public bool CanMakeMove()
	{
		if (State == AIState.Done)
		{
			return _aiDecisionTimer >= 1.5f;
		}
		return false;
	}

	private void Reset()
	{
		RecentMoveCalculated = Move.Invalid;
		MayForfeit = true;
		ResetThinking();
		MaxDepth = 0;
	}

	private void CalculatePreMovementStageOnSeparateThread()
	{
		if (OnBeginSeparateThread())
		{
			Move calculatedMove = CalculatePreMovementStageMove();
			OnExitSeparateThread(calculatedMove);
		}
	}

	private void CalculateMovementStageMoveOnSeparateThread()
	{
		if (OnBeginSeparateThread())
		{
			Move calculatedMove = CalculateMovementStageMove();
			OnExitSeparateThread(calculatedMove);
		}
	}

	private bool OnBeginSeparateThread()
	{
		bool flag = false;
		lock (_stateLock)
		{
			if (AbortRequested)
			{
				_state = AIState.Aborted;
				flag = true;
			}
			else
			{
				_state = AIState.Running;
			}
		}
		return !flag;
	}

	private void OnExitSeparateThread(Move calculatedMove)
	{
		lock (_stateLock)
		{
			if (AbortRequested)
			{
				_state = AIState.Aborted;
				RecentMoveCalculated = Move.Invalid;
			}
			else
			{
				_state = AIState.Done;
				RecentMoveCalculated = calculatedMove;
			}
		}
	}
}
