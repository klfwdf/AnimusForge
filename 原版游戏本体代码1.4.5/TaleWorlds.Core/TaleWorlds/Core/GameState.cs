using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public abstract class GameState : MBObjectBase
{
	public int Level;

	private List<IGameStateListener> _listeners;

	public static int NumberOfListenerActivations;

	public GameState Predecessor => GameStateManager.FindPredecessor(this);

	public bool IsActive
	{
		get
		{
			if (GameStateManager != null)
			{
				return GameStateManager.ActiveState == this;
			}
			return false;
		}
	}

	public IReadOnlyCollection<IGameStateListener> Listeners => _listeners.AsReadOnly();

	public GameStateManager GameStateManager { get; internal set; }

	public virtual bool IsMusicMenuState => false;

	public virtual bool IsMenuState => false;

	public bool Activated { get; private set; }

	protected GameState()
	{
		_listeners = new List<IGameStateListener>();
	}

	public bool RegisterListener(IGameStateListener listener)
	{
		if (listener == null)
		{
			Debug.FailedAssert("Can not register null listener to game state.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\GameState.cs", "RegisterListener", 47);
		}
		if (_listeners.Contains(listener))
		{
			return false;
		}
		_listeners.Add(listener);
		return true;
	}

	public bool UnregisterListener(IGameStateListener listener)
	{
		return _listeners.Remove(listener);
	}

	public T GetListenerOfType<T>()
	{
		foreach (IGameStateListener listener in _listeners)
		{
			IGameStateListener current;
			if ((current = listener) is T)
			{
				return (T)current;
			}
		}
		return default(T);
	}

	internal void HandleInitialize()
	{
		OnInitialize();
		foreach (IGameStateListener listener in _listeners)
		{
			listener.OnInitialize();
		}
	}

	protected virtual void OnInitialize()
	{
	}

	internal void HandleFinalize()
	{
		OnFinalize();
		foreach (IGameStateListener listener in _listeners)
		{
			listener.OnFinalize();
		}
		_listeners = null;
		GameStateManager = null;
	}

	protected virtual void OnFinalize()
	{
	}

	internal void HandleActivate()
	{
		NumberOfListenerActivations = 0;
		if (!IsActive)
		{
			return;
		}
		OnActivate();
		if (IsActive && _listeners.Count != 0 && NumberOfListenerActivations == 0)
		{
			foreach (IGameStateListener listener in _listeners)
			{
				listener.OnActivate();
			}
			NumberOfListenerActivations++;
		}
		if (!string.IsNullOrEmpty(GameStateManager.StateActivateCommand))
		{
			CommandLineFunctionality.CallFunction(GameStateManager.StateActivateCommand, "", out var _);
		}
		Debug.ReportMemoryBookmark("GameState Activated: " + GetType().Name);
	}

	protected virtual void OnActivate()
	{
		Activated = true;
	}

	internal void HandleDeactivate()
	{
		OnDeactivate();
		foreach (IGameStateListener listener in _listeners)
		{
			listener.OnDeactivate();
		}
	}

	protected virtual void OnDeactivate()
	{
		Activated = false;
	}

	protected internal virtual void OnTick(float dt)
	{
	}

	protected internal virtual void OnIdleTick(float dt)
	{
	}
}
