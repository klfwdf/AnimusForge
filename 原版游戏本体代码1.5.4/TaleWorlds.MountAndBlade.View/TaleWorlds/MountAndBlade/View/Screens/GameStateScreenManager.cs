using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens;

public class GameStateScreenManager : IGameStateManagerListener
{
	private Dictionary<Type, MBList<Type>> _screenTypes;

	private GameStateManager GameStateManager => GameStateManager.Current;

	public GameStateScreenManager()
	{
		_screenTypes = new Dictionary<Type, MBList<Type>>();
		CollectTypes();
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	internal void CollectTypes()
	{
		_screenTypes.Clear();
		Assembly assembly = typeof(GameStateScreen).Assembly;
		Assembly[] referencingAssembliesSafe = assembly.GetReferencingAssembliesSafe();
		CheckAssemblyScreens(assembly);
		Assembly[] array = referencingAssembliesSafe;
		foreach (Assembly assembly2 in array)
		{
			CheckAssemblyScreens(assembly2);
		}
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.ForceVSyncInMenus)
		{
			if (!BannerlordConfig.ForceVSyncInMenus)
			{
				Utilities.SetForceVsync(value: false);
			}
			else if (GameStateManager.ActiveState.IsMenuState)
			{
				Utilities.SetForceVsync(value: true);
			}
		}
	}

	private void CheckAssemblyScreens(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			object[] customAttributesSafe = item.GetCustomAttributesSafe(typeof(GameStateScreen), inherit: false);
			if (customAttributesSafe == null || customAttributesSafe.Length == 0)
			{
				continue;
			}
			object[] array = customAttributesSafe;
			for (int i = 0; i < array.Length; i++)
			{
				GameStateScreen gameStateScreen = (GameStateScreen)array[i];
				if (_screenTypes.ContainsKey(gameStateScreen.GameStateType))
				{
					_screenTypes[gameStateScreen.GameStateType].Add(item);
					continue;
				}
				_screenTypes.Add(gameStateScreen.GameStateType, new MBList<Type> { item });
			}
		}
	}

	public ScreenBase CreateScreen(GameState state)
	{
		Type type = null;
		if (_screenTypes.TryGetValue(state.GetType(), out var value))
		{
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].Assembly))
				{
					type = value[num];
					break;
				}
			}
			if (type != null)
			{
				return Activator.CreateInstance(type, state) as ScreenBase;
			}
			Debug.FailedAssert($"Failed to create game state screen for state: {state.GetType()}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Screens\\GameStateScreenManager.cs", "CreateScreen", 108);
		}
		return null;
	}

	public void BuildScreens()
	{
		int num = 0;
		foreach (GameState gameState in GameStateManager.GameStates)
		{
			ScreenBase screenBase = CreateScreen(gameState);
			gameState.RegisterListener(screenBase as IGameStateListener);
			if (screenBase != null)
			{
				if (num == 0)
				{
					ScreenManager.CleanAndPushScreen(screenBase);
				}
				else
				{
					ScreenManager.PushScreen(screenBase);
				}
			}
			num++;
		}
	}

	void IGameStateManagerListener.OnCreateState(GameState gameState)
	{
		ScreenBase screenBase = CreateScreen(gameState);
		if (screenBase == null)
		{
			Debug.FailedAssert($"Create screen for {gameState.GetName()} returned null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Screens\\GameStateScreenManager.cs", "OnCreateState", 145);
		}
		gameState.RegisterListener(screenBase as IGameStateListener);
	}

	void IGameStateManagerListener.OnPushState(GameState gameState, bool isTopGameState)
	{
		if (!gameState.IsMenuState)
		{
			Utilities.ClearOldResourcesAndObjects();
		}
		if (gameState.IsMenuState && BannerlordConfig.ForceVSyncInMenus)
		{
			Utilities.SetForceVsync(value: true);
		}
		else if (!gameState.IsMenuState)
		{
			Utilities.SetForceVsync(value: false);
		}
		ScreenBase listenerOfType;
		if ((listenerOfType = gameState.GetListenerOfType<ScreenBase>()) != null)
		{
			if (isTopGameState)
			{
				ScreenManager.CleanAndPushScreen(listenerOfType);
			}
			else
			{
				ScreenManager.PushScreen(listenerOfType);
			}
		}
		ThumbnailCacheManager.Current.ClearUnusedCache();
	}

	void IGameStateManagerListener.OnPopState(GameState gameState)
	{
		if (gameState.IsMenuState && BannerlordConfig.ForceVSyncInMenus)
		{
			Utilities.SetForceVsync(value: false);
		}
		if (GameStateManager.ActiveState != null && GameStateManager.ActiveState.IsMenuState && BannerlordConfig.ForceVSyncInMenus)
		{
			Utilities.SetForceVsync(value: true);
		}
		ScreenManager.PopScreen();
		if (!gameState.IsMenuState)
		{
			Utilities.ClearOldResourcesAndObjects();
		}
		ThumbnailCacheManager.Current.ClearUnusedCache();
	}

	void IGameStateManagerListener.OnCleanStates()
	{
		ScreenManager.CleanScreens();
	}

	void IGameStateManagerListener.OnSavedGameLoadFinished()
	{
		BuildScreens();
	}
}
