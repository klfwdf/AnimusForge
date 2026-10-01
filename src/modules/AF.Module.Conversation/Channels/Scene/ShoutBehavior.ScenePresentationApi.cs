using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Presentation-facing API for the persistent scene session. Reads are cheap; mutations run on the
// main thread and only change session membership/addressee — never the request pipeline itself.
public partial class ShoutBehavior
{
	public static int ScenePresentationVersionForExternal => ScenePresentationController._presentationVersion;

	public static bool IsScenePresentationActiveForExternal
		=> ScenePresentationController._presentationActive && ScenePresentationController._presentationMissionRef.TryGetTarget(out Mission mission) && ReferenceEquals(mission, Mission.Current);

	public static bool IsScenePresentationCollapsedForExternal => ScenePresentationController._presentationCollapsed;

	// True until the previous line's round is over: replies generated, postprocess settled, replies spoken.
	public static bool IsScenePresentationBusyForExternal
	{
		get
		{
			ShoutBehavior owner = CurrentInstance;
			return owner != null && owner.IsPresentationSubmitBlocked();
		}
	}

	private bool IsPresentationSubmitBlocked()
	{
		return _isProcessingShout || _isWaitingForScenePostprocessGate || IsPresentationRoundActive();
	}

	public static void SetScenePresentationCollapsedForExternal(bool collapsed)
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner == null || !owner.IsPresentationSessionLive() || ScenePresentationController._presentationCollapsed == collapsed)
		{
			return;
		}
		ScenePresentationController._presentationCollapsed = collapsed;
		BumpPresentation();
	}

	public static void EndScenePresentationForExternal(string reason)
	{
		CurrentInstance?.EndPresentationSession(string.IsNullOrWhiteSpace(reason) ? "ui" : reason);
	}

	public static bool SubmitScenePresentationTextForExternal(string text, out string status)
	{
  ShoutBehavior owner=CurrentInstance;
  if (owner==null) { status="场景会话已结束。"; return false; }
  if (!owner.Presentation.PrepareTextSubmission(text, IsBannerlordMainThreadForNativeActions, owner.IsPresentationSubmitBlocked,
   owner.IsPresentationRoundActive, out string content, out status)) return false;
		// A staged give/show rides on this line, through the same commit as the one-shot flow.
		if (owner.Presentation.TradeStaged)
		{
			return owner.SubmitPresentationTrade(content, out status);
		}
		int addressee = owner.Presentation._presentationAddresseeIndex;
		owner._activeShoutTargetingContext = owner.BuildPresentationTargetingContext();
		owner.BeginShoutProcessing("scene_presentation_submit");
		owner.ActivateMultiSceneMovementSuppression(new int[1] { addressee });
		owner.RunTrackedSceneShout(content, null, addressee);
		return true;
	}

	public static List<ScenePresentationParticipantInfo> GetScenePresentationParticipantsForExternal()
	{ return CurrentInstance?.Presentation.GetParticipants() ?? new List<ScenePresentationParticipantInfo>(); }

	public static List<ScenePresentationHistoryLine> GetScenePresentationHistoryForExternal(int maxLines = 40)
	{
		ShoutBehavior owner = CurrentInstance;
		return owner != null && owner.IsPresentationSessionLive()
			? owner.BuildPresentationHistory(Math.Max(1, Math.Min(200, maxLines)))
			: new List<ScenePresentationHistoryLine>();
	}

	// Card click: talk to this person next. An excluded member is brought back as participating.
	public static bool SetScenePresentationAddresseeForExternal(int agentIndex)
	{ return CurrentInstance?.Presentation.SetAddressee(agentIndex) == true; }

	// Seal click: 参与 → 屏蔽 → 锁定. The addressee skips 屏蔽.
	public static void CycleScenePresentationParticipantForExternal(int agentIndex)
	{ CurrentInstance?.Presentation.CycleParticipant(agentIndex); }

	// Batch bar: include everyone, or exclude everyone except the addressee and locked members.
	public static void SetAllScenePresentationParticipantsForExternal(bool include)
	{ CurrentInstance?.Presentation.SetAllParticipants(include); }

	public static bool OpenScenePresentationEncyclopediaForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		ScenePresentationController.Member member = owner?.IsPresentationSessionLive() == true ? owner.FindPresentationMember(owner.Presentation._presentationAddresseeIndex) : null;
		Hero hero = member?.Character?.HeroObject;
		if (hero == null)
		{
			return false;
		}
		OpenHeroEncyclopediaFromShoutInput(hero);
		return true;
	}

	// The wheel's center portrait: the framed primary target of the pending shout.
	public static Agent GetScenePresentationWheelTargetForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner?._activeShoutTargetingContext == null)
		{
			return null;
		}
		List<Agent> framed = owner.GetAgentsForShoutTargetingContext(owner._activeShoutTargetingContext);
		return ResolvePrimaryAgentForShoutTargetingContext(owner._activeShoutTargetingContext, framed) ?? framed.FirstOrDefault();
	}
}
