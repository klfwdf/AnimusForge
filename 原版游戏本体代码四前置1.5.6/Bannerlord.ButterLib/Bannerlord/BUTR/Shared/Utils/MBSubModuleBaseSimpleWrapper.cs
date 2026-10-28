using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.BUTR.Shared.Utils;

public class MBSubModuleBaseSimpleWrapper : MBSubModuleBase
{
	private delegate void OnSubModuleLoadDelegate();

	private delegate void OnSubModuleUnloadedDelegate();

	private delegate void OnBeforeInitialModuleScreenSetAsRootDelegate();

	private delegate void OnGameStartDelegate(Game game, IGameStarter gameStarterObject);

	private delegate void OnApplicationTickDelegate(float dt);

	private delegate void InitializeGameStarterDelegate(Game game, IGameStarter starterObject);

	private delegate void AfterRegisterSubModuleObjectsDelegate(bool isSavedCampaign);

	private delegate void AfterAsyncTickTickDelegate(float dt);

	private OnSubModuleLoadDelegate? OnSubModuleLoadInstance { get; }

	private OnSubModuleUnloadedDelegate? OnSubModuleUnloadedInstance { get; }

	private OnBeforeInitialModuleScreenSetAsRootDelegate? OnBeforeInitialModuleScreenSetAsRootInstance { get; }

	private OnGameStartDelegate? OnGameStartInstance { get; }

	private OnApplicationTickDelegate? OnApplicationTickInstance { get; }

	private InitializeGameStarterDelegate? InitializeGameStarterInstance { get; }

	private AfterRegisterSubModuleObjectsDelegate? AfterRegisterSubModuleObjectsInstance { get; }

	private AfterAsyncTickTickDelegate? AfterAsyncTickTickInstance { get; }

	public MBSubModuleBase SubModule { get; }

	public MBSubModuleBaseSimpleWrapper(MBSubModuleBase subModule)
	{
		SubModule = subModule;
		OnSubModuleLoadInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnSubModuleLoadDelegate, MBSubModuleBase>(subModule, "OnSubModuleLoad");
		OnSubModuleUnloadedInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnSubModuleUnloadedDelegate, MBSubModuleBase>(subModule, "OnSubModuleUnloaded");
		OnBeforeInitialModuleScreenSetAsRootInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnBeforeInitialModuleScreenSetAsRootDelegate, MBSubModuleBase>(subModule, "OnBeforeInitialModuleScreenSetAsRoot");
		OnGameStartInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnGameStartDelegate, MBSubModuleBase>(subModule, "OnGameStart");
		OnApplicationTickInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<OnApplicationTickDelegate, MBSubModuleBase>(subModule, "OnApplicationTick");
		InitializeGameStarterInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<InitializeGameStarterDelegate, MBSubModuleBase>(subModule, "InitializeGameStarter");
		AfterRegisterSubModuleObjectsInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<AfterRegisterSubModuleObjectsDelegate, MBSubModuleBase>(subModule, "AfterRegisterSubModuleObjects");
		AfterAsyncTickTickInstance = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<AfterAsyncTickTickDelegate, MBSubModuleBase>(subModule, "AfterAsyncTickTick");
	}

	protected override void OnSubModuleLoad()
	{
		OnSubModuleLoadInstance?.Invoke();
	}

	protected override void OnSubModuleUnloaded()
	{
		OnSubModuleUnloadedInstance?.Invoke();
	}

	protected override void OnApplicationTick(float dt)
	{
		OnApplicationTickInstance?.Invoke(dt);
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		OnBeforeInitialModuleScreenSetAsRootInstance?.Invoke();
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		OnGameStartInstance?.Invoke(game, gameStarterObject);
	}

	protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
		InitializeGameStarterInstance?.Invoke(game, starterObject);
	}

	protected override void AfterAsyncTickTick(float dt)
	{
		AfterAsyncTickTickInstance?.Invoke(dt);
	}

	public override bool DoLoading(Game game)
	{
		return SubModule.DoLoading(game);
	}

	public override void OnGameLoaded(Game game, object initializerObject)
	{
		SubModule.OnGameLoaded(game, initializerObject);
	}

	public override void OnCampaignStart(Game game, object starterObject)
	{
		SubModule.OnCampaignStart(game, starterObject);
	}

	public override void BeginGameStart(Game game)
	{
		SubModule.BeginGameStart(game);
	}

	public override void OnGameEnd(Game game)
	{
		SubModule.OnGameEnd(game);
	}

	public override void OnGameInitializationFinished(Game game)
	{
		SubModule.OnGameInitializationFinished(game);
	}

	public override void OnBeforeMissionBehaviorInitialize(Mission mission)
	{
		SubModule.OnBeforeMissionBehaviorInitialize(mission);
	}

	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		SubModule.OnMissionBehaviorInitialize(mission);
	}

	public override void OnMultiplayerGameStart(Game game, object starterObject)
	{
		SubModule.OnMultiplayerGameStart(game, starterObject);
	}

	public override void OnNewGameCreated(Game game, object initializerObject)
	{
		SubModule.OnNewGameCreated(game, initializerObject);
	}

	public override void RegisterSubModuleObjects(bool isSavedCampaign)
	{
		SubModule.RegisterSubModuleObjects(isSavedCampaign);
	}

	public override void OnAfterGameInitializationFinished(Game game, object starterObject)
	{
		SubModule.OnAfterGameInitializationFinished(game, starterObject);
	}

	public override void AfterRegisterSubModuleObjects(bool isSavedCampaign)
	{
		AfterRegisterSubModuleObjectsInstance?.Invoke(isSavedCampaign);
	}

	public override void OnConfigChanged()
	{
		SubModule.OnConfigChanged();
	}

	public override void OnInitialState()
	{
		SubModule.OnInitialState();
	}
}
