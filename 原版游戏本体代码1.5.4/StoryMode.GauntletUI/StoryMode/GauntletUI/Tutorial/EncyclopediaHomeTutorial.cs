using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaHomeTutorial")]
public class EncyclopediaHomeTutorial : TutorialItemBase
{
	private bool _isActive;

	public EncyclopediaHomeTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.EncyclopediaWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		_isActive = GauntletTutorialSystem.Current.CurrentEncyclopediaPageContext == EncyclopediaPages.Home;
		return _isActive;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_isActive)
		{
			return GauntletTutorialSystem.Current.CurrentEncyclopediaPageContext != EncyclopediaPages.Home;
		}
		return false;
	}
}
