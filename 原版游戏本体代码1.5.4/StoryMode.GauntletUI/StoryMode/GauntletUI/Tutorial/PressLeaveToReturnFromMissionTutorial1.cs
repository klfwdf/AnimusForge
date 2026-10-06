using System.Linq;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("PressLeaveToReturnFromMissionType1")]
public class PressLeaveToReturnFromMissionTutorial1 : TutorialItemBase
{
	private bool _changedContext;

	public PressLeaveToReturnFromMissionTutorial1()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _changedContext;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		_changedContext = true;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		string[] source = new string[6] { "center", "lordshall", "tavern", "prison", "village_center", "arena" };
		if (TutorialHelper.CurrentMissionLocation != null && source.Contains(TutorialHelper.CurrentMissionLocation.StringId) && TutorialHelper.PlayerIsInAnySettlement && !TutorialHelper.PlayerIsInAConversation)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.Mission;
		}
		return false;
	}
}
