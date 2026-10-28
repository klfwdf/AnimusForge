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
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.CharacterCreation;

[CharacterCreationStageView(typeof(CharacterCreationCultureStage))]
public class CharacterCreationCultureStageView : CharacterCreationStageViewBase
{
	private const string CultureParameterId = "MissionCulture";

	private readonly GauntletMovieIdentifier _movie;

	private GauntletLayer GauntletLayer;

	private CharacterCreationCultureStageVM _dataSource;

	private SpriteCategory _characterCreationCategory;

	private SpriteCategory _bannerEditorCategory;

	private readonly CharacterCreationManager _characterCreationManager;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	public CharacterCreationCultureStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
		: base(affirmativeAction, negativeAction, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
	{
		_characterCreationManager = characterCreationManager;
		GauntletLayer = new GauntletLayer("CharacterCreationCulture", 1, shouldClear: true)
		{
			IsFocusLayer = true
		};
		GauntletLayer.InputRestrictions.SetInputRestrictions();
		GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		ScreenManager.TrySetFocus(GauntletLayer);
		_dataSource = new CharacterCreationCultureStageVM(_characterCreationManager, NextStage, affirmativeActionText, PreviousStage, negativeActionText, OnCultureSelected);
		_movie = GauntletLayer.LoadMovie("CharacterCreationCultureStage", _dataSource);
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_characterCreationCategory = UIResourceManager.LoadSpriteCategory("ui_charactercreation");
		if (_characterCreationManager.GetStage<CharacterCreationBannerEditorStage>() != null)
		{
			_bannerEditorCategory = UIResourceManager.LoadSpriteCategory("ui_bannericons");
		}
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		GauntletLayer = null;
		_dataSource?.OnFinalize();
		_dataSource = null;
		_characterCreationCategory.Unload();
	}

	private void HandleLayerInput()
	{
		if (GauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			_dataSource.OnPreviousStage();
		}
		else if (GauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanAdvance)
		{
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			_dataSource.OnNextStage();
		}
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		if (_dataSource.IsActive)
		{
			HandleEscapeMenu(this, GauntletLayer);
			HandleLayerInput();
		}
	}

	public override void NextStage()
	{
		_characterCreationManager.CharacterCreationContent.SetMainCharacterName(NameGenerator.Current.GenerateFirstNameForPlayer(_dataSource.CurrentSelectedCulture.Culture, Hero.MainHero.IsFemale).ToString());
		_affirmativeAction();
	}

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

	public override void PreviousStage()
	{
		Game.Current.GameStateManager.PopState();
	}

	public override int GetVirtualStageCount()
	{
		return 1;
	}

	public override IEnumerable<ScreenLayer> GetLayers()
	{
		return new List<ScreenLayer> { GauntletLayer };
	}

	public override void LoadEscapeMenuMovie()
	{
		_escapeMenuDatasource = new EscapeMenuVM(GetEscapeMenuItems(this));
		_escapeMenuMovie = GauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDatasource);
	}

	public override void ReleaseEscapeMenuMovie()
	{
		GauntletLayer.ReleaseMovie(_escapeMenuMovie);
		_escapeMenuDatasource = null;
		_escapeMenuMovie = null;
	}
}
