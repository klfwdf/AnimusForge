using System;
using System.Collections.Generic;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.HeirSelectionPopup;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200002B RID: 43
	[OverrideView(typeof(HeirSelectionPopupView))]
	public class GauntletHeirSelectionPopupView : MapView
	{
		// Token: 0x06000217 RID: 535 RVA: 0x0000D4A0 File Offset: 0x0000B6A0
		public GauntletHeirSelectionPopupView(Dictionary<Hero, int> heirApparents)
		{
			this._heirApparents = heirApparents;
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0000D4B0 File Offset: 0x0000B6B0
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._gameOverCategory = UIResourceManager.LoadSpriteCategory("ui_gameover");
			this._dataSource = new HeirSelectionPopupVM(this._heirApparents);
			this.InitializeKeyVisuals();
			base.Layer = new GauntletLayer("HeirSelectionPopup", 203, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			this._movie = this._layerAsGauntletLayer.LoadMovie("HeirSelectionPopup", this._dataSource);
			base.MapScreen.AddLayer(base.Layer);
			base.MapScreen.SetIsHeirSelectionPopupActive(true);
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			Campaign.Current.SetTimeControlModeLock(true);
		}

		// Token: 0x06000219 RID: 537 RVA: 0x0000D5BD File Offset: 0x0000B7BD
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			HeirSelectionPopupVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.Update();
			}
			this.HandleInput();
		}

		// Token: 0x0600021A RID: 538 RVA: 0x0000D5DD File Offset: 0x0000B7DD
		protected override void OnMenuModeTick(float dt)
		{
			base.OnMenuModeTick(dt);
			HeirSelectionPopupVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.Update();
			}
			this.HandleInput();
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000D5FD File Offset: 0x0000B7FD
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			HeirSelectionPopupVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.Update();
			}
			this.HandleInput();
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000D620 File Offset: 0x0000B820
		protected override void OnFinalize()
		{
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			this._gameOverCategory.Unload();
			base.MapScreen.RemoveLayer(base.Layer);
			this._movie = null;
			this._dataSource = null;
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			base.MapScreen.SetIsHeirSelectionPopupActive(false);
			Campaign.Current.SetTimeControlModeLock(false);
			base.OnFinalize();
		}

		// Token: 0x0600021D RID: 541 RVA: 0x0000D693 File Offset: 0x0000B893
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000D6AF File Offset: 0x0000B8AF
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0000D6CB File Offset: 0x0000B8CB
		protected override bool IsEscaped()
		{
			return true;
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0000D6CE File Offset: 0x0000B8CE
		protected override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
		{
			return false;
		}

		// Token: 0x06000221 RID: 545 RVA: 0x0000D6D1 File Offset: 0x0000B8D1
		private void HandleInput()
		{
			if (this._dataSource != null && base.Layer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				this._dataSource.ExecuteSelectHeir();
			}
		}

		// Token: 0x06000222 RID: 546 RVA: 0x0000D707 File Offset: 0x0000B907
		private void InitializeKeyVisuals()
		{
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		}

		// Token: 0x040000B9 RID: 185
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000BA RID: 186
		private HeirSelectionPopupVM _dataSource;

		// Token: 0x040000BB RID: 187
		private GauntletMovieIdentifier _movie;

		// Token: 0x040000BC RID: 188
		private readonly Dictionary<Hero, int> _heirApparents;

		// Token: 0x040000BD RID: 189
		private SpriteCategory _gameOverCategory;
	}
}
