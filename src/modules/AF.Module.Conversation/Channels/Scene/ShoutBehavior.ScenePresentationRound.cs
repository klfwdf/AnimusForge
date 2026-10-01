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
	// Session sends still go through OnShoutConfirmedWithContext, so other modules' patches on it
	// (XihaiAction battle speech) keep seeing every player line.
	private void RunTrackedSceneShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		Presentation.BeginRound(GetApplicationTimeSafe());
		OnShoutConfirmedWithContext(shoutText, extraFact, forcedPrimaryAgentIndex);
	}

	// Called by ProcessCapturedScenePlayerShoutAsync once the group task exists (any thread).
	private void NotePresentationRoundGroup(Task groupTask) => Presentation.NoteRoundGroup(groupTask, _sceneConversationEpoch);

	private bool IsPresentationRoundActive() => Presentation.IsRoundActive(GetApplicationTimeSafe(), _sceneConversationEpoch, GetScenePostprocessGateTask().IsCompleted, IsSpeechPipelineBusy);

	private void ClearPresentationRound() => Presentation.ClearRound();

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
