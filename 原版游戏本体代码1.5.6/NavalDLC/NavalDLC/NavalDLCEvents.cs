using NavalDLC.Map;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace NavalDLC;

public class NavalDLCEvents : CampaignEventReceiver
{
	private readonly MbEvent<PartyBase, NavalStorylinePartyData> _isPartyQuestPartyEvent = new MbEvent<PartyBase, NavalStorylinePartyData>();

	private readonly MbEvent<bool> _onNavalStorylineActivityChangedEvent = new MbEvent<bool>();

	private readonly MbEvent _onGunnarSavedEvent = new MbEvent();

	private readonly MbEvent _onSisterRansomRequestedEvent = new MbEvent();

	private readonly MbEvent _onSisterRansomedEvent = new MbEvent();

	private readonly MbEvent<NavalStorylineData.StorylineCancelDetail> _onNavalStorylineCanceledEvent = new MbEvent<NavalStorylineData.StorylineCancelDetail>();

	private readonly MbEvent _onNavalStorylineTutorialSkippedEvent = new MbEvent();

	private readonly MbEvent<Storm> _onStormCreatedEvent = new MbEvent<Storm>();

	public static NavalDLCEvents Instance => NavalDLCManager.Instance.NavalDLCEvents;

	public static IMbEvent<PartyBase, NavalStorylinePartyData> IsNavalQuestPartyEvent => Instance._isPartyQuestPartyEvent;

	public static IMbEvent<bool> OnNavalStorylineActivityChangedEvent => Instance._onNavalStorylineActivityChangedEvent;

	public static IMbEvent OnSisterRansomedEvent => Instance._onSisterRansomedEvent;

	public static IMbEvent OnGunnarSavedEvent => Instance._onGunnarSavedEvent;

	public static IMbEvent OnSisterRansomRequestedEvent => Instance._onSisterRansomRequestedEvent;

	public static IMbEvent<NavalStorylineData.StorylineCancelDetail> OnNavalStorylineCanceledEvent => Instance._onNavalStorylineCanceledEvent;

	public static IMbEvent OnNavalStorylineTutorialSkippedEvent => Instance._onNavalStorylineTutorialSkippedEvent;

	public static IMbEvent<Storm> OnStormCreatedEvent => Instance._onStormCreatedEvent;

	public override void RemoveListeners(object obj)
	{
		_onNavalStorylineActivityChangedEvent.ClearListeners(obj);
		_isPartyQuestPartyEvent.ClearListeners(obj);
		_onGunnarSavedEvent.ClearListeners(obj);
		_onNavalStorylineCanceledEvent.ClearListeners(obj);
		_onStormCreatedEvent.ClearListeners(obj);
		_onSisterRansomedEvent.ClearListeners(obj);
		_onSisterRansomRequestedEvent.ClearListeners(obj);
		_onNavalStorylineTutorialSkippedEvent.ClearListeners(obj);
	}

	public void IsNavalQuestParty(PartyBase party, NavalStorylinePartyData result)
	{
		Instance._isPartyQuestPartyEvent.Invoke(party, result);
	}

	public void OnNavalStorylineActivityChanged(bool activity)
	{
		Instance._onNavalStorylineActivityChangedEvent.Invoke(activity);
	}

	public void OnSisterRansomRequested()
	{
		Instance._onSisterRansomRequestedEvent.Invoke();
	}

	public void OnSisterRansomed()
	{
		Instance._onSisterRansomedEvent.Invoke();
	}

	public void OnGunnarSaved()
	{
		Instance._onGunnarSavedEvent.Invoke();
	}

	public void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		Instance._onNavalStorylineCanceledEvent.Invoke(detail);
	}

	public void OnNavalStorylineTutorialSkipped()
	{
		Instance._onNavalStorylineTutorialSkippedEvent.Invoke();
	}

	public void OnStormCreated(Storm storm)
	{
		Instance._onStormCreatedEvent.Invoke(storm);
	}
}
