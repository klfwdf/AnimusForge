using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EnterVillageTutorial")]
public class EnterVillageTutorial : TutorialItemBase
{
	private bool _isEnterOptionSelected;

	private const string _enterGameMenuOptionId = "storymode_tutorial_village_enter";

	public EnterVillageTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "storymode_tutorial_village_enter";
		base.MouseRequired = true;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.CurrentContext == TutorialContexts.MapWindow)
		{
			return Settlement.CurrentSettlement?.StringId == "village_ES3_2";
		}
		return false;
	}

	public override void OnGameMenuOptionSelected(GameMenuOption obj)
	{
		base.OnGameMenuOptionSelected(obj);
		_isEnterOptionSelected = obj.IdString == "storymode_tutorial_village_enter";
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _isEnterOptionSelected;
	}
}
