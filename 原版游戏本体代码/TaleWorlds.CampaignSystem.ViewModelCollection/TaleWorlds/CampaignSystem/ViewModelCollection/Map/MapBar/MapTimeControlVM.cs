using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar
{
	// Token: 0x0200005F RID: 95
	public class MapTimeControlVM : ViewModel
	{
		// Token: 0x170001D7 RID: 471
		// (get) Token: 0x060006D9 RID: 1753 RVA: 0x00021DBC File Offset: 0x0001FFBC
		// (set) Token: 0x060006DA RID: 1754 RVA: 0x00021DC4 File Offset: 0x0001FFC4
		public bool IsInBattleSimulation { get; set; }

		// Token: 0x170001D8 RID: 472
		// (get) Token: 0x060006DB RID: 1755 RVA: 0x00021DCD File Offset: 0x0001FFCD
		// (set) Token: 0x060006DC RID: 1756 RVA: 0x00021DD5 File Offset: 0x0001FFD5
		public bool IsInRecruitment { get; set; }

		// Token: 0x170001D9 RID: 473
		// (get) Token: 0x060006DD RID: 1757 RVA: 0x00021DDE File Offset: 0x0001FFDE
		// (set) Token: 0x060006DE RID: 1758 RVA: 0x00021DE6 File Offset: 0x0001FFE6
		public bool IsEncyclopediaOpen { get; set; }

		// Token: 0x170001DA RID: 474
		// (get) Token: 0x060006DF RID: 1759 RVA: 0x00021DEF File Offset: 0x0001FFEF
		// (set) Token: 0x060006E0 RID: 1760 RVA: 0x00021DF7 File Offset: 0x0001FFF7
		public bool IsInArmyManagement { get; set; }

		// Token: 0x170001DB RID: 475
		// (get) Token: 0x060006E1 RID: 1761 RVA: 0x00021E00 File Offset: 0x00020000
		// (set) Token: 0x060006E2 RID: 1762 RVA: 0x00021E08 File Offset: 0x00020008
		public bool IsInTownManagement { get; set; }

		// Token: 0x170001DC RID: 476
		// (get) Token: 0x060006E3 RID: 1763 RVA: 0x00021E11 File Offset: 0x00020011
		// (set) Token: 0x060006E4 RID: 1764 RVA: 0x00021E19 File Offset: 0x00020019
		public bool IsInHideoutTroopManage { get; set; }

		// Token: 0x170001DD RID: 477
		// (get) Token: 0x060006E5 RID: 1765 RVA: 0x00021E22 File Offset: 0x00020022
		// (set) Token: 0x060006E6 RID: 1766 RVA: 0x00021E2A File Offset: 0x0002002A
		public bool IsInMap { get; set; }

		// Token: 0x170001DE RID: 478
		// (get) Token: 0x060006E7 RID: 1767 RVA: 0x00021E33 File Offset: 0x00020033
		// (set) Token: 0x060006E8 RID: 1768 RVA: 0x00021E3B File Offset: 0x0002003B
		public bool IsInCampaignOptions { get; set; }

		// Token: 0x170001DF RID: 479
		// (get) Token: 0x060006E9 RID: 1769 RVA: 0x00021E44 File Offset: 0x00020044
		// (set) Token: 0x060006EA RID: 1770 RVA: 0x00021E4C File Offset: 0x0002004C
		public bool IsEscapeMenuOpened { get; set; }

		// Token: 0x170001E0 RID: 480
		// (get) Token: 0x060006EB RID: 1771 RVA: 0x00021E55 File Offset: 0x00020055
		// (set) Token: 0x060006EC RID: 1772 RVA: 0x00021E5D File Offset: 0x0002005D
		public bool IsMarriageOfferPopupActive { get; set; }

		// Token: 0x170001E1 RID: 481
		// (get) Token: 0x060006ED RID: 1773 RVA: 0x00021E66 File Offset: 0x00020066
		// (set) Token: 0x060006EE RID: 1774 RVA: 0x00021E6E File Offset: 0x0002006E
		public bool IsHeirSelectionPopupActive { get; set; }

		// Token: 0x170001E2 RID: 482
		// (get) Token: 0x060006EF RID: 1775 RVA: 0x00021E77 File Offset: 0x00020077
		// (set) Token: 0x060006F0 RID: 1776 RVA: 0x00021E7F File Offset: 0x0002007F
		public bool IsMapCheatsActive { get; set; }

		// Token: 0x170001E3 RID: 483
		// (get) Token: 0x060006F1 RID: 1777 RVA: 0x00021E88 File Offset: 0x00020088
		// (set) Token: 0x060006F2 RID: 1778 RVA: 0x00021E90 File Offset: 0x00020090
		public bool IsMapIncidentActive { get; set; }

		// Token: 0x170001E4 RID: 484
		// (get) Token: 0x060006F3 RID: 1779 RVA: 0x00021E99 File Offset: 0x00020099
		// (set) Token: 0x060006F4 RID: 1780 RVA: 0x00021EA1 File Offset: 0x000200A1
		public bool IsOverlayContextMenuEnabled { get; set; }

		// Token: 0x060006F5 RID: 1781 RVA: 0x00021EAC File Offset: 0x000200AC
		public MapTimeControlVM(Func<MapBarShortcuts> getMapBarShortcuts, Action onTimeFlowStateChange, Action onCameraResetted)
		{
			this._onTimeFlowStateChange = onTimeFlowStateChange;
			this._getMapBarShortcuts = getMapBarShortcuts;
			this._onCameraReset = onCameraResetted;
			this.IsCenterPanelEnabled = false;
			this._lastSetDate = CampaignTime.Zero;
			this.PlayHint = new BasicTooltipViewModel();
			this.FastForwardHint = new BasicTooltipViewModel();
			this.PauseHint = new BasicTooltipViewModel();
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
			CampaignEvents.OnSaveStartedEvent.AddNonSerializedListener(this, new Action(this.OnSaveStarted));
			CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, new Action<bool, string>(this.OnSaveOver));
			this.RefreshValues();
		}

		// Token: 0x060006F6 RID: 1782 RVA: 0x00021F64 File Offset: 0x00020164
		public override void RefreshValues()
		{
			base.RefreshValues();
			this._shortcuts = this._getMapBarShortcuts();
			if (Input.IsGamepadActive)
			{
				this.PlayHint.SetHintCallback(() => GameTexts.FindText("str_play", null).ToString());
				this.FastForwardHint.SetHintCallback(() => GameTexts.FindText("str_fast_forward", null).ToString());
				this.PauseHint.SetHintCallback(() => GameTexts.FindText("str_pause", null).ToString());
			}
			else
			{
				this.PlayHint.SetHintCallback(delegate
				{
					GameTexts.SetVariable("TEXT", GameTexts.FindText("str_play", null).ToString());
					GameTexts.SetVariable("HOTKEY", this._shortcuts.PlayHotkey);
					return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
				});
				this.FastForwardHint.SetHintCallback(delegate
				{
					GameTexts.SetVariable("TEXT", GameTexts.FindText("str_fast_forward", null).ToString());
					GameTexts.SetVariable("HOTKEY", this._shortcuts.FastForwardHotkey);
					return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
				});
				this.PauseHint.SetHintCallback(delegate
				{
					GameTexts.SetVariable("TEXT", GameTexts.FindText("str_pause", null).ToString());
					GameTexts.SetVariable("HOTKEY", this._shortcuts.PauseHotkey);
					return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
				});
			}
			this.RefreshPausedText();
			this.Date = CampaignTime.Now.ToString();
			this._lastSetDate = CampaignTime.Now;
		}

		// Token: 0x060006F7 RID: 1783 RVA: 0x00022084 File Offset: 0x00020284
		private void RefreshPausedText()
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				Debug.FailedAssert("Main party is null when refreshing pause text", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Map\\MapBar\\MapTimeControlVM.cs", "RefreshPausedText", 107);
				this.PausedText = GameTexts.FindText("str_paused_capital", null).ToString();
				return;
			}
			if (this.IsCurrentlyPausedOnMap)
			{
				this.PausedText = GameTexts.FindText("str_paused_capital", null).ToString();
				return;
			}
			if (!MobileParty.MainParty.IsTransitionInProgress)
			{
				this.PausedText = string.Empty;
				return;
			}
			if (mainParty.IsCurrentlyAtSea)
			{
				this.PausedText = new TextObject("{=g1op0Thi}DISEMBARKING", null).ToString();
				return;
			}
			this.PausedText = new TextObject("{=Lt0PzKHN}EMBARKING", null).ToString();
		}

		// Token: 0x060006F8 RID: 1784 RVA: 0x00022134 File Offset: 0x00020334
		public override void OnFinalize()
		{
			base.OnFinalize();
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
			this._onTimeFlowStateChange = null;
			this._getMapBarShortcuts = null;
			this._onCameraReset = null;
			CampaignEvents.OnSaveStartedEvent.ClearListeners(this);
			CampaignEvents.OnSaveOverEvent.ClearListeners(this);
		}

		// Token: 0x060006F9 RID: 1785 RVA: 0x00022192 File Offset: 0x00020392
		private void OnGamepadActiveStateChanged()
		{
			this.RefreshValues();
		}

		// Token: 0x060006FA RID: 1786 RVA: 0x0002219A File Offset: 0x0002039A
		private void OnSaveStarted()
		{
			this._isSaving = true;
		}

		// Token: 0x060006FB RID: 1787 RVA: 0x000221A3 File Offset: 0x000203A3
		private void OnSaveOver(bool wasSuccessful, string saveName)
		{
			this._isSaving = false;
		}

		// Token: 0x060006FC RID: 1788 RVA: 0x000221AC File Offset: 0x000203AC
		public void Tick()
		{
			this.TimeFlowState = (int)Campaign.Current.GetSimplifiedTimeControlMode();
			this.IsCurrentlyPausedOnMap = (this.TimeFlowState == 0 || this.TimeFlowState == 6) && this.IsCenterPanelEnabled && !this.IsEscapeMenuOpened && !this._isSaving;
			this.IsCenterPanelEnabled = !this.IsInBattleSimulation && !this.IsInRecruitment && !this.IsEncyclopediaOpen && !this.IsInTownManagement && !this.IsInArmyManagement && this.IsInMap && !this.IsInCampaignOptions && !this.IsInHideoutTroopManage && !this.IsMarriageOfferPopupActive && !this.IsHeirSelectionPopupActive && !this.IsMapCheatsActive && !this.IsMapIncidentActive && !this.IsOverlayContextMenuEnabled;
			if (MobileParty.MainParty.IsTransitionInProgress != this._mainPartyPreviousTransitioning)
			{
				this._mainPartyPreviousTransitioning = MobileParty.MainParty.IsTransitionInProgress;
				this.RefreshPausedText();
			}
		}

		// Token: 0x060006FD RID: 1789 RVA: 0x00022298 File Offset: 0x00020498
		public void Refresh()
		{
			if (!this._lastSetDate.StringSameAs(CampaignTime.Now))
			{
				this.Date = CampaignTime.Now.ToString();
				this._lastSetDate = CampaignTime.Now;
			}
			this.Time = CampaignTime.Now.ToHours % (double)CampaignTime.HoursInDay;
			this.TimeOfDayHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTimeOfDayAndResetCameraTooltip());
		}

		// Token: 0x060006FE RID: 1790 RVA: 0x0002231F File Offset: 0x0002051F
		private void SetTimeSpeed(int speed)
		{
			Campaign.Current.SetTimeSpeed(speed);
			this._onTimeFlowStateChange();
		}

		// Token: 0x060006FF RID: 1791 RVA: 0x00022338 File Offset: 0x00020538
		public void ExecuteTimeControlChange(int selectedTimeSpeed)
		{
			if (Campaign.Current.CurrentMenuContext == null || (Campaign.Current.CurrentMenuContext.GameMenu.IsWaitActive && !Campaign.Current.TimeControlModeLock))
			{
				int num = selectedTimeSpeed;
				if (this._timeFlowState == 3 && num == 2)
				{
					num = 4;
				}
				else if (this._timeFlowState == 4 && num == 1)
				{
					num = 3;
				}
				else if (this._timeFlowState == 2 && num == 0)
				{
					num = 6;
				}
				if (num != this._timeFlowState)
				{
					this.TimeFlowState = num;
					this.SetTimeSpeed(selectedTimeSpeed);
				}
			}
		}

		// Token: 0x06000700 RID: 1792 RVA: 0x000223BC File Offset: 0x000205BC
		public void ExecuteResetCamera()
		{
			this._onCameraReset();
		}

		// Token: 0x170001E5 RID: 485
		// (get) Token: 0x06000701 RID: 1793 RVA: 0x000223C9 File Offset: 0x000205C9
		// (set) Token: 0x06000702 RID: 1794 RVA: 0x000223D1 File Offset: 0x000205D1
		[DataSourceProperty]
		public BasicTooltipViewModel TimeOfDayHint
		{
			get
			{
				return this._timeOfDayHint;
			}
			set
			{
				if (value != this._timeOfDayHint)
				{
					this._timeOfDayHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TimeOfDayHint");
				}
			}
		}

		// Token: 0x170001E6 RID: 486
		// (get) Token: 0x06000703 RID: 1795 RVA: 0x000223EF File Offset: 0x000205EF
		// (set) Token: 0x06000704 RID: 1796 RVA: 0x000223F7 File Offset: 0x000205F7
		[DataSourceProperty]
		public bool IsCurrentlyPausedOnMap
		{
			get
			{
				return this._isCurrentlyPausedOnMap;
			}
			set
			{
				if (value != this._isCurrentlyPausedOnMap)
				{
					this._isCurrentlyPausedOnMap = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentlyPausedOnMap");
					this.RefreshPausedText();
				}
			}
		}

		// Token: 0x170001E7 RID: 487
		// (get) Token: 0x06000705 RID: 1797 RVA: 0x0002241B File Offset: 0x0002061B
		// (set) Token: 0x06000706 RID: 1798 RVA: 0x00022423 File Offset: 0x00020623
		[DataSourceProperty]
		public bool IsCenterPanelEnabled
		{
			get
			{
				return this._isCenterPanelEnabled;
			}
			set
			{
				if (value != this._isCenterPanelEnabled)
				{
					this._isCenterPanelEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsCenterPanelEnabled");
				}
			}
		}

		// Token: 0x170001E8 RID: 488
		// (get) Token: 0x06000707 RID: 1799 RVA: 0x00022441 File Offset: 0x00020641
		// (set) Token: 0x06000708 RID: 1800 RVA: 0x00022449 File Offset: 0x00020649
		[DataSourceProperty]
		public double Time
		{
			get
			{
				return this._time;
			}
			set
			{
				if (this._time != value)
				{
					this._time = value;
					base.OnPropertyChangedWithValue(value, "Time");
				}
			}
		}

		// Token: 0x170001E9 RID: 489
		// (get) Token: 0x06000709 RID: 1801 RVA: 0x00022467 File Offset: 0x00020667
		// (set) Token: 0x0600070A RID: 1802 RVA: 0x0002246F File Offset: 0x0002066F
		[DataSourceProperty]
		public string PausedText
		{
			get
			{
				return this._pausedText;
			}
			set
			{
				if (this._pausedText != value)
				{
					this._pausedText = value;
					base.OnPropertyChangedWithValue<string>(value, "PausedText");
				}
			}
		}

		// Token: 0x170001EA RID: 490
		// (get) Token: 0x0600070B RID: 1803 RVA: 0x00022492 File Offset: 0x00020692
		// (set) Token: 0x0600070C RID: 1804 RVA: 0x0002249A File Offset: 0x0002069A
		[DataSourceProperty]
		public string Date
		{
			get
			{
				return this._date;
			}
			set
			{
				if (value != this._date)
				{
					this._date = value;
					base.OnPropertyChangedWithValue<string>(value, "Date");
				}
			}
		}

		// Token: 0x170001EB RID: 491
		// (get) Token: 0x0600070D RID: 1805 RVA: 0x000224BD File Offset: 0x000206BD
		// (set) Token: 0x0600070E RID: 1806 RVA: 0x000224C5 File Offset: 0x000206C5
		[DataSourceProperty]
		public int TimeFlowState
		{
			get
			{
				return this._timeFlowState;
			}
			set
			{
				if (value != this._timeFlowState)
				{
					this._timeFlowState = value;
					base.OnPropertyChangedWithValue(value, "TimeFlowState");
				}
			}
		}

		// Token: 0x170001EC RID: 492
		// (get) Token: 0x0600070F RID: 1807 RVA: 0x000224E3 File Offset: 0x000206E3
		// (set) Token: 0x06000710 RID: 1808 RVA: 0x000224EB File Offset: 0x000206EB
		[DataSourceProperty]
		public BasicTooltipViewModel PauseHint
		{
			get
			{
				return this._pauseHint;
			}
			set
			{
				if (value != this._pauseHint)
				{
					this._pauseHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "PauseHint");
				}
			}
		}

		// Token: 0x170001ED RID: 493
		// (get) Token: 0x06000711 RID: 1809 RVA: 0x00022509 File Offset: 0x00020709
		// (set) Token: 0x06000712 RID: 1810 RVA: 0x00022511 File Offset: 0x00020711
		[DataSourceProperty]
		public BasicTooltipViewModel PlayHint
		{
			get
			{
				return this._playHint;
			}
			set
			{
				if (value != this._playHint)
				{
					this._playHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "PlayHint");
				}
			}
		}

		// Token: 0x170001EE RID: 494
		// (get) Token: 0x06000713 RID: 1811 RVA: 0x0002252F File Offset: 0x0002072F
		// (set) Token: 0x06000714 RID: 1812 RVA: 0x00022537 File Offset: 0x00020737
		[DataSourceProperty]
		public BasicTooltipViewModel FastForwardHint
		{
			get
			{
				return this._fastForwardHint;
			}
			set
			{
				if (value != this._fastForwardHint)
				{
					this._fastForwardHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "FastForwardHint");
				}
			}
		}

		// Token: 0x04000303 RID: 771
		private bool _mainPartyPreviousTransitioning;

		// Token: 0x04000304 RID: 772
		private Action _onTimeFlowStateChange;

		// Token: 0x04000305 RID: 773
		private Func<MapBarShortcuts> _getMapBarShortcuts;

		// Token: 0x04000306 RID: 774
		private MapBarShortcuts _shortcuts;

		// Token: 0x04000307 RID: 775
		private Action _onCameraReset;

		// Token: 0x04000308 RID: 776
		private CampaignTime _lastSetDate;

		// Token: 0x04000309 RID: 777
		private bool _isSaving;

		// Token: 0x0400030A RID: 778
		private int _timeFlowState = -1;

		// Token: 0x0400030B RID: 779
		private double _time;

		// Token: 0x0400030C RID: 780
		private string _date;

		// Token: 0x0400030D RID: 781
		private string _pausedText;

		// Token: 0x0400030E RID: 782
		private bool _isCurrentlyPausedOnMap;

		// Token: 0x0400030F RID: 783
		private bool _isCenterPanelEnabled;

		// Token: 0x04000310 RID: 784
		private BasicTooltipViewModel _pauseHint;

		// Token: 0x04000311 RID: 785
		private BasicTooltipViewModel _playHint;

		// Token: 0x04000312 RID: 786
		private BasicTooltipViewModel _fastForwardHint;

		// Token: 0x04000313 RID: 787
		private BasicTooltipViewModel _timeOfDayHint;
	}
}
