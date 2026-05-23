using System;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu
{
	// Token: 0x02000025 RID: 37
	[OverrideView(typeof(MenuBaseView))]
	public class GauntletMenuBaseView : MenuView
	{
		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060001E3 RID: 483 RVA: 0x0000C25E File Offset: 0x0000A45E
		// (set) Token: 0x060001E4 RID: 484 RVA: 0x0000C266 File Offset: 0x0000A466
		public GameMenuVM GameMenuDataSource { get; private set; }

		// Token: 0x060001E5 RID: 485 RVA: 0x0000C270 File Offset: 0x0000A470
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this.GameMenuDataSource = new GameMenuVM(base.MenuContext);
			GameKey gameKey = HotKeyManager.GetCategory("Generic").GetGameKey(4);
			this.GameMenuDataSource.AddHotKey(GameMenuOption.LeaveType.Leave, gameKey);
			base.Layer = base.MenuViewContext.FindLayer<GauntletLayer>("MapMenuView");
			if (base.Layer == null)
			{
				base.Layer = new GauntletLayer("MapMenuView", 100, false);
				base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
				base.MenuViewContext.AddLayer(base.Layer);
			}
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			this._movie = this._layerAsGauntletLayer.LoadMovie("GameMenu", this.GameMenuDataSource);
			ScreenManager.TrySetFocus(base.Layer);
			this._layerAsGauntletLayer.UIContext.ContextAlpha = 1f;
			MBInformationManager.HideInformations();
			this.GainGamepadNavigationAfterSeconds(0.25f);
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0000C364 File Offset: 0x0000A564
		protected override void OnActivate()
		{
			base.OnActivate();
			this.GameMenuDataSource.Refresh(true);
			this.GameMenuDataSource.SetIdleMode(false);
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x0000C384 File Offset: 0x0000A584
		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			this.GameMenuDataSource.SetIdleMode(true);
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0000C398 File Offset: 0x0000A598
		protected override void OnResume()
		{
			base.OnResume();
			this.GameMenuDataSource.Refresh(true);
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x0000C3AC File Offset: 0x0000A5AC
		protected override void OnMenuContextRefreshed()
		{
			base.OnMenuContextRefreshed();
			this.GameMenuDataSource.Refresh(true);
		}

		// Token: 0x060001EA RID: 490 RVA: 0x0000C3C0 File Offset: 0x0000A5C0
		protected override void OnFinalize()
		{
			this.GameMenuDataSource.OnFinalize();
			this.GameMenuDataSource = null;
			ScreenManager.TryLoseFocus(base.Layer);
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			this._layerAsGauntletLayer = null;
			base.Layer = null;
			this._movie = null;
			base.OnFinalize();
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0000C416 File Offset: 0x0000A616
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.GameMenuDataSource.OnFrameTick();
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0000C42A File Offset: 0x0000A62A
		protected override void OnMapConversationActivated()
		{
			base.OnMapConversationActivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060001ED RID: 493 RVA: 0x0000C446 File Offset: 0x0000A646
		protected override void OnMapConversationDeactivated()
		{
			base.OnMapConversationDeactivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0000C462 File Offset: 0x0000A662
		protected override void OnMenuContextUpdated(MenuContext newMenuContext)
		{
			base.OnMenuContextUpdated(newMenuContext);
			this.GameMenuDataSource.UpdateMenuContext(newMenuContext);
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0000C477 File Offset: 0x0000A677
		protected override void OnBackgroundMeshNameSet(string name)
		{
			base.OnBackgroundMeshNameSet(name);
			this.GameMenuDataSource.Background = name;
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x0000C48C File Offset: 0x0000A68C
		private void GainGamepadNavigationAfterSeconds(float seconds)
		{
			this._layerAsGauntletLayer.UIContext.GamepadNavigation.GainNavigationAfterTime(seconds, () => this.GameMenuDataSource.ItemList.Count > 0);
		}

		// Token: 0x040000A2 RID: 162
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000A3 RID: 163
		private GauntletMovieIdentifier _movie;
	}
}
