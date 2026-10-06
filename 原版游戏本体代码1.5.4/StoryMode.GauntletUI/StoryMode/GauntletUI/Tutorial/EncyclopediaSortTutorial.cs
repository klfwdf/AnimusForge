using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaSortTutorial")]
public class EncyclopediaSortTutorial : TutorialItemBase
{
	private bool _isActive;

	private bool _isSortClicked;

	public EncyclopediaSortTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "EncyclopediaSortButton";
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
			Game.Current.EventManager.RegisterEvent<OnEncyclopediaListSortedEvent>(OnSortClicked);
		}
		else if (!_isActive && isActive)
		{
			Game.Current.EventManager.UnregisterEvent<OnEncyclopediaListSortedEvent>(OnSortClicked);
		}
		return _isActive;
	}

	private void OnSortClicked(OnEncyclopediaListSortedEvent evnt)
	{
		_isSortClicked = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_isActive && _isSortClicked)
		{
			Game.Current.EventManager.UnregisterEvent<OnEncyclopediaListSortedEvent>(OnSortClicked);
			return true;
		}
		return false;
	}
}
