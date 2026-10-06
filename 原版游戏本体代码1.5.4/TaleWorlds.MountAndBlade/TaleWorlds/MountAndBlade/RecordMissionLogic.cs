using System;
using System.Diagnostics;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class RecordMissionLogic : MissionLogic
{
	private float _lastRecordedTime = -1f;

	public override void OnBehaviorInitialize()
	{
		base.Mission.Recorder.StartRecording();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_lastRecordedTime + 0.02f < base.Mission.CurrentTime)
		{
			_lastRecordedTime = base.Mission.CurrentTime;
			base.Mission.Recorder.RecordCurrentState();
		}
	}

	public override void OnEndMissionInternal()
	{
		base.OnEndMissionInternal();
		base.Mission.Recorder.BackupRecordToFile("Mission_record_" + $"{DateTime.Now:yyyy-MM-dd_hh-mm-ss-tt}_" + Process.GetCurrentProcess().Id, Game.Current.GameType.GetType().Name, base.Mission.SceneLevels);
		GameNetwork.ResetMissionData();
	}
}
