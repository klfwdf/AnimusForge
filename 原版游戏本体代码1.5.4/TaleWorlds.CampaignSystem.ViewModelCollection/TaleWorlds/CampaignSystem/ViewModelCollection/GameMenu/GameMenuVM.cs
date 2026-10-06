using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;

public class GameMenuVM : ViewModel
{
	private class GameMenuItemPool<TItem> where TItem : class, new()
	{
		private readonly List<TItem> _pool;

		public GameMenuItemPool(int initialCapacity)
		{
			_pool = new List<TItem>(initialCapacity);
		}

		public TItem Get()
		{
			TItem result;
			if (_pool.Count > 0)
			{
				result = _pool[_pool.Count - 1];
				_pool.RemoveAt(_pool.Count - 1);
			}
			else
			{
				result = new TItem();
			}
			return result;
		}

		public void Release(TItem item)
		{
			_pool.Add(item);
		}
	}

	private class GameMenuItemComparer : IComparer<GameMenuItemVM>
	{
		public int Compare(GameMenuItemVM x, GameMenuItemVM y)
		{
			return x.Index.CompareTo(y.Index);
		}
	}

	private bool _isIdle;

	private bool _plunderEventRegistered;

	private GameMenuManager _gameMenuManager;

	private GameKey _leaveKey;

	private Dictionary<string, string> _menuTextAttributeStrings;

	private Dictionary<string, object> _menuTextAttributes;

	private TextObject _menuText = TextObject.GetEmpty();

	private GameMenuItemComparer _cachedItemComparer;

	private IViewDataTracker _viewDataTracker;

	private GameMenuItemPool<GameMenuItemVM> _gameMenuItemPool;

	private GameMenuItemPool<GameMenuItemProgressVM> _progressItemPool;

	private List<GameMenuItemVM.GameMenuItemCreationData> _newOptionsCache;

	private MBBindingList<GameMenuItemVM> _itemList;

	private MBBindingList<GameMenuItemProgressVM> _progressItemList;

	private string _titleText;

	private string _contextText;

	private string _background;

	private string _backgroundCopy;

	private string _menuId;

	private bool _isNight;

	private bool _isInSiegeMode;

	private bool _isEncounterMenu;

	private MBBindingList<GameMenuPlunderItemVM> _plunderItems;

	private string _latestTutorialElementID;

	private bool _isTavernButtonHighlightApplied;

	private bool _isSellPrisonerButtonHighlightApplied;

	private bool _isShopButtonHighlightApplied;

	private bool _isRecruitButtonHighlightApplied;

	private bool _isHostileActionButtonHighlightApplied;

	private bool _isTownBesiegeButtonHighlightApplied;

	private bool _isEnterTutorialVillageButtonHighlightApplied;

	private bool _requireContextTextUpdate;

	public MenuContext MenuContext { get; private set; }

	[DataSourceProperty]
	public bool IsNight
	{
		get
		{
			return _isNight;
		}
		set
		{
			if (value != _isNight)
			{
				_isNight = value;
				OnPropertyChangedWithValue(value, "IsNight");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInSiegeMode
	{
		get
		{
			return _isInSiegeMode;
		}
		set
		{
			if (value != _isInSiegeMode)
			{
				_isInSiegeMode = value;
				OnPropertyChangedWithValue(value, "IsInSiegeMode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEncounterMenu
	{
		get
		{
			return _isEncounterMenu;
		}
		set
		{
			if (value != _isEncounterMenu)
			{
				_isEncounterMenu = value;
				OnPropertyChangedWithValue(value, "IsEncounterMenu");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ContextText
	{
		get
		{
			return _contextText;
		}
		set
		{
			if (value != _contextText)
			{
				_contextText = value;
				OnPropertyChangedWithValue(value, "ContextText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameMenuItemVM> ItemList
	{
		get
		{
			return _itemList;
		}
		set
		{
			if (value != _itemList)
			{
				_itemList = value;
				OnPropertyChangedWithValue(value, "ItemList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameMenuItemProgressVM> ProgressItemList
	{
		get
		{
			return _progressItemList;
		}
		set
		{
			if (value != _progressItemList)
			{
				_progressItemList = value;
				OnPropertyChangedWithValue(value, "ProgressItemList");
			}
		}
	}

	[DataSourceProperty]
	public string Background
	{
		get
		{
			return _background;
		}
		set
		{
			if (value != _background)
			{
				_background = value;
				OnPropertyChangedWithValue(value, "Background");
				BackgroundCopy = value;
			}
		}
	}

	[DataSourceProperty]
	public string BackgroundCopy
	{
		get
		{
			return _backgroundCopy;
		}
		set
		{
			if (value != _backgroundCopy)
			{
				_backgroundCopy = value;
				OnPropertyChangedWithValue(value, "BackgroundCopy");
			}
		}
	}

	[DataSourceProperty]
	public string MenuId
	{
		get
		{
			return _menuId;
		}
		set
		{
			if (value != _menuId)
			{
				_menuId = value;
				OnPropertyChangedWithValue(value, "MenuId");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameMenuPlunderItemVM> PlunderItems
	{
		get
		{
			return _plunderItems;
		}
		set
		{
			if (value != _plunderItems)
			{
				_plunderItems = value;
				OnPropertyChangedWithValue(value, "PlunderItems");
			}
		}
	}

	public GameMenuVM(MenuContext menuContext)
	{
		_gameMenuManager = Campaign.Current.GameMenuManager;
		_gameMenuItemPool = new GameMenuItemPool<GameMenuItemVM>(10);
		_progressItemPool = new GameMenuItemPool<GameMenuItemProgressVM>(10);
		_newOptionsCache = new List<GameMenuItemVM.GameMenuItemCreationData>();
		_cachedItemComparer = new GameMenuItemComparer();
		_menuTextAttributeStrings = new Dictionary<string, string>();
		_menuTextAttributes = new Dictionary<string, object>();
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		MenuContext = menuContext;
		MenuId = menuContext.GameMenu.StringId;
		Background = menuContext.CurrentBackgroundMeshName;
		ItemList = new MBBindingList<GameMenuItemVM>();
		ProgressItemList = new MBBindingList<GameMenuItemProgressVM>();
		PlunderItems = new MBBindingList<GameMenuPlunderItemVM>();
		IsInSiegeMode = PlayerSiege.PlayerSiegeEvent != null;
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	public override void RefreshValues()
	{
		if (!_isIdle)
		{
			base.RefreshValues();
			ItemList.ApplyActionOnAllItems(delegate(GameMenuItemVM x)
			{
				x.RefreshValues();
			});
			ProgressItemList.ApplyActionOnAllItems(delegate(GameMenuItemProgressVM x)
			{
				x.RefreshValues();
			});
			Refresh(forceUpdateItems: true);
		}
	}

	public void SetIdleMode(bool isIdle)
	{
		_isIdle = isIdle;
	}

	public void Refresh(bool forceUpdateItems)
	{
		TitleText = MenuContext.GameMenu.MenuTitle?.ToString();
		MenuId = MenuContext.GameMenu.StringId;
		TaleWorlds.CampaignSystem.GameMenus.GameMenu gameMenu = MenuContext.GameMenu;
		IsEncounterMenu = gameMenu != null && gameMenu.OverlayType == TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuOverlayType.Encounter;
		Background = (string.IsNullOrEmpty(MenuContext.CurrentBackgroundMeshName) ? "wait_guards_stop" : MenuContext.CurrentBackgroundMeshName);
		if (forceUpdateItems)
		{
			_newOptionsCache.Clear();
			int virtualMenuOptionAmount = _gameMenuManager.GetVirtualMenuOptionAmount(MenuContext);
			for (int num = ProgressItemList.Count - 1; num >= 0; num--)
			{
				_progressItemPool.Release(ProgressItemList[num]);
				ProgressItemList.RemoveAt(num);
			}
			for (int i = 0; i < virtualMenuOptionAmount; i++)
			{
				_gameMenuManager.SetCurrentRepeatableIndex(MenuContext, i);
				if (_gameMenuManager.GetVirtualMenuOptionConditionsHold(MenuContext, i))
				{
					TextObject textObject;
					TextObject textObject2;
					if (_gameMenuManager.GetVirtualGameMenuOption(MenuContext, i).IsRepeatable)
					{
						textObject = new TextObject(_gameMenuManager.GetVirtualMenuOptionText(MenuContext, i).ToString());
						textObject2 = new TextObject(_gameMenuManager.GetVirtualMenuOptionText2(MenuContext, i).ToString());
					}
					else
					{
						textObject = _gameMenuManager.GetVirtualMenuOptionText(MenuContext, i);
						textObject2 = _gameMenuManager.GetVirtualMenuOptionText2(MenuContext, i);
					}
					TextObject virtualMenuOptionTooltip = _gameMenuManager.GetVirtualMenuOptionTooltip(MenuContext, i);
					TextObject textObject3 = textObject;
					TextObject textObject4 = textObject2;
					TextObject tooltip = virtualMenuOptionTooltip;
					TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuAndOptionType virtualMenuAndOptionType = _gameMenuManager.GetVirtualMenuAndOptionType(MenuContext);
					GameMenuOption virtualGameMenuOption = _gameMenuManager.GetVirtualGameMenuOption(MenuContext, i);
					GameKey shortcutKey = ((_gameMenuManager.GetLeaveMenuOption(MenuContext) == virtualGameMenuOption) ? _leaveKey : null);
					GameMenuOption.IssueQuestFlags optionQuestData = virtualGameMenuOption.OptionQuestData;
					GameMenuItemVM.GameMenuItemCreationData item = new GameMenuItemVM.GameMenuItemCreationData(MenuContext, i, textObject3, textObject4.IsEmpty() ? textObject3 : textObject4, tooltip, virtualMenuAndOptionType, optionQuestData, virtualGameMenuOption, shortcutKey);
					_newOptionsCache.Add(item);
					if (virtualMenuAndOptionType == TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption || virtualMenuAndOptionType == TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption)
					{
						GameMenuItemProgressVM gameMenuItemProgressVM = _progressItemPool.Get();
						gameMenuItemProgressVM.InitializeWith(MenuContext, i);
						ProgressItemList.Add(gameMenuItemProgressVM);
					}
				}
			}
			for (int num2 = ItemList.Count - 1; num2 >= 0; num2--)
			{
				GameMenuItemVM gameMenuItemVM = ItemList[num2];
				if (gameMenuItemVM.GameMenuOption.IsRepeatable)
				{
					ItemList.RemoveAt(num2);
					_gameMenuItemPool.Release(gameMenuItemVM);
				}
				else
				{
					bool flag = true;
					for (int num3 = _newOptionsCache.Count - 1; num3 >= 0; num3--)
					{
						GameMenuItemVM.GameMenuItemCreationData data = _newOptionsCache[num3];
						if (data.OptionID == gameMenuItemVM.OptionID)
						{
							flag = false;
							gameMenuItemVM.InitializeWith(in data);
							if (!string.IsNullOrEmpty(_latestTutorialElementID))
							{
								gameMenuItemVM.IsHighlightEnabled = data.OptionID == _latestTutorialElementID;
							}
							_newOptionsCache.RemoveAt(num3);
							break;
						}
					}
					if (flag)
					{
						ItemList.RemoveAt(num2);
						_gameMenuItemPool.Release(gameMenuItemVM);
					}
				}
			}
			for (int j = 0; j < _newOptionsCache.Count; j++)
			{
				GameMenuItemVM.GameMenuItemCreationData data2 = _newOptionsCache[j];
				GameMenuItemVM gameMenuItemVM2 = _gameMenuItemPool.Get();
				gameMenuItemVM2.InitializeWith(in data2);
				if (!string.IsNullOrEmpty(_latestTutorialElementID))
				{
					gameMenuItemVM2.IsHighlightEnabled = data2.OptionID == _latestTutorialElementID;
				}
				ItemList.Add(gameMenuItemVM2);
			}
			ItemList.Sort(_cachedItemComparer);
		}
		RefreshPlunderStatus();
		_requireContextTextUpdate = true;
	}

	private void RefreshPlunderStatus()
	{
		if (Campaign.Current.Models.EncounterGameMenuModel.IsPlunderMenu(MenuContext.GameMenu.StringId))
		{
			if (_plunderEventRegistered)
			{
				return;
			}
			PlunderItems.Clear();
			CampaignEvents.ItemsLooted.AddNonSerializedListener(this, OnItemsPlundered);
			MBReadOnlyList<ItemRosterElement> plunderItems = _viewDataTracker.GetPlunderItems();
			if (plunderItems != null)
			{
				for (int i = 0; i < plunderItems.Count; i++)
				{
					ItemRosterElement item = plunderItems[i];
					AddPlunderedItem(item);
				}
			}
			_plunderEventRegistered = true;
		}
		else if (_plunderEventRegistered)
		{
			PlunderItems.Clear();
			CampaignEvents.ItemsLooted.ClearListeners(this);
			_plunderEventRegistered = false;
		}
	}

	public void OnFrameTick()
	{
		IsInSiegeMode = PlayerSiege.PlayerSiegeEvent != null;
		if (_requireContextTextUpdate)
		{
			_menuText = _gameMenuManager.GetMenuText(MenuContext);
			ContextText = _menuText.ToString();
			_menuTextAttributes.Clear();
			_menuTextAttributeStrings.Clear();
			if (_menuText?.Attributes != null)
			{
				foreach (KeyValuePair<string, object> attribute in _menuText.Attributes)
				{
					_menuTextAttributes[attribute.Key] = attribute.Value;
					_menuTextAttributeStrings[attribute.Key] = attribute.Value.ToString();
				}
			}
			_requireContextTextUpdate = false;
		}
		for (int i = 0; i < ItemList.Count; i++)
		{
			ItemList[i].Refresh();
		}
		for (int j = 0; j < ProgressItemList.Count; j++)
		{
			ProgressItemList[j].OnTick();
		}
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			IsNight = Campaign.Current.IsNight;
		}
		_requireContextTextUpdate = IsMenuTextChanged();
	}

	private bool IsMenuTextChanged()
	{
		TextObject textObject = _gameMenuManager?.GetMenuText(MenuContext);
		if (_menuText != textObject)
		{
			return true;
		}
		if (_menuTextAttributes.Count != _menuText?.Attributes?.Count)
		{
			return true;
		}
		foreach (KeyValuePair<string, object> menuTextAttribute in _menuTextAttributes)
		{
			string key = menuTextAttribute.Key;
			object value = null;
			object obj = _menuTextAttributes[key];
			TextObject menuText = _menuText;
			if ((object)menuText == null || !menuText.Attributes.TryGetValue(key, out value))
			{
				return true;
			}
			if (obj != value)
			{
				return true;
			}
			if (_menuTextAttributeStrings[key] != value.ToString())
			{
				return true;
			}
		}
		return false;
	}

	public void UpdateMenuContext(MenuContext newMenuContext)
	{
		MenuContext = newMenuContext;
		ItemList.Clear();
		ProgressItemList.Clear();
		Refresh(forceUpdateItems: true);
	}

	public override void OnFinalize()
	{
		CampaignEvents.ItemsLooted.ClearListeners(this);
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		_gameMenuManager = null;
		MenuContext = null;
		ItemList.ApplyActionOnAllItems(delegate(GameMenuItemVM x)
		{
			x.OnFinalize();
		});
		ItemList.Clear();
		ItemList = null;
	}

	public void SetLeaveHotKey(GameKey gameKey)
	{
		_leaveKey = gameKey;
	}

	private void OnItemsPlundered(MobileParty mobileParty, ItemRoster newItems)
	{
		if (mobileParty == MobileParty.MainParty)
		{
			for (int i = 0; i < newItems.Count; i++)
			{
				ItemRosterElement item = newItems[i];
				AddPlunderedItem(item);
			}
		}
	}

	private void AddPlunderedItem(ItemRosterElement item)
	{
		int num = PlunderItems.FindIndex((GameMenuPlunderItemVM x) => x.Item.IsEqualTo(item.EquipmentElement));
		if (num != -1)
		{
			PlunderItems[num].Amount += item.Amount;
		}
		else
		{
			PlunderItems.Add(new GameMenuPlunderItemVM(item.EquipmentElement, item.Amount));
		}
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (obj.NewNotificationElementID != _latestTutorialElementID)
		{
			_latestTutorialElementID = obj.NewNotificationElementID;
		}
		if (_latestTutorialElementID != null)
		{
			if (_latestTutorialElementID != string.Empty)
			{
				if (_latestTutorialElementID == "town_backstreet" && !_isTavernButtonHighlightApplied)
				{
					_isTavernButtonHighlightApplied = SetGameMenuButtonHighlightState(_latestTutorialElementID, state: true);
				}
				else if (_latestTutorialElementID != "town_backstreet" && _isTavernButtonHighlightApplied)
				{
					_isTavernButtonHighlightApplied = !SetGameMenuButtonHighlightState("town_backstreet", state: false);
				}
				if (_latestTutorialElementID == "sell_all_prisoners" && !_isSellPrisonerButtonHighlightApplied)
				{
					_isSellPrisonerButtonHighlightApplied = SetGameMenuButtonHighlightState(_latestTutorialElementID, state: true);
				}
				else if (_latestTutorialElementID != "sell_all_prisoners" && _isSellPrisonerButtonHighlightApplied)
				{
					_isSellPrisonerButtonHighlightApplied = !SetGameMenuButtonHighlightState("sell_all_prisoners", state: false);
				}
				if (_latestTutorialElementID == "storymode_tutorial_village_buy" && !_isShopButtonHighlightApplied)
				{
					_isShopButtonHighlightApplied = SetGameMenuButtonHighlightState(_latestTutorialElementID, state: true);
				}
				else if (_latestTutorialElementID != "storymode_tutorial_village_buy" && _isShopButtonHighlightApplied)
				{
					_isShopButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_buy", state: false);
				}
				if (_latestTutorialElementID == "storymode_tutorial_village_recruit" && !_isRecruitButtonHighlightApplied)
				{
					_isRecruitButtonHighlightApplied = SetGameMenuButtonHighlightState(_latestTutorialElementID, state: true);
				}
				else if (_latestTutorialElementID != "storymode_tutorial_village_recruit" && _isRecruitButtonHighlightApplied)
				{
					_isRecruitButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_recruit", state: false);
				}
				if (_latestTutorialElementID == "hostile_action" && !_isHostileActionButtonHighlightApplied)
				{
					_isHostileActionButtonHighlightApplied = SetGameMenuButtonHighlightState(_latestTutorialElementID, state: true);
				}
				else if (_latestTutorialElementID != "hostile_action" && _isHostileActionButtonHighlightApplied)
				{
					_isHostileActionButtonHighlightApplied = !SetGameMenuButtonHighlightState("hostile_action", state: false);
				}
				if (_latestTutorialElementID == "town_besiege" && !_isTownBesiegeButtonHighlightApplied)
				{
					_isTownBesiegeButtonHighlightApplied = SetGameMenuButtonHighlightState(_latestTutorialElementID, state: true);
				}
				else if (_latestTutorialElementID != "town_besiege" && _isTownBesiegeButtonHighlightApplied)
				{
					_isTownBesiegeButtonHighlightApplied = !SetGameMenuButtonHighlightState("town_besiege", state: false);
				}
				if (_latestTutorialElementID == "storymode_tutorial_village_enter" && !_isEnterTutorialVillageButtonHighlightApplied)
				{
					_isEnterTutorialVillageButtonHighlightApplied = SetGameMenuButtonHighlightState(_latestTutorialElementID, state: true);
				}
				else if (_latestTutorialElementID != "storymode_tutorial_village_enter" && _isEnterTutorialVillageButtonHighlightApplied)
				{
					_isEnterTutorialVillageButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_enter", state: false);
				}
			}
			else
			{
				if (_isTavernButtonHighlightApplied)
				{
					_isTavernButtonHighlightApplied = !SetGameMenuButtonHighlightState("town_backstreet", state: false);
				}
				if (_isSellPrisonerButtonHighlightApplied)
				{
					_isSellPrisonerButtonHighlightApplied = !SetGameMenuButtonHighlightState("sell_all_prisoners", state: false);
				}
				if (_isShopButtonHighlightApplied)
				{
					_isShopButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_buy", state: false);
				}
				if (_isRecruitButtonHighlightApplied)
				{
					_isRecruitButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_recruit", state: false);
				}
				if (_isHostileActionButtonHighlightApplied)
				{
					_isHostileActionButtonHighlightApplied = !SetGameMenuButtonHighlightState("hostile_action", state: false);
				}
				if (_isTownBesiegeButtonHighlightApplied)
				{
					_isTownBesiegeButtonHighlightApplied = !SetGameMenuButtonHighlightState("town_besiege", state: false);
				}
				if (_isEnterTutorialVillageButtonHighlightApplied)
				{
					_isEnterTutorialVillageButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_enter", state: false);
				}
			}
		}
		else
		{
			if (_isTavernButtonHighlightApplied)
			{
				_isTavernButtonHighlightApplied = !SetGameMenuButtonHighlightState("town_backstreet", state: false);
			}
			if (_isSellPrisonerButtonHighlightApplied)
			{
				_isSellPrisonerButtonHighlightApplied = !SetGameMenuButtonHighlightState("sell_all_prisoners", state: false);
			}
			if (_isShopButtonHighlightApplied)
			{
				_isShopButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_buy", state: false);
			}
			if (_isRecruitButtonHighlightApplied)
			{
				_isRecruitButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_recruit", state: false);
			}
			if (_isHostileActionButtonHighlightApplied)
			{
				_isHostileActionButtonHighlightApplied = !SetGameMenuButtonHighlightState("hostile_action", state: false);
			}
			if (_isTownBesiegeButtonHighlightApplied)
			{
				_isTownBesiegeButtonHighlightApplied = !SetGameMenuButtonHighlightState("town_besiege", state: false);
			}
			if (_isEnterTutorialVillageButtonHighlightApplied)
			{
				_isEnterTutorialVillageButtonHighlightApplied = !SetGameMenuButtonHighlightState("storymode_tutorial_village_enter", state: false);
			}
		}
	}

	private bool SetGameMenuButtonHighlightState(string buttonID, bool state)
	{
		for (int i = 0; i < ItemList.Count; i++)
		{
			GameMenuItemVM gameMenuItemVM = ItemList[i];
			if (gameMenuItemVM.OptionID == buttonID)
			{
				gameMenuItemVM.IsHighlightEnabled = state;
				return true;
			}
		}
		return false;
	}
}
