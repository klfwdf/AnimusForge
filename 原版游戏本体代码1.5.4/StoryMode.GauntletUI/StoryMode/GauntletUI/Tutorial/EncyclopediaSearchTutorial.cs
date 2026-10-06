using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaSearchTutorial")]
public class EncyclopediaSearchTutorial : TutorialItemBase
{
	private bool _isActive;

	private bool _isSearchButtonPressed;

	public EncyclopediaSearchTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "EncyclopediaSearchButton";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.EncyclopediaWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		bool isActive = _isActive;
		_isActive = TutorialHelper.CurrentContext == TutorialContexts.EncyclopediaWindow;
		if (!isActive && _isActive)
		{
			Game.Current.EventManager.RegisterEvent<OnEncyclopediaSearchActivatedEvent>(OnEncyclopediaSearchBarUsed);
		}
		else if (!_isActive && isActive)
		{
			Game.Current.EventManager.UnregisterEvent<OnEncyclopediaSearchActivatedEvent>(OnEncyclopediaSearchBarUsed);
		}
		return _isActive;
	}

	private void OnEncyclopediaSearchBarUsed(OnEncyclopediaSearchActivatedEvent evnt)
	{
		_isSearchButtonPressed = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_isActive && _isSearchButtonPressed)
		{
			Game.Current.EventManager.UnregisterEvent<OnEncyclopediaSearchActivatedEvent>(OnEncyclopediaSearchBarUsed);
			return true;
		}
		return false;
	}
}
