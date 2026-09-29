using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

// One "round" of the persistent scene session = the player's line -> NPC group replies -> postprocess
// actions -> the replies being spoken. The session panel may not send the next line until the round is
// over (replies are never cut off, requests never stack); 打断 ends the round on purpose with the same
// semantics as a player re-shout (new conversation epoch: queued speech and in-flight replies retire).
// Read by the UI at ~6 Hz: a task-state read, one lock and one queue check.
public partial class ShoutBehavior
{
	// Replies may still be spoken after they are generated; bounded so ambient speech can't lock the session.
	private const float PresentationRoundSpeechGraceSeconds = 45f;

	// Between the send and the group start (gate wait, main-thread hop) there is no task yet. If another
	// channel claims the line (XihaiAction battle speech) no group starts, so the pending mark expires.
	private const float PresentationRoundStartTimeoutSeconds = 8f;

	private Task _presentationRoundTask;
	private int _presentationRoundEpoch = -1;
	private float _presentationRoundFinishedAt = -1f;
	private float _presentationRoundPendingSince = -1f;

	// Session sends still go through OnShoutConfirmedWithContext, so other modules' patches on it
	// (XihaiAction battle speech) keep seeing every player line.
	private void RunTrackedSceneShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		ClearPresentationRound();
		_presentationRoundPendingSince = GetApplicationTimeSafe();
		OnShoutConfirmedWithContext(shoutText, extraFact, forcedPrimaryAgentIndex);
	}

	// Called by ProcessCapturedScenePlayerShoutAsync once the group task exists (any thread).
	private void NotePresentationRoundGroup(Task groupTask)
	{
		if (groupTask == null || _presentationRoundPendingSince < 0f)
		{
			return;
		}
		_presentationRoundTask = groupTask;
		_presentationRoundEpoch = Volatile.Read(ref _sceneConversationEpoch);
		_presentationRoundFinishedAt = -1f;
		_presentationRoundPendingSince = -1f;
	}

	private bool IsPresentationRoundActive()
	{
		Task task = _presentationRoundTask;
		float now = GetApplicationTimeSafe();
		if (task == null)
		{
			if (_presentationRoundPendingSince < 0f)
			{
				return false;
			}
			if (now - _presentationRoundPendingSince < PresentationRoundStartTimeoutSeconds)
			{
				return true;
			}
			_presentationRoundPendingSince = -1f;
			return false;
		}
		// An interrupt or a newer player line retired this round.
		if (_presentationRoundEpoch != Volatile.Read(ref _sceneConversationEpoch) && task.IsCompleted)
		{
			ClearPresentationRound();
			return false;
		}
		if (!task.IsCompleted || !GetScenePostprocessGateTask().IsCompleted)
		{
			return true;
		}
		if (_presentationRoundFinishedAt < 0f)
		{
			_presentationRoundFinishedAt = now;
		}
		if (now - _presentationRoundFinishedAt < PresentationRoundSpeechGraceSeconds && IsSpeechPipelineBusy())
		{
			return true;
		}
		ClearPresentationRound();
		return false;
	}

	private void ClearPresentationRound()
	{
		_presentationRoundTask = null;
		_presentationRoundEpoch = -1;
		_presentationRoundFinishedAt = -1f;
		_presentationRoundPendingSince = -1f;
	}

	public static bool CanInterruptScenePresentationForExternal
	{
		get
		{
			ShoutBehavior owner = CurrentInstance;
			return owner != null && owner.IsPresentationSessionLive() && owner.IsPresentationRoundActive();
		}
	}

	// 打断: retire the running round like a new player line does. Postprocess actions already started keep
	// running (their gate still holds the next request until they settle).
	public static bool InterruptScenePresentationForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner == null || !owner.IsPresentationSessionLive() || !IsBannerlordMainThreadForNativeActions() || !owner.IsPresentationRoundActive())
		{
			return false;
		}
		owner.BeginNewPlayerDrivenSceneConversationEpoch();
		owner.EndShoutProcessing("scene_presentation_interrupt");
		owner.ClearPresentationRound();
		Logger.Log("ScenePresentation", "round interrupted by player");
		BumpPresentation();
		return true;
	}
}
