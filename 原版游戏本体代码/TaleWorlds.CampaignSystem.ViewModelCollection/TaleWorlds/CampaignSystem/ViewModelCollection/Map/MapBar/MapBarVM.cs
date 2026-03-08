using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar
{
	// Token: 0x0200005A RID: 90
	public class MapBarVM : ViewModel
	{
		// Token: 0x06000669 RID: 1641 RVA: 0x000208ED File Offset: 0x0001EAED
		protected virtual MapInfoVM CreateInfoVM()
		{
			return new MapInfoVM();
		}

		// Token: 0x0600066A RID: 1642 RVA: 0x000208F4 File Offset: 0x0001EAF4
		public void Initialize(INavigationHandler navigationHandler, IMapStateHandler mapStateHandler, Func<MapBarShortcuts> getMapBarShortcuts, Action openArmyManagement)
		{
			this._navigationHandler = navigationHandler;
			this._refreshTimeSpan = ((Campaign.Current.GetSimplifiedTimeControlMode() == CampaignTimeControlMode.UnstoppableFastForward) ? 0.1f : 2f);
			this._openArmyManagement = openArmyManagement;
			this._mapStateHandler = mapStateHandler;
			this.TutorialNotification = new ElementNotificationVM();
			this.MapInfo = this.CreateInfoVM();
			this.MapTimeControl = new MapTimeControlVM(getMapBarShortcuts, new Action(this.OnTimeControlChange), delegate()
			{
				mapStateHandler.ResetCamera(false, false);
			});
			this.MapNavigation = new MapNavigationVM(navigationHandler, getMapBarShortcuts);
			this.GatherArmyHint = new HintViewModel();
			this.OnRefresh();
			this.IsEnabled = true;
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x0600066B RID: 1643 RVA: 0x000209C3 File Offset: 0x0001EBC3
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.MapInfo.RefreshValues();
			this.MapTimeControl.RefreshValues();
			this.MapNavigation.RefreshValues();
		}

		// Token: 0x0600066C RID: 1644 RVA: 0x000209EC File Offset: 0x0001EBEC
		public void OnRefresh()
		{
			this.MapInfo.Refresh();
			this.MapTimeControl.Refresh();
			this.MapNavigation.Refresh();
		}

		// Token: 0x0600066D RID: 1645 RVA: 0x00020A10 File Offset: 0x0001EC10
		public void Tick(float dt)
		{
			int simplifiedTimeControlMode = (int)Campaign.Current.GetSimplifiedTimeControlMode();
			this._refreshTimeSpan -= dt;
			if (this._refreshTimeSpan < 0f)
			{
				this.OnRefresh();
				this._refreshTimeSpan = ((simplifiedTimeControlMode == 2) ? 0.1f : 0.2f);
			}
			this.MapInfo.Tick();
			this.MapTimeControl.Tick();
			this.MapNavigation.Tick();
			if (this._mapStateHandler != null)
			{
				this.IsCameraCentered = this._mapStateHandler.IsCameraLockedToPlayerParty();
			}
			this.IsGatherArmyVisible = this.GetIsGatherArmyVisible();
			if (this.IsGatherArmyVisible)
			{
				this.UpdateCanGatherArmyAndReason();
			}
		}

		// Token: 0x0600066E RID: 1646 RVA: 0x00020AB4 File Offset: 0x0001ECB4
		private void UpdateCanGatherArmyAndReason()
		{
			TextObject hintText;
			this.CanGatherArmy = Campaign.Current.Models.ArmyManagementCalculationModel.CanPlayerCreateArmy(out hintText);
			this.GatherArmyHint.HintText = hintText;
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x00020AEC File Offset: 0x0001ECEC
		private bool GetIsGatherArmyVisible()
		{
			if (this.MapTimeControl.IsInMap)
			{
				MobileParty mainParty = MobileParty.MainParty;
				if (((mainParty != null) ? mainParty.Army : null) == null && !Hero.MainHero.IsPrisoner && Hero.MainHero.PartyBelongedTo != null && MobileParty.MainParty.MapEvent == null)
				{
					return this.MapTimeControl.IsCenterPanelEnabled;
				}
			}
			return false;
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x00020B4A File Offset: 0x0001ED4A
		private void OnTimeControlChange()
		{
			this._refreshTimeSpan = ((Campaign.Current.GetSimplifiedTimeControlMode() == CampaignTimeControlMode.UnstoppableFastForward) ? 0.1f : 2f);
		}

		// Token: 0x06000671 RID: 1649 RVA: 0x00020B6B File Offset: 0x0001ED6B
		private void ExecuteResetCamera()
		{
			IMapStateHandler mapStateHandler = this._mapStateHandler;
			if (mapStateHandler == null)
			{
				return;
			}
			mapStateHandler.FastMoveCameraToMainParty();
		}

		// Token: 0x06000672 RID: 1650 RVA: 0x00020B7D File Offset: 0x0001ED7D
		public void ExecuteArmyManagement()
		{
			this._openArmyManagement();
		}

		// Token: 0x06000673 RID: 1651 RVA: 0x00020B8C File Offset: 0x0001ED8C
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = string.Empty;
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = this._latestTutorialElementID;
					if (this._latestTutorialElementID == "PartySpeedLabel" && !this.MapInfo.IsInfoBarExtended)
					{
						this.MapInfo.IsInfoBarExtended = true;
					}
				}
			}
		}

		// Token: 0x06000674 RID: 1652 RVA: 0x00020C14 File Offset: 0x0001EE14
		public override void OnFinalize()
		{
			base.OnFinalize();
			this._mapStateHandler = null;
			MapNavigationVM mapNavigation = this._mapNavigation;
			if (mapNavigation != null)
			{
				mapNavigation.OnFinalize();
			}
			MapTimeControlVM mapTimeControl = this._mapTimeControl;
			if (mapTimeControl != null)
			{
				mapTimeControl.OnFinalize();
			}
			this._mapInfo = null;
			this._mapNavigation = null;
			this._mapTimeControl = null;
			Game game = Game.Current;
			if (game == null)
			{
				return;
			}
			EventManager eventManager = game.EventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x170001B6 RID: 438
		// (get) Token: 0x06000675 RID: 1653 RVA: 0x00020C8A File Offset: 0x0001EE8A
		// (set) Token: 0x06000676 RID: 1654 RVA: 0x00020C92 File Offset: 0x0001EE92
		[DataSourceProperty]
		public MapInfoVM MapInfo
		{
			get
			{
				return this._mapInfo;
			}
			set
			{
				if (value != this._mapInfo)
				{
					this._mapInfo = value;
					base.OnPropertyChangedWithValue<MapInfoVM>(value, "MapInfo");
				}
			}
		}

		// Token: 0x170001B7 RID: 439
		// (get) Token: 0x06000677 RID: 1655 RVA: 0x00020CB0 File Offset: 0x0001EEB0
		// (set) Token: 0x06000678 RID: 1656 RVA: 0x00020CB8 File Offset: 0x0001EEB8
		[DataSourceProperty]
		public MapTimeControlVM MapTimeControl
		{
			get
			{
				return this._mapTimeControl;
			}
			set
			{
				if (value != this._mapTimeControl)
				{
					this._mapTimeControl = value;
					base.OnPropertyChangedWithValue<MapTimeControlVM>(value, "MapTimeControl");
				}
			}
		}

		// Token: 0x170001B8 RID: 440
		// (get) Token: 0x06000679 RID: 1657 RVA: 0x00020CD6 File Offset: 0x0001EED6
		// (set) Token: 0x0600067A RID: 1658 RVA: 0x00020CDE File Offset: 0x0001EEDE
		[DataSourceProperty]
		public MapNavigationVM MapNavigation
		{
			get
			{
				return this._mapNavigation;
			}
			set
			{
				if (value != this._mapNavigation)
				{
					this._mapNavigation = value;
					base.OnPropertyChangedWithValue<MapNavigationVM>(value, "MapNavigation");
				}
			}
		}

		// Token: 0x170001B9 RID: 441
		// (get) Token: 0x0600067B RID: 1659 RVA: 0x00020CFC File Offset: 0x0001EEFC
		// (set) Token: 0x0600067C RID: 1660 RVA: 0x00020D04 File Offset: 0x0001EF04
		[DataSourceProperty]
		public bool IsGatherArmyVisible
		{
			get
			{
				return this._isGatherArmyVisible;
			}
			set
			{
				if (value != this._isGatherArmyVisible)
				{
					this._isGatherArmyVisible = value;
					base.OnPropertyChangedWithValue(value, "IsGatherArmyVisible");
				}
			}
		}

		// Token: 0x170001BA RID: 442
		// (get) Token: 0x0600067D RID: 1661 RVA: 0x00020D22 File Offset: 0x0001EF22
		// (set) Token: 0x0600067E RID: 1662 RVA: 0x00020D2A File Offset: 0x0001EF2A
		[DataSourceProperty]
		public bool IsInInfoMode
		{
			get
			{
				return this._isInInfoMode;
			}
			set
			{
				if (value != this._isInInfoMode)
				{
					this._isInInfoMode = value;
					base.OnPropertyChangedWithValue(value, "IsInInfoMode");
				}
			}
		}

		// Token: 0x170001BB RID: 443
		// (get) Token: 0x0600067F RID: 1663 RVA: 0x00020D48 File Offset: 0x0001EF48
		// (set) Token: 0x06000680 RID: 1664 RVA: 0x00020D50 File Offset: 0x0001EF50
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x170001BC RID: 444
		// (get) Token: 0x06000681 RID: 1665 RVA: 0x00020D6E File Offset: 0x0001EF6E
		// (set) Token: 0x06000682 RID: 1666 RVA: 0x00020D76 File Offset: 0x0001EF76
		[DataSourceProperty]
		public bool CanGatherArmy
		{
			get
			{
				return this._canGatherArmy;
			}
			set
			{
				if (value != this._canGatherArmy)
				{
					this._canGatherArmy = value;
					base.OnPropertyChangedWithValue(value, "CanGatherArmy");
				}
			}
		}

		// Token: 0x170001BD RID: 445
		// (get) Token: 0x06000683 RID: 1667 RVA: 0x00020D94 File Offset: 0x0001EF94
		// (set) Token: 0x06000684 RID: 1668 RVA: 0x00020D9C File Offset: 0x0001EF9C
		[DataSourceProperty]
		public HintViewModel GatherArmyHint
		{
			get
			{
				return this._gatherArmyHint;
			}
			set
			{
				if (value != this._gatherArmyHint)
				{
					this._gatherArmyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "GatherArmyHint");
				}
			}
		}

		// Token: 0x170001BE RID: 446
		// (get) Token: 0x06000685 RID: 1669 RVA: 0x00020DBA File Offset: 0x0001EFBA
		// (set) Token: 0x06000686 RID: 1670 RVA: 0x00020DC2 File Offset: 0x0001EFC2
		[DataSourceProperty]
		public bool IsCameraCentered
		{
			get
			{
				return this._isCameraCentered;
			}
			set
			{
				if (value != this._isCameraCentered)
				{
					this._isCameraCentered = value;
					base.OnPropertyChangedWithValue(value, "IsCameraCentered");
				}
			}
		}

		// Token: 0x170001BF RID: 447
		// (get) Token: 0x06000687 RID: 1671 RVA: 0x00020DE0 File Offset: 0x0001EFE0
		// (set) Token: 0x06000688 RID: 1672 RVA: 0x00020DE8 File Offset: 0x0001EFE8
		[DataSourceProperty]
		public string CurrentScreen
		{
			get
			{
				return this._currentScreen;
			}
			set
			{
				if (this._currentScreen != value)
				{
					this._currentScreen = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentScreen");
				}
			}
		}

		// Token: 0x170001C0 RID: 448
		// (get) Token: 0x06000689 RID: 1673 RVA: 0x00020E0B File Offset: 0x0001F00B
		// (set) Token: 0x0600068A RID: 1674 RVA: 0x00020E13 File Offset: 0x0001F013
		[DataSourceProperty]
		public ElementNotificationVM TutorialNotification
		{
			get
			{
				return this._tutorialNotification;
			}
			set
			{
				if (value != this._tutorialNotification)
				{
					this._tutorialNotification = value;
					base.OnPropertyChangedWithValue<ElementNotificationVM>(value, "TutorialNotification");
				}
			}
		}

		// Token: 0x040002BB RID: 699
		protected INavigationHandler _navigationHandler;

		// Token: 0x040002BC RID: 700
		private IMapStateHandler _mapStateHandler;

		// Token: 0x040002BD RID: 701
		private Action _openArmyManagement;

		// Token: 0x040002BE RID: 702
		private float _refreshTimeSpan;

		// Token: 0x040002BF RID: 703
		private string _latestTutorialElementID;

		// Token: 0x040002C0 RID: 704
		private bool _isGatherArmyVisible;

		// Token: 0x040002C1 RID: 705
		private MapInfoVM _mapInfo;

		// Token: 0x040002C2 RID: 706
		private MapTimeControlVM _mapTimeControl;

		// Token: 0x040002C3 RID: 707
		private MapNavigationVM _mapNavigation;

		// Token: 0x040002C4 RID: 708
		private bool _isEnabled;

		// Token: 0x040002C5 RID: 709
		private bool _isCameraCentered;

		// Token: 0x040002C6 RID: 710
		private bool _canGatherArmy;

		// Token: 0x040002C7 RID: 711
		private bool _isInInfoMode;

		// Token: 0x040002C8 RID: 712
		private string _currentScreen;

		// Token: 0x040002C9 RID: 713
		private HintViewModel _gatherArmyHint;

		// Token: 0x040002CA RID: 714
		private ElementNotificationVM _tutorialNotification;
	}
}
