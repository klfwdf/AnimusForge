using NavalDLC.Storyline;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class NavalStorylinePirateBattleMissionView : MissionView
{
	private bool _isInitialized;

	private PirateBattleMissionController _controller;

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!_isInitialized)
		{
			InitializeView();
		}
	}

	private void OnBeginScreenFade(float fadeDuration, float blackScreenDuration)
	{
		ScreenFadeController.BeginFadeOutAndIn(fadeDuration, blackScreenDuration, fadeDuration);
	}

	private void OnCameraBearingNeedsUpdate(float direction)
	{
		base.MissionScreen.CameraBearing = direction;
	}

	private void InitializeView()
	{
		_controller = base.Mission.GetMissionBehavior<PirateBattleMissionController>();
		_isInitialized = _controller != null;
		if (_controller != null)
		{
			_controller.OnBeginScreenFadeEvent += OnBeginScreenFade;
			_controller.OnCameraBearingNeedsUpdateEvent += OnCameraBearingNeedsUpdate;
			_controller.OnShipsInitializedEvent += OnShipsInitialized;
		}
	}

	private void OnShipsInitialized()
	{
		OnShipsInitializedInternal();
	}

	protected virtual void OnShipsInitializedInternal()
	{
	}
}
