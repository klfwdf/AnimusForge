using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.BannerEditor
{
	// Token: 0x02000050 RID: 80
	[GameStateScreen(typeof(BannerEditorState))]
	public class GauntletBannerEditorScreen : ScreenBase, IGameStateListener
	{
		// Token: 0x060003CF RID: 975 RVA: 0x00017AA4 File Offset: 0x00015CA4
		public GauntletBannerEditorScreen(BannerEditorState bannerEditorState)
		{
			LoadingWindow.EnableGlobalLoadingWindow();
			this._clan = bannerEditorState.GetClan();
			this._bannerEditorLayer = new BannerEditorView(bannerEditorState.GetCharacter(), bannerEditorState.GetClan().Banner, new ControlCharacterCreationStage(this.OnDone), new TextObject("{=WiNRdfsm}Done", null), new ControlCharacterCreationStage(this.OnCancel), new TextObject("{=3CpNUnVl}Cancel", null), null, null, null, null, null);
			this._bannerEditorLayer.DataSource.SetClanRelatedRules(bannerEditorState.GetClan().Kingdom == null);
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x00017B35 File Offset: 0x00015D35
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this._bannerEditorLayer.OnTick(dt);
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x00017B4C File Offset: 0x00015D4C
		public void OnDone()
		{
			uint primaryColor = this._bannerEditorLayer.DataSource.BannerVM.Banner.GetPrimaryColor();
			uint firstIconColor = this._bannerEditorLayer.DataSource.BannerVM.Banner.GetFirstIconColor();
			this._clan.Color2 = firstIconColor;
			if (this._bannerEditorLayer.DataSource.CanChangeBackgroundColor)
			{
				this._clan.Color = primaryColor;
				this._clan.UpdateBannerColor(primaryColor, firstIconColor);
			}
			else
			{
				this._clan.UpdateBannerColor(this._clan.Color, firstIconColor);
			}
			Game.Current.GameStateManager.PopState(0);
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x00017BEF File Offset: 0x00015DEF
		public void OnCancel()
		{
			Game.Current.GameStateManager.PopState(0);
		}

		// Token: 0x060003D3 RID: 979 RVA: 0x00017C01 File Offset: 0x00015E01
		protected override void OnInitialize()
		{
			base.OnInitialize();
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
			InformationManager.HideAllMessages();
		}

		// Token: 0x060003D4 RID: 980 RVA: 0x00017C1E File Offset: 0x00015E1E
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this._bannerEditorLayer.OnFinalize();
			if (LoadingWindow.IsLoadingWindowActive)
			{
				LoadingWindow.DisableGlobalLoadingWindow();
			}
			Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
		}

		// Token: 0x060003D5 RID: 981 RVA: 0x00017C4D File Offset: 0x00015E4D
		protected override void OnActivate()
		{
			base.OnActivate();
			base.AddLayer(this._bannerEditorLayer.GauntletLayer);
			base.AddLayer(this._bannerEditorLayer.SceneLayer);
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x00017C77 File Offset: 0x00015E77
		protected override void OnDeactivate()
		{
			this._bannerEditorLayer.OnDeactivate();
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x00017C84 File Offset: 0x00015E84
		void IGameStateListener.OnActivate()
		{
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x00017C86 File Offset: 0x00015E86
		void IGameStateListener.OnDeactivate()
		{
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x00017C88 File Offset: 0x00015E88
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x060003DA RID: 986 RVA: 0x00017C8A File Offset: 0x00015E8A
		void IGameStateListener.OnFinalize()
		{
		}

		// Token: 0x040001BF RID: 447
		private const int ViewOrderPriority = 15;

		// Token: 0x040001C0 RID: 448
		private readonly BannerEditorView _bannerEditorLayer;

		// Token: 0x040001C1 RID: 449
		private readonly Clan _clan;
	}
}
