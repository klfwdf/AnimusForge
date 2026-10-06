using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaTrackTutorial")]
public class EncyclopediaTrackTutorial : TutorialItemBase
{
	private bool _isActive;

	private bool _usedTrackFromEncyclopedia;

	public EncyclopediaTrackTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "EncyclopediaItemTrackButton";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.EncyclopediaWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		bool isActive = _isActive;
		_isActive = TutorialHelper.CurrentEncyclopediaPage == EncyclopediaPages.Settlement;
		if (!isActive && _isActive)
		{
			Game.Current.EventManager.RegisterEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(OnTrackToggledFromEncyclopedia);
		}
		else if (!_isActive && isActive)
		{
			Game.Current.EventManager.UnregisterEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(OnTrackToggledFromEncyclopedia);
		}
		return _isActive;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_isActive)
		{
			bool flag = false;
			if (_isActive)
			{
				if (TutorialHelper.CurrentContext != TutorialContexts.EncyclopediaWindow)
				{
					flag = true;
				}
				if (TutorialHelper.CurrentEncyclopediaPage != EncyclopediaPages.Hero && TutorialHelper.CurrentEncyclopediaPage != EncyclopediaPages.Settlement)
				{
					flag = true;
				}
				if (_usedTrackFromEncyclopedia)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Game.Current.EventManager.UnregisterEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(OnTrackToggledFromEncyclopedia);
				return true;
			}
		}
		return false;
	}

	private void OnTrackToggledFromEncyclopedia(PlayerToggleTrackSettlementFromEncyclopediaEvent callback)
	{
		_usedTrackFromEncyclopedia = true;
	}
}
