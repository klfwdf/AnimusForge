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
	public static int ScenePresentationVersionForExternal => _presentationVersion;

	public static bool IsScenePresentationActiveForExternal
		=> _presentationActive && _presentationMissionRef.TryGetTarget(out Mission mission) && ReferenceEquals(mission, Mission.Current);

	public static bool IsScenePresentationCollapsedForExternal => _presentationCollapsed;

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
		if (owner == null || !owner.IsPresentationSessionLive() || _presentationCollapsed == collapsed)
		{
			return;
		}
		_presentationCollapsed = collapsed;
		BumpPresentation();
	}

	public static void EndScenePresentationForExternal(string reason)
	{
		CurrentInstance?.EndPresentationSession(string.IsNullOrWhiteSpace(reason) ? "ui" : reason);
	}

	public static bool SubmitScenePresentationTextForExternal(string text, out string status)
	{
		status = "";
		ShoutBehavior owner = CurrentInstance;
		string content = (text ?? "").Replace("\r", "").Trim();
		if (owner == null || !owner.IsPresentationSessionLive())
		{
			status = "场景会话已结束。";
			return false;
		}
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		if (!IsBannerlordMainThreadForNativeActions())
		{
			return false;
		}
		if (owner.IsPresentationSubmitBlocked())
		{
			status = owner.IsPresentationRoundActive() ? "上一轮回应还没结束；想插话请点「打断」。" : "上一句还在处理中，请稍候。";
			return false;
		}
		if (!owner.IsPresentationAudience(owner.FindPresentationMember(owner._presentationAddresseeIndex)))
		{
			owner._presentationAddresseeIndex = owner.ChoosePresentationAddressee();
			if (owner._presentationAddresseeIndex < 0)
			{
				owner.EndPresentationSession("no_participants");
				status = "周围已经没有可交谈的人。";
				return false;
			}
		}
		// A staged give/show rides on this line, through the same commit as the one-shot flow.
		if (owner._presentationTradeStaged)
		{
			return owner.SubmitPresentationTrade(content, out status);
		}
		int addressee = owner._presentationAddresseeIndex;
		owner._activeShoutTargetingContext = owner.BuildPresentationTargetingContext();
		owner.BeginShoutProcessing("scene_presentation_submit");
		owner.ActivateMultiSceneMovementSuppression(new int[1] { addressee });
		owner.RunTrackedSceneShout(content, null, addressee);
		return true;
	}

	public static List<ScenePresentationParticipantInfo> GetScenePresentationParticipantsForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		List<ScenePresentationParticipantInfo> result = new List<ScenePresentationParticipantInfo>();
		if (owner == null || !owner.IsPresentationSessionLive())
		{
			return result;
		}
		foreach (ScenePresentationMember member in owner._presentationMembers)
		{
			result.Add(new ScenePresentationParticipantInfo
			{
				AgentIndex = member.AgentIndex,
				Name = member.Name ?? "",
				Role = member.Role ?? "",
				State = member.State,
				IsAddressee = member.AgentIndex == owner._presentationAddresseeIndex,
				IsInRange = member.InRange,
				Character = member.Character
			});
		}
		return result;
	}

	public static List<ScenePresentationHistoryLine> GetScenePresentationHistoryForExternal(int maxLines = 40)
	{
		ShoutBehavior owner = CurrentInstance;
		return owner != null && owner.IsPresentationSessionLive()
			? owner.BuildPresentationHistory(Math.Max(1, Math.Min(200, maxLines)))
			: new List<ScenePresentationHistoryLine>();
	}

	// Card click: talk to this person next. An excluded member is brought back as participating.
	public static bool SetScenePresentationAddresseeForExternal(int agentIndex)
	{
		ShoutBehavior owner = CurrentInstance;
		ScenePresentationMember member = owner?.IsPresentationSessionLive() == true ? owner.FindPresentationMember(agentIndex) : null;
		if (member == null)
		{
			return false;
		}
		if (member.State == ScenePresentationParticipantState.Excluded)
		{
			member.State = ScenePresentationParticipantState.Participating;
		}
		if (!owner.IsPresentationAudience(member))
		{
			return false;
		}
		owner._presentationAddresseeIndex = agentIndex;
		owner.ActivateMultiSceneMovementSuppression(new int[1] { agentIndex });
		BumpPresentation();
		return true;
	}

	// Seal click: 参与 → 屏蔽 → 锁定. The addressee skips 屏蔽.
	public static void CycleScenePresentationParticipantForExternal(int agentIndex)
	{
		ShoutBehavior owner = CurrentInstance;
		ScenePresentationMember member = owner?.IsPresentationSessionLive() == true ? owner.FindPresentationMember(agentIndex) : null;
		if (member == null)
		{
			return;
		}
		member.State = ScenePresentationPolicy.NextState(member.State, agentIndex == owner._presentationAddresseeIndex);
		BumpPresentation();
	}

	// Batch bar: include everyone, or exclude everyone except the addressee and locked members.
	public static void SetAllScenePresentationParticipantsForExternal(bool include)
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner == null || !owner.IsPresentationSessionLive())
		{
			return;
		}
		foreach (ScenePresentationMember member in owner._presentationMembers)
		{
			if (include && member.State == ScenePresentationParticipantState.Excluded)
			{
				member.State = ScenePresentationParticipantState.Participating;
			}
			else if (!include && member.State == ScenePresentationParticipantState.Participating && member.AgentIndex != owner._presentationAddresseeIndex)
			{
				member.State = ScenePresentationParticipantState.Excluded;
			}
		}
		BumpPresentation();
	}

	public static bool OpenScenePresentationEncyclopediaForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		ScenePresentationMember member = owner?.IsPresentationSessionLive() == true ? owner.FindPresentationMember(owner._presentationAddresseeIndex) : null;
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
