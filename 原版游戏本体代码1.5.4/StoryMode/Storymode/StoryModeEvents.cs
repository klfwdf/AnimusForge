using TaleWorlds.CampaignSystem;

namespace StoryMode;

public class StoryModeEvents : CampaignEventReceiver
{
	private readonly MbEvent<MainStoryLineSide> _onMainStoryLineSideChosenEvent = new MbEvent<MainStoryLineSide>();

	private readonly MbEvent _onStoryModeTutorialEndedEvent = new MbEvent();

	private readonly MbEvent _onStealthTutorialActivatedEvent = new MbEvent();

	private readonly MbEvent _onBannerPieceCollectedEvent = new MbEvent();

	private readonly MbEvent _onConspiracyActivatedEvent = new MbEvent();

	private readonly MbEvent _onTravelToVillageTutorialQuestStartedEvent = new MbEvent();

	public static StoryModeEvents Instance => StoryModeManager.Current?.StoryModeEvents;

	public static IMbEvent<MainStoryLineSide> OnMainStoryLineSideChosenEvent => Instance._onMainStoryLineSideChosenEvent;

	public static IMbEvent OnStoryModeTutorialEndedEvent => Instance._onStoryModeTutorialEndedEvent;

	public static IMbEvent OnStealthTutorialActivatedEvent => Instance._onStealthTutorialActivatedEvent;

	public static IMbEvent OnBannerPieceCollectedEvent => Instance._onBannerPieceCollectedEvent;

	public static IMbEvent OnConspiracyActivatedEvent => Instance._onConspiracyActivatedEvent;

	public static IMbEvent OnTravelToVillageTutorialQuestStartedEvent => Instance._onTravelToVillageTutorialQuestStartedEvent;

	public override void RemoveListeners(object obj)
	{
		_onMainStoryLineSideChosenEvent.ClearListeners(obj);
		_onStoryModeTutorialEndedEvent.ClearListeners(obj);
		_onStealthTutorialActivatedEvent.ClearListeners(obj);
		_onBannerPieceCollectedEvent.ClearListeners(obj);
		_onConspiracyActivatedEvent.ClearListeners(obj);
		_onTravelToVillageTutorialQuestStartedEvent.ClearListeners(obj);
	}

	public void OnMainStoryLineSideChosen(MainStoryLineSide side)
	{
		Instance._onMainStoryLineSideChosenEvent.Invoke(side);
	}

	public void OnStoryModeTutorialEnded()
	{
		Instance._onStoryModeTutorialEndedEvent.Invoke();
	}

	public void OnStealthTutorialActivated()
	{
		Instance._onStealthTutorialActivatedEvent.Invoke();
	}

	public void OnBannerPieceCollected()
	{
		Instance._onBannerPieceCollectedEvent.Invoke();
	}

	public void OnConspiracyActivated()
	{
		Instance._onConspiracyActivatedEvent.Invoke();
	}

	public void OnTravelToVillageTutorialQuestStarted()
	{
		Instance._onTravelToVillageTutorialQuestStartedEvent.Invoke();
	}
}
