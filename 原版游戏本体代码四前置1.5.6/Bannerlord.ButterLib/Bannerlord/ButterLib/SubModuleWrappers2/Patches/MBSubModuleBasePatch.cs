using System.Collections.Generic;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.SubModuleWrappers2.Patches;

internal sealed class MBSubModuleBasePatch
{
	private static void OnSubModuleLoadPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnSubModuleLoad();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnSubModuleLoad();
		}
	}

	private static void OnSubModuleUnloadedPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnSubModuleUnloaded();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnSubModuleUnloaded();
		}
	}

	private static void OnApplicationTickPostfix(MBSubModuleBase __instance, ref float dt)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnApplicationTick(dt);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnApplicationTick(dt);
		}
	}

	private static void OnBeforeInitialModuleScreenSetAsRootPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnBeforeInitialModuleScreenSetAsRoot();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnBeforeInitialModuleScreenSetAsRoot();
		}
	}

	private static void OnGameStartPostfix(MBSubModuleBase __instance, Game game, IGameStarter gameStarterObject)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnGameStart(game, gameStarterObject);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnGameStart(game, gameStarterObject);
		}
	}

	private static void InitializeGameStarterPostfix(MBSubModuleBase __instance, Game game, IGameStarter starterObject)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.InitializeGameStarter(game, starterObject);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.InitializeGameStarter(game, starterObject);
		}
	}

	private static void OnServiceRegistrationPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnServiceRegistration();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnServiceRegistration();
		}
	}

	private static void DoLoadingPostfix(MBSubModuleBase __instance, ref bool __result, Game game)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				__result = mBSubModuleBaseListWrapper.DoLoading(game);
			}
		}
		else
		{
			__result = mBSubModuleBaseWrapper.DoLoading(game);
		}
	}

	private static void OnGameLoadedPostfix(MBSubModuleBase __instance, Game game, object initializerObject)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnGameLoaded(game, initializerObject);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnGameLoaded(game, initializerObject);
		}
	}

	private static void OnCampaignStartPostfix(MBSubModuleBase __instance, Game game, object starterObject)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnCampaignStart(game, starterObject);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnCampaignStart(game, starterObject);
		}
	}

	private static void BeginGameStartPostfix(MBSubModuleBase __instance, Game game)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.BeginGameStart(game);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.BeginGameStart(game);
		}
	}

	private static void OnGameEndPostfix(MBSubModuleBase __instance, Game game)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnGameEnd(game);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnGameEnd(game);
		}
	}

	private static void OnGameInitializationFinishedPostfix(MBSubModuleBase __instance, Game game)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnGameInitializationFinished(game);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnGameInitializationFinished(game);
		}
	}

	private static void OnBeforeMissionBehaviourInitializePostfix(MBSubModuleBase __instance, Mission mission)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnBeforeMissionBehaviorInitialize(mission);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnBeforeMissionBehaviorInitialize(mission);
		}
	}

	private static void OnMissionBehaviourInitializePostfix(MBSubModuleBase __instance, Mission mission)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnMissionBehaviorInitialize(mission);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnMissionBehaviorInitialize(mission);
		}
	}

	private static void OnMultiplayerGameStartPostfix(MBSubModuleBase __instance, Game game, object starterObject)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnMultiplayerGameStart(game, starterObject);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnMultiplayerGameStart(game, starterObject);
		}
	}

	private static void OnNewGameCreatedPostfix(MBSubModuleBase __instance, Game game, object initializerObject)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnNewGameCreated(game, initializerObject);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnNewGameCreated(game, initializerObject);
		}
	}

	private static void RegisterSubModuleObjectsPostfix(MBSubModuleBase __instance, bool isSavedCampaign)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.RegisterSubModuleObjects(isSavedCampaign);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.RegisterSubModuleObjects(isSavedCampaign);
		}
	}

	private static void AfterRegisterSubModuleObjectsPostfix(MBSubModuleBase __instance, bool isSavedCampaign)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.AfterRegisterSubModuleObjects(isSavedCampaign);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.AfterRegisterSubModuleObjects(isSavedCampaign);
		}
	}

	private static void OnAfterGameInitializationFinishedPostfix(MBSubModuleBase __instance, Game game, object starterObject)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnAfterGameInitializationFinished(game, starterObject);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnAfterGameInitializationFinished(game, starterObject);
		}
	}

	private static void OnConfigChangedPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnConfigChanged();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnConfigChanged();
		}
	}

	private static void OnInitialStatePostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnInitialState();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnInitialState();
		}
	}

	private static void AfterAsyncTickTickPostfix(MBSubModuleBase __instance, float dt)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.AfterAsyncTickTick(dt);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.AfterAsyncTickTick(dt);
		}
	}

	private static void OnNetworkTickPostfix(MBSubModuleBase __instance, float dt)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnNetworkTick(dt);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnNetworkTick(dt);
		}
	}

	private static void OnNewModuleLoadPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnNewModuleLoad();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnNewModuleLoad();
		}
	}

	private static void RegisterSubModuleTypesPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.RegisterSubModuleTypes();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.RegisterSubModuleTypes();
		}
	}

	private static void InitializeSubModuleGameObjectsPostfix(MBSubModuleBase __instance, Game game)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.InitializeSubModuleGameObjects(game);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.InitializeSubModuleGameObjects(game);
		}
	}

	private static void OnAfterGameLoadedPostfix(MBSubModuleBase __instance, Game game)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnAfterGameLoaded(game);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnAfterGameLoaded(game);
		}
	}

	private static void OnBeforeGameStartPostfix(MBSubModuleBase __instance, MBGameManager mbGameManager, List<string> disabledModules)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnBeforeGameStart(mbGameManager, disabledModules);
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnBeforeGameStart(mbGameManager, disabledModules);
		}
	}

	private static void OnSubModuleActivatedPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnSubModuleActivated();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnSubModuleActivated();
		}
	}

	private static void OnSubModuleDeactivatedPostfix(MBSubModuleBase __instance)
	{
		if (!(__instance is MBSubModuleBaseWrapper mBSubModuleBaseWrapper))
		{
			if (__instance is MBSubModuleBaseListWrapper mBSubModuleBaseListWrapper)
			{
				mBSubModuleBaseListWrapper.OnSubModuleDeactivated();
			}
		}
		else
		{
			mBSubModuleBaseWrapper.OnSubModuleDeactivated();
		}
	}

	internal static bool Enable(Harmony harmony)
	{
		return harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnSubModuleLoad"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnSubModuleLoadPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnSubModuleUnloaded"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnSubModuleUnloadedPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnApplicationTick"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnApplicationTickPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnBeforeInitialModuleScreenSetAsRoot"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnBeforeInitialModuleScreenSetAsRootPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnGameStart"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnGameStartPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "InitializeGameStarter"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "InitializeGameStarterPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnServiceRegistration", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnServiceRegistrationPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "DoLoading"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "DoLoadingPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnGameLoaded"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnGameLoadedPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnCampaignStart"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnCampaignStartPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "BeginGameStart"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "BeginGameStartPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnGameEnd"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnGameEndPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnGameInitializationFinished"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnGameInitializationFinishedPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnBeforeMissionBehaviorInitialize"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnBeforeMissionBehaviourInitializePostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnMissionBehaviorInitialize"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnMissionBehaviourInitializePostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnMultiplayerGameStart"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnMultiplayerGameStartPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnNewGameCreated"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnNewGameCreatedPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "RegisterSubModuleObjects"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "RegisterSubModuleObjectsPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "AfterRegisterSubModuleObjects"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "AfterRegisterSubModuleObjectsPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnAfterGameInitializationFinished"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnAfterGameInitializationFinishedPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnConfigChanged"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnConfigChangedPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnInitialState"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnInitialStatePostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "AfterAsyncTickTick"), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "AfterAsyncTickTickPostfix")) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnNetworkTick", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnNetworkTickPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnNewModuleLoad", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnNewModuleLoadPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "RegisterSubModuleTypes", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "RegisterSubModuleTypesPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "InitializeSubModuleGameObjects", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "InitializeSubModuleGameObjectsPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnAfterGameLoaded", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnAfterGameLoadedPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnBeforeGameStart", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnBeforeGameStartPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnSubModuleActivated", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnSubModuleActivatedPostfix", null, null, logErrorInTrace: false)) & harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBase), "OnSubModuleDeactivated", null, null, logErrorInTrace: false), null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MBSubModuleBasePatch), "OnSubModuleDeactivatedPostfix", null, null, logErrorInTrace: false));
	}
}
