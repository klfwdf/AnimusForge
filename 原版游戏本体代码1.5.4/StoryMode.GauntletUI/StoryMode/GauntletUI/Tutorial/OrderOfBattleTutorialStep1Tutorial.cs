using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("OrderOfBattleTutorialStep1")]
public class OrderOfBattleTutorialStep1Tutorial : TutorialItemBase
{
	private bool _playerAssignedACaptainToFormationInOoB;

	public OrderOfBattleTutorialStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Center;
		base.HighlightedVisualElementID = "AssignCaptain";
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

	public override void OnOrderOfBattleHeroAssignedToFormation(OrderOfBattleHeroAssignedToFormationEvent obj)
	{
		_playerAssignedACaptainToFormationInOoB = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerAssignedACaptainToFormationInOoB;
	}
}
