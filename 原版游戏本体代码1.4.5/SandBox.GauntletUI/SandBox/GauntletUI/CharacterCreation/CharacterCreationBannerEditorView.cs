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

namespace SandBox.GauntletUI.CharacterCreation;

[CharacterCreationStageView(typeof(CharacterCreationBannerEditorStage))]
public class CharacterCreationBannerEditorView : CharacterCreationStageViewBase
{
	private readonly BannerEditorView _bannerEditorView;

	private bool _isFinalized;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	public CharacterCreationBannerEditorView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh = null, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction = null, ControlCharacterCreationStageReturnInt getTotalStageCountAction = null, ControlCharacterCreationStageReturnInt getFurthestIndexAction = null, ControlCharacterCreationStageWithInt goToIndexAction = null)
		: this(CharacterObject.PlayerCharacter, Clan.PlayerClan.Banner, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
	{
	}

	public CharacterCreationBannerEditorView(BasicCharacterObject character, Banner banner, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh = null, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction = null, ControlCharacterCreationStageReturnInt getTotalStageCountAction = null, ControlCharacterCreationStageReturnInt getFurthestIndexAction = null, ControlCharacterCreationStageWithInt goToIndexAction = null)
		: base(affirmativeAction, negativeAction, onRefresh, getTotalStageCountAction, getCurrentStageIndexAction, getFurthestIndexAction, goToIndexAction)
	{
		_bannerEditorView = new BannerEditorView(character, banner, AffirmativeAction, affirmativeActionText, negativeAction, negativeActionText, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction);
	}

	public override IEnumerable<ScreenLayer> GetLayers()
	{
		return new List<ScreenLayer> { _bannerEditorView.SceneLayer, _bannerEditorView.GauntletLayer };
	}

	public override void PreviousStage()
	{
		_bannerEditorView.Exit(isCancel: true);
	}

	public override void NextStage()
	{
		_bannerEditorView.Exit(isCancel: false);
	}

	public override void Tick(float dt)
	{
		if (!_isFinalized)
		{
			_bannerEditorView.OnTick(dt);
			if (!_isFinalized)
			{
				HandleEscapeMenu(this, _bannerEditorView.SceneLayer);
			}
		}
	}

	public override int GetVirtualStageCount()
	{
		return 1;
	}

	public override void GoToIndex(int index)
	{
		_bannerEditorView.GoToIndex(index);
	}

	protected override void OnFinalize()
	{
		_bannerEditorView.OnDeactivate();
		_bannerEditorView.OnFinalize();
		_isFinalized = true;
		base.OnFinalize();
	}

	private void AffirmativeAction()
	{
		uint primaryColor = _bannerEditorView.Banner.GetPrimaryColor();
		uint firstIconColor = _bannerEditorView.Banner.GetFirstIconColor();
		Clan playerClan = Clan.PlayerClan;
		playerClan.Color = primaryColor;
		playerClan.Color2 = firstIconColor;
		playerClan.UpdateBannerColor(primaryColor, firstIconColor);
		(GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager.CharacterCreationContent.SetMainClanBanner(_bannerEditorView.Banner);
		_affirmativeAction();
	}

	public override void LoadEscapeMenuMovie()
	{
		_escapeMenuDatasource = new EscapeMenuVM(GetEscapeMenuItems(this));
		_escapeMenuMovie = _bannerEditorView.GauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDatasource);
	}

	public override void ReleaseEscapeMenuMovie()
	{
		_bannerEditorView.GauntletLayer.ReleaseMovie(_escapeMenuMovie);
		_escapeMenuDatasource = null;
		_escapeMenuMovie = null;
	}
}
