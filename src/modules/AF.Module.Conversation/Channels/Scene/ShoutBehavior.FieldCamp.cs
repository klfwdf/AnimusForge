using System;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Mission lifecycle belongs to CampaignMissionComponent, exactly as in settlements.
// These entries only own the camp envoy's ambient session, never duplicate mission events.
public partial class ShoutBehavior
{
    private Agent _lastPresentedFieldCampEnvoy;
    private Mission _lastPresentedFieldCampEnvoyMission;
    private long _lastPresentedFieldCampEnvoyGeneration;

	/// <summary>
	/// A camp soldier raises a camp matter through the ambient "approach the player" session.
	/// The camp walks him over with its campaign navigator; once he stands next to the player this opens the session in
	/// place: one LLM opening line on <paramref name="issueText"/> with bubble, voice and history
	/// like any scene line, then the player answers with a normal shout whose postprocess
	/// carries the camp issue tags. Ignores the ambient MCM switch, but never starts while the
	/// player or the scene is busy or another ambient session runs. Main thread; called at
	/// most once per camp upkeep tick while an envoy is waiting.
	/// </summary>
	internal static bool TryStartFieldCampEnvoyForExternal(Agent envoy, string issueText, Action onPresented = null)
	{
		ShoutBehavior shout = CurrentInstance;
		Mission mission = Mission.Current;
		if (shout == null || mission == null || mission.IsMissionEnding || FieldCampMissionLogic.Current == null
            || ExecutionSceneConflictBridge.IsSceneControlled(mission) || envoy == null || !envoy.IsActive() || Agent.Main == null || !Agent.Main.IsActive()
			|| string.IsNullOrWhiteSpace(issueText) || shout._ambientSession != null || shout.IsAmbientPlayerBusy()
			|| !CanAgentParticipateInSceneSpeech(envoy) || envoy.Position.Distance(Agent.Main.Position) > AmbientSeatedTalkDistance)
		{
			return false;
		}
		if (!ReferenceEquals(shout._ambientMission, mission))
		{
			shout.ResetAmbientForMission(mission);
		}
		string topic = ConversationRuntimeTexts.FormatPrompt("AF_FC_EnvoyTopic", ("PLAYER", GetPlayerDisplayNameForShout()), ("ISSUE", issueText.Trim()));
        if (!shout.BeginAmbientSession(envoy, null, topic, mission.CurrentTime, targetsPlayer: true, null, null, external: true)) return false;
        shout._lastPresentedFieldCampEnvoy = null;
        shout._lastPresentedFieldCampEnvoyMission = null;
        shout._ambientSession.OnPresented = onPresented;
        return true;
	}

    /// <summary>True only after actual text output or attached audio/subtitles began; retained after this session ends.</summary>
    internal static bool IsFieldCampEnvoyOpeningPresentedForExternal(Agent agent)
    {
        ShoutBehavior shout = CurrentInstance;
        return shout != null && agent != null && ReferenceEquals(shout._lastPresentedFieldCampEnvoy, agent)
            && ReferenceEquals(shout._lastPresentedFieldCampEnvoyMission, Mission.Current)
            && SaveRuntimeGuard.IsCurrentGeneration(shout._lastPresentedFieldCampEnvoyGeneration);
    }

    internal static bool IsEnvoyOpeningPresentedForExternal(Agent agent)
        => IsFieldCampEnvoyOpeningPresentedForExternal(agent);

    /// <summary>Ceremony/assembly/drop cancels only this envoy; late generation and queued audio cannot publish.</summary>
    internal static void CancelFieldCampEnvoyForExternal(Agent agent)
    {
        ShoutBehavior shout = CurrentInstance;
        AmbientSession session = shout?._ambientSession;
        if (session == null || !session.External || (agent != null && !ReferenceEquals(session.Initiator, agent)))
        {
            AmbientSession transfer = shout?._ambientCampTransfer;
            if (transfer != null && transfer.External && (agent == null || ReferenceEquals(transfer.Initiator, agent)))
                shout.ReleaseAmbientCampTransfer(force: true);
            return;
        }
        bool playerTookOver = session.Epoch != shout._sceneConversationEpoch
            || Campaign.Current?.ConversationManager?.IsConversationInProgress == true;
        shout.EndAmbientSession("external_cancelled", applyCooldown: false, releaseAgents: !playerTookOver);
        if (ExecutionSceneConflictBridge.IsSceneControlled(Mission.Current)) shout.ReleaseAmbientCampTransfer(force: true);
    }

	/// <summary>True while an envoy session (opening line or the player's answer window) is running.</summary>
	internal static bool IsFieldCampEnvoyActiveForExternal()
	{
		AmbientSession session = CurrentInstance?._ambientSession;
		return session != null && session.External;
	}

    /// <summary>Camp return/upkeep must not overwrite the relay/native interaction that took over this agent.</summary>
    internal static bool IsSceneConversationAgentHeldForExternal(Agent agent)
    {
        ShoutBehavior shout = CurrentInstance;
        if (shout == null || agent == null || !agent.IsActive()) return false;
        AmbientSession session = shout._ambientSession;
        return (session != null && (ReferenceEquals(session.Initiator, agent) || ReferenceEquals(session.Partner, agent)))
            || shout._activeInteractionSessions.ContainsKey(agent.Index)
            || shout._pendingInteractionTimeoutArms.ContainsKey(agent.Index)
            || shout.IsStaringAgentIndex(agent.Index)
            || shout._sceneMovement.IsAgentBusyWithSceneSummonErrand(agent.Index)
            || shout._sceneMovement.IsAgentBusyWithSceneGuideErrand(agent.Index)
            || shout._sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent);
    }

	/// <summary>The player is shouting, in a reply, or a scene conversation is still running.</summary>
	internal static bool IsSceneSpeechBusyForExternal()
	{
		return CurrentInstance?.IsAmbientPlayerBusy() == true;
	}
}
