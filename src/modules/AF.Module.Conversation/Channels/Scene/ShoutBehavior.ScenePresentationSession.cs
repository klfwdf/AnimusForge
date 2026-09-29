using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class ScenePresentationParticipantInfo
{
	public int AgentIndex { get; internal set; }
	public string Name { get; internal set; } = "";
	public string Role { get; internal set; } = "";
	public ScenePresentationParticipantState State { get; internal set; }
	public bool IsAddressee { get; internal set; }
	public bool IsInRange { get; internal set; }
	public CharacterObject Character { get; internal set; }
}

public sealed class ScenePresentationHistoryLine
{
	public string Speaker { get; internal set; } = "";
	public string Text { get; internal set; } = "";
	public string Kind { get; internal set; } = "";
}

// Persistent scene conversation for presentation modules (DialogueUI). The host owns the state and
// every rule; each message still runs the unchanged ProcessShoutConfirmedInternal pipeline, so
// preprocess, main reply, postprocess, history and memory stay identical to one-shot scene shouts.
// The session never pauses the mission. Main thread only; ticked at 10 Hz.
public partial class ShoutBehavior
{
	private sealed class ScenePresentationMember
	{
		internal Agent Agent;
		internal int AgentIndex;
		internal string Name;
		internal string Role;
		internal CharacterObject Character;
		internal ScenePresentationParticipantState State;
		internal bool InRange = true;
	}

	// Set by a presentation module. When it returns true, T/Y open the wheel and "交流" opens a session.
	public static Func<bool> ScenePresentationSessionHook;

	// Set by a presentation module while its own modal UI (the action wheel) is open; scene hotkeys pause.
	public static Func<bool> ScenePresentationBlocksHotkeysHook;

	private const float PresentationTickSeconds = 0.1f;
	private const float PresentationTapSeconds = 0.25f;
	private const int PresentationContextLines = 4;

	// Read every frame by the UI; plain static ints so polling costs nothing.
	private static volatile int _presentationVersion;
	private static volatile bool _presentationActive;
	private static volatile bool _presentationCollapsed;

	private readonly List<ScenePresentationMember> _presentationMembers = new List<ScenePresentationMember>();
	private Mission _presentationMission;
	// Lets the static UI getter reject a stale flag (save reload, mission torn down without reset)
	// without an instance lookup and without pinning the old Mission in memory.
	private static readonly WeakReference<Mission> _presentationMissionRef = new WeakReference<Mission>(null);
	private int _presentationAddresseeIndex = -1;
	private long _presentationStartSequence;
	private long _presentationHistoryFingerprint;
	private float _presentationTickElapsed;
	private float _presentationLastPlayerHealth = -1f;
	private bool _shoutHotkeyChargeMergesIntoSession;

	private static bool IsScenePresentationSessionEnabled()
	{
		try
		{
			return ScenePresentationSessionHook?.Invoke() == true;
		}
		catch
		{
			return false;
		}
	}

	private bool IsPresentationSessionLive()
	{
		return _presentationActive && _presentationMission != null && ReferenceEquals(_presentationMission, Mission.Current);
	}

	// Only the main thread writes; the UI reads the latest value.
	private static void BumpPresentation()
	{
		_presentationVersion = unchecked(_presentationVersion + 1);
	}

	private static bool IsUsablePresentationAgent(Agent agent, Mission mission)
	{
		try
		{
			return CanAgentParticipateInSceneSpeech(agent) && ReferenceEquals(agent.Mission, mission) && agent != Agent.Main;
		}
		catch
		{
			return false;
		}
	}

	private ScenePresentationMember FindPresentationMember(int agentIndex)
	{
		for (int i = 0; i < _presentationMembers.Count; i++)
		{
			if (_presentationMembers[i].AgentIndex == agentIndex)
			{
				return _presentationMembers[i];
			}
		}
		return null;
	}

	private bool IsPresentationAudience(ScenePresentationMember member)
	{
		return member != null && ScenePresentationPolicy.IsAudience(member.State, IsUsablePresentationAgent(member.Agent, _presentationMission), member.InRange);
	}

	private int ChoosePresentationAddressee()
	{
		int count = _presentationMembers.Count;
		int[] indices = new int[count];
		ScenePresentationParticipantState[] states = new ScenePresentationParticipantState[count];
		bool[] audience = new bool[count];
		for (int i = 0; i < count; i++)
		{
			indices[i] = _presentationMembers[i].AgentIndex;
			states[i] = _presentationMembers[i].State;
			audience[i] = IsPresentationAudience(_presentationMembers[i]);
		}
		return ScenePresentationPolicy.ChooseAddressee(indices, states, audience);
	}

	private static string BuildPresentationRole(Agent agent, CharacterObject character)
	{
		try
		{
			Hero hero = character?.HeroObject;
			if (hero != null)
			{
				string clan = hero.Clan?.Name?.ToString();
				string title = hero.IsLord ? "领主" : hero.IsNotable ? "显要" : hero.IsWanderer ? "流浪者" : "";
				return string.IsNullOrWhiteSpace(clan) ? title : (string.IsNullOrWhiteSpace(title) ? clan : clan + " · " + title);
			}
			return character?.Name?.ToString() ?? agent?.Character?.Name?.ToString() ?? "";
		}
		catch
		{
			return "";
		}
	}

	// Adds agents not yet in the session (join order kept). Excluded members stay excluded.
	private bool AddPresentationMembers(IEnumerable<Agent> agents)
	{
		bool changed = false;
		foreach (Agent agent in agents ?? Enumerable.Empty<Agent>())
		{
			if (!IsUsablePresentationAgent(agent, _presentationMission) || FindPresentationMember(agent.Index) != null)
			{
				continue;
			}
			CharacterObject character = agent.Character as CharacterObject;
			_presentationMembers.Add(new ScenePresentationMember
			{
				Agent = agent,
				AgentIndex = agent.Index,
				Name = agent.Name?.ToString() ?? character?.Name?.ToString() ?? "NPC",
				Role = BuildPresentationRole(agent, character),
				Character = character,
				State = ScenePresentationParticipantState.Participating
			});
			changed = true;
		}
		return changed;
	}

	private bool EnsurePresentationSession(IReadOnlyList<Agent> framedAgents, int primaryAgentIndex)
	{
		Mission mission = Mission.Current;
		if (mission == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		if (!IsPresentationSessionLive())
		{
			EndPresentationSession("restart");
			_presentationMission = mission;
			_presentationMissionRef.SetTarget(mission);
			_presentationActive = true;
			_presentationCollapsed = false;
			_presentationStartSequence = Interlocked.Read(ref _currentConversationEventSequence) + 1;
			_presentationLastPlayerHealth = Agent.Main.Health;
			_presentationTickElapsed = 0f;
			Logger.Log("ScenePresentation", "session begin mission=" + (mission.SceneName ?? ""));
		}
		AddPresentationMembers(framedAgents);
		ScenePresentationMember primary = FindPresentationMember(primaryAgentIndex);
		if (primary != null && primary.State == ScenePresentationParticipantState.Excluded)
		{
			primary.State = ScenePresentationParticipantState.Participating;
		}
		if (primary != null)
		{
			primary.InRange = true;
		}
		_presentationAddresseeIndex = IsPresentationAudience(primary) ? primaryAgentIndex : ChoosePresentationAddressee();
		if (_presentationAddresseeIndex < 0)
		{
			EndPresentationSession("no_participants");
			return false;
		}
		_presentationCollapsed = false;
		ActivateMultiSceneMovementSuppression(new int[1] { _presentationAddresseeIndex });
		BumpPresentation();
		return true;
	}

	private void EndPresentationSession(string reason)
	{
		bool wasActive = _presentationActive;
		_presentationActive = false;
		_presentationCollapsed = false;
		_presentationMembers.Clear();
		_presentationMission = null;
		_presentationMissionRef.SetTarget(null);
		_presentationAddresseeIndex = -1;
		_presentationHistoryFingerprint = 0;
		_shoutHotkeyChargeMergesIntoSession = false;
		_presentationTradeRequestMode = null;
		ReleasePresentationTrade();
		ClearPresentationRound();
		if (wasActive)
		{
			DeactivateMultiSceneMovementSuppression();
			Logger.Log("ScenePresentation", "session end reason=" + (reason ?? ""));
			BumpPresentation();
		}
	}
}
