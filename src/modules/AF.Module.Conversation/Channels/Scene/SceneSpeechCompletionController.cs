using System;
using System.Threading;
namespace AnimusForge;
internal sealed class SceneSpeechCompletionPorts
{
    internal Action<int> PrepareInteractionCompletion;
    internal Action<int> FlushDialogueFeed;
    internal Func<int, bool> TryFlushLordsHallEntry;
    internal Action<int> FlushMeetingRelease;
    internal Action<int> FlushWorldMapExit;
    internal Action<int> FlushAutonomyRestore;
    internal Action<int> CleanupLipSync;
    internal Action<string, int, Action> RunStep;
    internal Func<int> MainThreadQueueCount;
}
internal sealed class SceneSpeechCompletionController
{
    private readonly SceneSpeechCompletionPorts _ports;
    private readonly SceneMovementController _movement;
    internal SceneSpeechCompletionController(SceneSpeechCompletionPorts ports, SceneMovementController movement)
    { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); _movement = movement ?? throw new ArgumentNullException(nameof(movement)); }
    internal void Complete(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return;
		}
		FreezeWatchdog.Mark("SceneTts.playback_finished.main_begin", "agent=" + agentIndex + " thread=" + Thread.CurrentThread.ManagedThreadId, immediate: true);
		_ports.PrepareInteractionCompletion(agentIndex);
		_ports.RunStep("dialogue_feed", agentIndex, delegate
		{
			_ports.FlushDialogueFeed(agentIndex);
		});
		_ports.RunStep("follow_command", agentIndex, delegate
		{
			_movement.FlushSceneFollowCommandAfterSpeech(agentIndex);
		});
		_ports.RunStep("meeting_release", agentIndex, delegate
		{
			if (_ports.TryFlushLordsHallEntry(agentIndex))
			{
				return;
			}
			_ports.FlushMeetingRelease(agentIndex);
			_ports.FlushWorldMapExit(agentIndex);
		});
		_ports.RunStep("summon_return", agentIndex, delegate
		{
			_movement.FlushSceneSummonReturnAfterSpeech(agentIndex);
		});
		_ports.RunStep("guide_return", agentIndex, delegate
		{
			_movement.FlushSceneGuideReturnAfterSpeech(agentIndex);
		});
		_ports.RunStep("autonomy_restore", agentIndex, delegate
		{
			_ports.FlushAutonomyRestore(agentIndex);
		});
		_ports.RunStep("pending_launches", agentIndex, delegate
		{
			_movement.FlushPendingSceneSummonLaunches(agentIndex);
			_movement.FlushPendingSceneGuideLaunches(agentIndex);
		});
		_ports.RunStep("lipsync_cleanup", agentIndex, delegate
		{
			_ports.CleanupLipSync(agentIndex);
		});
		FreezeWatchdog.Mark("SceneTts.playback_finished.main_end", "agent=" + agentIndex + " remaining=" + _ports.MainThreadQueueCount(), immediate: true);
	}
}
