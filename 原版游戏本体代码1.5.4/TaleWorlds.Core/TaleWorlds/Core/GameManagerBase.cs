using System;
using System.Collections.Generic;

namespace TaleWorlds.Core;

public abstract class GameManagerBase
{
	private EntitySystem<GameManagerComponent> _entitySystem;

	private GameManagerLoadingSteps _stepNo;

	private Game _game;

	private bool _initialized;

	public static GameManagerBase Current { get; private set; }

	public Game Game
	{
		get
		{
			return _game;
		}
		internal set
		{
			if (value == null)
			{
				_game = null;
				_initialized = false;
			}
			else
			{
				_game = value;
				Initialize();
			}
		}
	}

	public IEnumerable<GameManagerComponent> Components => _entitySystem.Components;

	public abstract float ApplicationTime { get; }

	public abstract bool CheatMode { get; }

	public abstract bool IsDevelopmentMode { get; }

	public abstract bool IsEditModeOn { get; }

	public abstract UnitSpawnPrioritizations UnitSpawnPrioritization { get; }

	public void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
		}
	}

	protected GameManagerBase()
	{
		Current = this;
		_entitySystem = new EntitySystem<GameManagerComponent>();
		_stepNo = GameManagerLoadingSteps.PreInitializeZerothStep;
	}

	public GameManagerComponent AddComponent(Type componentType)
	{
		GameManagerComponent gameManagerComponent = _entitySystem.AddComponent(componentType);
		gameManagerComponent.GameManager = this;
		return gameManagerComponent;
	}

	public T AddComponent<T>() where T : GameManagerComponent, new()
	{
		return (T)AddComponent(typeof(T));
	}

	public GameManagerComponent GetComponent(Type componentType)
	{
		return _entitySystem.GetComponent(componentType);
	}

	public T GetComponent<T>() where T : GameManagerComponent
	{
		return _entitySystem.GetComponent<T>();
	}

	public IEnumerable<T> GetComponents<T>() where T : GameManagerComponent
	{
		return _entitySystem.GetComponents<T>();
	}

	public void RemoveComponent<T>() where T : GameManagerComponent
	{
		T component = _entitySystem.GetComponent<T>();
		RemoveComponent(component);
	}

	public void RemoveComponent(GameManagerComponent component)
	{
		_entitySystem.RemoveComponent(component);
	}

	public void OnTick(float dt)
	{
		foreach (GameManagerComponent component in _entitySystem.Components)
		{
			component.OnTick();
		}
		if (Game != null)
		{
			Game.OnTick(dt);
		}
	}

	public void OnGameNetworkBegin()
	{
		foreach (GameManagerComponent component in _entitySystem.Components)
		{
			component.OnGameNetworkBegin();
		}
		if (Game != null)
		{
			Game.OnGameNetworkBegin();
		}
	}

	public void OnGameNetworkEnd()
	{
		foreach (GameManagerComponent component in _entitySystem.Components)
		{
			component.OnGameNetworkEnd();
		}
		if (Game != null)
		{
			Game.OnGameNetworkEnd();
		}
	}

	public void OnPlayerConnect(VirtualPlayer peer)
	{
		foreach (GameManagerComponent component in _entitySystem.Components)
		{
			component.OnEarlyPlayerConnect(peer);
		}
		if (Game != null)
		{
			Game.OnEarlyPlayerConnect(peer);
		}
		foreach (GameManagerComponent component2 in _entitySystem.Components)
		{
			component2.OnPlayerConnect(peer);
		}
		if (Game != null)
		{
			Game.OnPlayerConnect(peer);
		}
	}

	public void OnPlayerDisconnect(VirtualPlayer peer)
	{
		foreach (GameManagerComponent component in _entitySystem.Components)
		{
			component.OnPlayerDisconnect(peer);
		}
		if (Game != null)
		{
			Game.OnPlayerDisconnect(peer);
		}
	}

	public virtual void OnGameEnd(Game game)
	{
		Current = null;
		Game = null;
	}

	protected virtual void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingStep, out GameManagerLoadingSteps nextStep)
	{
		nextStep = GameManagerLoadingSteps.None;
	}

	public bool DoLoadingForGameManager()
	{
		bool result = false;
		GameManagerLoadingSteps nextStep = GameManagerLoadingSteps.None;
		switch (_stepNo)
		{
		case GameManagerLoadingSteps.LoadingIsOver:
			result = true;
			break;
		case GameManagerLoadingSteps.PreInitializeZerothStep:
			DoLoadingForGameManager(GameManagerLoadingSteps.PreInitializeZerothStep, out nextStep);
			if (nextStep == GameManagerLoadingSteps.FirstInitializeFirstStep)
			{
				_stepNo++;
			}
			break;
		case GameManagerLoadingSteps.FirstInitializeFirstStep:
			DoLoadingForGameManager(GameManagerLoadingSteps.FirstInitializeFirstStep, out nextStep);
			if (nextStep == GameManagerLoadingSteps.WaitSecondStep)
			{
				_stepNo++;
			}
			break;
		case GameManagerLoadingSteps.WaitSecondStep:
			DoLoadingForGameManager(GameManagerLoadingSteps.WaitSecondStep, out nextStep);
			if (nextStep == GameManagerLoadingSteps.SecondInitializeThirdState)
			{
				_stepNo++;
			}
			break;
		case GameManagerLoadingSteps.SecondInitializeThirdState:
			DoLoadingForGameManager(GameManagerLoadingSteps.SecondInitializeThirdState, out nextStep);
			if (nextStep == GameManagerLoadingSteps.PostInitializeFourthState)
			{
				_stepNo++;
			}
			break;
		case GameManagerLoadingSteps.PostInitializeFourthState:
			DoLoadingForGameManager(GameManagerLoadingSteps.PostInitializeFourthState, out nextStep);
			if (nextStep == GameManagerLoadingSteps.FinishLoadingFifthStep)
			{
				_stepNo++;
			}
			break;
		case GameManagerLoadingSteps.FinishLoadingFifthStep:
			DoLoadingForGameManager(GameManagerLoadingSteps.FinishLoadingFifthStep, out nextStep);
			if (nextStep == GameManagerLoadingSteps.None)
			{
				_stepNo++;
				result = true;
			}
			break;
		}
		return result;
	}

	public virtual void OnLoadFinished()
	{
	}

	public virtual void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
	}

	public abstract void OnGameStart(Game game, IGameStarter gameStarter);

	public abstract void BeginGameStart(Game game);

	public abstract void OnNewCampaignStart(Game game, object starterObject);

	public abstract void OnAfterCampaignStart(Game game);

	public abstract void RegisterSubModuleObjects(bool isSavedCampaign);

	public abstract void AfterRegisterSubModuleObjects(bool isSavedCampaign);

	public abstract void OnGameInitializationFinished(Game game);

	public abstract void OnNewGameCreated(Game game, object initializerObject);

	public abstract void OnGameLoaded(Game game, object initializerObject);

	public abstract void OnAfterGameLoaded(Game game);

	public abstract void OnAfterGameInitializationFinished(Game game, object initializerObject);

	public abstract void RegisterSubModuleTypes();

	public virtual void InitializeSubModuleGameObjects(Game game)
	{
	}
}
