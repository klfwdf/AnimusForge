using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
internal static string TryRunSceneUnifiedActionPostprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string entityPostprocessContext = null, bool siegeInterventionRuleInjected = false, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, string chainName = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, DetachedPromptSections detachedMainPromptSections = null)
	{
		return ConversationActionPostprocessOwner.TryRunSceneUnifiedActionPostprocess(targetHero, targetCharacter, targetAgentIndex, npcName, playerText, historyText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, marriageRuleInjected, duelStakeOptions, kingdomServiceRules, sceneMechanismRules, CapturePostprocessSummonTargets(sceneSummonTargets), CapturePostprocessGuideTargets(sceneGuideTargets), entityPostprocessContext, siegeInterventionRuleInjected, replyIsDirectPlayerResponse, preprocessRuleHits, chainName, relayRuleInjected, relayCandidates, relayPrimaryTargetAgentIndex, relaySingleFramedNpc, customPolicyAgendaRuleInjected, detachedMainPromptSections);
	}

internal static bool TryRequestSceneUnifiedActionPostprocess(string systemPrompt, string userPrompt, out string content, out string error)
	{
		return ConversationActionPostprocessOwner.TryRequestSceneUnifiedActionPostprocess(systemPrompt, userPrompt, out content, out error);
	}

internal static string CompleteSceneUnifiedActionPostprocess(SceneActionPostprocessWorkItem workItem, bool succeeded, string content, string error)
	{
		return ConversationActionPostprocessOwner.CompleteSceneUnifiedActionPostprocess(workItem, succeeded, content, error);
	}

internal static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string entityPostprocessContext = null, bool siegeInterventionRuleInjected = false, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, string chainName = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, DetachedPromptSections detachedMainPromptSections = null, bool offerSceneActionDirective = false, string relayConversationContext = null)
	{
		return ConversationActionPostprocessOwner.PrepareSceneUnifiedActionPostprocess(targetHero, targetCharacter, targetAgentIndex, npcName, playerText, historyText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, marriageRuleInjected, duelStakeOptions, kingdomServiceRules, sceneMechanismRules, CapturePostprocessSummonTargets(sceneSummonTargets), CapturePostprocessGuideTargets(sceneGuideTargets), entityPostprocessContext, siegeInterventionRuleInjected, replyIsDirectPlayerResponse, preprocessRuleHits, chainName, relayRuleInjected, relayCandidates, relayPrimaryTargetAgentIndex, relaySingleFramedNpc, customPolicyAgendaRuleInjected, detachedMainPromptSections, offerSceneActionDirective, relayConversationContext);
	}
}
