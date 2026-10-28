using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("OrderOfBattleTutorialStep2")]
public class OrderOfBattleTutorialStep2Tutorial : TutorialItemBase
{
	private bool _playerChangedAFormationType;

	private bool _playerChangedAFormationWeight;

	public OrderOfBattleTutorialStep2Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Top;
		base.HighlightedVisualElementID = "CreateFormation";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialHelper.IsOrderOfBattleOpenAndReady && TutorialHelper.IsPlayerEncounterLeader)
		{
			return !TutorialHelper.IsNavalMission;
		}
		return false;
	}

	public override void OnOrderOfBattleFormationClassChanged(OrderOfBattleFormationClassChangedEvent obj)
	{
		_playerChangedAFormationType = true;
	}

	public override void OnOrderOfBattleFormationWeightChanged(OrderOfBattleFormationWeightChangedEvent obj)
	{
		_playerChangedAFormationWeight = _playerChangedAFormationType;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_playerChangedAFormationType)
		{
			return _playerChangedAFormationWeight;
		}
		return false;
	}
}
