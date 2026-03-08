using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200003C RID: 60
	[OverrideView(typeof(MapOverlayView))]
	public class GauntletMapOverlayView : MapView
	{
		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060002C6 RID: 710 RVA: 0x00010542 File Offset: 0x0000E742
		public bool IsInArmyManagement
		{
			get
			{
				return this._armyManagementLayer != null && this._armyManagementDatasource != null;
			}
		}

		// Token: 0x060002C7 RID: 711 RVA: 0x00010557 File Offset: 0x0000E757
		public GauntletMapOverlayView(MapScreen.MapOverlayType type)
		{
			this._type = type;
		}

		// Token: 0x060002C8 RID: 712 RVA: 0x00010568 File Offset: 0x0000E768
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._overlayDataSource = this.GetOverlay(this._type);
			this._overlayDataSource.SetExitInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			base.Layer = new GauntletLayer("MapArmyOverlay", 201, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
			MapScreen.MapOverlayType type = this._type;
			if (type == MapScreen.MapOverlayType.Army)
			{
				this._movie = this._layerAsGauntletLayer.LoadMovie("ArmyOverlay", this._overlayDataSource);
				(this._overlayDataSource as ArmyMenuOverlayVM).OpenArmyManagement = new Action(this.OpenArmyManagement);
			}
			else
			{
				Debug.FailedAssert("This kind of overlay not supported in gauntlet", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapOverlayView.cs", "CreateLayout", 62);
			}
			base.MapScreen.AddLayer(base.Layer);
		}

		// Token: 0x060002C9 RID: 713 RVA: 0x0001066B File Offset: 0x0000E86B
		public GameMenuOverlay GetOverlay(MapScreen.MapOverlayType mapOverlayType)
		{
			if (mapOverlayType == MapScreen.MapOverlayType.Army)
			{
				return new ArmyMenuOverlayVM();
			}
			Debug.FailedAssert("Game menu overlay: " + mapOverlayType.ToString() + " could not be found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapOverlayView.cs", "GetOverlay", 75);
			return null;
		}

		// Token: 0x060002CA RID: 714 RVA: 0x000106A5 File Offset: 0x0000E8A5
		protected override void OnArmyLeft()
		{
			base.MapScreen.RemoveArmyOverlay();
		}

		// Token: 0x060002CB RID: 715 RVA: 0x000106B2 File Offset: 0x0000E8B2
		protected override TutorialContexts GetTutorialContext()
		{
			if (this.IsInArmyManagement)
			{
				return TutorialContexts.ArmyManagement;
			}
			return base.GetTutorialContext();
		}

		// Token: 0x060002CC RID: 716 RVA: 0x000106C8 File Offset: 0x0000E8C8
		protected override void OnFinalize()
		{
			if (this._armyManagementLayer != null)
			{
				this.CloseArmyManagement();
			}
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			if (this._gauntletArmyManagementMovie != null)
			{
				this._layerAsGauntletLayer.ReleaseMovie(this._gauntletArmyManagementMovie);
			}
			base.MapScreen.SetIsOverlayContextMenuActive(false);
			base.MapScreen.RemoveLayer(base.Layer);
			this._movie = null;
			this._overlayDataSource = null;
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			SpriteCategory armyManagementCategory = this._armyManagementCategory;
			if (armyManagementCategory != null)
			{
				armyManagementCategory.Unload();
			}
			base.OnFinalize();
		}

		// Token: 0x060002CD RID: 717 RVA: 0x0001075D File Offset: 0x0000E95D
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060002CE RID: 718 RVA: 0x00010779 File Offset: 0x0000E979
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x060002CF RID: 719 RVA: 0x00010795 File Offset: 0x0000E995
		protected override void OnHourlyTick()
		{
			base.OnHourlyTick();
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource == null)
			{
				return;
			}
			overlayDataSource.HourlyTick();
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x000107B0 File Offset: 0x0000E9B0
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null && this._overlayDataSource != null)
			{
				this._overlayDataSource.IsInfoBarExtended = mapScreen.IsBarExtended;
			}
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource == null)
			{
				return;
			}
			overlayDataSource.OnFrameTick(dt);
		}

		// Token: 0x060002D1 RID: 721 RVA: 0x000107FC File Offset: 0x0000E9FC
		protected override bool IsEscaped()
		{
			if (this._armyManagementDatasource != null)
			{
				this._armyManagementDatasource.ExecuteCancel();
				return true;
			}
			return false;
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x00010814 File Offset: 0x0000EA14
		protected override void OnActivate()
		{
			base.OnActivate();
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource == null)
			{
				return;
			}
			overlayDataSource.Refresh();
		}

		// Token: 0x060002D3 RID: 723 RVA: 0x0001082C File Offset: 0x0000EA2C
		protected override void OnResume()
		{
			base.OnResume();
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource == null)
			{
				return;
			}
			overlayDataSource.Refresh();
		}

		// Token: 0x060002D4 RID: 724 RVA: 0x00010844 File Offset: 0x0000EA44
		protected override void OnMapScreenUpdate(float dt)
		{
			base.OnMapScreenUpdate(dt);
			if (!this._isContextMenuEnabled && this._overlayDataSource.IsContextMenuEnabled)
			{
				this._isContextMenuEnabled = true;
				base.MapScreen.SetIsOverlayContextMenuActive(true);
				base.Layer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(base.Layer);
			}
			else if (this._isContextMenuEnabled && !this._overlayDataSource.IsContextMenuEnabled)
			{
				this._isContextMenuEnabled = false;
				base.MapScreen.SetIsOverlayContextMenuActive(false);
				base.Layer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(base.Layer);
			}
			if (this._isContextMenuEnabled && base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._overlayDataSource.IsContextMenuEnabled = false;
			}
			this.HandleArmyManagementInput();
		}

		// Token: 0x060002D5 RID: 725 RVA: 0x00010914 File Offset: 0x0000EB14
		protected override void OnMenuModeTick(float dt)
		{
			base.OnMenuModeTick(dt);
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null && this._overlayDataSource != null)
			{
				this._overlayDataSource.IsInfoBarExtended = mapScreen.IsBarExtended;
			}
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource == null)
			{
				return;
			}
			overlayDataSource.OnFrameTick(dt);
		}

		// Token: 0x060002D6 RID: 726 RVA: 0x00010960 File Offset: 0x0000EB60
		private void OpenArmyManagement()
		{
			this._armyManagementDatasource = new ArmyManagementVM(new Action(this.CloseArmyManagement));
			this._armyManagementDatasource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._armyManagementDatasource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._armyManagementDatasource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			this._armyManagementDatasource.SetRemoveInputKey(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory").GetHotKey("RemoveParty"));
			this._armyManagementCategory = UIResourceManager.LoadSpriteCategory("ui_armymanagement");
			this._armyManagementLayer = new GauntletLayer("MapArmyManagement", 300, false);
			this._gauntletArmyManagementMovie = this._armyManagementLayer.LoadMovie("ArmyManagement", this._armyManagementDatasource);
			this._armyManagementLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory"));
			this._armyManagementLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._armyManagementLayer);
			base.MapScreen.AddLayer(this._armyManagementLayer);
			this._timeControlModeBeforeArmyManagementOpened = Campaign.Current.TimeControlMode;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			Campaign.Current.SetTimeControlModeLock(true);
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInArmyManagement(true);
			}
		}

		// Token: 0x060002D7 RID: 727 RVA: 0x00010AEC File Offset: 0x0000ECEC
		private void CloseArmyManagement()
		{
			if (this._armyManagementLayer != null && this._gauntletArmyManagementMovie != null)
			{
				this._armyManagementLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this._armyManagementLayer);
				this._armyManagementLayer.InputRestrictions.ResetInputRestrictions();
				this._armyManagementLayer.ReleaseMovie(this._gauntletArmyManagementMovie);
				base.MapScreen.RemoveLayer(this._armyManagementLayer);
			}
			ArmyManagementVM armyManagementDatasource = this._armyManagementDatasource;
			if (armyManagementDatasource != null)
			{
				armyManagementDatasource.OnFinalize();
			}
			this._gauntletArmyManagementMovie = null;
			this._armyManagementDatasource = null;
			this._armyManagementLayer = null;
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource != null)
			{
				overlayDataSource.Refresh();
			}
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInArmyManagement(false);
			}
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.MapWindow));
			Campaign.Current.SetTimeControlModeLock(false);
			Campaign.Current.TimeControlMode = this._timeControlModeBeforeArmyManagementOpened;
		}

		// Token: 0x060002D8 RID: 728 RVA: 0x00010BD0 File Offset: 0x0000EDD0
		private void HandleArmyManagementInput()
		{
			if (this._armyManagementLayer != null && this._armyManagementDatasource != null)
			{
				if (this._armyManagementLayer.Input.IsHotKeyReleased("Exit"))
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._armyManagementDatasource.ExecuteCancel();
					return;
				}
				if (this._armyManagementLayer.Input.IsHotKeyReleased("Confirm"))
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._armyManagementDatasource.ExecuteDone();
					return;
				}
				if (this._armyManagementLayer.Input.IsHotKeyReleased("Reset"))
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._armyManagementDatasource.ExecuteReset();
					return;
				}
				if (this._armyManagementLayer.Input.IsHotKeyReleased("RemoveParty") && this._armyManagementDatasource.FocusedItem != null)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._armyManagementDatasource.FocusedItem.ExecuteAction();
				}
			}
		}

		// Token: 0x0400010A RID: 266
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x0400010B RID: 267
		private GameMenuOverlay _overlayDataSource;

		// Token: 0x0400010C RID: 268
		private readonly MapScreen.MapOverlayType _type;

		// Token: 0x0400010D RID: 269
		private GauntletMovieIdentifier _movie;

		// Token: 0x0400010E RID: 270
		private bool _isContextMenuEnabled;

		// Token: 0x0400010F RID: 271
		private GauntletLayer _armyManagementLayer;

		// Token: 0x04000110 RID: 272
		private SpriteCategory _armyManagementCategory;

		// Token: 0x04000111 RID: 273
		private ArmyManagementVM _armyManagementDatasource;

		// Token: 0x04000112 RID: 274
		private GauntletMovieIdentifier _gauntletArmyManagementMovie;

		// Token: 0x04000113 RID: 275
		private CampaignTimeControlMode _timeControlModeBeforeArmyManagementOpened;
	}
}
