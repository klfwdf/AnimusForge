using System;
using SandBox.View.Menu;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu
{
	// Token: 0x02000024 RID: 36
	[OverrideView(typeof(MenuBackgroundView))]
	public class GauntletMenuBackground : MenuView
	{
		// Token: 0x060001DE RID: 478 RVA: 0x0000C160 File Offset: 0x0000A360
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._layerAsGauntletLayer = base.MenuViewContext.FindLayer<GauntletLayer>("MapMenuView");
			if (this._layerAsGauntletLayer == null)
			{
				this._layerAsGauntletLayer = new GauntletLayer("MapMenuView", 100, false);
				base.MenuViewContext.AddLayer(this._layerAsGauntletLayer);
			}
			base.Layer = this._layerAsGauntletLayer;
			this._movie = this._layerAsGauntletLayer.LoadMovie("GameMenuBackground", null);
			this._layerAsGauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0000C1EA File Offset: 0x0000A3EA
		protected override void OnFinalize()
		{
			GauntletLayer layerAsGauntletLayer = this._layerAsGauntletLayer;
			if (layerAsGauntletLayer != null)
			{
				layerAsGauntletLayer.ReleaseMovie(this._movie);
			}
			this._layerAsGauntletLayer = null;
			base.Layer = null;
			this._movie = null;
			base.OnFinalize();
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x0000C21E File Offset: 0x0000A41E
		protected override void OnMapConversationActivated()
		{
			base.OnMapConversationActivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x0000C23A File Offset: 0x0000A43A
		protected override void OnMapConversationDeactivated()
		{
			base.OnMapConversationDeactivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x0400009F RID: 159
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000A0 RID: 160
		private GauntletMovieIdentifier _movie;
	}
}
