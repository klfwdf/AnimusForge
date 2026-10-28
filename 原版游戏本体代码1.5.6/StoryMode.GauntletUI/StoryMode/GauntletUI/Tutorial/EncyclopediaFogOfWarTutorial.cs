using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaFogOfWarTutorial")]
public class EncyclopediaFogOfWarTutorial : TutorialItemBase
{
	private EncyclopediaPages _activatedPage;

	private bool _registeredEvents;

	private bool _lastActiveState;

	private bool _isActive;

	public EncyclopediaFogOfWarTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		if (!_registeredEvents && TutorialHelper.CurrentContext == TutorialContexts.EncyclopediaWindow)
		{
			Game.Current.EventManager.RegisterEvent<EncyclopediaPageChangedEvent>(OnLimitedInformationPageOpened);
			_registeredEvents = true;
		}
		else if (_registeredEvents && TutorialHelper.CurrentContext != TutorialContexts.EncyclopediaWindow)
		{
			Game.Current.EventManager.UnregisterEvent<EncyclopediaPageChangedEvent>(OnLimitedInformationPageOpened);
			_registeredEvents = false;
		}
		return TutorialContexts.EncyclopediaWindow;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent evnt)
	{
		base.OnTutorialContextChanged(evnt);
		if (_registeredEvents && evnt.NewContext != TutorialContexts.EncyclopediaWindow)
		{
			Game.Current.EventManager.UnregisterEvent<EncyclopediaPageChangedEvent>(OnLimitedInformationPageOpened);
			_registeredEvents = false;
		}
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!_registeredEvents)
		{
			Game.Current.EventManager.RegisterEvent<EncyclopediaPageChangedEvent>(OnLimitedInformationPageOpened);
			_registeredEvents = true;
		}
		return _isActive;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (!_lastActiveState && _isActive)
		{
			_activatedPage = TutorialHelper.CurrentEncyclopediaPage;
		}
		if (_lastActiveState && _isActive && _activatedPage != TutorialHelper.CurrentEncyclopediaPage)
		{
			Game.Current.EventManager.UnregisterEvent<EncyclopediaPageChangedEvent>(OnLimitedInformationPageOpened);
			return true;
		}
		_lastActiveState = _isActive;
		return false;
	}

	private void OnLimitedInformationPageOpened(EncyclopediaPageChangedEvent evnt)
	{
		if (evnt.NewPageHasHiddenInformation)
		{
			_isActive = true;
		}
	}
}
