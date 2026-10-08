using System;
using AnimusForge.Refactor.Adapters;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
namespace AnimusForge;
public partial class ShoutBehavior
{
    private static readonly AnimusForge.Refactor.Adapters.SceneAgentIdentityPromptCaptureAdapter NativePromptIdentityCapture = new AnimusForge.Refactor.Adapters.SceneAgentIdentityPromptCaptureAdapter(MyBehavior.GetNpcPersonaForExternal);
    private static readonly AnimusForge.Refactor.Adapters.SceneAgentIdentityPromptCaptureAdapter.RoleIntroCapturePorts SceneRoleIntroCapture = new(NativePromptIdentityCapture, CreateScenePersonaEquipmentPromptCaptureAdapter, CaptureSceneActionReadPorts);
    private AnimusForge.Refactor.Adapters.SceneMechanismPromptCaptureAdapter _sceneMechanismPromptCapture;
    private AnimusForge.Refactor.Adapters.SceneMechanismPromptCaptureAdapter SceneMechanismPromptCapture => _sceneMechanismPromptCapture ??= new AnimusForge.Refactor.Adapters.SceneMechanismPromptCaptureAdapter(
        index => _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(index) != null,
        _sceneMovement.IsAgentFollowingPlayerBySceneCommand, _sceneMovement.BuildSceneSummonClosurePromptInstruction, _sceneMovement.BuildSceneFollowControlPromptInstruction);
    private AnimusForge.Refactor.Adapters.SharedPromptCaptureBannerlordAdapter _nativeSharedPromptCapture;
    private AnimusForge.Refactor.Adapters.SharedPromptCaptureBannerlordAdapter NativeSharedPromptCapture => _nativeSharedPromptCapture ??= new AnimusForge.Refactor.Adapters.SharedPromptCaptureBannerlordAdapter(
        NativePromptIdentityCapture, _sceneMovement.BuildSceneSummonPromptTargets, _sceneMovement.BuildSceneGuidePromptTargets,
        (npc, summon, guide) => SceneMechanismPromptCapture.BuildRuntimeSceneMechanismPostprocessRulesForScene(npc, summon, guide));
    private SceneHistoryPromptCaptureAdapter.NativePreparationPorts _nativePreparationCapturePorts;
    private SceneHistoryPromptCaptureAdapter.NativePreparationPorts NativePreparationCapturePorts => _nativePreparationCapturePorts ??=
        new SceneHistoryPromptCaptureAdapter.NativePreparationPorts(NativeAdmissions, _nativeSessionOwner, NativePromptIdentityCapture,
            _sceneMovement.BuildSceneSummonPromptTargets, _sceneMovement.BuildSceneGuidePromptTargets, SceneMechanismPromptCapture.BuildPreprocessExcludedRuleIdsForCurrentInteraction);
    private Action<string,int,float> _targetedFactImmediateReaction;
    private Action<string,int,float> TargetedFactImmediateReaction => _targetedFactImmediateReaction ??=
        (text,index,delay) => _sceneConversation.TriggerImmediateSceneBehaviorReaction(text,index,true,false,delay,skipSceneFactRecord:true);
    private static readonly Func<AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter> CurrentSceneHistoryPromptCapture = () => CurrentInstance?.SceneHistoryPromptCapture;
    private static readonly Func<Hero,string> CaptureFirstMeetingPromptText = hero => MyBehavior.GetFirstMeetingNpcFactTextForPromptIfNeeded(hero, persistToHistory: false);
    private AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter _sceneHistoryPromptCapture;
    private AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter SceneHistoryPromptCapture => _sceneHistoryPromptCapture ??= new AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter(
        () => SceneHistoryOwner, _scenePendingAfefFactsOwner, _nativeSessionOwner,
        _sceneRevisitRecords.TryInjectSceneFirstMeetingFactsBeforePlayerMessage, _sceneRevisitRecords.TryInjectSceneRevisitFactsBeforePlayerMessage, _deferredHistoryFacts.Flush,
        new SceneHistoryPromptCaptureAdapter.PersistedHistoryCapturePorts(_conversationGameThreadDispatcher,
            () => ReferenceEquals(CurrentInstance, this), () => _sceneConversation.ConversationEpoch, MyBehavior.ResolveHistoryWorkCapturePorts));
    private SceneConversationHistoryOwner _sceneConversationHistoryOwner;
    private SceneConversationHistoryOwner SceneHistoryOwner
    {
        get { lock (_historyLock) { return _sceneConversationHistoryOwner ??= new SceneConversationHistoryOwner(_historyLock); } }
    }
    private static string CaptureNativeConversationHistoryKey(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null)
    {
        return SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
    }
    private static string CaptureNativeConversationNonHeroUnnamedKey(CharacterObject character, string npcName, int agentIndex)
    {
        return SceneHistoryPromptCaptureAdapter.CaptureNativeConversationNonHeroUnnamedKey(character, npcName, agentIndex, SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMobileParty);
    }
    private static void AppendNativeConversationSessionHistoryCaptured(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, bool bridgeToSceneHistory = true, int targetAgentIndex = -1, NpcDataPacket npc = null, int playerTargetAgentIndex = -1, string playerTargetName = null, string capturedHistoryKey = null)
    {
        AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter.AppendNativeConversationSessionHistoryCaptured(_nativeSessionOwner, CurrentSceneHistoryPromptCapture, targetHero, targetCharacter, npcName, speaker, text, kind, eventSequence, bridgeToSceneHistory, targetAgentIndex, npc, playerTargetAgentIndex, playerTargetName, capturedHistoryKey);
    }
    private static List<string> CaptureAndBuildVisibleSceneHistoryLines(List<ConversationMessage> history, int viewerAgentIndex, string targetNpcName = "", bool useNpcNameAddress = false)
    {
        return SceneHistoryPromptCaptureAdapter.CaptureAndBuildVisibleSceneHistoryLines(history, viewerAgentIndex, targetNpcName, useNpcNameAddress);
    }
    private List<ConversationMessage> CaptureNpcConversationHistory(int npcAgentIndex)
    {
        return SceneHistoryPromptCapture.CaptureNpcConversationHistory(npcAgentIndex);
    }
    private List<string> CaptureAuxiliarySceneDialogueHistoryLines(int targetAgentIndex, int maxLines)
    {
        return SceneHistoryPromptCapture.CaptureAuxiliarySceneDialogueHistoryLines(targetAgentIndex, maxLines);
    }
}
