using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using static AnimusForge.ShoutBehavior;
namespace AnimusForge;
internal sealed class NativeConversationGameEffectPorts
{
    internal delegate bool TryTriggerNativeConversationOpenLordsHallActionCapability(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content);
    internal TryTriggerNativeConversationOpenLordsHallActionCapability TryTriggerNativeConversationOpenLordsHallAction;
    internal delegate bool TryProcessSetsOwnedSettlementMassacreActionTagsCapability(int targetAgentIndex, ref string content);
    internal TryProcessSetsOwnedSettlementMassacreActionTagsCapability TryProcessSetsOwnedSettlementMassacreActionTags;
    internal delegate void QueueNativeConversationNpcSurrenderCapability(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason);
    internal QueueNativeConversationNpcSurrenderCapability QueueNativeConversationNpcSurrender;
    internal delegate void RecordGeneratedNpcAfefFactsForNativeConversationCapability(Hero targetHero, CharacterObject targetCharacter, IEnumerable<string> factLines);
    internal RecordGeneratedNpcAfefFactsForNativeConversationCapability RecordGeneratedNpcAfefFactsForNativeConversation;
    internal delegate bool TryQueueNativeSceneMechanismActionAfterConversationExitCapability(NpcDataPacket npc, List<NpcDataPacket> allNpcData, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, ref string content);
    internal TryQueueNativeSceneMechanismActionAfterConversationExitCapability TryQueueNativeSceneMechanismActionAfterConversationExit;
    internal delegate void TrySpeakNativeConversationReplyWithTtsCapability(Hero targetHero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex, string visibleText);
    internal TrySpeakNativeConversationReplyWithTtsCapability TrySpeakNativeConversationReplyWithTts;
    internal delegate bool IsNativeConversationAdmissionCurrentCapability(NativeConversationAdmission admission, out string reason);
    internal IsNativeConversationAdmissionCurrentCapability IsNativeConversationAdmissionCurrent;
    internal delegate bool IsNativeConversationContextCurrentCapability(NativeConversationAdmission admission, out string reason);
    internal IsNativeConversationContextCurrentCapability IsNativeConversationContextCurrent;
    internal Func<long, bool> IsPresentationCurrent;
    internal Action<Action> PostMainThread;
    internal Action<NativeConversationAdmission, string, long, string> RollbackPendingPlayerHistory;
}
