using System;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu
{
	// Token: 0x02000028 RID: 40
	[OverrideView(typeof(MenuTournamentLeaderboardView))]
	public class GauntletMenuTournamentLeaderboardView : MenuView
	{
		// Token: 0x06000204 RID: 516 RVA: 0x0000CBD8 File Offset: 0x0000ADD8
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._dataSource = new TournamentLeaderboardVM
			{
				IsEnabled = true
			};
			base.Layer = new GauntletLayer("MapTournamentLeaderboard", 206, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._movie = this._layerAsGauntletLayer.LoadMovie("GameMenuTournamentLeaderboard", this._dataSource);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			base.MenuViewContext.AddLayer(base.Layer);
		}

		// Token: 0x06000205 RID: 517 RVA: 0x0000CCB4 File Offset: 0x0000AEB4
		protected override void OnFinalize()
		{
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			base.MenuViewContext.RemoveLayer(base.Layer);
			this._movie = null;
			base.Layer = null;
			base.OnFinalize();
		}

		// Token: 0x06000206 RID: 518 RVA: 0x0000CD20 File Offset: 0x0000AF20
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (base.Layer.Input.IsHotKeyReleased("Exit") || base.Layer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.IsEnabled = false;
			}
			if (!this._dataSource.IsEnabled)
			{
				base.MenuViewContext.CloseTournamentLeaderboard();
			}
		}

		// Token: 0x06000207 RID: 519 RVA: 0x0000CD90 File Offset: 0x0000AF90
		protected override void OnMapConversationActivated()
		{
			base.OnMapConversationActivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x06000208 RID: 520 RVA: 0x0000CDAC File Offset: 0x0000AFAC
		protected override void OnMapConversationDeactivated()
		{
			base.OnMapConversationDeactivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x040000AA RID: 170
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000AB RID: 171
		private TournamentLeaderboardVM _dataSource;

		// Token: 0x040000AC RID: 172
		private GauntletMovieIdentifier _movie;
	}
}
