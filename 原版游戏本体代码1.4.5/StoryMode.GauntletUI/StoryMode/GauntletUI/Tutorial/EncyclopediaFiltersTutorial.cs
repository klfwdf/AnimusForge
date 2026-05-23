using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaFiltersTutorial")]
public class EncyclopediaFiltersTutorial : TutorialItemBase
{
	private bool _isActive;

	private bool _isAnyFilterSelected;

	public EncyclopediaFiltersTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "EncyclopediaFiltersContainer";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.EncyclopediaWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		bool isActive = _isActive;
		EncyclopediaPages currentEncyclopediaPage = TutorialHelper.CurrentEncyclopediaPage;
		if ((uint)(currentEncyclopediaPage - 2) <= 6u)
		{
			_isActive = true;
		}
		else
		{
			_isActive = false;
		}
		if (!isActive && _isActive)
		{
			Game.Current.EventManager.RegisterEvent<OnEncyclopediaFilterActivatedEvent>(OnFilterClicked);
		}
		else if (!_isActive && isActive)
		{
			Game.Current.EventManager.UnregisterEvent<OnEncyclopediaFilterActivatedEvent>(OnFilterClicked);
		}
		return _isActive;
	}

	private void OnFilterClicked(OnEncyclopediaFilterActivatedEvent evnt)
	{
		_isAnyFilterSelected = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_isActive && _isAnyFilterSelected)
		{
			Game.Current.EventManager.UnregisterEvent<OnEncyclopediaFilterActivatedEvent>(OnFilterClicked);
			return true;
		}
		return false;
	}
}
