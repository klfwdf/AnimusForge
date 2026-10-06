using System;
using NavalDLC.Storyline;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews.Storyline;

public class HelpingAnAllyMissionView : MissionView
{
	private HelpingAnAllySetPieceBattleMissionController _controller;

	public override void OnBehaviorInitialize()
	{
		_controller = base.Mission.GetMissionBehavior<HelpingAnAllySetPieceBattleMissionController>();
		if (_controller != null)
		{
			HelpingAnAllySetPieceBattleMissionController controller = _controller;
			controller.OnShipsInitializedEvent = (Action)Delegate.Combine(controller.OnShipsInitializedEvent, new Action(OnShipsInitialized));
			HelpingAnAllySetPieceBattleMissionController controller2 = _controller;
			controller2.OnDefeatedEvent = (Action<float>)Delegate.Combine(controller2.OnDefeatedEvent, new Action<float>(OnDefeated));
		}
	}

	private void OnDefeated(float duration)
	{
		ScreenFadeController.BeginFadeOut(duration);
	}

	private void OnShipsInitialized()
	{
		OnShipsInitializedInternal();
	}

	protected virtual void OnShipsInitializedInternal()
	{
	}
}
