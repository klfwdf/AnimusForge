using System;
using System.Linq;
using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Menu
{
	// Token: 0x02000029 RID: 41
	[OverrideView(typeof(MenuTownManagementView))]
	public class GauntletMenuTownManagementView : MenuView
	{
		// Token: 0x0600020A RID: 522 RVA: 0x0000CDD0 File Offset: 0x0000AFD0
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._dataSource = new TownManagementVM();
			this._spriteCategory = UIResourceManager.LoadSpriteCategory("ui_town_management");
			base.Layer = new GauntletLayer("MapTownManagement", 206, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.MenuViewContext.AddLayer(base.Layer);
			if (!base.Layer.Input.IsCategoryRegistered(HotKeyManager.GetCategory("GenericPanelGameKeyCategory")))
			{
				base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			}
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._layerAsGauntletLayer.LoadMovie("TownManagement", this._dataSource);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			this._dataSource.Show = true;
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInTownManagement(true);
			}
		}

		// Token: 0x0600020B RID: 523 RVA: 0x0000CEEC File Offset: 0x0000B0EC
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this._spriteCategory.Unload();
			base.MenuViewContext.RemoveLayer(base.Layer);
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInTownManagement(false);
			}
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._layerAsGauntletLayer = null;
			base.Layer = null;
			base.OnFinalize();
		}

		// Token: 0x0600020C RID: 524 RVA: 0x0000CF70 File Offset: 0x0000B170
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (base.Layer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				if (this._dataSource.ReserveControl.IsEnabled)
				{
					this._dataSource.ReserveControl.ExecuteConfirm();
				}
				else
				{
					this._dataSource.ExecuteDone();
				}
			}
			else if (base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				if (this._dataSource.IsSelectingGovernor)
				{
					this._dataSource.IsSelectingGovernor = false;
				}
				else if (this._dataSource.ReserveControl.IsEnabled)
				{
					this._dataSource.ReserveControl.ExecuteCancel();
				}
				else
				{
					SettlementBuildingProjectVM settlementBuildingProjectVM = this._dataSource.ProjectSelection.AvailableProjects.FirstOrDefault((SettlementBuildingProjectVM x) => x.IsSelected);
					if (settlementBuildingProjectVM != null)
					{
						settlementBuildingProjectVM.IsSelected = false;
					}
					else
					{
						UISoundsHelper.PlayUISound("event:/ui/default");
						this._dataSource.ExecuteDone();
					}
				}
			}
			if (!this._dataSource.Show)
			{
				base.MenuViewContext.CloseTownManagement();
			}
		}

		// Token: 0x0600020D RID: 525 RVA: 0x0000D0A3 File Offset: 0x0000B2A3
		protected override void OnMapConversationActivated()
		{
			base.OnMapConversationActivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x0600020E RID: 526 RVA: 0x0000D0BF File Offset: 0x0000B2BF
		protected override void OnMapConversationDeactivated()
		{
			base.OnMapConversationDeactivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x040000AD RID: 173
		private SpriteCategory _spriteCategory;

		// Token: 0x040000AE RID: 174
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000AF RID: 175
		private TownManagementVM _dataSource;
	}
}
