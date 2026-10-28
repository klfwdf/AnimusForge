using System.Collections.Generic;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.SubModuleWrappers2;

public class MBSubModuleBaseWrapper : MBSubModuleBase
{
	private delegate void OnSubModuleLoadDelegate();

	private delegate void OnSubModuleUnloadedDelegate();

	private delegate void OnBeforeInitialModuleScreenSetAsRootDelegate();

	private delegate void OnGameStartDelegate(Game game, IGameStarter gameStarterObject);

	private delegate void OnApplicationTickDelegate(float dt);

	private delegate void InitializeGameStarterDelegate(Game game, IGameStarter starterObject);

	private delegate void OnServiceRegistrationDelegate();

	private delegate void OnConfigChangedDelegate();

	private delegate void OnGameLoadedDelegate(Game game, object initializerObject);

	private delegate void OnNewGameCreatedDelegate(Game game, object initializerObject);

	private delegate void BeginGameStartDelegate(Game game);

	private delegate void OnCampaignStartDelegate(Game game, object starterObject);

	private delegate void RegisterSubModuleObjectsDelegate(bool isSavedCampaign);

	private delegate void AfterRegisterSubModuleObjectsDelegate(bool isSavedCampaign);

	private delegate void OnMultiplayerGameStartDelegate(Game game, object starterObject);

	private delegate void OnGameInitializationFinishedDelegate(Game game);

	private delegate void OnAfterGameInitializationFinishedDelegate(Game game, object starterObject);

	private delegate bool DoLoadingDelegate(Game game);

	private delegate void OnGameEndDelegate(Game game);

	private delegate void OnBeforeMissionBehaviourInitializeDelegate(Mission mission);

	private delegate void OnMissionBehaviourInitializeDelegate(Mission mission);

	private delegate void OnInitialStateDelegate();

	private delegate void AfterAsyncTickTickDelegate(float dt);

	private delegate void OnNetworkTickDelegate(float dt);

	private delegate void OnNewModuleLoadDelegate();

	private delegate void RegisterSubModuleTypesDelegate();

	private delegate void InitializeSubModuleGameObjectsDelegate(Game game);

	private delegate void OnAfterGameLoadedDelegate(Game game);

	private delegate void OnBeforeGameStartDelegate(MBGameManager mbGameManager, List<string> disabledModules);

	private delegate void OnSubModuleActivatedDelegate();

	private delegate void OnSubModuleDeactivatedDelegate();

	private OnSubModuleLoadDelegate? OnSubModuleLoadInstance { get; }

	private OnSubModuleUnloadedDelegate? OnSubModuleUnloadedInstance { get; }

	private OnBeforeInitialModuleScreenSetAsRootDelegate? OnBeforeInitialModuleScreenSetAsRootInstance { get; }

	private OnGameStartDelegate? OnGameStartInstance { get; }

	private OnApplicationTickDelegate? OnApplicationTickInstance { get; }

	private InitializeGameStarterDelegate? InitializeGameStarterInstance { get; }

	private OnServiceRegistrationDelegate? OnServiceRegistrationInstance { get; }

	private OnConfigChangedDelegate? OnConfigChangedInstance { get; }

	private OnGameLoadedDelegate? OnGameLoadedInstance { get; }

	private OnNewGameCreatedDelegate? OnNewGameCreatedInstance { get; }

	private BeginGameStartDelegate? BeginGameStartInstance { get; }

	private OnCampaignStartDelegate? OnCampaignStartInstance { get; }

	private RegisterSubModuleObjectsDelegate? RegisterSubModuleObjectsInstance { get; }

	private AfterRegisterSubModuleObjectsDelegate? AfterRegisterSubModuleObjectsInstance { get; }

	private OnMultiplayerGameStartDelegate? OnMultiplayerGameStartInstance { get; }

	private OnGameInitializationFinishedDelegate? OnGameInitializationFinishedInstance { get; }

	private OnAfterGameInitializationFinishedDelegate? OnAfterGameInitializationFinishedInstance { get; }

	private DoLoadingDelegate? DoLoadingInstance { get; }

	private OnGameEndDelegate? OnGameEndInstance { get; }

	private OnBeforeMissionBehaviourInitializeDelegate? OnBeforeMissionBehaviourInitializeInstance { get; }

	private OnMissionBehaviourInitializeDelegate? OnMissionBehaviourInitializeInstance { get; }

	private OnInitialStateDelegate? OnInitialStateInstance { get; }

	private AfterAsyncTickTickDelegate? AfterAsyncTickTickInstance { get; }

	private OnNetworkTickDelegate? OnNetworkTickInstance { get; }

	private OnNewModuleLoadDelegate? OnNewModuleLoadInstance { get; }

	private RegisterSubModuleTypesDelegate? RegisterSubModuleTypesInstance { get; }

	private InitializeSubModuleGameObjectsDelegate? InitializeSubModuleGameObjectsInstance { get; }

	private OnAfterGameLoadedDelegate? OnAfterGameLoadedInstance { get; }

	private OnBeforeGameStartDelegate? OnBeforeGameStartInstance { get; }

	private OnSubModuleActivatedDelegate? OnSubModuleActivatedInstance { get; }

	private OnSubModuleDeactivatedDelegate? OnSubModuleDeactivatedInstance { get; }

	public MBSubModuleBase SubModule { get; }

	public MBSubModuleBaseWrapper(MBSubModuleBase subModule)
	{
		SubModule = subModule;
		OnSubModuleLoadInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnSubModuleLoadDelegate, MBSubModuleBase>(subModule, "OnSubModuleLoad");
		OnSubModuleUnloadedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnSubModuleUnloadedDelegate, MBSubModuleBase>(subModule, "OnSubModuleUnloaded");
		OnBeforeInitialModuleScreenSetAsRootInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnBeforeInitialModuleScreenSetAsRootDelegate, MBSubModuleBase>(subModule, "OnBeforeInitialModuleScreenSetAsRoot");
		OnGameStartInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnGameStartDelegate, MBSubModuleBase>(subModule, "OnGameStart");
		OnApplicationTickInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnApplicationTickDelegate, MBSubModuleBase>(subModule, "OnApplicationTick");
		InitializeGameStarterInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<InitializeGameStarterDelegate, MBSubModuleBase>(subModule, "InitializeGameStarter");
		OnServiceRegistrationInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnServiceRegistrationDelegate, MBSubModuleBase>(subModule, "OnServiceRegistration", null, null, logErrorInTrace: false);
		OnConfigChangedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnConfigChangedDelegate, MBSubModuleBase>(subModule, "OnConfigChanged");
		OnGameLoadedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnGameLoadedDelegate, MBSubModuleBase>(subModule, "OnGameLoaded");
		OnNewGameCreatedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnNewGameCreatedDelegate, MBSubModuleBase>(subModule, "OnNewGameCreated");
		BeginGameStartInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<BeginGameStartDelegate, MBSubModuleBase>(subModule, "BeginGameStart");
		OnCampaignStartInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnCampaignStartDelegate, MBSubModuleBase>(subModule, "OnCampaignStart");
		RegisterSubModuleObjectsInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<RegisterSubModuleObjectsDelegate, MBSubModuleBase>(subModule, "RegisterSubModuleObjects");
		AfterRegisterSubModuleObjectsInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<AfterRegisterSubModuleObjectsDelegate, MBSubModuleBase>(subModule, "AfterRegisterSubModuleObjects");
		OnMultiplayerGameStartInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnMultiplayerGameStartDelegate, MBSubModuleBase>(subModule, "OnMultiplayerGameStart");
		OnGameInitializationFinishedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnGameInitializationFinishedDelegate, MBSubModuleBase>(subModule, "OnGameInitializationFinished");
		OnAfterGameInitializationFinishedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnAfterGameInitializationFinishedDelegate, MBSubModuleBase>(subModule, "OnAfterGameInitializationFinished");
		DoLoadingInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<DoLoadingDelegate, MBSubModuleBase>(subModule, "DoLoading");
		OnGameEndInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnGameEndDelegate, MBSubModuleBase>(subModule, "OnGameEnd");
		OnBeforeMissionBehaviourInitializeInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnBeforeMissionBehaviourInitializeDelegate, MBSubModuleBase>(subModule, "OnBeforeMissionBehaviorInitialize");
		OnMissionBehaviourInitializeInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnMissionBehaviourInitializeDelegate, MBSubModuleBase>(subModule, "OnMissionBehaviorInitialize");
		OnInitialStateInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnInitialStateDelegate, MBSubModuleBase>(subModule, "OnInitialState");
		AfterAsyncTickTickInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<AfterAsyncTickTickDelegate, MBSubModuleBase>(subModule, "AfterAsyncTickTick");
		OnNetworkTickInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnNetworkTickDelegate, MBSubModuleBase>(subModule, "OnNetworkTick", null, null, logErrorInTrace: false);
		OnNewModuleLoadInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnNewModuleLoadDelegate, MBSubModuleBase>(subModule, "OnNewModuleLoad", null, null, logErrorInTrace: false);
		RegisterSubModuleTypesInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<RegisterSubModuleTypesDelegate, MBSubModuleBase>(subModule, "RegisterSubModuleTypes", null, null, logErrorInTrace: false);
		InitializeSubModuleGameObjectsInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<InitializeSubModuleGameObjectsDelegate, MBSubModuleBase>(subModule, "InitializeSubModuleGameObjects", null, null, logErrorInTrace: false);
		OnAfterGameLoadedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnAfterGameLoadedDelegate, MBSubModuleBase>(subModule, "OnAfterGameLoaded", null, null, logErrorInTrace: false);
		OnBeforeGameStartInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnBeforeGameStartDelegate, MBSubModuleBase>(subModule, "OnBeforeGameStart", null, null, logErrorInTrace: false);
		OnSubModuleActivatedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnSubModuleActivatedDelegate, MBSubModuleBase>(subModule, "OnSubModuleActivated", null, null, logErrorInTrace: false);
		OnSubModuleDeactivatedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnSubModuleDeactivatedDelegate, MBSubModuleBase>(subModule, "OnSubModuleDeactivated", null, null, logErrorInTrace: false);
	}

	public new virtual void OnSubModuleLoad()
	{
		OnSubModuleLoadInstance?.Invoke();
	}

	public new virtual void OnSubModuleUnloaded()
	{
		OnSubModuleUnloadedInstance?.Invoke();
	}

	public new virtual void OnApplicationTick(float dt)
	{
		OnApplicationTickInstance?.Invoke(dt);
	}

	public new virtual void OnBeforeInitialModuleScreenSetAsRoot()
	{
		OnBeforeInitialModuleScreenSetAsRootInstance?.Invoke();
	}

	public new virtual void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		OnGameStartInstance?.Invoke(game, gameStarterObject);
	}

	public new virtual void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
		InitializeGameStarterInstance?.Invoke(game, starterObject);
	}

	public virtual void OnServiceRegistration()
	{
		OnServiceRegistrationInstance?.Invoke();
	}

	public new virtual bool DoLoading(Game game)
	{
		return DoLoadingInstance?.Invoke(game) ?? false;
	}

	public new virtual void OnGameLoaded(Game game, object initializerObject)
	{
		OnGameLoadedInstance?.Invoke(game, initializerObject);
	}

	public new virtual void OnCampaignStart(Game game, object starterObject)
	{
		OnCampaignStartInstance?.Invoke(game, starterObject);
	}

	public new virtual void BeginGameStart(Game game)
	{
		BeginGameStartInstance?.Invoke(game);
	}

	public new virtual void OnGameEnd(Game game)
	{
		OnGameEndInstance?.Invoke(game);
	}

	public new virtual void OnGameInitializationFinished(Game game)
	{
		OnGameInitializationFinishedInstance?.Invoke(game);
	}

	public new virtual void OnBeforeMissionBehaviorInitialize(Mission mission)
	{
		OnBeforeMissionBehaviourInitializeInstance?.Invoke(mission);
	}

	public new virtual void OnMissionBehaviorInitialize(Mission mission)
	{
		OnMissionBehaviourInitializeInstance?.Invoke(mission);
	}

	public new virtual void OnMultiplayerGameStart(Game game, object starterObject)
	{
		OnMultiplayerGameStartInstance?.Invoke(game, starterObject);
	}

	public new virtual void OnNewGameCreated(Game game, object initializerObject)
	{
		OnNewGameCreatedInstance?.Invoke(game, initializerObject);
	}

	public new virtual void RegisterSubModuleObjects(bool isSavedCampaign)
	{
		RegisterSubModuleObjectsInstance?.Invoke(isSavedCampaign);
	}

	public new virtual void AfterRegisterSubModuleObjects(bool isSavedCampaign)
	{
		AfterRegisterSubModuleObjectsInstance?.Invoke(isSavedCampaign);
	}

	public new virtual void OnAfterGameInitializationFinished(Game game, object starterObject)
	{
		OnAfterGameInitializationFinishedInstance?.Invoke(game, starterObject);
	}

	public new virtual void OnConfigChanged()
	{
		OnConfigChangedInstance?.Invoke();
	}

	public new virtual void OnInitialState()
	{
		OnInitialStateInstance?.Invoke();
	}

	public new virtual void AfterAsyncTickTick(float dt)
	{
		AfterAsyncTickTickInstance?.Invoke(dt);
	}

	public new virtual void OnNetworkTick(float dt)
	{
		OnNetworkTickInstance?.Invoke(dt);
	}

	public new virtual void OnNewModuleLoad()
	{
		OnNewModuleLoadInstance?.Invoke();
	}

	public new virtual void RegisterSubModuleTypes()
	{
		RegisterSubModuleTypesInstance?.Invoke();
	}

	public new virtual void InitializeSubModuleGameObjects(Game game)
	{
		InitializeSubModuleGameObjectsInstance?.Invoke(game);
	}

	public new virtual void OnAfterGameLoaded(Game game)
	{
		OnAfterGameLoadedInstance?.Invoke(game);
	}

	public new virtual void OnBeforeGameStart(MBGameManager mbGameManager, List<string> disabledModules)
	{
		OnBeforeGameStartInstance?.Invoke(mbGameManager, disabledModules);
	}

	public new virtual void OnSubModuleActivated()
	{
		OnSubModuleActivatedInstance?.Invoke();
	}

	public new virtual void OnSubModuleDeactivated()
	{
		OnSubModuleDeactivatedInstance?.Invoke();
	}
}
