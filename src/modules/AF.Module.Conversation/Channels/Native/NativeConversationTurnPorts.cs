using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

// Bound narrow game capabilities; never stores a ShoutBehavior or a whole phase callback.
internal sealed class NativeConversationTurnPorts
{
    internal delegate Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsyncCapability(Hero targetHero, CharacterObject targetCharacter, NpcDataPacket npc, List<NpcDataPacket> allNpcData, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string content, string npcName, int targetAgentIndex, string playerText, ConversationManager expectedConversationManager, int expectedConversationToken, NativeConversationAdmission admission, NativeConversationCompletionRequest completion = null);
    internal ApplyNativeConversationGameActionsOnMainThreadAsyncCapability ApplyNativeConversationGameActionsOnMainThreadAsync;

    internal Action<NativeConversationAdmission> RetireOpeningForAcceptedReply;
    internal Action<NativeConversationAdmission> ConfirmMeetingElapsedBoundary;
    internal ConversationGameThreadDispatcher PromptDispatcher;
    internal Func<bool> IsPromptOwnerAvailable;
    internal Func<NativePromptCaptureRequest, SharedPromptRoutingWork> CapturePromptRoutingWork;
    internal Func<PromptBuildPhases, Hero, PromptKnowledgeWorkInput> CapturePromptKnowledge;
    internal Func<PromptBuildPhases, Hero, CharacterObject, MyBehavior.WeeklyPromptSnapshot, MyBehavior.ShoutPromptContext> CompletePromptCapture;

    internal delegate List<PostprocessRuleEntry> BuildRuntimeSceneMechanismPostprocessRulesForSceneCapability(NpcDataPacket speaker, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets, bool includeGenericRules = true);
    internal BuildRuntimeSceneMechanismPostprocessRulesForSceneCapability BuildRuntimeSceneMechanismPostprocessRulesForScene;

    internal delegate string BuildSceneFollowControlPromptInstructionCapability(NpcDataPacket speaker);
    internal BuildSceneFollowControlPromptInstructionCapability BuildSceneFollowControlPromptInstruction;

    internal delegate string BuildSceneSummonClosurePromptInstructionCapability(IEnumerable<NpcDataPacket> participants);
    internal BuildSceneSummonClosurePromptInstructionCapability BuildSceneSummonClosurePromptInstruction;

    internal delegate List<object> BuildStrictSceneMessagesForNpcCapability(int npcAgentIndex, string systemPrompt, IEnumerable<string> prefixUserSections, IEnumerable<string> suffixUserSections = null, bool currentInputAlreadyRecorded = true, string currentPlayerInput = null, int maxHistoryMessages = 0, bool suppressReplyFormatInstruction = false, IEnumerable<ConversationMessage> injectedHistoryMessages = null, bool includeSceneHistory = true, IEnumerable<ConversationMessage> persistentHistoryMessages = null, IEnumerable<ConversationMessage> pendingCurrentAfefFactMessages = null, bool useSceneDistanceSpeechLabels = true);
    internal BuildStrictSceneMessagesForNpcCapability BuildStrictSceneMessagesForNpc;

    internal delegate NativeConversationPreparationSnapshot CaptureNativeConversationPreparationCapability(
        NativeConversationAdmission admission, Hero targetHero, CharacterObject targetCharacter,
        string npcName, string routingInput, out string reason);
    internal CaptureNativeConversationPreparationCapability CaptureNativeConversationPreparation;

    internal delegate Task<bool> EnsureNativeConversationPersonaReadyAsyncCapability(NativeConversationAdmission admission, Action<string> onStreamText);
    internal EnsureNativeConversationPersonaReadyAsyncCapability EnsureNativeConversationPersonaReadyAsync;

    internal delegate bool IsNativeConversationAdmissionCurrentCapability(NativeConversationAdmission admission, out string reason);
    internal IsNativeConversationAdmissionCurrentCapability IsNativeConversationAdmissionCurrent;

    internal delegate void LogTtsReportCapability(string stage, int agentIndex, string extra = null);
    internal LogTtsReportCapability LogTtsReport;

    internal delegate Task<NativeConversationPendingHistory> PrepareNativeConversationPendingHistoryAsyncCapability(
        NativeConversationAdmission admission, NpcDataPacket npc, string npcName, int agentIndex,
        string playerText, bool recordPlayerInput);
    internal PrepareNativeConversationPendingHistoryAsyncCapability PrepareNativeConversationPendingHistoryAsync;

    internal delegate Task RollbackNativeConversationPendingPlayerHistoryAsyncCapability(NativeConversationAdmission admission,
        string historyKey, long eventSequence, string reason);
    internal RollbackNativeConversationPendingPlayerHistoryAsyncCapability RollbackNativeConversationPendingPlayerHistoryAsync;

    internal delegate void TrySpeakNativeConversationReplyWithTtsCapability(Hero targetHero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex, string visibleText);
    internal TrySpeakNativeConversationReplyWithTtsCapability TrySpeakNativeConversationReplyWithTts;

    internal Func<string, string, int, Func<bool>, bool, Task<bool>> DispatchValidation;
    internal Task<bool> RunNativeConversationMainThreadFuncAsync(string phase, string target, int agentIndex, Func<bool> capture, bool fallback) => DispatchValidation(phase, target, agentIndex, capture, fallback);

    internal Func<string, string, int, Func<NativeConversationPreparationSnapshot>, NativeConversationPreparationSnapshot, Task<NativeConversationPreparationSnapshot>> DispatchPreparation;
    internal Task<NativeConversationPreparationSnapshot> RunNativeConversationMainThreadFuncAsync(string phase, string target, int agentIndex, Func<NativeConversationPreparationSnapshot> capture, NativeConversationPreparationSnapshot fallback) => DispatchPreparation(phase, target, agentIndex, capture, fallback);

    internal Func<string, string, int, Func<Func<string>>, Func<string>, Task<Func<string>>> DispatchHistoryWork;
    internal Task<Func<string>> RunNativeConversationMainThreadFuncAsync(string phase, string target, int agentIndex, Func<Func<string>> capture, Func<string> fallback) => DispatchHistoryWork(phase, target, agentIndex, capture, fallback);

    internal Func<string, string, int, Func<MyBehavior.WeeklyPromptSnapshot>, MyBehavior.WeeklyPromptSnapshot, Task<MyBehavior.WeeklyPromptSnapshot>> DispatchWeeklySnapshot;
    internal Task<MyBehavior.WeeklyPromptSnapshot> RunNativeConversationMainThreadFuncAsync(string phase, string target, int agentIndex, Func<MyBehavior.WeeklyPromptSnapshot> capture, MyBehavior.WeeklyPromptSnapshot fallback) => DispatchWeeklySnapshot(phase, target, agentIndex, capture, fallback);

}
