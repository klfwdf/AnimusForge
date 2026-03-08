using System;
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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu
{
	// Token: 0x0200009E RID: 158
	public class GameMenuVM : ViewModel
	{
		// Token: 0x170004E9 RID: 1257
		// (get) Token: 0x06000F33 RID: 3891 RVA: 0x0003EB33 File Offset: 0x0003CD33
		// (set) Token: 0x06000F34 RID: 3892 RVA: 0x0003EB3B File Offset: 0x0003CD3B
		public MenuContext MenuContext { get; private set; }

		// Token: 0x06000F35 RID: 3893 RVA: 0x0003EB44 File Offset: 0x0003CD44
		public GameMenuVM(MenuContext menuContext)
		{
			this._gameMenuManager = Campaign.Current.GameMenuManager;
			this._shortcutKeys = new Dictionary<GameMenuOption.LeaveType, GameKey>();
			this._gameMenuItemPool = new GameMenuVM.GameMenuItemPool<GameMenuItemVM>(10);
			this._progressItemPool = new GameMenuVM.GameMenuItemPool<GameMenuItemProgressVM>(10);
			this._newOptionsCache = new List<GameMenuItemVM.GameMenuItemCreationData>();
			this._cachedItemComparer = new GameMenuVM.GameMenuItemComparer();
			this._menuTextAttributeStrings = new Dictionary<string, string>();
			this._menuTextAttributes = new Dictionary<string, object>();
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			this.MenuContext = menuContext;
			this.MenuId = menuContext.GameMenu.StringId;
			this.Background = menuContext.CurrentBackgroundMeshName;
			this.ItemList = new MBBindingList<GameMenuItemVM>();
			this.ProgressItemList = new MBBindingList<GameMenuItemProgressVM>();
			this.PlunderItems = new MBBindingList<GameMenuPlunderItemVM>();
			this.IsInSiegeMode = PlayerSiege.PlayerSiegeEvent != null;
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x06000F36 RID: 3894 RVA: 0x0003EC44 File Offset: 0x0003CE44
		public override void RefreshValues()
		{
			if (this._isIdle)
			{
				return;
			}
			base.RefreshValues();
			this.ItemList.ApplyActionOnAllItems(delegate(GameMenuItemVM x)
			{
				x.RefreshValues();
			});
			this.ProgressItemList.ApplyActionOnAllItems(delegate(GameMenuItemProgressVM x)
			{
				x.RefreshValues();
			});
			this.Refresh(true);
		}

		// Token: 0x06000F37 RID: 3895 RVA: 0x0003ECBB File Offset: 0x0003CEBB
		public void SetIdleMode(bool isIdle)
		{
			this._isIdle = isIdle;
		}

		// Token: 0x06000F38 RID: 3896 RVA: 0x0003ECC4 File Offset: 0x0003CEC4
		public void Refresh(bool forceUpdateItems)
		{
			TextObject menuTitle = this.MenuContext.GameMenu.MenuTitle;
			this.TitleText = ((menuTitle != null) ? menuTitle.ToString() : null);
			this.MenuId = this.MenuContext.GameMenu.StringId;
			GameMenu gameMenu = this.MenuContext.GameMenu;
			this.IsEncounterMenu = gameMenu != null && gameMenu.OverlayType == GameMenu.MenuOverlayType.Encounter;
			this.Background = (string.IsNullOrEmpty(this.MenuContext.CurrentBackgroundMeshName) ? "wait_guards_stop" : this.MenuContext.CurrentBackgroundMeshName);
			if (forceUpdateItems)
			{
				this._newOptionsCache.Clear();
				int virtualMenuOptionAmount = this._gameMenuManager.GetVirtualMenuOptionAmount(this.MenuContext);
				for (int i = this.ProgressItemList.Count - 1; i >= 0; i--)
				{
					this._progressItemPool.Release(this.ProgressItemList[i]);
					this.ProgressItemList.RemoveAt(i);
				}
				for (int j = 0; j < virtualMenuOptionAmount; j++)
				{
					this._gameMenuManager.SetCurrentRepeatableIndex(this.MenuContext, j);
					if (this._gameMenuManager.GetVirtualMenuOptionConditionsHold(this.MenuContext, j))
					{
						TextObject textObject;
						TextObject textObject2;
						if (this._gameMenuManager.GetVirtualGameMenuOption(this.MenuContext, j).IsRepeatable)
						{
							textObject = new TextObject(this._gameMenuManager.GetVirtualMenuOptionText(this.MenuContext, j).ToString(), null);
							textObject2 = new TextObject(this._gameMenuManager.GetVirtualMenuOptionText2(this.MenuContext, j).ToString(), null);
						}
						else
						{
							textObject = this._gameMenuManager.GetVirtualMenuOptionText(this.MenuContext, j);
							textObject2 = this._gameMenuManager.GetVirtualMenuOptionText2(this.MenuContext, j);
						}
						TextObject virtualMenuOptionTooltip = this._gameMenuManager.GetVirtualMenuOptionTooltip(this.MenuContext, j);
						TextObject textObject3 = textObject;
						TextObject textObject4 = textObject2;
						TextObject tooltip = virtualMenuOptionTooltip;
						GameMenu.MenuAndOptionType virtualMenuAndOptionType = this._gameMenuManager.GetVirtualMenuAndOptionType(this.MenuContext);
						GameMenuOption virtualGameMenuOption = this._gameMenuManager.GetVirtualGameMenuOption(this.MenuContext, j);
						GameKey shortcutKey = (this._shortcutKeys.ContainsKey(virtualGameMenuOption.OptionLeaveType) ? this._shortcutKeys[virtualGameMenuOption.OptionLeaveType] : null);
						GameMenuOption.IssueQuestFlags optionQuestData = virtualGameMenuOption.OptionQuestData;
						GameMenuItemVM.GameMenuItemCreationData item = new GameMenuItemVM.GameMenuItemCreationData(this.MenuContext, j, textObject3, textObject4.IsEmpty() ? textObject3 : textObject4, tooltip, virtualMenuAndOptionType, optionQuestData, virtualGameMenuOption, shortcutKey);
						this._newOptionsCache.Add(item);
						if (virtualMenuAndOptionType == GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption || virtualMenuAndOptionType == GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption)
						{
							GameMenuItemProgressVM gameMenuItemProgressVM = this._progressItemPool.Get();
							gameMenuItemProgressVM.InitializeWith(this.MenuContext, j);
							this.ProgressItemList.Add(gameMenuItemProgressVM);
						}
					}
				}
				for (int k = this.ItemList.Count - 1; k >= 0; k--)
				{
					GameMenuItemVM gameMenuItemVM = this.ItemList[k];
					if (gameMenuItemVM.GameMenuOption.IsRepeatable)
					{
						this.ItemList.RemoveAt(k);
						this._gameMenuItemPool.Release(gameMenuItemVM);
					}
					else
					{
						bool flag = true;
						for (int l = this._newOptionsCache.Count - 1; l >= 0; l--)
						{
							GameMenuItemVM.GameMenuItemCreationData gameMenuItemCreationData = this._newOptionsCache[l];
							if (gameMenuItemCreationData.OptionID == gameMenuItemVM.OptionID)
							{
								flag = false;
								gameMenuItemVM.InitializeWith(gameMenuItemCreationData);
								if (!string.IsNullOrEmpty(this._latestTutorialElementID))
								{
									gameMenuItemVM.IsHighlightEnabled = gameMenuItemCreationData.OptionID == this._latestTutorialElementID;
								}
								this._newOptionsCache.RemoveAt(l);
								break;
							}
						}
						if (flag)
						{
							this.ItemList.RemoveAt(k);
							this._gameMenuItemPool.Release(gameMenuItemVM);
						}
					}
				}
				for (int m = 0; m < this._newOptionsCache.Count; m++)
				{
					GameMenuItemVM.GameMenuItemCreationData gameMenuItemCreationData2 = this._newOptionsCache[m];
					GameMenuItemVM gameMenuItemVM2 = this._gameMenuItemPool.Get();
					gameMenuItemVM2.InitializeWith(gameMenuItemCreationData2);
					if (!string.IsNullOrEmpty(this._latestTutorialElementID))
					{
						gameMenuItemVM2.IsHighlightEnabled = gameMenuItemCreationData2.OptionID == this._latestTutorialElementID;
					}
					this.ItemList.Add(gameMenuItemVM2);
				}
				this.ItemList.Sort(this._cachedItemComparer);
			}
			this.RefreshPlunderStatus();
			this._requireContextTextUpdate = true;
		}

		// Token: 0x06000F39 RID: 3897 RVA: 0x0003F0DC File Offset: 0x0003D2DC
		private void RefreshPlunderStatus()
		{
			if (Campaign.Current.Models.EncounterGameMenuModel.IsPlunderMenu(this.MenuContext.GameMenu.StringId))
			{
				if (!this._plunderEventRegistered)
				{
					this.PlunderItems.Clear();
					CampaignEvents.ItemsLooted.AddNonSerializedListener(this, new Action<MobileParty, ItemRoster>(this.OnItemsPlundered));
					MBReadOnlyList<ItemRosterElement> plunderItems = this._viewDataTracker.GetPlunderItems();
					if (plunderItems != null)
					{
						for (int i = 0; i < plunderItems.Count; i++)
						{
							ItemRosterElement item = plunderItems[i];
							this.AddPlunderedItem(item);
						}
					}
					this._plunderEventRegistered = true;
					return;
				}
			}
			else if (this._plunderEventRegistered)
			{
				this.PlunderItems.Clear();
				CampaignEvents.ItemsLooted.ClearListeners(this);
				this._plunderEventRegistered = false;
			}
		}

		// Token: 0x06000F3A RID: 3898 RVA: 0x0003F198 File Offset: 0x0003D398
		public void OnFrameTick()
		{
			this.IsInSiegeMode = PlayerSiege.PlayerSiegeEvent != null;
			if (this._requireContextTextUpdate)
			{
				this._menuText = this._gameMenuManager.GetMenuText(this.MenuContext);
				this.ContextText = this._menuText.ToString();
				this._menuTextAttributes.Clear();
				this._menuTextAttributeStrings.Clear();
				TextObject menuText = this._menuText;
				if (((menuText != null) ? menuText.Attributes : null) != null)
				{
					foreach (KeyValuePair<string, object> keyValuePair in this._menuText.Attributes)
					{
						this._menuTextAttributes[keyValuePair.Key] = keyValuePair.Value;
						this._menuTextAttributeStrings[keyValuePair.Key] = keyValuePair.Value.ToString();
					}
				}
				this._requireContextTextUpdate = false;
			}
			for (int i = 0; i < this.ItemList.Count; i++)
			{
				this.ItemList[i].Refresh();
			}
			for (int j = 0; j < this.ProgressItemList.Count; j++)
			{
				this.ProgressItemList[j].OnTick();
			}
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				this.IsNight = Campaign.Current.IsNight;
			}
			this._requireContextTextUpdate = this.IsMenuTextChanged();
		}

		// Token: 0x06000F3B RID: 3899 RVA: 0x0003F30C File Offset: 0x0003D50C
		private bool IsMenuTextChanged()
		{
			GameMenuManager gameMenuManager = this._gameMenuManager;
			TextObject rhs = ((gameMenuManager != null) ? gameMenuManager.GetMenuText(this.MenuContext) : null);
			if (this._menuText != rhs)
			{
				return true;
			}
			int count = this._menuTextAttributes.Count;
			TextObject menuText = this._menuText;
			int? num;
			if (menuText == null)
			{
				num = null;
			}
			else
			{
				Dictionary<string, object> attributes = menuText.Attributes;
				num = ((attributes != null) ? new int?(attributes.Count) : null);
			}
			int? num2 = num;
			if (!((count == num2.GetValueOrDefault()) & (num2 != null)))
			{
				return true;
			}
			foreach (KeyValuePair<string, object> keyValuePair in this._menuTextAttributes)
			{
				string key = keyValuePair.Key;
				object obj = null;
				object obj2 = this._menuTextAttributes[key];
				TextObject menuText2 = this._menuText;
				if (menuText2 == null || !menuText2.Attributes.TryGetValue(key, out obj))
				{
					return true;
				}
				if (obj2 != obj)
				{
					return true;
				}
				if (this._menuTextAttributeStrings[key] != obj.ToString())
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000F3C RID: 3900 RVA: 0x0003F448 File Offset: 0x0003D648
		public void UpdateMenuContext(MenuContext newMenuContext)
		{
			this.MenuContext = newMenuContext;
			this.ItemList.Clear();
			this.ProgressItemList.Clear();
			this.Refresh(true);
		}

		// Token: 0x06000F3D RID: 3901 RVA: 0x0003F470 File Offset: 0x0003D670
		public override void OnFinalize()
		{
			CampaignEvents.ItemsLooted.ClearListeners(this);
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this._gameMenuManager = null;
			this.MenuContext = null;
			this.ItemList.ApplyActionOnAllItems(delegate(GameMenuItemVM x)
			{
				x.OnFinalize();
			});
			this.ItemList.Clear();
			this.ItemList = null;
		}

		// Token: 0x06000F3E RID: 3902 RVA: 0x0003F4ED File Offset: 0x0003D6ED
		public void AddHotKey(GameMenuOption.LeaveType leaveType, GameKey gameKey)
		{
			if (this._shortcutKeys.ContainsKey(leaveType))
			{
				this._shortcutKeys[leaveType] = gameKey;
				return;
			}
			this._shortcutKeys.Add(leaveType, gameKey);
		}

		// Token: 0x06000F3F RID: 3903 RVA: 0x0003F518 File Offset: 0x0003D718
		private void OnItemsPlundered(MobileParty mobileParty, ItemRoster newItems)
		{
			if (mobileParty == MobileParty.MainParty)
			{
				for (int i = 0; i < newItems.Count; i++)
				{
					ItemRosterElement item = newItems[i];
					this.AddPlunderedItem(item);
				}
			}
		}

		// Token: 0x06000F40 RID: 3904 RVA: 0x0003F550 File Offset: 0x0003D750
		private void AddPlunderedItem(ItemRosterElement item)
		{
			int num = this.PlunderItems.FindIndex((GameMenuPlunderItemVM x) => x.Item.IsEqualTo(item.EquipmentElement));
			if (num != -1)
			{
				this.PlunderItems[num].Amount += item.Amount;
				return;
			}
			this.PlunderItems.Add(new GameMenuPlunderItemVM(item.EquipmentElement, item.Amount));
		}

		// Token: 0x06000F41 RID: 3905 RVA: 0x0003F5D0 File Offset: 0x0003D7D0
		public void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x170004EA RID: 1258
		// (get) Token: 0x06000F42 RID: 3906 RVA: 0x0003F5E2 File Offset: 0x0003D7E2
		// (set) Token: 0x06000F43 RID: 3907 RVA: 0x0003F5EA File Offset: 0x0003D7EA
		[DataSourceProperty]
		public bool IsNight
		{
			get
			{
				return this._isNight;
			}
			set
			{
				if (value != this._isNight)
				{
					this._isNight = value;
					base.OnPropertyChangedWithValue(value, "IsNight");
				}
			}
		}

		// Token: 0x170004EB RID: 1259
		// (get) Token: 0x06000F44 RID: 3908 RVA: 0x0003F608 File Offset: 0x0003D808
		// (set) Token: 0x06000F45 RID: 3909 RVA: 0x0003F610 File Offset: 0x0003D810
		[DataSourceProperty]
		public bool IsInSiegeMode
		{
			get
			{
				return this._isInSiegeMode;
			}
			set
			{
				if (value != this._isInSiegeMode)
				{
					this._isInSiegeMode = value;
					base.OnPropertyChangedWithValue(value, "IsInSiegeMode");
				}
			}
		}

		// Token: 0x170004EC RID: 1260
		// (get) Token: 0x06000F46 RID: 3910 RVA: 0x0003F62E File Offset: 0x0003D82E
		// (set) Token: 0x06000F47 RID: 3911 RVA: 0x0003F636 File Offset: 0x0003D836
		[DataSourceProperty]
		public bool IsEncounterMenu
		{
			get
			{
				return this._isEncounterMenu;
			}
			set
			{
				if (value != this._isEncounterMenu)
				{
					this._isEncounterMenu = value;
					base.OnPropertyChangedWithValue(value, "IsEncounterMenu");
				}
			}
		}

		// Token: 0x170004ED RID: 1261
		// (get) Token: 0x06000F48 RID: 3912 RVA: 0x0003F654 File Offset: 0x0003D854
		// (set) Token: 0x06000F49 RID: 3913 RVA: 0x0003F65C File Offset: 0x0003D85C
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x170004EE RID: 1262
		// (get) Token: 0x06000F4A RID: 3914 RVA: 0x0003F67F File Offset: 0x0003D87F
		// (set) Token: 0x06000F4B RID: 3915 RVA: 0x0003F687 File Offset: 0x0003D887
		[DataSourceProperty]
		public string ContextText
		{
			get
			{
				return this._contextText;
			}
			set
			{
				if (value != this._contextText)
				{
					this._contextText = value;
					base.OnPropertyChangedWithValue<string>(value, "ContextText");
				}
			}
		}

		// Token: 0x170004EF RID: 1263
		// (get) Token: 0x06000F4C RID: 3916 RVA: 0x0003F6AA File Offset: 0x0003D8AA
		// (set) Token: 0x06000F4D RID: 3917 RVA: 0x0003F6B2 File Offset: 0x0003D8B2
		[DataSourceProperty]
		public MBBindingList<GameMenuItemVM> ItemList
		{
			get
			{
				return this._itemList;
			}
			set
			{
				if (value != this._itemList)
				{
					this._itemList = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuItemVM>>(value, "ItemList");
				}
			}
		}

		// Token: 0x170004F0 RID: 1264
		// (get) Token: 0x06000F4E RID: 3918 RVA: 0x0003F6D0 File Offset: 0x0003D8D0
		// (set) Token: 0x06000F4F RID: 3919 RVA: 0x0003F6D8 File Offset: 0x0003D8D8
		[DataSourceProperty]
		public MBBindingList<GameMenuItemProgressVM> ProgressItemList
		{
			get
			{
				return this._progressItemList;
			}
			set
			{
				if (value != this._progressItemList)
				{
					this._progressItemList = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuItemProgressVM>>(value, "ProgressItemList");
				}
			}
		}

		// Token: 0x170004F1 RID: 1265
		// (get) Token: 0x06000F50 RID: 3920 RVA: 0x0003F6F6 File Offset: 0x0003D8F6
		// (set) Token: 0x06000F51 RID: 3921 RVA: 0x0003F6FE File Offset: 0x0003D8FE
		[DataSourceProperty]
		public string Background
		{
			get
			{
				return this._background;
			}
			set
			{
				if (value != this._background)
				{
					this._background = value;
					base.OnPropertyChangedWithValue<string>(value, "Background");
					this.BackgroundCopy = value;
				}
			}
		}

		// Token: 0x170004F2 RID: 1266
		// (get) Token: 0x06000F52 RID: 3922 RVA: 0x0003F728 File Offset: 0x0003D928
		// (set) Token: 0x06000F53 RID: 3923 RVA: 0x0003F730 File Offset: 0x0003D930
		[DataSourceProperty]
		public string BackgroundCopy
		{
			get
			{
				return this._backgroundCopy;
			}
			set
			{
				if (value != this._backgroundCopy)
				{
					this._backgroundCopy = value;
					base.OnPropertyChangedWithValue<string>(value, "BackgroundCopy");
				}
			}
		}

		// Token: 0x170004F3 RID: 1267
		// (get) Token: 0x06000F54 RID: 3924 RVA: 0x0003F753 File Offset: 0x0003D953
		// (set) Token: 0x06000F55 RID: 3925 RVA: 0x0003F75B File Offset: 0x0003D95B
		[DataSourceProperty]
		public string MenuId
		{
			get
			{
				return this._menuId;
			}
			set
			{
				if (value != this._menuId)
				{
					this._menuId = value;
					base.OnPropertyChangedWithValue<string>(value, "MenuId");
				}
			}
		}

		// Token: 0x170004F4 RID: 1268
		// (get) Token: 0x06000F56 RID: 3926 RVA: 0x0003F77E File Offset: 0x0003D97E
		// (set) Token: 0x06000F57 RID: 3927 RVA: 0x0003F786 File Offset: 0x0003D986
		[DataSourceProperty]
		public MBBindingList<GameMenuPlunderItemVM> PlunderItems
		{
			get
			{
				return this._plunderItems;
			}
			set
			{
				if (value != this._plunderItems)
				{
					this._plunderItems = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuPlunderItemVM>>(value, "PlunderItems");
				}
			}
		}

		// Token: 0x06000F58 RID: 3928 RVA: 0x0003F7A4 File Offset: 0x0003D9A4
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				this._latestTutorialElementID = obj.NewNotificationElementID;
			}
			if (this._latestTutorialElementID != null)
			{
				if (this._latestTutorialElementID != string.Empty)
				{
					if (this._latestTutorialElementID == "town_backstreet" && !this._isTavernButtonHighlightApplied)
					{
						this._isTavernButtonHighlightApplied = this.SetGameMenuButtonHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "town_backstreet" && this._isTavernButtonHighlightApplied)
					{
						this._isTavernButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("town_backstreet", false);
					}
					if (this._latestTutorialElementID == "sell_all_prisoners" && !this._isSellPrisonerButtonHighlightApplied)
					{
						this._isSellPrisonerButtonHighlightApplied = this.SetGameMenuButtonHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "sell_all_prisoners" && this._isSellPrisonerButtonHighlightApplied)
					{
						this._isSellPrisonerButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("sell_all_prisoners", false);
					}
					if (this._latestTutorialElementID == "storymode_tutorial_village_buy" && !this._isShopButtonHighlightApplied)
					{
						this._isShopButtonHighlightApplied = this.SetGameMenuButtonHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "storymode_tutorial_village_buy" && this._isShopButtonHighlightApplied)
					{
						this._isShopButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_buy", false);
					}
					if (this._latestTutorialElementID == "storymode_tutorial_village_recruit" && !this._isRecruitButtonHighlightApplied)
					{
						this._isRecruitButtonHighlightApplied = this.SetGameMenuButtonHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "storymode_tutorial_village_recruit" && this._isRecruitButtonHighlightApplied)
					{
						this._isRecruitButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_recruit", false);
					}
					if (this._latestTutorialElementID == "hostile_action" && !this._isHostileActionButtonHighlightApplied)
					{
						this._isHostileActionButtonHighlightApplied = this.SetGameMenuButtonHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "hostile_action" && this._isHostileActionButtonHighlightApplied)
					{
						this._isHostileActionButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("hostile_action", false);
					}
					if (this._latestTutorialElementID == "town_besiege" && !this._isTownBesiegeButtonHighlightApplied)
					{
						this._isTownBesiegeButtonHighlightApplied = this.SetGameMenuButtonHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "town_besiege" && this._isTownBesiegeButtonHighlightApplied)
					{
						this._isTownBesiegeButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("town_besiege", false);
					}
					if (this._latestTutorialElementID == "storymode_tutorial_village_enter" && !this._isEnterTutorialVillageButtonHighlightApplied)
					{
						this._isEnterTutorialVillageButtonHighlightApplied = this.SetGameMenuButtonHighlightState(this._latestTutorialElementID, true);
						return;
					}
					if (this._latestTutorialElementID != "storymode_tutorial_village_enter" && this._isEnterTutorialVillageButtonHighlightApplied)
					{
						this._isEnterTutorialVillageButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_enter", false);
						return;
					}
				}
				else
				{
					if (this._isTavernButtonHighlightApplied)
					{
						this._isTavernButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("town_backstreet", false);
					}
					if (this._isSellPrisonerButtonHighlightApplied)
					{
						this._isSellPrisonerButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("sell_all_prisoners", false);
					}
					if (this._isShopButtonHighlightApplied)
					{
						this._isShopButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_buy", false);
					}
					if (this._isRecruitButtonHighlightApplied)
					{
						this._isRecruitButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_recruit", false);
					}
					if (this._isHostileActionButtonHighlightApplied)
					{
						this._isHostileActionButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("hostile_action", false);
					}
					if (this._isTownBesiegeButtonHighlightApplied)
					{
						this._isTownBesiegeButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("town_besiege", false);
					}
					if (this._isEnterTutorialVillageButtonHighlightApplied)
					{
						this._isEnterTutorialVillageButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_enter", false);
						return;
					}
				}
			}
			else
			{
				if (this._isTavernButtonHighlightApplied)
				{
					this._isTavernButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("town_backstreet", false);
				}
				if (this._isSellPrisonerButtonHighlightApplied)
				{
					this._isSellPrisonerButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("sell_all_prisoners", false);
				}
				if (this._isShopButtonHighlightApplied)
				{
					this._isShopButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_buy", false);
				}
				if (this._isRecruitButtonHighlightApplied)
				{
					this._isRecruitButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_recruit", false);
				}
				if (this._isHostileActionButtonHighlightApplied)
				{
					this._isHostileActionButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("hostile_action", false);
				}
				if (this._isTownBesiegeButtonHighlightApplied)
				{
					this._isTownBesiegeButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("town_besiege", false);
				}
				if (this._isEnterTutorialVillageButtonHighlightApplied)
				{
					this._isEnterTutorialVillageButtonHighlightApplied = !this.SetGameMenuButtonHighlightState("storymode_tutorial_village_enter", false);
				}
			}
		}

		// Token: 0x06000F59 RID: 3929 RVA: 0x0003FC24 File Offset: 0x0003DE24
		private bool SetGameMenuButtonHighlightState(string buttonID, bool state)
		{
			for (int i = 0; i < this.ItemList.Count; i++)
			{
				GameMenuItemVM gameMenuItemVM = this.ItemList[i];
				if (gameMenuItemVM.OptionID == buttonID)
				{
					gameMenuItemVM.IsHighlightEnabled = state;
					return true;
				}
			}
			return false;
		}

		// Token: 0x040006E1 RID: 1761
		private bool _isIdle;

		// Token: 0x040006E2 RID: 1762
		private bool _plunderEventRegistered;

		// Token: 0x040006E3 RID: 1763
		private GameMenuManager _gameMenuManager;

		// Token: 0x040006E4 RID: 1764
		private Dictionary<GameMenuOption.LeaveType, GameKey> _shortcutKeys;

		// Token: 0x040006E5 RID: 1765
		private Dictionary<string, string> _menuTextAttributeStrings;

		// Token: 0x040006E6 RID: 1766
		private Dictionary<string, object> _menuTextAttributes;

		// Token: 0x040006E7 RID: 1767
		private TextObject _menuText = TextObject.GetEmpty();

		// Token: 0x040006E8 RID: 1768
		private GameMenuVM.GameMenuItemComparer _cachedItemComparer;

		// Token: 0x040006E9 RID: 1769
		private IViewDataTracker _viewDataTracker;

		// Token: 0x040006EB RID: 1771
		private GameMenuVM.GameMenuItemPool<GameMenuItemVM> _gameMenuItemPool;

		// Token: 0x040006EC RID: 1772
		private GameMenuVM.GameMenuItemPool<GameMenuItemProgressVM> _progressItemPool;

		// Token: 0x040006ED RID: 1773
		private List<GameMenuItemVM.GameMenuItemCreationData> _newOptionsCache;

		// Token: 0x040006EE RID: 1774
		private MBBindingList<GameMenuItemVM> _itemList;

		// Token: 0x040006EF RID: 1775
		private MBBindingList<GameMenuItemProgressVM> _progressItemList;

		// Token: 0x040006F0 RID: 1776
		private string _titleText;

		// Token: 0x040006F1 RID: 1777
		private string _contextText;

		// Token: 0x040006F2 RID: 1778
		private string _background;

		// Token: 0x040006F3 RID: 1779
		private string _backgroundCopy;

		// Token: 0x040006F4 RID: 1780
		private string _menuId;

		// Token: 0x040006F5 RID: 1781
		private bool _isNight;

		// Token: 0x040006F6 RID: 1782
		private bool _isInSiegeMode;

		// Token: 0x040006F7 RID: 1783
		private bool _isEncounterMenu;

		// Token: 0x040006F8 RID: 1784
		private MBBindingList<GameMenuPlunderItemVM> _plunderItems;

		// Token: 0x040006F9 RID: 1785
		private string _latestTutorialElementID;

		// Token: 0x040006FA RID: 1786
		private bool _isTavernButtonHighlightApplied;

		// Token: 0x040006FB RID: 1787
		private bool _isSellPrisonerButtonHighlightApplied;

		// Token: 0x040006FC RID: 1788
		private bool _isShopButtonHighlightApplied;

		// Token: 0x040006FD RID: 1789
		private bool _isRecruitButtonHighlightApplied;

		// Token: 0x040006FE RID: 1790
		private bool _isHostileActionButtonHighlightApplied;

		// Token: 0x040006FF RID: 1791
		private bool _isTownBesiegeButtonHighlightApplied;

		// Token: 0x04000700 RID: 1792
		private bool _isEnterTutorialVillageButtonHighlightApplied;

		// Token: 0x04000701 RID: 1793
		private bool _requireContextTextUpdate;

		// Token: 0x02000210 RID: 528
		private class GameMenuItemPool<TItem> where TItem : class, new()
		{
			// Token: 0x06002436 RID: 9270 RVA: 0x0007F67A File Offset: 0x0007D87A
			public GameMenuItemPool(int initialCapacity)
			{
				this._pool = new List<TItem>(initialCapacity);
			}

			// Token: 0x06002437 RID: 9271 RVA: 0x0007F690 File Offset: 0x0007D890
			public TItem Get()
			{
				TItem result;
				if (this._pool.Count > 0)
				{
					result = this._pool[this._pool.Count - 1];
					this._pool.RemoveAt(this._pool.Count - 1);
				}
				else
				{
					result = Activator.CreateInstance<TItem>();
				}
				return result;
			}

			// Token: 0x06002438 RID: 9272 RVA: 0x0007F6E5 File Offset: 0x0007D8E5
			public void Release(TItem item)
			{
				this._pool.Add(item);
			}

			// Token: 0x040011A6 RID: 4518
			private readonly List<TItem> _pool;
		}

		// Token: 0x02000211 RID: 529
		private class GameMenuItemComparer : IComparer<GameMenuItemVM>
		{
			// Token: 0x06002439 RID: 9273 RVA: 0x0007F6F3 File Offset: 0x0007D8F3
			public int Compare(GameMenuItemVM x, GameMenuItemVM y)
			{
				return x.Index.CompareTo(y.Index);
			}
		}
	}
}
