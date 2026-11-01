using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// The field camp mission has no CampaignMissionComponent, so the campaign
/// mission-started/ended events never fire there. The camp calls these two entries
/// itself (once each per camp mission) so scene shout gets its mission behaviour,
/// the overhead speech-bubble view and a fresh per-mission history session, exactly
/// as in towns and villages.
/// </summary>
public partial class ShoutBehavior
{
	internal static void NotifyFieldCampMissionStartedForExternal(Mission mission)
	{
		ShoutBehavior shout = CurrentInstance;
		if (shout == null || mission == null || mission.GetMissionBehavior<ShoutMissionBehavior>() != null)
		{
			return;
		}
		shout.OnMissionStarted(mission);
	}

	internal static void NotifyFieldCampMissionEndedForExternal(Mission mission)
	{
		CurrentInstance?.OnMissionEnded(mission);
	}

	/// <summary>
	/// A camp soldier raises a camp matter through the ambient "approach the player" session.
	/// The camp walks him over itself (camp agents have no campaign navigator, so the ambient
	/// approach cannot move them); once he stands next to the player this opens the session in
	/// place: one LLM opening line on <paramref name="topic"/> with bubble, voice and history
	/// like any scene line, then the player answers with a normal shout whose postprocess
	/// carries the camp issue tags. Ignores the ambient MCM switch, but never starts while the
	/// player or the scene is busy or another ambient session runs. Main thread; called at
	/// most once per camp upkeep tick while an envoy is waiting.
	/// </summary>
	internal static bool TryStartFieldCampEnvoyForExternal(Agent envoy, string issueText)
	{
		ShoutBehavior shout = CurrentInstance;
		Mission mission = Mission.Current;
		if (shout == null || mission == null || envoy == null || !envoy.IsActive() || Agent.Main == null || !Agent.Main.IsActive()
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
		shout.BeginAmbientSession(envoy, null, topic, mission.CurrentTime, targetsPlayer: true, null, null);
		shout._ambientSession.External = true;
		return true;
	}

	/// <summary>True while an envoy session (opening line or the player's answer window) is running.</summary>
	internal static bool IsFieldCampEnvoyActiveForExternal()
	{
		AmbientSession session = CurrentInstance?._ambientSession;
		return session != null && session.External;
	}

	/// <summary>The player is shouting, in a reply, or a scene conversation is still running.</summary>
	internal static bool IsSceneSpeechBusyForExternal()
	{
		return CurrentInstance?.IsAmbientPlayerBusy() == true;
	}
}
