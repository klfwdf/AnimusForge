using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public class GameStateManager
{
	public enum GameStateManagerType
	{
		Game,
		Global
	}

	private struct GameStateJob
	{
		public enum JobType
		{
			None,
			Push,
			Pop,
			CleanAndPushState,
			CleanStates
		}

		public readonly JobType Job;

		public readonly GameState GameState;

		public readonly int PopLevel;

		public GameStateJob(JobType job, GameState gameState, int popLevel)
		{
			Job = job;
			GameState = gameState;
			PopLevel = popLevel;
		}
	}

	private static GameStateManager _current;

	public static string StateActivateCommand;

	private readonly List<GameState> _gameStates;

	private readonly List<IGameStateManagerListener> _listeners;

	private readonly List<WeakReference> _activeStateDisableRequests;

	private readonly Queue<GameStateJob> _gameStateJobs;

	public static GameStateManager Current
	{
		get
		{
			return _current;
		}
		set
		{
			_current?.CleanStates();
			_current = value;
		}
	}

	public IReadOnlyCollection<IGameStateManagerListener> Listeners => _listeners.AsReadOnly();

	public GameStateManagerType CurrentType { get; private set; }

	public IGameStateManagerOwner Owner { get; private set; }

	public IEnumerable<GameState> GameStates => _gameStates.AsReadOnly();

	public bool ActiveStateDisabledByUser => _activeStateDisableRequests.Count > 0;

	public GameState ActiveState
	{
		get
		{
			if (_gameStates.Count <= 0)
			{
				return null;
			}
			return _gameStates[_gameStates.Count - 1];
		}
	}

	public GameStateManager(IGameStateManagerOwner owner, GameStateManagerType gameStateManagerType)
	{
		Owner = owner;
		CurrentType = gameStateManagerType;
		_gameStateJobs = new Queue<GameStateJob>();
		_gameStates = new List<GameState>();
		_listeners = new List<IGameStateManagerListener>();
		_activeStateDisableRequests = new List<WeakReference>();
	}

	internal GameState FindPredecessor(GameState gameState)
	{
		GameState result = null;
		int num = _gameStates.IndexOf(gameState);
		if (num > 0)
		{
			result = _gameStates[num - 1];
		}
		return result;
	}

	public bool RegisterListener(IGameStateManagerListener listener)
	{
		if (_listeners.Contains(listener))
		{
			return false;
		}
		_listeners.Add(listener);
		return true;
	}

	public bool UnregisterListener(IGameStateManagerListener listener)
	{
		return _listeners.Remove(listener);
	}

	public T GetListenerOfType<T>()
	{
		foreach (IGameStateManagerListener listener in _listeners)
		{
			IGameStateManagerListener current;
			if ((current = listener) is T)
			{
				return (T)current;
			}
		}
		return default(T);
	}

	public void RegisterActiveStateDisableRequest(object requestingInstance)
	{
		if (!_activeStateDisableRequests.Contains(requestingInstance))
		{
			_activeStateDisableRequests.Add(new WeakReference(requestingInstance));
		}
	}

	public void UnregisterActiveStateDisableRequest(object requestingInstance)
	{
		for (int i = 0; i < _activeStateDisableRequests.Count; i++)
		{
			if (_activeStateDisableRequests[i]?.Target == requestingInstance)
			{
				_activeStateDisableRequests.RemoveAt(i);
				break;
			}
		}
	}

	public void OnSavedGameLoadFinished()
	{
		foreach (IGameStateManagerListener listener in _listeners)
		{
			listener.OnSavedGameLoadFinished();
		}
	}

	public T LastOrDefault<T>() where T : GameState
	{
		return _gameStates.LastOrDefault((GameState g) => g is T) as T;
	}

	public T CreateState<T>() where T : GameState, new()
	{
		T val = new T();
		HandleCreateState(val);
		return val;
	}

	public T CreateState<T>(params object[] parameters) where T : GameState, new()
	{
		GameState gameState = (GameState)Activator.CreateInstance(typeof(T), parameters);
		HandleCreateState(gameState);
		return (T)gameState;
	}

	private void HandleCreateState(GameState state)
	{
		state.GameStateManager = this;
		foreach (IGameStateManagerListener listener in _listeners)
		{
			listener.OnCreateState(state);
		}
	}

	public void OnTick(float dt)
	{
		CleanRequests();
		if (ActiveState != null)
		{
			if (ActiveStateDisabledByUser)
			{
				ActiveState.OnIdleTick(dt);
			}
			else
			{
				ActiveState.OnTick(dt);
			}
		}
	}

	private void CleanRequests()
	{
		for (int num = _activeStateDisableRequests.Count - 1; num >= 0; num--)
		{
			WeakReference weakReference = _activeStateDisableRequests[num];
			if (weakReference == null || !weakReference.IsAlive)
			{
				_activeStateDisableRequests.RemoveAt(num);
			}
		}
	}

	public void PushState(GameState gameState, int level = 0)
	{
		if (!TWParallel.IsMainThread())
		{
			Debug.FailedAssert("State should be changed from main thread", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\GameStateManager.cs", "PushState", 222);
		}
		GameStateJob item = new GameStateJob(GameStateJob.JobType.Push, gameState, level);
		_gameStateJobs.Enqueue(item);
		DoGameStateJobs();
	}

	public void PopState(int level = 0)
	{
		if (!TWParallel.IsMainThread())
		{
			Debug.FailedAssert("State should be changed from main thread", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\GameStateManager.cs", "PopState", 235);
		}
		GameStateJob item = new GameStateJob(GameStateJob.JobType.Pop, null, level);
		_gameStateJobs.Enqueue(item);
		DoGameStateJobs();
	}

	public void CleanAndPushState(GameState gameState, int level = 0)
	{
		GameStateJob item = new GameStateJob(GameStateJob.JobType.CleanAndPushState, gameState, level);
		_gameStateJobs.Enqueue(item);
		DoGameStateJobs();
	}

	public void CleanStates(int level = 0)
	{
		GameStateJob item = new GameStateJob(GameStateJob.JobType.CleanStates, null, level);
		_gameStateJobs.Enqueue(item);
		DoGameStateJobs();
	}

	private void OnPushState(GameState gameState)
	{
		GameState activeState = ActiveState;
		bool isTopGameState = _gameStates.Count == 0;
		int num = _gameStates.FindLastIndex((GameState state) => state.Level <= gameState.Level);
		if (num == -1)
		{
			_gameStates.Add(gameState);
		}
		else
		{
			_gameStates.Insert(num + 1, gameState);
		}
		GameState activeState2 = ActiveState;
		if (activeState2 != activeState)
		{
			if (activeState != null && activeState.Activated)
			{
				activeState.HandleDeactivate();
			}
			foreach (IGameStateManagerListener listener in _listeners)
			{
				listener.OnPushState(activeState2, isTopGameState);
			}
			activeState2.HandleInitialize();
			activeState2.HandleActivate();
			Owner.OnStateChanged(activeState);
		}
		Common.MemoryCleanupGC();
	}

	private void OnPopState(int level)
	{
		GameState activeState = ActiveState;
		int index = _gameStates.FindLastIndex((GameState state) => state.Level == level);
		GameState gameState = _gameStates[index];
		gameState.HandleDeactivate();
		gameState.HandleFinalize();
		_gameStates.RemoveAt(index);
		GameState activeState2 = ActiveState;
		foreach (IGameStateManagerListener listener in _listeners)
		{
			listener.OnPopState(gameState);
		}
		if (activeState2 != activeState)
		{
			if (activeState2 != null)
			{
				activeState2.HandleActivate();
			}
			else if (_gameStateJobs.Count == 0 || (_gameStateJobs.Peek().Job != GameStateJob.JobType.Push && _gameStateJobs.Peek().Job != GameStateJob.JobType.CleanAndPushState))
			{
				Owner.OnStateStackEmpty();
			}
			Owner.OnStateChanged(gameState);
		}
		Common.MemoryCleanupGC();
	}

	private void OnCleanAndPushState(GameState gameState)
	{
		int num = -1;
		for (int i = 0; i < _gameStates.Count; i++)
		{
			if (_gameStates[i].Level >= gameState.Level)
			{
				num = i - 1;
				break;
			}
		}
		GameState activeState = ActiveState;
		for (int num2 = _gameStates.Count - 1; num2 > num; num2--)
		{
			GameState gameState2 = _gameStates[num2];
			if (gameState2.Activated)
			{
				gameState2.HandleDeactivate();
			}
			gameState2.HandleFinalize();
			_gameStates.RemoveAt(num2);
		}
		OnPushState(gameState);
		Owner.OnStateChanged(activeState);
	}

	private void OnCleanStates(int popLevel)
	{
		int num = -1;
		for (int i = 0; i < _gameStates.Count; i++)
		{
			if (_gameStates[i].Level >= popLevel)
			{
				num = i - 1;
				break;
			}
		}
		GameState activeState = ActiveState;
		for (int num2 = _gameStates.Count - 1; num2 > num; num2--)
		{
			GameState gameState = _gameStates[num2];
			if (gameState.Activated)
			{
				gameState.HandleDeactivate();
			}
			gameState.HandleFinalize();
			_gameStates.RemoveAt(num2);
		}
		foreach (IGameStateManagerListener listener in _listeners)
		{
			listener.OnCleanStates();
		}
		GameState activeState2 = ActiveState;
		if (activeState != activeState2)
		{
			if (activeState2 != null)
			{
				activeState2.HandleActivate();
			}
			else if (_gameStateJobs.Count == 0 || (_gameStateJobs.Peek().Job != GameStateJob.JobType.Push && _gameStateJobs.Peek().Job != GameStateJob.JobType.CleanAndPushState))
			{
				Owner.OnStateStackEmpty();
			}
			Owner.OnStateChanged(activeState);
		}
	}

	private void DoGameStateJobs()
	{
		while (_gameStateJobs.Count > 0)
		{
			GameStateJob gameStateJob = _gameStateJobs.Dequeue();
			switch (gameStateJob.Job)
			{
			case GameStateJob.JobType.Push:
				OnPushState(gameStateJob.GameState);
				break;
			case GameStateJob.JobType.Pop:
				OnPopState(gameStateJob.PopLevel);
				break;
			case GameStateJob.JobType.CleanAndPushState:
				OnCleanAndPushState(gameStateJob.GameState);
				break;
			case GameStateJob.JobType.CleanStates:
				OnCleanStates(gameStateJob.PopLevel);
				break;
			}
		}
	}
}
