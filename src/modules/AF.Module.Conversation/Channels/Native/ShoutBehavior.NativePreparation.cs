using System;
using AnimusForge.Refactor.Adapters;
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
    internal sealed class NativeConversationPreparationSnapshot
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
        return SceneHistoryPromptCaptureAdapter.CaptureNativeConversationPreparation(NativePreparationCapturePorts, admission, targetHero, targetCharacter, npcName, routingInput, out reason);
    }
}
