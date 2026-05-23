using System;
using System.Collections.Generic;
using SandBox.GauntletUI.BannerEditor;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.CharacterCreation
{
	// Token: 0x02000048 RID: 72
	[CharacterCreationStageView(typeof(CharacterCreationBannerEditorStage))]
	public class CharacterCreationBannerEditorView : CharacterCreationStageViewBase
	{
		// Token: 0x06000338 RID: 824 RVA: 0x00013068 File Offset: 0x00011268
		public CharacterCreationBannerEditorView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh = null, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction = null, ControlCharacterCreationStageReturnInt getTotalStageCountAction = null, ControlCharacterCreationStageReturnInt getFurthestIndexAction = null, ControlCharacterCreationStageWithInt goToIndexAction = null)
			: this(CharacterObject.PlayerCharacter, Clan.PlayerClan.Banner, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
		{
		}

		// Token: 0x06000339 RID: 825 RVA: 0x0001309C File Offset: 0x0001129C
		public CharacterCreationBannerEditorView(BasicCharacterObject character, Banner banner, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh = null, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction = null, ControlCharacterCreationStageReturnInt getTotalStageCountAction = null, ControlCharacterCreationStageReturnInt getFurthestIndexAction = null, ControlCharacterCreationStageWithInt goToIndexAction = null)
			: base(affirmativeAction, negativeAction, onRefresh, getTotalStageCountAction, getCurrentStageIndexAction, getFurthestIndexAction, goToIndexAction)
		{
			this._bannerEditorView = new BannerEditorView(character, banner, new ControlCharacterCreationStage(this.AffirmativeAction), affirmativeActionText, negativeAction, negativeActionText, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction);
		}

		// Token: 0x0600033A RID: 826 RVA: 0x000130E5 File Offset: 0x000112E5
		public override IEnumerable<ScreenLayer> GetLayers()
		{
			return new List<ScreenLayer>
			{
				this._bannerEditorView.SceneLayer,
				this._bannerEditorView.GauntletLayer
			};
		}

		// Token: 0x0600033B RID: 827 RVA: 0x0001310E File Offset: 0x0001130E
		public override void PreviousStage()
		{
			this._bannerEditorView.Exit(true);
		}

		// Token: 0x0600033C RID: 828 RVA: 0x0001311C File Offset: 0x0001131C
		public override void NextStage()
		{
			this._bannerEditorView.Exit(false);
		}

		// Token: 0x0600033D RID: 829 RVA: 0x0001312A File Offset: 0x0001132A
		public override void Tick(float dt)
		{
			if (!this._isFinalized)
			{
				this._bannerEditorView.OnTick(dt);
				if (this._isFinalized)
				{
					return;
				}
				base.HandleEscapeMenu(this, this._bannerEditorView.SceneLayer);
			}
		}

		// Token: 0x0600033E RID: 830 RVA: 0x0001315B File Offset: 0x0001135B
		public override int GetVirtualStageCount()
		{
			return 1;
		}

		// Token: 0x0600033F RID: 831 RVA: 0x0001315E File Offset: 0x0001135E
		public override void GoToIndex(int index)
		{
			this._bannerEditorView.GoToIndex(index);
		}

		// Token: 0x06000340 RID: 832 RVA: 0x0001316C File Offset: 0x0001136C
		protected override void OnFinalize()
		{
			this._bannerEditorView.OnDeactivate();
			this._bannerEditorView.OnFinalize();
			this._isFinalized = true;
			base.OnFinalize();
		}

		// Token: 0x06000341 RID: 833 RVA: 0x00013194 File Offset: 0x00011394
		private void AffirmativeAction()
		{
			uint primaryColor = this._bannerEditorView.Banner.GetPrimaryColor();
			uint firstIconColor = this._bannerEditorView.Banner.GetFirstIconColor();
			Clan playerClan = Clan.PlayerClan;
			playerClan.Color = primaryColor;
			playerClan.Color2 = firstIconColor;
			playerClan.UpdateBannerColor(primaryColor, firstIconColor);
			(GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager.CharacterCreationContent.SetMainClanBanner(this._bannerEditorView.Banner);
			this._affirmativeAction();
		}

		// Token: 0x06000342 RID: 834 RVA: 0x00013211 File Offset: 0x00011411
		public override void LoadEscapeMenuMovie()
		{
			this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
			this._escapeMenuMovie = this._bannerEditorView.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
		}

		// Token: 0x06000343 RID: 835 RVA: 0x00013247 File Offset: 0x00011447
		public override void ReleaseEscapeMenuMovie()
		{
			this._bannerEditorView.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x04000149 RID: 329
		private readonly BannerEditorView _bannerEditorView;

		// Token: 0x0400014A RID: 330
		private bool _isFinalized;

		// Token: 0x0400014B RID: 331
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x0400014C RID: 332
		private GauntletMovieIdentifier _escapeMenuMovie;
	}
}
