using System;
using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu
{
	// Token: 0x02000026 RID: 38
	[OverrideView(typeof(MenuOverlayBaseView))]
	public class GauntletMenuOverlayBaseView : MenuView
	{
		// Token: 0x060001F3 RID: 499 RVA: 0x0000C4D0 File Offset: 0x0000A6D0
		protected override void OnInitialize()
		{
			GameMenu.MenuOverlayType menuOverlayType = Campaign.Current.GameMenuManager.GetMenuOverlayType(base.MenuContext);
			this._overlayDataSource = GameMenuOverlayFactory.GetOverlay(menuOverlayType);
			this._overlayDataSource.SetExitInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			base.Layer = new GauntletLayer("MapMenuOverlay", 202, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			this._layerAsGauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
			base.MenuViewContext.AddLayer(base.Layer);
			if (this._overlayDataSource is EncounterMenuOverlayVM)
			{
				this._layerAsGauntletLayer.LoadMovie("EncounterOverlay", this._overlayDataSource);
			}
			else if (this._overlayDataSource is SettlementMenuOverlayVM)
			{
				this._layerAsGauntletLayer.LoadMovie("SettlementOverlay", this._overlayDataSource);
			}
			else if (this._overlayDataSource is ArmyMenuOverlayVM)
			{
				Debug.FailedAssert("Trying to open army overlay in menu. Should be opened in map overlay", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Menu\\GauntletMenuOverlayBaseView.cs", "OnInitialize", 49);
			}
			else
			{
				Debug.FailedAssert("Game menu overlay not supported in gauntlet overlay", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Menu\\GauntletMenuOverlayBaseView.cs", "OnInitialize", 53);
			}
			base.OnInitialize();
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0000C610 File Offset: 0x0000A810
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource != null)
			{
				overlayDataSource.OnFrameTick(dt);
			}
			if (ScreenManager.TopScreen is MapScreen && this._overlayDataSource != null)
			{
				GameMenuOverlay overlayDataSource2 = this._overlayDataSource;
				MapScreen mapScreen = ScreenManager.TopScreen as MapScreen;
				overlayDataSource2.IsInfoBarExtended = mapScreen != null && mapScreen.IsBarExtended;
			}
			if (!this._isContextMenuEnabled && this._overlayDataSource.IsContextMenuEnabled)
			{
				this._isContextMenuEnabled = true;
				MapScreen instance = MapScreen.Instance;
				if (instance != null)
				{
					instance.SetIsOverlayContextMenuActive(true);
				}
				base.Layer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(base.Layer);
			}
			else if (this._isContextMenuEnabled && !this._overlayDataSource.IsContextMenuEnabled)
			{
				this._isContextMenuEnabled = false;
				MapScreen instance2 = MapScreen.Instance;
				if (instance2 != null)
				{
					instance2.SetIsOverlayContextMenuActive(false);
				}
				base.Layer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(base.Layer);
			}
			if (this._isContextMenuEnabled && base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._overlayDataSource.IsContextMenuEnabled = false;
			}
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0000C72A File Offset: 0x0000A92A
		protected override void OnHourlyTick()
		{
			base.OnHourlyTick();
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource == null)
			{
				return;
			}
			overlayDataSource.Refresh();
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0000C742 File Offset: 0x0000A942
		protected override void OnOverlayTypeChange(GameMenu.MenuOverlayType newType)
		{
			base.OnOverlayTypeChange(newType);
			GameMenuOverlay overlayDataSource = this._overlayDataSource;
			if (overlayDataSource == null)
			{
				return;
			}
			overlayDataSource.UpdateOverlayType(newType);
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0000C75C File Offset: 0x0000A95C
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

		// Token: 0x060001F8 RID: 504 RVA: 0x0000C774 File Offset: 0x0000A974
		protected override void OnFinalize()
		{
			MapScreen instance = MapScreen.Instance;
			if (instance != null)
			{
				instance.SetIsOverlayContextMenuActive(false);
			}
			base.MenuViewContext.RemoveLayer(base.Layer);
			this._overlayDataSource.OnFinalize();
			this._overlayDataSource = null;
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			base.OnFinalize();
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x0000C7C9 File Offset: 0x0000A9C9
		protected override void OnMapConversationActivated()
		{
			base.OnMapConversationActivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0000C7E5 File Offset: 0x0000A9E5
		protected override void OnMapConversationDeactivated()
		{
			base.OnMapConversationDeactivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x040000A4 RID: 164
		private GameMenuOverlay _overlayDataSource;

		// Token: 0x040000A5 RID: 165
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000A6 RID: 166
		private bool _isContextMenuEnabled;
	}
}
