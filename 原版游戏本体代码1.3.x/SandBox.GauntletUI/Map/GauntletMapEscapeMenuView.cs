using System;
using System.Collections.Generic;
using SandBox.View.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000035 RID: 53
	[OverrideView(typeof(MapEscapeMenuView))]
	public class GauntletMapEscapeMenuView : MapView
	{
		// Token: 0x06000288 RID: 648 RVA: 0x0000F63F File Offset: 0x0000D83F
		public GauntletMapEscapeMenuView(List<EscapeMenuItemVM> items)
		{
			this._menuItems = items;
		}

		// Token: 0x06000289 RID: 649 RVA: 0x0000F650 File Offset: 0x0000D850
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._escapeMenuDatasource = new EscapeMenuVM(this._menuItems, null);
			base.Layer = new GauntletLayer("MapEscapeMenu", 4400, false)
			{
				IsFocusLayer = true
			};
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			this._escapeMenuMovie = this._layerAsGauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.MapScreen.AddLayer(base.Layer);
			base.MapScreen.PauseAmbientSounds();
			ScreenManager.TrySetFocus(base.Layer);
		}

		// Token: 0x0600028A RID: 650 RVA: 0x0000F714 File Offset: 0x0000D914
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu") || base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				MapScreen.Instance.CloseEscapeMenu();
			}
		}

		// Token: 0x0600028B RID: 651 RVA: 0x0000F760 File Offset: 0x0000D960
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			if (base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu") || base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				MapScreen.Instance.CloseEscapeMenu();
			}
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000F7AC File Offset: 0x0000D9AC
		protected override bool IsEscaped()
		{
			return base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu");
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000F7C4 File Offset: 0x0000D9C4
		protected override void OnFinalize()
		{
			base.OnFinalize();
			base.Layer.InputRestrictions.ResetInputRestrictions();
			base.MapScreen.RemoveLayer(base.Layer);
			base.MapScreen.RestartAmbientSounds();
			ScreenManager.TryLoseFocus(base.Layer);
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000F82A File Offset: 0x0000DA2A
		protected override TutorialContexts GetTutorialContext()
		{
			return TutorialContexts.EscapeMenu;
		}

		// Token: 0x040000E9 RID: 233
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000EA RID: 234
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x040000EB RID: 235
		private GauntletMovieIdentifier _escapeMenuMovie;

		// Token: 0x040000EC RID: 236
		private readonly List<EscapeMenuItemVM> _menuItems;
	}
}
