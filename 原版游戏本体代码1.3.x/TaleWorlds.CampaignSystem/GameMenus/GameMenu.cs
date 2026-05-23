using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus
{
	// Token: 0x020000E1 RID: 225
	public class GameMenu
	{
		// Token: 0x170005CC RID: 1484
		// (get) Token: 0x0600150C RID: 5388 RVA: 0x00060349 File Offset: 0x0005E549
		// (set) Token: 0x0600150D RID: 5389 RVA: 0x00060351 File Offset: 0x0005E551
		public GameMenu.MenuAndOptionType Type { get; private set; }

		// Token: 0x170005CD RID: 1485
		// (get) Token: 0x0600150E RID: 5390 RVA: 0x0006035A File Offset: 0x0005E55A
		// (set) Token: 0x0600150F RID: 5391 RVA: 0x00060362 File Offset: 0x0005E562
		public string StringId { get; private set; }

		// Token: 0x170005CE RID: 1486
		// (get) Token: 0x06001510 RID: 5392 RVA: 0x0006036B File Offset: 0x0005E56B
		// (set) Token: 0x06001511 RID: 5393 RVA: 0x00060373 File Offset: 0x0005E573
		public object RelatedObject { get; private set; }

		// Token: 0x170005CF RID: 1487
		// (get) Token: 0x06001512 RID: 5394 RVA: 0x0006037C File Offset: 0x0005E57C
		// (set) Token: 0x06001513 RID: 5395 RVA: 0x00060384 File Offset: 0x0005E584
		public TextObject MenuTitle { get; private set; }

		// Token: 0x170005D0 RID: 1488
		// (get) Token: 0x06001514 RID: 5396 RVA: 0x0006038D File Offset: 0x0005E58D
		// (set) Token: 0x06001515 RID: 5397 RVA: 0x00060395 File Offset: 0x0005E595
		public GameMenu.MenuOverlayType OverlayType { get; private set; }

		// Token: 0x170005D1 RID: 1489
		// (get) Token: 0x06001516 RID: 5398 RVA: 0x0006039E File Offset: 0x0005E59E
		// (set) Token: 0x06001517 RID: 5399 RVA: 0x000603A6 File Offset: 0x0005E5A6
		public bool IsReady { get; private set; }

		// Token: 0x170005D2 RID: 1490
		// (get) Token: 0x06001518 RID: 5400 RVA: 0x000603AF File Offset: 0x0005E5AF
		public int MenuItemAmount
		{
			get
			{
				return this._menuItems.Count;
			}
		}

		// Token: 0x170005D3 RID: 1491
		// (get) Token: 0x06001519 RID: 5401 RVA: 0x000603BC File Offset: 0x0005E5BC
		// (set) Token: 0x0600151A RID: 5402 RVA: 0x000603C4 File Offset: 0x0005E5C4
		public List<object> MenuRepeatObjects { get; private set; } = new List<object>();

		// Token: 0x170005D4 RID: 1492
		// (get) Token: 0x0600151B RID: 5403 RVA: 0x000603CD File Offset: 0x0005E5CD
		public object CurrentRepeatableObject
		{
			get
			{
				if (this.MenuRepeatObjects.Count <= this.CurrentRepeatableIndex)
				{
					return null;
				}
				return this.MenuRepeatObjects[this.CurrentRepeatableIndex];
			}
		}

		// Token: 0x170005D5 RID: 1493
		// (get) Token: 0x0600151C RID: 5404 RVA: 0x000603F5 File Offset: 0x0005E5F5
		// (set) Token: 0x0600151D RID: 5405 RVA: 0x000603FD File Offset: 0x0005E5FD
		public bool IsWaitMenu { get; private set; }

		// Token: 0x170005D6 RID: 1494
		// (get) Token: 0x0600151E RID: 5406 RVA: 0x00060406 File Offset: 0x0005E606
		// (set) Token: 0x0600151F RID: 5407 RVA: 0x0006040E File Offset: 0x0005E60E
		public bool IsWaitActive { get; private set; }

		// Token: 0x170005D7 RID: 1495
		// (get) Token: 0x06001520 RID: 5408 RVA: 0x00060417 File Offset: 0x0005E617
		public bool IsEmpty
		{
			get
			{
				return this.MenuRepeatObjects.Count == 0 && this.MenuItemAmount == 0;
			}
		}

		// Token: 0x170005D8 RID: 1496
		// (get) Token: 0x06001521 RID: 5409 RVA: 0x00060431 File Offset: 0x0005E631
		// (set) Token: 0x06001522 RID: 5410 RVA: 0x00060439 File Offset: 0x0005E639
		public float Progress { get; private set; }

		// Token: 0x170005D9 RID: 1497
		// (get) Token: 0x06001523 RID: 5411 RVA: 0x00060442 File Offset: 0x0005E642
		// (set) Token: 0x06001524 RID: 5412 RVA: 0x0006044A File Offset: 0x0005E64A
		public float TargetWaitHours { get; private set; }

		// Token: 0x170005DA RID: 1498
		// (get) Token: 0x06001525 RID: 5413 RVA: 0x00060453 File Offset: 0x0005E653
		// (set) Token: 0x06001526 RID: 5414 RVA: 0x0006045B File Offset: 0x0005E65B
		public OnTickDelegate OnTick { get; private set; }

		// Token: 0x170005DB RID: 1499
		// (get) Token: 0x06001527 RID: 5415 RVA: 0x00060464 File Offset: 0x0005E664
		// (set) Token: 0x06001528 RID: 5416 RVA: 0x0006046C File Offset: 0x0005E66C
		public OnConditionDelegate OnCondition { get; private set; }

		// Token: 0x170005DC RID: 1500
		// (get) Token: 0x06001529 RID: 5417 RVA: 0x00060475 File Offset: 0x0005E675
		// (set) Token: 0x0600152A RID: 5418 RVA: 0x0006047D File Offset: 0x0005E67D
		public OnConsequenceDelegate OnConsequence { get; private set; }

		// Token: 0x170005DD RID: 1501
		// (get) Token: 0x0600152B RID: 5419 RVA: 0x00060486 File Offset: 0x0005E686
		// (set) Token: 0x0600152C RID: 5420 RVA: 0x0006048E File Offset: 0x0005E68E
		public int CurrentRepeatableIndex { get; set; }

		// Token: 0x170005DE RID: 1502
		// (get) Token: 0x0600152D RID: 5421 RVA: 0x00060497 File Offset: 0x0005E697
		public IEnumerable<GameMenuOption> MenuOptions
		{
			get
			{
				return this._menuItems;
			}
		}

		// Token: 0x0600152E RID: 5422 RVA: 0x0006049F File Offset: 0x0005E69F
		internal GameMenu(string idString)
		{
			this.StringId = idString;
			this._menuItems = new List<GameMenuOption>();
		}

		// Token: 0x0600152F RID: 5423 RVA: 0x000604C4 File Offset: 0x0005E6C4
		internal void Initialize(TextObject text, OnInitDelegate initDelegate, GameMenu.MenuOverlayType overlay, GameMenu.MenuFlags flags = GameMenu.MenuFlags.None, object relatedObject = null)
		{
			this.CurrentRepeatableIndex = 0;
			this.LastSelectedMenuObject = null;
			this._defaultText = text;
			this.OnInit = initDelegate;
			this.OverlayType = overlay;
			this.AutoSelectFirst = (flags & GameMenu.MenuFlags.AutoSelectFirst) > GameMenu.MenuFlags.None;
			this.RelatedObject = relatedObject;
			this.IsReady = true;
		}

		// Token: 0x06001530 RID: 5424 RVA: 0x00060510 File Offset: 0x0005E710
		internal void Initialize(TextObject text, OnInitDelegate initDelegate, OnConditionDelegate condition, OnConsequenceDelegate consequence, OnTickDelegate tick, GameMenu.MenuAndOptionType type, GameMenu.MenuOverlayType overlay, float targetWaitHours = 0f, GameMenu.MenuFlags flags = GameMenu.MenuFlags.None, object relatedObject = null)
		{
			this.CurrentRepeatableIndex = 0;
			this.LastSelectedMenuObject = null;
			this._defaultText = text;
			this.OnInit = initDelegate;
			this.OverlayType = overlay;
			this.AutoSelectFirst = (flags & GameMenu.MenuFlags.AutoSelectFirst) > GameMenu.MenuFlags.None;
			this.RelatedObject = relatedObject;
			this.OnConsequence = consequence;
			this.OnCondition = condition;
			this.Type = type;
			this.OnTick = tick;
			this.TargetWaitHours = targetWaitHours;
			this.IsWaitMenu = type > GameMenu.MenuAndOptionType.RegularMenuOption;
			this.IsReady = true;
		}

		// Token: 0x06001531 RID: 5425 RVA: 0x0006058F File Offset: 0x0005E78F
		public void SetMenuRepeatObjects(IEnumerable<object> list)
		{
			this.MenuRepeatObjects = list.ToList<object>();
		}

		// Token: 0x06001532 RID: 5426 RVA: 0x0006059D File Offset: 0x0005E79D
		private void AddOption(GameMenuOption newOption, int index = -1)
		{
			if (index >= 0 && this._menuItems.Count >= index)
			{
				this._menuItems.Insert(index, newOption);
				return;
			}
			this._menuItems.Add(newOption);
		}

		// Token: 0x06001533 RID: 5427 RVA: 0x000605CB File Offset: 0x0005E7CB
		public bool GetMenuOptionConditionsHold(Game game, MenuContext menuContext, int menuItemNumber)
		{
			if (this.IsWaitMenu)
			{
				return this._menuItems[menuItemNumber].GetConditionsHold(game, menuContext) && this.RunWaitMenuCondition(menuContext);
			}
			return this._menuItems[menuItemNumber].GetConditionsHold(game, menuContext);
		}

		// Token: 0x06001534 RID: 5428 RVA: 0x00060607 File Offset: 0x0005E807
		public TextObject GetMenuOptionText(int menuItemNumber)
		{
			return this._menuItems[menuItemNumber].Text;
		}

		// Token: 0x06001535 RID: 5429 RVA: 0x0006061A File Offset: 0x0005E81A
		public GameMenuOption GetGameMenuOption(int menuItemNumber)
		{
			return this._menuItems[menuItemNumber];
		}

		// Token: 0x06001536 RID: 5430 RVA: 0x00060628 File Offset: 0x0005E828
		public TextObject GetMenuOptionText2(int menuItemNumber)
		{
			return this._menuItems[menuItemNumber].Text2;
		}

		// Token: 0x06001537 RID: 5431 RVA: 0x0006063B File Offset: 0x0005E83B
		public string GetMenuOptionIdString(int menuItemNumber)
		{
			return this._menuItems[menuItemNumber].IdString;
		}

		// Token: 0x06001538 RID: 5432 RVA: 0x0006064E File Offset: 0x0005E84E
		public TextObject GetMenuOptionTooltip(int menuItemNumber)
		{
			return this._menuItems[menuItemNumber].Tooltip;
		}

		// Token: 0x06001539 RID: 5433 RVA: 0x00060661 File Offset: 0x0005E861
		public bool GetMenuOptionIsLeave(int menuItemNumber)
		{
			return this._menuItems[menuItemNumber].IsLeave;
		}

		// Token: 0x0600153A RID: 5434 RVA: 0x00060674 File Offset: 0x0005E874
		public void SetProgressOfWaitingInMenu(float progress)
		{
			this.Progress = progress;
		}

		// Token: 0x0600153B RID: 5435 RVA: 0x0006067D File Offset: 0x0005E87D
		public void SetTargetedWaitingTimeAndInitialProgress(float targetedWaitingTime, float initialProgress)
		{
			this.TargetWaitHours = targetedWaitingTime;
			this.SetProgressOfWaitingInMenu(initialProgress);
		}

		// Token: 0x0600153C RID: 5436 RVA: 0x00060690 File Offset: 0x0005E890
		public GameMenuOption GetLeaveMenuOption(Game game, MenuContext menuContext)
		{
			for (int i = 0; i < this._menuItems.Count; i++)
			{
				if (this._menuItems[i].IsLeave && this._menuItems[i].IsEnabled && this._menuItems[i].GetConditionsHold(game, menuContext))
				{
					return this._menuItems[i];
				}
			}
			return null;
		}

		// Token: 0x0600153D RID: 5437 RVA: 0x000606FC File Offset: 0x0005E8FC
		public void RunOnTick(MenuContext menuContext, float dt)
		{
			if (this.IsWaitMenu && this.IsWaitActive)
			{
				if (this.OnTick != null)
				{
					MenuCallbackArgs args = new MenuCallbackArgs(menuContext, this.MenuTitle);
					this.OnTick(args, CampaignTime.Now - this._previousTickTime);
					this._previousTickTime = CampaignTime.Now;
				}
				if (this.Progress >= 1f)
				{
					this.EndWait();
					this.RunWaitMenuConsequence(menuContext);
				}
			}
		}

		// Token: 0x0600153E RID: 5438 RVA: 0x00060770 File Offset: 0x0005E970
		public bool RunWaitMenuCondition(MenuContext menuContext)
		{
			if (this.OnCondition != null)
			{
				MenuCallbackArgs args = new MenuCallbackArgs(menuContext, this.MenuTitle);
				bool flag = this.OnCondition(args);
				if (flag && !this.IsWaitActive)
				{
					menuContext.GameMenu.StartWait();
				}
				return flag;
			}
			return true;
		}

		// Token: 0x0600153F RID: 5439 RVA: 0x000607B8 File Offset: 0x0005E9B8
		public void RunWaitMenuConsequence(MenuContext menuContext)
		{
			if (this.OnConsequence != null)
			{
				MenuCallbackArgs args = new MenuCallbackArgs(menuContext, this.MenuTitle);
				this.OnConsequence(args);
			}
		}

		// Token: 0x06001540 RID: 5440 RVA: 0x000607E8 File Offset: 0x0005E9E8
		public void RunMenuOptionConsequence(MenuContext menuContext, int menuItemNumber)
		{
			if (menuItemNumber >= this._menuItems.Count || menuItemNumber < 0)
			{
				Debug.FailedAssert("menuItemNumber out of bounds", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenu.cs", "RunMenuOptionConsequence", 263);
				menuItemNumber = this._menuItems.Count - 1;
			}
			GameMenuOption gameMenuOption = this._menuItems[menuItemNumber];
			if (gameMenuOption.IsLeave && this.IsWaitMenu)
			{
				this.EndWait();
			}
			gameMenuOption.RunConsequence(menuContext);
			if (Campaign.Current != null)
			{
				CampaignEventDispatcher.Instance.OnGameMenuOptionSelected(this, gameMenuOption);
			}
		}

		// Token: 0x06001541 RID: 5441 RVA: 0x0006086C File Offset: 0x0005EA6C
		public void StartWait()
		{
			this._previousTickTime = CampaignTime.Now;
			this.IsWaitActive = true;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppableFastForward;
		}

		// Token: 0x06001542 RID: 5442 RVA: 0x0006088B File Offset: 0x0005EA8B
		public void EndWait()
		{
			this.IsWaitActive = false;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		}

		// Token: 0x06001543 RID: 5443 RVA: 0x0006089F File Offset: 0x0005EA9F
		private void ResetVariablesOnInit()
		{
			this.Progress = 0f;
			this.CurrentRepeatableIndex = 0;
			this.MenuRepeatObjects.Clear();
		}

		// Token: 0x06001544 RID: 5444 RVA: 0x000608C0 File Offset: 0x0005EAC0
		public void RunOnInit(Game game, MenuContext menuContext)
		{
			this.ResetVariablesOnInit();
			MenuCallbackArgs menuCallbackArgs = new MenuCallbackArgs(menuContext, this.MenuTitle);
			if (this.OnInit != null)
			{
				Debug.Print("[GAME MENU] " + menuContext.GameMenu.StringId, 0, Debug.DebugColor.White, 17592186044416UL);
				this.OnInit(menuCallbackArgs);
				this.MenuTitle = menuCallbackArgs.MenuTitle;
			}
			CampaignEventDispatcher.Instance.OnGameMenuOpened(menuCallbackArgs);
		}

		// Token: 0x06001545 RID: 5445 RVA: 0x00060934 File Offset: 0x0005EB34
		public void PreInit(MenuContext menuContext)
		{
			MenuCallbackArgs args = new MenuCallbackArgs(menuContext, this.MenuTitle);
			CampaignEventDispatcher.Instance.BeforeGameMenuOpened(args);
		}

		// Token: 0x06001546 RID: 5446 RVA: 0x0006095C File Offset: 0x0005EB5C
		public void AfterInit(MenuContext menuContext)
		{
			MenuCallbackArgs args = new MenuCallbackArgs(menuContext, this.MenuTitle);
			CampaignEventDispatcher.Instance.AfterGameMenuInitialized(args);
		}

		// Token: 0x06001547 RID: 5447 RVA: 0x00060981 File Offset: 0x0005EB81
		public TextObject GetText()
		{
			return this._defaultText;
		}

		// Token: 0x170005DF RID: 1503
		// (get) Token: 0x06001548 RID: 5448 RVA: 0x00060989 File Offset: 0x0005EB89
		// (set) Token: 0x06001549 RID: 5449 RVA: 0x00060991 File Offset: 0x0005EB91
		public bool AutoSelectFirst { get; private set; }

		// Token: 0x0600154A RID: 5450 RVA: 0x0006099C File Offset: 0x0005EB9C
		public static void ActivateGameMenu(string menuId)
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			if (Campaign.Current.CurrentMenuContext == null)
			{
				Campaign.Current.GameMenuManager.SetNextMenu(menuId);
				MapState mapState = Game.Current.GameStateManager.LastOrDefault<MapState>();
				if (mapState != null)
				{
					mapState.EnterMenuMode();
				}
				bool flag;
				if (mapState == null)
				{
					flag = null != null;
				}
				else
				{
					MenuContext menuContext = mapState.MenuContext;
					flag = ((menuContext != null) ? menuContext.GameMenu : null) != null;
				}
				if (flag)
				{
					GameMenu gameMenu = mapState.MenuContext.GameMenu;
					if (gameMenu != null && gameMenu.IsWaitMenu)
					{
						mapState.MenuContext.GameMenu.StartWait();
						return;
					}
				}
			}
			else
			{
				GameMenu.SwitchToMenu(menuId);
			}
		}

		// Token: 0x0600154B RID: 5451 RVA: 0x00060A34 File Offset: 0x0005EC34
		public static void SwitchToMenu(string menuId)
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			MenuContext currentMenuContext = Campaign.Current.CurrentMenuContext;
			if (currentMenuContext != null)
			{
				currentMenuContext.SwitchToMenu(menuId);
				if (currentMenuContext.GameMenu.IsWaitMenu && Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
				{
					currentMenuContext.GameMenu.StartWait();
					return;
				}
			}
			else
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameMenus\\GameMenu.cs", "SwitchToMenu", 384);
			}
		}

		// Token: 0x0600154C RID: 5452 RVA: 0x00060A9F File Offset: 0x0005EC9F
		public static void ExitToLast()
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			Campaign.Current.GameMenuManager.ExitToLast();
		}

		// Token: 0x0600154D RID: 5453 RVA: 0x00060ABC File Offset: 0x0005ECBC
		internal void AddOption(string optionId, TextObject optionText, GameMenuOption.OnConditionDelegate condition, GameMenuOption.OnConsequenceDelegate consequence, int index = -1, bool isLeave = false, bool isRepeatable = false, object relatedObject = null)
		{
			this.AddOption(new GameMenuOption(GameMenu.MenuAndOptionType.RegularMenuOption, optionId, optionText, optionText, condition, consequence, isLeave, isRepeatable, relatedObject), index);
		}

		// Token: 0x0600154E RID: 5454 RVA: 0x00060AE3 File Offset: 0x0005ECE3
		internal void RemoveMenuOption(GameMenuOption option)
		{
			this._menuItems.Remove(option);
		}

		// Token: 0x040006F3 RID: 1779
		private TextObject _defaultText;

		// Token: 0x040006F9 RID: 1785
		public OnInitDelegate OnInit;

		// Token: 0x040006FC RID: 1788
		public object LastSelectedMenuObject;

		// Token: 0x04000704 RID: 1796
		private CampaignTime _previousTickTime;

		// Token: 0x04000705 RID: 1797
		private readonly List<GameMenuOption> _menuItems;

		// Token: 0x0200055E RID: 1374
		public enum MenuOverlayType
		{
			// Token: 0x040016B1 RID: 5809
			None,
			// Token: 0x040016B2 RID: 5810
			SettlementWithParties,
			// Token: 0x040016B3 RID: 5811
			SettlementWithCharacters,
			// Token: 0x040016B4 RID: 5812
			SettlementWithBoth,
			// Token: 0x040016B5 RID: 5813
			Encounter
		}

		// Token: 0x0200055F RID: 1375
		public enum MenuFlags
		{
			// Token: 0x040016B7 RID: 5815
			None,
			// Token: 0x040016B8 RID: 5816
			AutoSelectFirst
		}

		// Token: 0x02000560 RID: 1376
		public enum MenuAndOptionType
		{
			// Token: 0x040016BA RID: 5818
			RegularMenuOption,
			// Token: 0x040016BB RID: 5819
			WaitMenuShowProgressAndHoursOption,
			// Token: 0x040016BC RID: 5820
			WaitMenuShowOnlyProgressOption,
			// Token: 0x040016BD RID: 5821
			WaitMenuHideProgressAndHoursOption
		}
	}
}
