using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

public abstract class EncyclopediaPageTutorialBase : TutorialItemBase
{
	private bool _isActive;

	private readonly EncyclopediaPages _activationPage;

	private readonly EncyclopediaPages _alternateActivationPage;

	private EncyclopediaPages _lastActivatedPage;

	public EncyclopediaPageTutorialBase(EncyclopediaPages activationPage, EncyclopediaPages alternateActivationPage)
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "";
		base.MouseRequired = false;
		_activationPage = activationPage;
		_alternateActivationPage = alternateActivationPage;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.EncyclopediaWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		EncyclopediaPages currentEncyclopediaPageContext = GauntletTutorialSystem.Current.CurrentEncyclopediaPageContext;
		bool isActive = _isActive;
		_isActive = currentEncyclopediaPageContext == _activationPage || currentEncyclopediaPageContext == _alternateActivationPage;
		if (!isActive && _isActive)
		{
			_lastActivatedPage = currentEncyclopediaPageContext;
		}
		return _isActive;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_isActive)
		{
			EncyclopediaPages currentEncyclopediaPageContext = GauntletTutorialSystem.Current.CurrentEncyclopediaPageContext;
			if (_lastActivatedPage == _alternateActivationPage)
			{
				return currentEncyclopediaPageContext != _alternateActivationPage;
			}
			if (currentEncyclopediaPageContext != EncyclopediaPages.Settlement)
			{
				return currentEncyclopediaPageContext != EncyclopediaPages.ListSettlements;
			}
			return false;
		}
		return false;
	}
}
