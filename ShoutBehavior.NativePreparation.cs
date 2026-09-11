using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    // Request-owned data from one main-thread capture, not a public game-object API.
    private sealed class NativeConversationPreparationSnapshot
    {
        public NpcDataPacket Npc;
        public string TargetLog;
        public List<NpcDataPacket> PresentNpcs;
        public string CultureId;
        public bool HadSessionHistory;
        public string MeetingTauntRuleBlock;
        public List<SceneSummonPromptTarget> SummonTargets;
        public List<SceneGuidePromptTarget> GuideTargets;
        public List<string> ExcludedRuleIds;
    }

    private NativeConversationPreparationSnapshot CaptureNativeConversationPreparation(
        NativeConversationAdmission admission, Hero targetHero, CharacterObject targetCharacter,
        string npcName, string routingInput, out string reason)
    {
        if (!IsBannerlordMainThreadForNativeActions())
        {
            reason = "main_thread_required";
            return null;
        }
        if (!IsNativeConversationAdmissionCurrent(admission, out reason)) return null;
        // Same existing builders and inputs; no LLM/recall task may be started inside this capture.
		int nativeTargetAgentIndex = admission.AgentIndex;
		NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, targetCharacter);
		npc.AgentIndex = nativeTargetAgentIndex;
		string nativeTargetLog = targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? "unknown";
		List<NpcDataPacket> presentNpcs = new List<NpcDataPacket> { npc };
		string cultureId = npc.CultureId ?? "neutral";
		bool hadNativeConversationSessionHistoryBeforeTurn = HasNativeConversationSessionHistory(targetHero, targetCharacter, npcName, nativeTargetAgentIndex, npc);
		string nativeMeetingTauntRuleBlock = "";
		PartyBase nativeMeetingTauntParty = null;
		if (targetHero == null)
		{
			TryResolveNativeConversationMeetingTauntParty(targetHero, targetCharacter, nativeTargetAgentIndex, out nativeMeetingTauntParty);
		}
		string nativeMeetingTauntInstruction = (LordEncounterBehavior.BuildMeetingTauntRuntimeInstructionForExternal(targetHero, targetCharacter, nativeMeetingTauntParty) ?? "").Trim();
		if (string.IsNullOrWhiteSpace(nativeMeetingTauntInstruction))
		{
			nativeMeetingTauntInstruction = (SceneTauntBehavior.BuildSceneTauntRuntimeInstructionForExternal(targetHero, targetCharacter, nativeTargetAgentIndex) ?? "").Trim();
		}
		if (AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MeetingTauntRuleId, nativeTargetAgentIndex) && !string.IsNullOrWhiteSpace(nativeMeetingTauntInstruction))
		{
			nativeMeetingTauntRuleBlock = AfGcczShoutBridge.MeetingTauntRuleBlockMarker + Environment.NewLine + nativeMeetingTauntInstruction;
		}
		Dictionary<int, Hero> nativeResolvedHeroes = new Dictionary<int, Hero>();
		if (nativeTargetAgentIndex >= 0 && targetHero != null)
		{
			nativeResolvedHeroes[nativeTargetAgentIndex] = targetHero;
		}
		List<SceneSummonPromptTarget> nativeSceneSummonTargets = (nativeTargetAgentIndex >= 0) ? BuildSceneSummonPromptTargets(presentNpcs, nativeResolvedHeroes) : null;
		int nativeSceneGuideFirstPromptId = ((nativeSceneSummonTargets != null && nativeSceneSummonTargets.Count > 0) ? nativeSceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
		Agent nativeTargetAgent = (nativeTargetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == nativeTargetAgentIndex) : null;
		List<SceneGuidePromptTarget> nativeSceneGuideTargets = (nativeTargetAgentIndex >= 0) ? BuildSceneGuidePromptTargets(nativeTargetAgent, nativeSceneGuideFirstPromptId) : null;
		List<string> preprocessExcludedRuleIds = BuildPreprocessExcludedRuleIdsForCurrentInteraction(targetHero, targetCharacter, nativeTargetAgentIndex, npc.IsHero, nativeSceneSummonTargets, nativeSceneGuideTargets, npc, presentNpcs, routingInput);
        return new NativeConversationPreparationSnapshot
        {
            Npc = npc,
            TargetLog = nativeTargetLog,
            PresentNpcs = presentNpcs,
            CultureId = cultureId,
            HadSessionHistory = hadNativeConversationSessionHistoryBeforeTurn,
            MeetingTauntRuleBlock = nativeMeetingTauntRuleBlock,
            SummonTargets = nativeSceneSummonTargets,
            GuideTargets = nativeSceneGuideTargets,
            ExcludedRuleIds = preprocessExcludedRuleIds
        };
    }
}
