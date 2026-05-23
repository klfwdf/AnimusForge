using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class ReplayMissionView : MissionView
{
	private float _resetTime;

	private bool _isInputOverridden;

	private ReplayMissionLogic _replayMissionLogic;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_resetTime = 0f;
		_replayMissionLogic = base.Mission.GetMissionBehavior<ReplayMissionLogic>();
	}

	public override void OnPreMissionTick(float dt)
	{
		base.OnPreMissionTick(dt);
		base.Mission.Recorder.ProcessRecordUntilTime(base.Mission.CurrentTime - _resetTime);
		_ = _isInputOverridden;
		if (base.Mission.CurrentState == Mission.State.Continuing && base.Mission.Recorder.IsEndOfRecord())
		{
			if (MBEditor._isEditorMissionOn)
			{
				MBEditor.LeaveEditMissionMode();
			}
			else
			{
				base.Mission.EndMission();
			}
		}
	}

	public void OverrideInput(bool isOverridden)
	{
		_isInputOverridden = isOverridden;
	}

	public void ResetReplay()
	{
		_resetTime = base.Mission.CurrentTime;
		base.Mission.ResetMission();
		base.Mission.Teams.Clear();
		base.Mission.Recorder.RestartRecord();
		MBCommon.UnPauseGameEngine();
		base.Mission.Scene.TimeSpeed = 1f;
	}

	public void Rewind(float time)
	{
		_resetTime = MathF.Min(_resetTime + time, base.Mission.CurrentTime);
		base.Mission.ResetMission();
		base.Mission.Teams.Clear();
		base.Mission.Recorder.RestartRecord();
	}

	public void FastForward(float time)
	{
		_resetTime -= time;
	}

	public void Pause()
	{
		if (!MBCommon.IsPaused && base.Mission.Scene.TimeSpeed.ApproximatelyEqualsTo(1f))
		{
			MBCommon.PauseGameEngine();
		}
	}

	public void Resume()
	{
		if (MBCommon.IsPaused || !base.Mission.Scene.TimeSpeed.ApproximatelyEqualsTo(1f))
		{
			MBCommon.UnPauseGameEngine();
			base.Mission.Scene.TimeSpeed = 1f;
		}
	}
}
