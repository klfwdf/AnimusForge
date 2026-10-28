using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ModuleLoader.SubModuleWrappers;

public class MBSubModuleBaseListWrapper : MBSubModuleBase
{
	protected readonly List<MBSubModuleBaseWrapper> _subModules = new List<MBSubModuleBaseWrapper>();

	public IReadOnlyList<MBSubModuleBaseWrapper> SubModules => _subModules.AsReadOnly();

	public new virtual void OnSubModuleLoad()
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnSubModuleLoad();
		}
	}

	public new virtual void OnSubModuleUnloaded()
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnSubModuleUnloaded();
		}
	}

	public new virtual void OnApplicationTick(float dt)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnApplicationTick(dt);
		}
	}

	public new virtual void OnBeforeInitialModuleScreenSetAsRoot()
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnBeforeInitialModuleScreenSetAsRoot();
		}
	}

	public new virtual void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnGameStart(game, gameStarterObject);
		}
	}

	public new virtual void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.InitializeGameStarter(game, starterObject);
		}
	}

	public virtual void OnServiceRegistration()
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnServiceRegistration();
		}
	}

	public new virtual bool DoLoading(Game game)
	{
		return SubModules.All((MBSubModuleBaseWrapper subModule) => subModule.DoLoading(game));
	}

	public new virtual void OnGameLoaded(Game game, object initializerObject)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnGameLoaded(game, initializerObject);
		}
	}

	public new virtual void OnCampaignStart(Game game, object starterObject)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnCampaignStart(game, starterObject);
		}
	}

	public new virtual void BeginGameStart(Game game)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.BeginGameStart(game);
		}
	}

	public new virtual void OnGameEnd(Game game)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnGameEnd(game);
		}
	}

	public new virtual void OnGameInitializationFinished(Game game)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnGameInitializationFinished(game);
		}
	}

	public new virtual void OnBeforeMissionBehaviorInitialize(Mission mission)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnBeforeMissionBehaviorInitialize(mission);
		}
	}

	public new virtual void OnMissionBehaviorInitialize(Mission mission)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnMissionBehaviorInitialize(mission);
		}
	}

	public new virtual void OnMultiplayerGameStart(Game game, object starterObject)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnMultiplayerGameStart(game, starterObject);
		}
	}

	public new virtual void OnNewGameCreated(Game game, object initializerObject)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnNewGameCreated(game, initializerObject);
		}
	}

	public new virtual void RegisterSubModuleObjects(bool isSavedCampaign)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.RegisterSubModuleObjects(isSavedCampaign);
		}
	}

	public new virtual void AfterRegisterSubModuleObjects(bool isSavedCampaign)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.AfterRegisterSubModuleObjects(isSavedCampaign);
		}
	}

	public new virtual void OnAfterGameInitializationFinished(Game game, object starterObject)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnAfterGameInitializationFinished(game, starterObject);
		}
	}

	public new virtual void OnConfigChanged()
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnConfigChanged();
		}
	}

	public new virtual void OnInitialState()
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.OnInitialState();
		}
	}

	public new virtual void AfterAsyncTickTick(float dt)
	{
		foreach (MBSubModuleBaseWrapper subModule in SubModules)
		{
			subModule.AfterAsyncTickTick(dt);
		}
	}
}
