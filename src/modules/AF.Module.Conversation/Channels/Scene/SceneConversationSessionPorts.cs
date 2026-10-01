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
using static AnimusForge.SceneMovementController;
namespace AnimusForge;
internal sealed class SceneConversationSessionPorts
{
    internal delegate void AbsorbPresentationAudienceCapability(IReadOnlyList<Agent> audienceAgents);
    internal AbsorbPresentationAudienceCapability AbsorbPresentationAudience;
    internal delegate void ActivateMultiSceneMovementSuppressionCapability(IEnumerable<int> participantAgentIndices);
    internal ActivateMultiSceneMovementSuppressionCapability ActivateMultiSceneMovementSuppression;
    internal delegate Task<string> AwaitPrecomputedPersistedHistoryContextAsyncCapability(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Task<string> startedTask, string reason);
    internal AwaitPrecomputedPersistedHistoryContextAsyncCapability AwaitPrecomputedPersistedHistoryContextAsync;
    internal delegate string BuildPersistedHeroHistoryContextCapability(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes);
    internal BuildPersistedHeroHistoryContextCapability BuildPersistedHeroHistoryContext;
    internal delegate List<string> BuildPreprocessExcludedRuleIdsForCurrentInteractionCapability(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, bool hasAnyHero, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, NpcDataPacket sceneSpeakerNpc = null, List<NpcDataPacket> sceneCandidates = null, string currentPlayerText = null);
    internal BuildPreprocessExcludedRuleIdsForCurrentInteractionCapability BuildPreprocessExcludedRuleIdsForCurrentInteraction;
    internal delegate List<PostprocessRuleEntry> BuildRuntimeSceneMechanismPostprocessRulesForSceneCapability(NpcDataPacket speaker, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets, bool includeGenericRules = true);
    internal BuildRuntimeSceneMechanismPostprocessRulesForSceneCapability BuildRuntimeSceneMechanismPostprocessRulesForScene;
    internal delegate string BuildScenePresentNpcListBlockForPromptCapability(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc, Dictionary<int, Hero> resolvedHeroes = null, bool useAllegianceLabel = false);
    internal BuildScenePresentNpcListBlockForPromptCapability BuildScenePresentNpcListBlockForPrompt;
    internal delegate List<object> BuildStrictSceneMessagesForNpcCapability(int npcAgentIndex, string systemPrompt, IEnumerable<string> prefixUserSections, IEnumerable<string> suffixUserSections = null, bool currentInputAlreadyRecorded = true, string currentPlayerInput = null, int maxHistoryMessages = 0, bool suppressReplyFormatInstruction = false, IEnumerable<ConversationMessage> injectedHistoryMessages = null, bool includeSceneHistory = true, IEnumerable<ConversationMessage> persistentHistoryMessages = null, IEnumerable<ConversationMessage> pendingCurrentAfefFactMessages = null, bool useSceneDistanceSpeechLabels = true);
    internal BuildStrictSceneMessagesForNpcCapability BuildStrictSceneMessagesForNpc;
    internal delegate void ClearPendingSceneConversationAttentionReleaseCapability();
    internal ClearPendingSceneConversationAttentionReleaseCapability ClearPendingSceneConversationAttentionRelease;
    internal delegate void ClearQueuedSceneSpeechCapability();
    internal ClearQueuedSceneSpeechCapability ClearQueuedSceneSpeech;
    internal delegate void DeactivateMultiSceneMovementSuppressionCapability();
    internal DeactivateMultiSceneMovementSuppressionCapability DeactivateMultiSceneMovementSuppression;
    internal delegate void EnqueueSpeechLineCapability(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool skipHistory = false, bool suppressStare = false, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, Func<bool> canStillPublish = null);
    internal EnqueueSpeechLineCapability EnqueueSpeechLine;
    internal delegate void EnqueueSpeechLineWithOptionsCapability(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string afterSpeechInfoMessage = null, TaskCompletionSource<bool> completionSource = null, float interactionTimeoutSeconds = -1f, int interactionParticipantCount = 1, Func<bool> canStillPublish = null, string playerDirectedActionText = null, string playerDirectedNpcReplyText = null);
    internal EnqueueSpeechLineWithOptionsCapability EnqueueSpeechLineWithOptions;
    internal delegate Task EnsurePersonaForCandidatesAsyncCapability(List<NpcDataPacket> candidates, Dictionary<int, Hero> resolvedHeroes);
    internal EnsurePersonaForCandidatesAsyncCapability EnsurePersonaForCandidatesAsync;
    internal delegate void ExtendStaringHoldForPlayerDrivenSceneRoundCapability(int participantCount);
    internal ExtendStaringHoldForPlayerDrivenSceneRoundCapability ExtendStaringHoldForPlayerDrivenSceneRound;
    internal delegate List<Agent> GetAgentsForShoutTargetingContextCapability(ShoutTargetingContext targetingContext);
    internal GetAgentsForShoutTargetingContextCapability GetAgentsForShoutTargetingContext;
    internal delegate string GetLatestSceneNpcUtteranceCapability(int targetAgentIndex);
    internal GetLatestSceneNpcUtteranceCapability GetLatestSceneNpcUtterance;
    internal delegate HashSet<int> GetPresentationExcludedAgentIndicesCapability();
    internal GetPresentationExcludedAgentIndicesCapability GetPresentationExcludedAgentIndices;
    internal delegate void HoldSceneConversationAgentsCapability(List<Agent> participants);
    internal HoldSceneConversationAgentsCapability HoldSceneConversationAgents;
    internal delegate void NotePresentationRoundGroupCapability(Task groupTask);
    internal NotePresentationRoundGroupCapability NotePresentationRoundGroup;
    internal delegate bool PersistExtraFactToNamedHeroesCapability(
		string extraFact,
		List<NpcDataPacket> nearbyData,
		int personalizedAgentIndex = -1,
		bool requireMemoryReceipt = false);
    internal PersistExtraFactToNamedHeroesCapability PersistExtraFactToNamedHeroes;
    internal delegate bool PersistNpcSpeechToNamedHeroesCapability(int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData, bool requireMemoryReceipt = false);
    internal PersistNpcSpeechToNamedHeroesCapability PersistNpcSpeechToNamedHeroes;
    internal delegate void PrepareAutoGroupParticipantsForIdleTimeoutCapability(List<NpcDataPacket> participants, string trailingSpeechText = null, string playerText = null, List<string> npcVisibleTexts = null, int distinctNpcSpeakerCount = 0);
    internal PrepareAutoGroupParticipantsForIdleTimeoutCapability PrepareAutoGroupParticipantsForIdleTimeout;
    internal delegate void PromotePersonalizedExtraFactInScenePrivateHistoryCapability(
		string personalizedExtraFact,
		int personalizedAgentIndex);
    internal PromotePersonalizedExtraFactInScenePrivateHistoryCapability PromotePersonalizedExtraFactInScenePrivateHistory;
    internal delegate void QueueSceneInfoMessageCapability(string message, Color color, int requiredConversationEpoch = 0, string soundEventPath = "");
    internal QueueSceneInfoMessageCapability QueueSceneInfoMessage;
    internal delegate void RecordExtraFactToSceneHistoryCapability(string extraFact, List<NpcDataPacket> nearbyData);
    internal RecordExtraFactToSceneHistoryCapability RecordExtraFactToSceneHistory;
    internal delegate bool RecordPlayerMessageCapability(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "", Dictionary<int, Agent> audienceAgentsByIndex = null, bool requireMemoryReceipt = false);
    internal RecordPlayerMessageCapability RecordPlayerMessage;
    internal delegate void RecordPlayerSpeechToMessageFeedCapability(string content);
    internal RecordPlayerSpeechToMessageFeedCapability RecordPlayerSpeechToMessageFeed;
    internal delegate bool RecordResponseForAllNearbySafeCapability(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt = false);
    internal RecordResponseForAllNearbySafeCapability RecordResponseForAllNearbySafe;
    internal delegate void ReleaseSceneConversationConstraintsCapability(List<NpcDataPacket> participants, int fallbackAgentIndex = -1, bool stopAutoGroupSession = true, bool clearQueuedSpeech = true, bool forceFullAutonomyRelease = false);
    internal ReleaseSceneConversationConstraintsCapability ReleaseSceneConversationConstraints;
    internal delegate void RemoveSceneMovementSuppressionAgentsCapability(IEnumerable<int> participantAgentIndices);
    internal RemoveSceneMovementSuppressionAgentsCapability RemoveSceneMovementSuppressionAgents;
    internal delegate void ResetStaringBehaviorCapability();
    internal ResetStaringBehaviorCapability ResetStaringBehavior;
    internal delegate void ResetStaringForActiveInteractionCapability(List<Agent> nearbyAgents, Agent primaryTarget);
    internal ResetStaringForActiveInteractionCapability ResetStaringForActiveInteraction;
    internal delegate Hero ResolveHeroFromAgentIndexCapability(int agentIndex);
    internal ResolveHeroFromAgentIndexCapability ResolveHeroFromAgentIndex;
    internal delegate void ResumeGameCapability();
    internal ResumeGameCapability ResumeGame;
    internal delegate void SetPendingHeroHistoryExtraFactAfterSceneReplyCapability(
		string extraFact,
		List<NpcDataPacket> nearbyData,
		int personalizedAgentIndex = -1);
    internal SetPendingHeroHistoryExtraFactAfterSceneReplyCapability SetPendingHeroHistoryExtraFactAfterSceneReply;
    internal delegate Task<string> StartPrecomputedPersistedHistoryContextTaskCapability(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, string reason);
    internal StartPrecomputedPersistedHistoryContextTaskCapability StartPrecomputedPersistedHistoryContextTask;
    internal delegate void StopAllLipSyncPlaybackAndCleanupCapability();
    internal StopAllLipSyncPlaybackAndCleanupCapability StopAllLipSyncPlaybackAndCleanup;
    internal delegate void TrackPlayerInteractionCapability(NpcDataPacket primaryTarget, int participantCount = 1, float timeoutSeconds = -1f, bool returnSceneSummonOnTimeout = false, ShoutTargetingContext shoutTargetingContext = null);
    internal TrackPlayerInteractionCapability TrackPlayerInteraction;
    internal delegate bool TryApplyDeferredSceneMoodTagCapability(NpcDataPacket speaker, string tags);
    internal TryApplyDeferredSceneMoodTagCapability TryApplyDeferredSceneMoodTag;
    internal delegate bool TryApplyDeferredScenePostprocessActionTagsDirectlyCapability(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		ref string tags,
		string playerText,
		string npcReplyText,
		string chainName,
		bool replyIsDirectPlayerResponse,
		DetachedDuelDispatchContext duelDispatchContext = null);
    internal TryApplyDeferredScenePostprocessActionTagsDirectlyCapability TryApplyDeferredScenePostprocessActionTagsDirectly;
    internal Func<bool> IsOwnerCurrent, GetProcessing;
    internal Action<bool> SetProcessing;
    internal Func<int> SceneSessionId;
    internal Func<long> ProcessingSequence;
    internal Func<ShoutTargetingContext> ActiveTargetingContext;
    internal Action<Action> PostMainThread;
    internal Func<int,string,bool,List<string>> CaptureVisibleSceneHistoryLines;
    internal Action<string> PreparePlayerRound;
    internal Action ClearPendingHeroFacts;
    internal Action<string> ShowPlayerSpeech;
    internal Action<int> ClearImmediateInteractionTimeouts;


    internal Func<ScenePromptCaptureRequest, SharedPromptRoutingWork> CapturePromptRoutingWork;
    internal Func<PromptBuildPhases, Hero, PromptKnowledgeWorkInput> CapturePromptKnowledge;
    internal Func<PromptBuildPhases, Hero, CharacterObject, MyBehavior.ShoutPromptContext> CompletePromptCapture;
}
internal sealed class ScenePromptCaptureRequest
{
    internal readonly Hero Hero;
    internal readonly CharacterObject Character;
    internal readonly string Input, ExtraFact, CultureId, KingdomId, Lore;
    internal readonly int AgentIndex;
    internal readonly bool IsHero, HasLore;
    internal readonly List<string> ExcludedRules;
    internal ScenePromptCaptureRequest(Hero hero, CharacterObject character, string input, string extraFact, string cultureId, string kingdomId, int agentIndex, bool isHero, bool hasLore, string lore, List<string> excludedRules)
    { Hero=hero;Character=character;Input=input;ExtraFact=extraFact;CultureId=cultureId;KingdomId=kingdomId;AgentIndex=agentIndex;IsHero=isHero;HasLore=hasLore;Lore=lore;ExcludedRules=excludedRules; }
}
