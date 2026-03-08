using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000044 RID: 68
	[OverrideView(typeof(MarriageOfferPopupView))]
	public class GauntletMarriageOfferPopupView : MapView
	{
		// Token: 0x06000317 RID: 791 RVA: 0x00011E3D File Offset: 0x0001003D
		public GauntletMarriageOfferPopupView(Hero suitor, Hero maiden)
		{
			this._suitor = suitor;
			this._maiden = maiden;
		}

		// Token: 0x06000318 RID: 792 RVA: 0x00011E54 File Offset: 0x00010054
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource = new MarriageOfferPopupVM(this._suitor, this._maiden, new Action(this.OnPopupClosed));
			this.InitializeKeyVisuals();
			base.Layer = new GauntletLayer("MapMarriageOffer", 203, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			this._movie = this._layerAsGauntletLayer.LoadMovie("MarriageOfferPopup", this._dataSource);
			base.MapScreen.AddLayer(base.Layer);
			base.MapScreen.SetIsMarriageOfferPopupActive(true);
			this._previousTimeControlMode = Campaign.Current.TimeControlMode;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			Campaign.Current.SetTimeControlModeLock(true);
		}

		// Token: 0x06000319 RID: 793 RVA: 0x00011F73 File Offset: 0x00010173
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.HandleInput();
			MarriageOfferPopupVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.Update();
		}

		// Token: 0x0600031A RID: 794 RVA: 0x00011F92 File Offset: 0x00010192
		protected override void OnMenuModeTick(float dt)
		{
			base.OnMenuModeTick(dt);
			this.HandleInput();
			MarriageOfferPopupVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.Update();
		}

		// Token: 0x0600031B RID: 795 RVA: 0x00011FB1 File Offset: 0x000101B1
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			this.HandleInput();
			MarriageOfferPopupVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.Update();
		}

		// Token: 0x0600031C RID: 796 RVA: 0x00011FD0 File Offset: 0x000101D0
		protected override void OnFinalize()
		{
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			base.MapScreen.RemoveLayer(base.Layer);
			this._movie = null;
			this._dataSource = null;
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			base.MapScreen.SetIsMarriageOfferPopupActive(false);
			Campaign.Current.SetTimeControlModeLock(false);
			Campaign.Current.TimeControlMode = this._previousTimeControlMode;
			base.OnFinalize();
		}

		// Token: 0x0600031D RID: 797 RVA: 0x00012048 File Offset: 0x00010248
		protected override bool IsEscaped()
		{
			MarriageOfferPopupVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.ExecuteDeclineOffer();
			}
			return true;
		}

		// Token: 0x0600031E RID: 798 RVA: 0x0001205C File Offset: 0x0001025C
		protected override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
		{
			return false;
		}

		// Token: 0x0600031F RID: 799 RVA: 0x0001205F File Offset: 0x0001025F
		private void OnPopupClosed()
		{
			base.MapScreen.CloseMarriageOfferPopup();
		}

		// Token: 0x06000320 RID: 800 RVA: 0x0001206C File Offset: 0x0001026C
		private void HandleInput()
		{
			if (this._dataSource != null)
			{
				if (base.Layer.Input.IsGameKeyPressed(39))
				{
					base.MapScreen.OpenEncyclopedia();
					return;
				}
				if (base.Layer.Input.IsHotKeyReleased("Confirm"))
				{
					UISoundsHelper.PlayUISound("event:/ui/panels/next");
					this._dataSource.ExecuteAcceptOffer();
					return;
				}
				if (base.Layer.Input.IsHotKeyReleased("Exit"))
				{
					UISoundsHelper.PlayUISound("event:/ui/panels/next");
					this._dataSource.ExecuteDeclineOffer();
				}
			}
		}

		// Token: 0x06000321 RID: 801 RVA: 0x000120FA File Offset: 0x000102FA
		private void InitializeKeyVisuals()
		{
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		}

		// Token: 0x0400012E RID: 302
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x0400012F RID: 303
		private MarriageOfferPopupVM _dataSource;

		// Token: 0x04000130 RID: 304
		private GauntletMovieIdentifier _movie;

		// Token: 0x04000131 RID: 305
		private CampaignTimeControlMode _previousTimeControlMode;

		// Token: 0x04000132 RID: 306
		private Hero _suitor;

		// Token: 0x04000133 RID: 307
		private Hero _maiden;
	}
}
