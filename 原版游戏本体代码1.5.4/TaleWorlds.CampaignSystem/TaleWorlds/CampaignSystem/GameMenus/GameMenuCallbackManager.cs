using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.GameMenus;

public class GameMenuCallbackManager
{
	private Dictionary<string, GameMenuInitializationHandlerDelegate> _gameMenuInitializationHandlers;

	private Dictionary<string, Dictionary<string, GameMenuEventHandlerDelegate>> _eventHandlers;

	public static GameMenuCallbackManager Instance => Campaign.Current.GameMenuCallbackManager;

	public GameMenuCallbackManager()
	{
		FillInitializationHandlers();
		FillEventHandlers();
	}

	private void FillInitializationHandlers()
	{
		_gameMenuInitializationHandlers = new Dictionary<string, GameMenuInitializationHandlerDelegate>();
		Assembly assembly = typeof(GameMenuInitializationHandler).Assembly;
		FillInitializationHandlerWith(assembly);
		Assembly[] assemblies = GetAssemblies();
		foreach (Assembly assembly2 in assemblies)
		{
			FillInitializationHandlerWith(assembly2);
		}
	}

	private static Assembly[] GetAssemblies()
	{
		return typeof(GameMenu).Assembly.GetActiveReferencingGameAssembliesSafe();
	}

	public void OnGameLoad()
	{
		FillInitializationHandlers();
		FillEventHandlers();
	}

	private void FillInitializationHandlerWith(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			MethodInfo[] methods = item.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo method in methods)
			{
				object[] customAttributesSafe = method.GetCustomAttributesSafe(typeof(GameMenuInitializationHandler), inherit: false);
				if (customAttributesSafe == null || customAttributesSafe.Length == 0)
				{
					continue;
				}
				object[] array = customAttributesSafe;
				for (int j = 0; j < array.Length; j++)
				{
					GameMenuInitializationHandler gameMenuInitializationHandler = (GameMenuInitializationHandler)array[j];
					GameMenuInitializationHandlerDelegate value = Delegate.CreateDelegate(typeof(GameMenuInitializationHandlerDelegate), method) as GameMenuInitializationHandlerDelegate;
					if (!_gameMenuInitializationHandlers.ContainsKey(gameMenuInitializationHandler.MenuId))
					{
						_gameMenuInitializationHandlers.Add(gameMenuInitializationHandler.MenuId, value);
					}
					else
					{
						_gameMenuInitializationHandlers[gameMenuInitializationHandler.MenuId] = value;
					}
				}
			}
		}
	}

	private void FillEventHandlers()
	{
		_eventHandlers = new Dictionary<string, Dictionary<string, GameMenuEventHandlerDelegate>>();
		Assembly assembly = typeof(GameMenuEventHandler).Assembly;
		FillEventHandlersWith(assembly);
		Assembly[] assemblies = GetAssemblies();
		foreach (Assembly assembly2 in assemblies)
		{
			FillEventHandlersWith(assembly2);
		}
	}

	private void FillEventHandlersWith(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			MethodInfo[] methods = item.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo method in methods)
			{
				object[] customAttributesSafe = method.GetCustomAttributesSafe(typeof(GameMenuEventHandler), inherit: false);
				if (customAttributesSafe == null || customAttributesSafe.Length == 0)
				{
					continue;
				}
				object[] array = customAttributesSafe;
				for (int j = 0; j < array.Length; j++)
				{
					GameMenuEventHandler gameMenuEventHandler = (GameMenuEventHandler)array[j];
					GameMenuEventHandlerDelegate value = Delegate.CreateDelegate(typeof(GameMenuEventHandlerDelegate), method) as GameMenuEventHandlerDelegate;
					if (!_eventHandlers.TryGetValue(gameMenuEventHandler.MenuId, out var value2))
					{
						value2 = new Dictionary<string, GameMenuEventHandlerDelegate>();
						_eventHandlers.Add(gameMenuEventHandler.MenuId, value2);
					}
					if (!value2.ContainsKey(gameMenuEventHandler.MenuOptionId))
					{
						value2.Add(gameMenuEventHandler.MenuOptionId, value);
					}
					else
					{
						value2[gameMenuEventHandler.MenuOptionId] = value;
					}
				}
			}
		}
	}

	public void InitializeState(string menuId, MenuContext state)
	{
		GameMenuInitializationHandlerDelegate value = null;
		if (_gameMenuInitializationHandlers.TryGetValue(menuId, out value))
		{
			MenuCallbackArgs args = new MenuCallbackArgs(state, null);
			value(args);
		}
	}

	public void OnConsequence(string menuId, GameMenuOption gameMenuOption, MenuContext state)
	{
		Dictionary<string, GameMenuEventHandlerDelegate> value = null;
		if (_eventHandlers.TryGetValue(menuId, out value))
		{
			GameMenuEventHandlerDelegate value2 = null;
			if (value.TryGetValue(gameMenuOption.IdString, out value2))
			{
				MenuCallbackArgs args = new MenuCallbackArgs(state, gameMenuOption.Text);
				value2(args);
			}
		}
	}

	public TextObject GetMenuOptionTooltip(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.GetMenuOptionTooltip(menuItemNumber);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetMenuOptionText");
		}
		return null;
	}

	public TextObject GetVirtualMenuOptionTooltip(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null)
		{
			int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
			if (virtualMenuItemIndex < num)
			{
				return GetMenuOptionTooltip(menuContext, 0);
			}
			return GetMenuOptionTooltip(menuContext, virtualMenuItemIndex + 1 - num);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return null;
	}

	public TextObject GetVirtualMenuOptionText(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null)
		{
			int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
			if (virtualMenuItemIndex < num)
			{
				return GetMenuOptionText(menuContext, 0);
			}
			return GetMenuOptionText(menuContext, virtualMenuItemIndex + 1 - num);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return null;
	}

	public TextObject GetMenuOptionText(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.GetMenuOptionText(menuItemNumber);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetMenuOptionText");
		}
		return null;
	}
}
