using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed class ScenePresentationBannerlordAdapter
{
    private readonly ScenePresentationBannerlordAdapterPorts _ports;
    internal ScenePresentationBannerlordAdapter(ScenePresentationBannerlordAdapterPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal bool TryShowNpcBubble(Agent liveAgent, string content, float typingDurationSeconds = -1f)
	{
		try
		{
			if (!CanAgentParticipateInSceneSpeech(liveAgent))
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}
			FloatingTextMissionView floatingTextView = _floatingTextView;
			if (floatingTextView == null || !floatingTextView.IsBubbleReady())
			{
				return false;
			}
			floatingTextView.AddOrUpdateText(liveAgent, content, isAppend: false, typingDurationSeconds);
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryShowPassiveNpcBubbleForExternal(Agent liveAgent, string content, float typingDurationSeconds = -1f)
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null)
			{
				Logger.LogImmediate("TownAmbient", "bubble_failed reason=current_instance_null");
				return false;
			}
			FloatingTextMissionView floatingTextView = instance._j17ScenePresentationBannerlordAdapter._floatingTextView;
			if (floatingTextView == null || !floatingTextView.IsBubbleReady())
			{
				Logger.LogImmediate("TownAmbient", "bubble_failed reason=floating_text_not_ready view=" + (floatingTextView != null));
				return false;
			}
			bool shown = instance._j17ScenePresentationBannerlordAdapter.TryShowNpcBubble(liveAgent, content, typingDurationSeconds);
			if (!shown)
			{
				Logger.LogImmediate("TownAmbient", "bubble_failed reason=agent_guard agent=" + (liveAgent?.Index.ToString() ?? "null"));
			}
			return shown;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsScenePresentationBusyForExternal
	{
		get
		{
			ShoutBehavior owner = CurrentInstance;
			return owner != null && owner._j17ScenePresentationBannerlordAdapter.IsPresentationSubmitBlocked();
		}
	}

	internal bool IsPresentationSubmitBlocked()
	{
		return _isProcessingShout || _isWaitingForScenePostprocessGate || IsPresentationRoundActive();
	}

	internal static void SetScenePresentationCollapsedForExternal(bool collapsed)
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner == null || !owner._j17ScenePresentationBannerlordAdapter.Presentation.IsPresentationSessionLive() || ScenePresentationController._presentationCollapsed == collapsed)
		{
			return;
		}
		ScenePresentationController._presentationCollapsed = collapsed;
		BumpPresentation();
	}

	internal static void EndScenePresentationForExternal(string reason)
	{
		CurrentInstance?._j17ScenePresentationBannerlordAdapter.Presentation.EndPresentationSession(string.IsNullOrWhiteSpace(reason) ? "ui" : reason);
	}

	internal static bool SubmitScenePresentationTextForExternal(string text, out string status)
	{
  ShoutBehavior owner=CurrentInstance;
  if (owner==null) { status="场景会话已结束。"; return false; }
  if (!owner._j17ScenePresentationBannerlordAdapter.Presentation.PrepareTextSubmission(text, IsBannerlordMainThreadForNativeActions, owner._j17ScenePresentationBannerlordAdapter.IsPresentationSubmitBlocked,
   owner._j17ScenePresentationBannerlordAdapter.IsPresentationRoundActive, out string content, out status)) return false;
		// A staged give/show rides on this line, through the same commit as the one-shot flow.
		if (owner._j17SceneTradeController.TradeStaged)
		{
			return owner._j17SceneTradeController.SubmitPresentationTrade(content, out status);
		}
		int addressee = owner._j17ScenePresentationBannerlordAdapter.Presentation._presentationAddresseeIndex;
		owner._j17SceneShoutInputController._activeShoutTargetingContext = owner._j17ScenePresentationBannerlordAdapter.BuildPresentationTargetingContext();
		owner._j17SceneShoutInputController.BeginShoutProcessing("scene_presentation_submit");
		owner._j17SceneAttentionController.ActivateMultiSceneMovementSuppression(new int[1] { addressee });
		owner._j17ScenePresentationBannerlordAdapter.RunTrackedSceneShout(content, null, addressee);
		return true;
	}

	internal static List<ScenePresentationParticipantInfo> GetScenePresentationParticipantsForExternal()
	{ return CurrentInstance?._j17ScenePresentationBannerlordAdapter.Presentation.GetParticipants() ?? new List<ScenePresentationParticipantInfo>(); }

	internal static List<ScenePresentationHistoryLine> GetScenePresentationHistoryForExternal(int maxLines = 40)
	{
		ShoutBehavior owner = CurrentInstance;
		return owner != null && owner._j17ScenePresentationBannerlordAdapter.Presentation.IsPresentationSessionLive()
			? owner._j17ScenePresentationBannerlordAdapter.BuildPresentationHistory(Math.Max(1, Math.Min(200, maxLines)))
			: new List<ScenePresentationHistoryLine>();
	}

	internal static bool SetScenePresentationAddresseeForExternal(int agentIndex)
	{ return CurrentInstance?._j17ScenePresentationBannerlordAdapter.Presentation.SetAddressee(agentIndex) == true; }

	internal static void CycleScenePresentationParticipantForExternal(int agentIndex)
	{ CurrentInstance?._j17ScenePresentationBannerlordAdapter.Presentation.CycleParticipant(agentIndex); }

	internal static void SetAllScenePresentationParticipantsForExternal(bool include)
	{ CurrentInstance?._j17ScenePresentationBannerlordAdapter.Presentation.SetAllParticipants(include); }

	internal static bool OpenScenePresentationEncyclopediaForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		ScenePresentationController.Member member = owner?._j17ScenePresentationBannerlordAdapter.Presentation.IsPresentationSessionLive() == true ? owner._j17ScenePresentationBannerlordAdapter.Presentation.FindPresentationMember(owner._j17ScenePresentationBannerlordAdapter.Presentation._presentationAddresseeIndex) : null;
		Hero hero = member?.Character?.HeroObject;
		if (hero == null)
		{
			return false;
		}
		OpenHeroEncyclopediaFromShoutInput(hero);
		return true;
	}

	internal static Agent GetScenePresentationWheelTargetForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner?._j17SceneShoutInputController._activeShoutTargetingContext == null)
		{
			return null;
		}
		List<Agent> framed = owner._j17ScenePresentationBannerlordAdapter.GetAgentsForShoutTargetingContext(owner._j17SceneShoutInputController._activeShoutTargetingContext);
		return ResolvePrimaryAgentForShoutTargetingContext(owner._j17SceneShoutInputController._activeShoutTargetingContext, framed) ?? framed.FirstOrDefault();
	}

	internal void RunTrackedSceneShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		Presentation.BeginRound(GetApplicationTimeSafe());
		OnShoutConfirmedWithContext(shoutText, extraFact, forcedPrimaryAgentIndex);
	}

	internal static bool CanInterruptScenePresentationForExternal
	{
		get
		{
			ShoutBehavior owner = CurrentInstance;
			return owner != null && owner._j17ScenePresentationBannerlordAdapter.Presentation.IsPresentationSessionLive() && owner._j17ScenePresentationBannerlordAdapter.IsPresentationRoundActive();
		}
	}

	internal static bool InterruptScenePresentationForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner == null || !owner._j17ScenePresentationBannerlordAdapter.Presentation.IsPresentationSessionLive() || !IsBannerlordMainThreadForNativeActions() || !owner._j17ScenePresentationBannerlordAdapter.IsPresentationRoundActive())
		{
			return false;
		}
		owner._j17ScenePresentationBannerlordAdapter._ports.BeginNewPlayerDrivenSceneConversationEpoch();
		owner._j17SceneShoutInputController.EndShoutProcessing("scene_presentation_interrupt");
		owner._j17ScenePresentationBannerlordAdapter.Presentation.ClearRound();
		Logger.Log("ScenePresentation", "round interrupted by player");
		BumpPresentation();
		return true;
	}

	internal static string GetPresentationCombatEndReason(Mission mission)
	{
		if (IsMeetingPseudoCombatContext())
		{
			return "meeting";
		}
		if (IsActiveSceneConversationDuelCombat())
		{
			return "duel";
		}
		try
		{
			if (mission.GetMissionBehavior<SandBox.Missions.MissionLogics.MissionFightHandler>()?.IsThereActiveFight() == true)
			{
				return "fight";
			}
			if (mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.Duel)
			{
				return "battle_mode";
			}
		}
		catch
		{
		}
		return null;
	}

	internal ShoutTargetingContext BuildPresentationTargetingContext()
	{
		GetConfiguredShoutRange(out var _, out var maxRange);
		ShoutTargetingContext context = new ShoutTargetingContext
		{
			RangeMeters = maxRange,
			HalfAngleRadians = SceneShoutInputController.ShoutMaxTotalAngleRadians * 0.5f,
			PrimaryAgentIndex = Presentation._presentationAddresseeIndex
		};
  Presentation.VisitAudience((index, agent) => {
   context.CandidateAgentIndices.Add(index); context.PreviewCandidateAgents.Add(agent);
   if (TryGetPlayerPlanarDistanceMeters(agent, out var distance)) context.CandidatePlayerDistancesMeters[index]=distance;
  });
		return context;
	}

	internal void MergeFramedIntoPresentation(ShoutTargetingContext framed)
	{
		List<Agent> agents = GetAgentsForShoutTargetingContext(framed);
		Agent primary = ResolvePrimaryAgentForShoutTargetingContext(framed, agents);
		if (agents.Count == 0)
		{
			ScenePresentationController._presentationCollapsed = false;
			BumpPresentation();
			return;
		}
		EnsurePresentationSession(agents, primary?.Index ?? Presentation._presentationAddresseeIndex);
	}

	internal bool TryOpenPresentationSessionFromWheel()
	{
		if (!IsScenePresentationSessionEnabled() || !EnsurePresentationSessionForWheelAction())
		{
			return false;
		}
		ResumeGame();
		return true;
	}

	internal bool EnsurePresentationSessionForWheelAction()
	{
		if (!IsScenePresentationSessionEnabled())
		{
			return false;
		}
		// The 10 Hz tick would end a session opened mid-fight at once; fall back to the one-shot flow instead.
		Mission mission = Mission.Current;
		if (mission == null || GetPresentationCombatEndReason(mission) != null)
		{
			return false;
		}
		List<Agent> framed = GetAgentsForShoutTargetingContext(_activeShoutTargetingContext);
		Agent primary = ResolvePrimaryAgentForShoutTargetingContext(_activeShoutTargetingContext, framed);
		return EnsurePresentationSession(framed, primary?.Index ?? -1);
	}

	internal List<ScenePresentationHistoryLine> BuildPresentationHistory(int maxLines)
 {
  lock (_historyLock)
  {
   return Presentation.BuildHistory(SceneHistoryOwner.PublicCount, index => {
    SceneHistoryScalarRecord message=SceneHistoryOwner.ReadPublicScalar(index);
    return !message.Exists ? default : new ScenePresentationHistoryRecord { Exists=true, EventSequence=message.EventSequence,
     Role=message.Role, Content=message.Content, TargetName=message.TargetName, SpeakerName=message.SpeakerName };
   }, SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt, maxLines);
  }
 }

 internal const float PresentationTapSeconds = 0.25f;

 internal const int PresentationContextLines = 4;

 internal static bool IsScenePresentationSessionEnabled()
 {
  try { return ScenePresentationSessionHook?.Invoke() == true; }
  catch { return false; }
 }

    private bool _isProcessingShout { get => _ports.Get_isProcessingShout(); set => _ports.Set_isProcessingShout(value); }
    private ShoutTargetingContext _activeShoutTargetingContext { get => _ports.Get_activeShoutTargetingContext(); set => _ports.Set_activeShoutTargetingContext(value); }
    private List<Agent> GetAgentsForShoutTargetingContext(ShoutTargetingContext targetingContext) => _ports.GetAgentsForShoutTargetingContext_L1285(targetingContext);
    private object _historyLock { get => _ports.Get_historyLock(); }
    private FloatingTextMissionView _floatingTextView { get => _ports.Get_floatingTextView(); }
    private void OnShoutConfirmedWithContext(string shoutText, string extraFact, int? forcedPrimaryAgentIndex) => _ports.OnShoutConfirmedWithContext_L17001(shoutText, extraFact, forcedPrimaryAgentIndex);
    private void ResumeGame() => _ports.ResumeGame_L21311();
    private SceneConversationHistoryOwner SceneHistoryOwner { get => _ports.GetSceneHistoryOwner(); }
    private bool IsPresentationRoundActive() => _ports.IsPresentationRoundActive_L26();
    private ScenePresentationController Presentation { get => _ports.GetPresentation(); }
    private bool EnsurePresentationSession(IReadOnlyList<Agent> agents, int primary) => _ports.EnsurePresentationSession_L55(agents, primary);
    private bool _isWaitingForScenePostprocessGate { get => _ports.Get_isWaitingForScenePostprocessGate(); set => _ports.Set_isWaitingForScenePostprocessGate(value); }
}

internal sealed class ScenePresentationBannerlordAdapterPorts
{
    internal Func<int> BeginNewPlayerDrivenSceneConversationEpoch;
    internal Func<bool> Get_isProcessingShout;
    internal Action<bool> Set_isProcessingShout;
    internal Func<ShoutTargetingContext> Get_activeShoutTargetingContext;
    internal Action<ShoutTargetingContext> Set_activeShoutTargetingContext;
    internal delegate List<Agent> GetAgentsForShoutTargetingContext_L1285Callback(ShoutTargetingContext targetingContext);
    internal GetAgentsForShoutTargetingContext_L1285Callback GetAgentsForShoutTargetingContext_L1285;
    internal Func<object> Get_historyLock;
    internal Func<FloatingTextMissionView> Get_floatingTextView;
    internal delegate void OnShoutConfirmedWithContext_L17001Callback(string shoutText, string extraFact, int? forcedPrimaryAgentIndex);
    internal OnShoutConfirmedWithContext_L17001Callback OnShoutConfirmedWithContext_L17001;
    internal delegate void ResumeGame_L21311Callback();
    internal ResumeGame_L21311Callback ResumeGame_L21311;
    internal Func<SceneConversationHistoryOwner> GetSceneHistoryOwner;
    internal delegate bool IsPresentationRoundActive_L26Callback();
    internal IsPresentationRoundActive_L26Callback IsPresentationRoundActive_L26;
    internal Func<ScenePresentationController> GetPresentation;
    internal delegate bool EnsurePresentationSession_L55Callback(IReadOnlyList<Agent> agents, int primary);
    internal EnsurePresentationSession_L55Callback EnsurePresentationSession_L55;
    internal Func<bool> Get_isWaitingForScenePostprocessGate;
    internal Action<bool> Set_isWaitingForScenePostprocessGate;
}
