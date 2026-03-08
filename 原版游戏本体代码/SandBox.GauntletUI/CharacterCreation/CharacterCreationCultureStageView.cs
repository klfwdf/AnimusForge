using System;
using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.CharacterCreation
{
	// Token: 0x0200004A RID: 74
	[CharacterCreationStageView(typeof(CharacterCreationCultureStage))]
	public class CharacterCreationCultureStageView : CharacterCreationStageViewBase
	{
		// Token: 0x0600035B RID: 859 RVA: 0x000140B4 File Offset: 0x000122B4
		public CharacterCreationCultureStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
			: base(affirmativeAction, negativeAction, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
		{
			this._characterCreationManager = characterCreationManager;
			this.GauntletLayer = new GauntletLayer("CharacterCreationCulture", 1, true)
			{
				IsFocusLayer = true
			};
			this.GauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this.GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			ScreenManager.TrySetFocus(this.GauntletLayer);
			this._dataSource = new CharacterCreationCultureStageVM(this._characterCreationManager, new Action(this.NextStage), affirmativeActionText, new Action(this.PreviousStage), negativeActionText, new Action<CultureObject>(this.OnCultureSelected));
			this._movie = this.GauntletLayer.LoadMovie("CharacterCreationCultureStage", this._dataSource);
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._characterCreationCategory = UIResourceManager.LoadSpriteCategory("ui_charactercreation");
			if (this._characterCreationManager.GetStage<CharacterCreationBannerEditorStage>() != null)
			{
				this._bannerEditorCategory = UIResourceManager.LoadSpriteCategory("ui_bannericons");
			}
		}

		// Token: 0x0600035C RID: 860 RVA: 0x000141EC File Offset: 0x000123EC
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this.GauntletLayer = null;
			CharacterCreationCultureStageVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnFinalize();
			}
			this._dataSource = null;
			this._characterCreationCategory.Unload();
		}

		// Token: 0x0600035D RID: 861 RVA: 0x00014220 File Offset: 0x00012420
		private void HandleLayerInput()
		{
			if (this.GauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				this._dataSource.OnPreviousStage();
				return;
			}
			if (this.GauntletLayer.Input.IsHotKeyReleased("Confirm") && this._dataSource.CanAdvance)
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				this._dataSource.OnNextStage();
			}
		}

		// Token: 0x0600035E RID: 862 RVA: 0x00014293 File Offset: 0x00012493
		public override void Tick(float dt)
		{
			base.Tick(dt);
			if (this._dataSource.IsActive)
			{
				base.HandleEscapeMenu(this, this.GauntletLayer);
				this.HandleLayerInput();
			}
		}

		// Token: 0x0600035F RID: 863 RVA: 0x000142BC File Offset: 0x000124BC
		public override void NextStage()
		{
			this._characterCreationManager.CharacterCreationContent.SetMainCharacterName(NameGenerator.Current.GenerateFirstNameForPlayer(this._dataSource.CurrentSelectedCulture.Culture, Hero.MainHero.IsFemale).ToString());
			this._affirmativeAction();
		}

		// Token: 0x06000360 RID: 864 RVA: 0x00014310 File Offset: 0x00012510
		private void OnCultureSelected(CultureObject culture)
		{
			MissionSoundParametersView.SoundParameterMissionCulture soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.None;
			if (culture.StringId == "aserai")
			{
				soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Aserai;
			}
			else if (culture.StringId == "khuzait")
			{
				soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Khuzait;
			}
			else if (culture.StringId == "vlandia")
			{
				soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Vlandia;
			}
			else if (culture.StringId == "sturgia")
			{
				soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Sturgia;
			}
			else if (culture.StringId == "battania")
			{
				soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Battania;
			}
			else if (culture.StringId == "empire")
			{
				soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Empire;
			}
			else if (culture.StringId == "nord")
			{
				soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Nord;
			}
			SoundManager.SetGlobalParameter("MissionCulture", (float)soundParameterMissionCulture);
		}

		// Token: 0x06000361 RID: 865 RVA: 0x000143C6 File Offset: 0x000125C6
		public override void PreviousStage()
		{
			Game.Current.GameStateManager.PopState(0);
		}

		// Token: 0x06000362 RID: 866 RVA: 0x000143D8 File Offset: 0x000125D8
		public override int GetVirtualStageCount()
		{
			return 1;
		}

		// Token: 0x06000363 RID: 867 RVA: 0x000143DB File Offset: 0x000125DB
		public override IEnumerable<ScreenLayer> GetLayers()
		{
			return new List<ScreenLayer> { this.GauntletLayer };
		}

		// Token: 0x06000364 RID: 868 RVA: 0x000143EE File Offset: 0x000125EE
		public override void LoadEscapeMenuMovie()
		{
			this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
			this._escapeMenuMovie = this.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
		}

		// Token: 0x06000365 RID: 869 RVA: 0x0001441F File Offset: 0x0001261F
		public override void ReleaseEscapeMenuMovie()
		{
			this.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x04000164 RID: 356
		private const string CultureParameterId = "MissionCulture";

		// Token: 0x04000165 RID: 357
		private readonly GauntletMovieIdentifier _movie;

		// Token: 0x04000166 RID: 358
		private GauntletLayer GauntletLayer;

		// Token: 0x04000167 RID: 359
		private CharacterCreationCultureStageVM _dataSource;

		// Token: 0x04000168 RID: 360
		private SpriteCategory _characterCreationCategory;

		// Token: 0x04000169 RID: 361
		private SpriteCategory _bannerEditorCategory;

		// Token: 0x0400016A RID: 362
		private readonly CharacterCreationManager _characterCreationManager;

		// Token: 0x0400016B RID: 363
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x0400016C RID: 364
		private GauntletMovieIdentifier _escapeMenuMovie;
	}
}
