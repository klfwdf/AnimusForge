using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus;

public class GameMenuManager
{
	private Dictionary<string, GameMenu> _gameMenus;

	public int PreviouslySelectedGameMenuItem = -1;

	public Location NextLocation;

	public Location PreviousLocation;

	public List<Location> MenuLocations = new List<Location>();

	public object PreviouslySelectedGameMenuObject;

	public string NextGameMenuId { get; private set; }

	public GameMenu NextMenu
	{
		get
		{
			_gameMenus.TryGetValue(NextGameMenuId, out var value);
			return value;
		}
	}

	public GameMenuManager()
	{
		NextGameMenuId = null;
		_gameMenus = new Dictionary<string, GameMenu>();
	}

	public void SetNextMenu(string name)
	{
		NextGameMenuId = name;
	}

	public void ExitToLast()
	{
		if (Campaign.Current.CurrentMenuContext != null)
		{
			Game.Current.GameStateManager.LastOrDefault<MapState>().ExitMenuMode();
		}
	}

	internal object GetSelectedRepeatableObject(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.LastSelectedMenuObject;
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetSelectedObject");
		}
		return 0;
	}

	internal object ObjectGetCurrentRepeatableObject(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.CurrentRepeatableObject;
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not return CurrentRepeatableIndex");
		}
		return null;
	}

	public void SetCurrentRepeatableIndex(MenuContext menuContext, int index)
	{
		if (menuContext.GameMenu != null)
		{
			menuContext.GameMenu.CurrentRepeatableIndex = index;
		}
		else if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run SetCurrentRepeatableIndex");
		}
	}

	public bool GetMenuOptionConditionsHold(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu != null)
		{
			if (Game.Current == null)
			{
				throw new MBNullParameterException("Game");
			}
			return menuContext.GameMenu.GetMenuOptionConditionsHold(Game.Current, menuContext, menuItemNumber);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetMenuOptionConditionsHold");
		}
		return false;
	}

	public void RefreshMenuOptions(MenuContext menuContext)
	{
		if (menuContext.GameMenu == null)
		{
			Debug.FailedAssert("Current game menu empty, can not run RefreshMenuOptions", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenuManager.cs", "RefreshMenuOptions", 143);
		}
		else if (Game.Current == null)
		{
			Debug.FailedAssert("Game is null during RefreshMenuOptions", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenuManager.cs", "RefreshMenuOptions", 148);
		}
		else
		{
			menuContext.Handler.OnMenuRefresh();
		}
	}

	public void RefreshMenuOptionConditions(MenuContext menuContext)
	{
		if (menuContext.GameMenu == null)
		{
			Debug.FailedAssert("Current game menu empty, can not run RefreshMenuOptionConditions", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenuManager.cs", "RefreshMenuOptionConditions", 161);
			return;
		}
		if (Game.Current == null)
		{
			Debug.FailedAssert("Game is null during RefreshMenuOptionConditions", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenuManager.cs", "RefreshMenuOptionConditions", 166);
			return;
		}
		int virtualMenuOptionAmount = Campaign.Current.GameMenuManager.GetVirtualMenuOptionAmount(menuContext);
		for (int i = 0; i < virtualMenuOptionAmount; i++)
		{
			GetMenuOptionConditionsHold(menuContext, i);
		}
	}

	public string GetMenuOptionIdString(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu == null)
		{
			Debug.FailedAssert("Current game menu empty, can not run GetMenuOptionIdString", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenuManager.cs", "GetMenuOptionIdString", 183);
			return "";
		}
		return menuContext.GameMenu.GetMenuOptionIdString(menuItemNumber);
	}

	internal bool GetMenuOptionIsLeave(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.GetMenuOptionIsLeave(menuItemNumber);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetMenuOptionText");
		}
		return false;
	}

	public void RunConsequencesOfMenuOption(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu != null)
		{
			if (Game.Current == null)
			{
				throw new MBNullParameterException("Game");
			}
			menuContext.GameMenu.RunMenuOptionConsequence(menuContext, menuItemNumber);
		}
		else if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run RunConsequencesOfMenuOption");
		}
	}

	internal void SetRepeatObjectList(MenuContext menuContext, IEnumerable<object> list)
	{
		if (menuContext.GameMenu != null)
		{
			menuContext.GameMenu.SetMenuRepeatObjects(list);
		}
		else
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenuManager.cs", "SetRepeatObjectList", 237);
		}
	}

	public TextObject GetVirtualMenuOptionTooltip(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null && !menuContext.GameMenu.IsEmpty)
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

	public GameMenu.MenuOverlayType GetMenuOverlayType(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.OverlayType;
		}
		return GameMenu.MenuOverlayType.SettlementWithCharacters;
	}

	public TextObject GetVirtualMenuOptionText(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null && !menuContext.GameMenu.IsEmpty)
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

	public GameMenuOption GetVirtualGameMenuOption(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetGameMenuOption");
		}
		int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
		if (virtualMenuItemIndex < num)
		{
			return menuContext.GameMenu.GetGameMenuOption(0);
		}
		return menuContext.GameMenu.GetGameMenuOption(virtualMenuItemIndex + 1 - num);
	}

	public TextObject GetVirtualMenuOptionText2(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null && !menuContext.GameMenu.IsEmpty)
		{
			int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
			if (virtualMenuItemIndex < num)
			{
				return GetMenuOptionText2(menuContext, 0);
			}
			return GetMenuOptionText2(menuContext, virtualMenuItemIndex + 1 - num);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return null;
	}

	public float GetVirtualMenuProgress(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.Progress;
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return 0f;
	}

	public GameMenu.MenuAndOptionType GetVirtualMenuAndOptionType(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.Type;
		}
		return GameMenu.MenuAndOptionType.RegularMenuOption;
	}

	public bool GetVirtualMenuIsWaitActive(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.IsWaitActive;
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return false;
	}

	public float GetVirtualMenuTargetWaitHours(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.TargetWaitHours;
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return 0f;
	}

	public bool GetVirtualMenuOptionIsEnabled(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null && !menuContext.GameMenu.IsEmpty)
		{
			int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
			if (virtualMenuItemIndex < num)
			{
				return menuContext.GameMenu.MenuOptions.ElementAt(0).IsEnabled;
			}
			return menuContext.GameMenu.MenuOptions.ElementAt(virtualMenuItemIndex + 1 - num).IsEnabled;
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return false;
	}

	public int GetVirtualMenuOptionAmount(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			int count = menuContext.GameMenu.MenuRepeatObjects.Count;
			int menuItemAmount = menuContext.GameMenu.MenuItemAmount;
			if (count == 0)
			{
				return menuItemAmount;
			}
			return menuItemAmount - 1 + count;
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionAmount");
		}
		return 0;
	}

	public bool GetVirtualMenuOptionIsLeave(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null && !menuContext.GameMenu.IsEmpty)
		{
			int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
			if (virtualMenuItemIndex < num)
			{
				return GetMenuOptionIsLeave(menuContext, 0);
			}
			return GetMenuOptionIsLeave(menuContext, virtualMenuItemIndex + 1 - num);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionText");
		}
		return false;
	}

	public GameMenuOption GetLeaveMenuOption(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.GetLeaveMenuOption(Game.Current, menuContext);
		}
		return null;
	}

	internal void RunConsequenceOfVirtualMenuOption(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null)
		{
			int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
			if (virtualMenuItemIndex < num)
			{
				if (menuContext.GameMenu.MenuRepeatObjects.Count > 0)
				{
					menuContext.GameMenu.LastSelectedMenuObject = menuContext.GameMenu.MenuRepeatObjects[virtualMenuItemIndex];
				}
				RunConsequencesOfMenuOption(menuContext, 0);
			}
			else
			{
				RunConsequencesOfMenuOption(menuContext, virtualMenuItemIndex + 1 - num);
			}
		}
		else if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run RunVirtualMenuItemConsequence");
		}
	}

	public bool GetVirtualMenuOptionConditionsHold(MenuContext menuContext, int virtualMenuItemIndex)
	{
		if (menuContext.GameMenu != null && !menuContext.GameMenu.IsEmpty)
		{
			int num = ((menuContext.GameMenu.MenuRepeatObjects.Count <= 0) ? 1 : menuContext.GameMenu.MenuRepeatObjects.Count);
			if (virtualMenuItemIndex < num)
			{
				return GetMenuOptionConditionsHold(menuContext, 0);
			}
			return GetMenuOptionConditionsHold(menuContext, virtualMenuItemIndex + 1 - num);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetVirtualMenuOptionConditionsHold");
		}
		return false;
	}

	public void OnFrameTick(MenuContext menuContext, float dt)
	{
		if (menuContext.GameMenu != null)
		{
			menuContext.GameMenu.RunOnTick(menuContext, dt);
		}
	}

	public TextObject GetMenuText(MenuContext menuContext)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.GetText();
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetMenuText");
		}
		return null;
	}

	private TextObject GetMenuOptionText(MenuContext menuContext, int menuItemNumber)
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

	private TextObject GetMenuOptionText2(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu != null)
		{
			return menuContext.GameMenu.GetMenuOptionText2(menuItemNumber);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetMenuOptionText");
		}
		return null;
	}

	private TextObject GetMenuOptionTooltip(MenuContext menuContext, int menuItemNumber)
	{
		if (menuContext.GameMenu != null && !menuContext.GameMenu.IsEmpty)
		{
			return menuContext.GameMenu.GetMenuOptionTooltip(menuItemNumber);
		}
		if (menuContext.GameMenu == null)
		{
			throw new MBMisuseException("Current game menu empty, can not run GetMenuOptionText");
		}
		return null;
	}

	public void AddGameMenu(GameMenu gameMenu)
	{
		_gameMenus.Add(gameMenu.StringId, gameMenu);
	}

	public void RemoveRelatedGameMenus(object relatedObject)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, GameMenu> gameMenu in _gameMenus)
		{
			if (gameMenu.Value.RelatedObject == relatedObject)
			{
				list.Add(gameMenu.Key);
			}
		}
		foreach (string item in list)
		{
			_gameMenus.Remove(item);
		}
	}

	public void RemoveRelatedGameMenuOptions(object relatedObject)
	{
		foreach (KeyValuePair<string, GameMenu> item in _gameMenus.ToList())
		{
			foreach (GameMenuOption item2 in item.Value.MenuOptions.ToList())
			{
				if (item2.RelatedObject == relatedObject)
				{
					item.Value.RemoveMenuOption(item2);
				}
			}
		}
	}

	internal void UnregisterNonReadyObjects()
	{
		MBList<KeyValuePair<string, GameMenu>> mBList = _gameMenus.ToMBList();
		for (int num = mBList.Count - 1; num >= 0; num--)
		{
			if (!mBList[num].Value.IsReady)
			{
				_gameMenus.Remove(mBList[num].Key);
			}
		}
	}

	public GameMenu GetGameMenu(string menuId)
	{
		_gameMenus.TryGetValue(menuId, out var value);
		return value;
	}
}
