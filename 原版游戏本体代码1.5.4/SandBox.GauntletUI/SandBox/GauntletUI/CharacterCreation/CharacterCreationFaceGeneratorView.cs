using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.CharacterCreation;

[CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))]
public class CharacterCreationFaceGeneratorView : CharacterCreationStageViewBase
{
	private BodyGeneratorView _faceGeneratorView;

	private readonly CharacterCreationManager _characterCreationManager;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	public CharacterCreationFaceGeneratorView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
		: base(affirmativeAction, negativeAction, onRefresh, getTotalStageCountAction, getCurrentStageIndexAction, getFurthestIndexAction, goToIndexAction)
	{
		_characterCreationManager = characterCreationManager;
		Equipment dressedEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_show_" + CharacterObject.PlayerCharacter?.Culture?.StringId)?.DefaultEquipment;
		_faceGeneratorView = new BodyGeneratorView(NextStage, affirmativeActionText, PreviousStage, negativeActionText, CharacterObject.PlayerCharacter, openedFromMultiplayer: false, null, dressedEquipment, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction, _characterCreationManager.FaceGenHistory);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_faceGeneratorView.OnFinalize();
		_faceGeneratorView = null;
	}

	public override IEnumerable<ScreenLayer> GetLayers()
	{
		return new List<ScreenLayer> { _faceGeneratorView.SceneLayer, _faceGeneratorView.GauntletLayer };
	}

	public override void PreviousStage()
	{
		_negativeAction();
	}

	public override void NextStage()
	{
		_affirmativeAction();
	}

	public override void Tick(float dt)
	{
		_faceGeneratorView.OnTick(dt);
	}

	public override int GetVirtualStageCount()
	{
		return 1;
	}

	public override void GoToIndex(int index)
	{
		_goToIndexAction(index);
	}

	public override void LoadEscapeMenuMovie()
	{
		_escapeMenuDatasource = new EscapeMenuVM(GetEscapeMenuItems(this));
		_escapeMenuMovie = _faceGeneratorView.GauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDatasource);
	}

	public override void ReleaseEscapeMenuMovie()
	{
		_faceGeneratorView.GauntletLayer.ReleaseMovie(_escapeMenuMovie);
		_escapeMenuDatasource = null;
		_escapeMenuMovie = null;
	}
}
