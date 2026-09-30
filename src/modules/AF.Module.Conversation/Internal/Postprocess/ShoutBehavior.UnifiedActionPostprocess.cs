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
	// One invocation owns this prepared state until completion. Its normalizer captures
	// live Hero/Character references and request-local rule/asset candidates: it is not
	// a detached DTO. Prepare and complete on the owning game context after checking
	// that the request is still current; send only the two prompt strings to a worker.
	private sealed class SceneActionPostprocessWorkItem
	{
		private int _completionStarted;

		public string SystemPrompt { get; }
		public string UserPrompt { get; }
		public string ReplyText { get; }
		public string ImmediateResult { get; }
		public Func<string, string> Normalize { get; }
		public bool RequiresNetwork => Normalize != null;

		public bool TryBeginCompletion()
		{
			return Interlocked.CompareExchange(ref _completionStarted, 1, 0) == 0;
		}

		public SceneActionPostprocessWorkItem(string immediateResult)
		{
			ReplyText = immediateResult;
			ImmediateResult = immediateResult;
		}

		public SceneActionPostprocessWorkItem(string systemPrompt, string userPrompt, string replyText, Func<string, string> normalize)
		{
			SystemPrompt = systemPrompt;
			UserPrompt = userPrompt;
			ReplyText = replyText;
			Normalize = normalize ?? throw new ArgumentNullException(nameof(normalize));
		}
	}

	private static string TryRunSceneUnifiedActionPostprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string entityPostprocessContext = null, bool siegeInterventionRuleInjected = false, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, string chainName = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, DetachedPromptSections detachedMainPromptSections = null)
	{
		SceneActionPostprocessWorkItem workItem = PrepareSceneUnifiedActionPostprocess(targetHero, targetCharacter, targetAgentIndex, npcName, playerText, historyText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, marriageRuleInjected, duelStakeOptions, kingdomServiceRules, sceneMechanismRules, sceneSummonTargets, sceneGuideTargets, entityPostprocessContext, siegeInterventionRuleInjected, replyIsDirectPlayerResponse, preprocessRuleHits, chainName, relayRuleInjected, relayCandidates, relayPrimaryTargetAgentIndex, relaySingleFramedNpc, customPolicyAgendaRuleInjected, detachedMainPromptSections);
		if (!workItem.RequiresNetwork)
		{
			return CompleteSceneUnifiedActionPostprocess(workItem, true, null, null);
		}
		bool succeeded = TryRequestSceneUnifiedActionPostprocess(workItem.SystemPrompt, workItem.UserPrompt, out string content, out string error);
		return CompleteSceneUnifiedActionPostprocess(workItem, succeeded, content, error);
	}

	private static bool TryRequestSceneUnifiedActionPostprocess(string systemPrompt, string userPrompt, out string content, out string error)
	{
		return AIConfigHandler.TryCallAuxiliaryActionPostprocess(systemPrompt, userPrompt, 5000, 0f, out content, out error);
	}

	private static string CompleteSceneUnifiedActionPostprocess(SceneActionPostprocessWorkItem workItem, bool succeeded, string content, string error)
	{
		if (workItem == null)
		{
			throw new ArgumentNullException(nameof(workItem));
		}
		if (!workItem.TryBeginCompletion())
		{
			throw new InvalidOperationException("Scene postprocess completion has already started; it must not be retried.");
		}
		if (!workItem.RequiresNetwork)
		{
			return workItem.ImmediateResult;
		}
		if (!succeeded)
		{
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] 调用失败: " + error);
			return (workItem.ReplyText + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
		}
		return workItem.Normalize(content);
	}

	private static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string entityPostprocessContext = null, bool siegeInterventionRuleInjected = false, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, string chainName = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, DetachedPromptSections detachedMainPromptSections = null, bool offerSceneActionDirective = false)
	{
		string text = StripActionTagsForSceneSpeech(replyText ?? "");
		string resolvedChainName = string.IsNullOrWhiteSpace(chainName) ? ResolveScenePostprocessChainName() : chainName.Trim();
		bool kingdomVassalagePreprocessHit = HasPreprocessRuleHit(preprocessRuleHits, "kingdom_vassalage");
		bool nobleGatheringRuleInjected = HasPreprocessRuleHit(preprocessRuleHits, "noble_gathering");
		bool persistentAdpDebtRuleInjected = HasPreprocessRuleHit(preprocessRuleHits, PersistentAdpDebtPostprocessRuleId);
		bool customPolicyAgendaPreprocessHit = HasPreprocessRuleHit(preprocessRuleHits, CustomPolicyAgendaPostprocessRuleId);
		customPolicyAgendaRuleInjected = replyIsDirectPlayerResponse && (customPolicyAgendaRuleInjected || customPolicyAgendaPreprocessHit);
		if (customPolicyAgendaRuleInjected && !TeamModuleServices.Policy.IsEligibleTargetForExternal(targetHero ?? targetCharacter?.HeroObject, out var customPolicyAgendaBlockedReason))
		{
			customPolicyAgendaRuleInjected = false;
			Logger.Log("ShoutBehavior", "[CustomPolicyAgendaPostprocess] blocked chain=" + resolvedChainName + " target=" + (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "null") + " reason=" + (customPolicyAgendaBlockedReason ?? ""));
		}
		kingdomVassalageRuleInjected = kingdomVassalageRuleInjected || kingdomVassalagePreprocessHit;
		bool royalPostprocessEligible = AIConfigHandler.IsRoyalAbdicationPostprocessTargetForExternal(targetHero ?? targetCharacter?.HeroObject);
		bool royalDiplomacyRequested = diplomacyRuleInjected || kingdomAnnexationRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "diplomacy");
		bool royalDiplomacyRuleInjected = royalDiplomacyRequested && DiplomacyBehavior.CanUseDiplomacyActionPostprocessForExternal(targetHero, targetCharacter);
		bool independentClanPeaceResident = replyIsDirectPlayerResponse && DiplomacyBehavior.CanUseIndependentClanPeaceForExternal(targetHero, targetCharacter);
		diplomacyRuleInjected = royalDiplomacyRuleInjected || independentClanPeaceResident;
		kingdomAnnexationRuleInjected = false;
		duelRuleInjected = duelRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.DuelRuleId, targetAgentIndex);
		rewardRuleInjected = rewardRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.RewardRuleId, targetAgentIndex);
		loanRuleInjected = loanRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.LoanRuleId, targetAgentIndex);
		persistentAdpDebtRuleInjected = persistentAdpDebtRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.PersistentDebtRuleId, targetAgentIndex);
		kingdomServiceRuleInjected = kingdomServiceRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomServiceRuleId, targetAgentIndex);
		kingdomVassalageRuleInjected = kingdomVassalageRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomVassalageRuleId, targetAgentIndex);
		lordsHallRuleInjected = lordsHallRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.LordsHallRuleId, targetAgentIndex);
		meetingReleaseRuleInjected = meetingReleaseRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.EncounterReleaseRuleId, targetAgentIndex);
		vanillaIssueRuleInjected = vanillaIssueRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.VanillaIssueRuleId, targetAgentIndex);
		heroJoinPartyRuleInjected = heroJoinPartyRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomServiceRuleId, targetAgentIndex);
		sceneMechanismRuleInjected = sceneMechanismRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.SceneMechanismRuleId, targetAgentIndex);
		partyTransferRuleInjected = partyTransferRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.PartyTransferRuleId, targetAgentIndex);
		voteDealRuleInjected = voteDealRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomAgendaRuleId, targetAgentIndex);
		customPolicyAgendaRuleInjected = customPolicyAgendaRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomAgendaRuleId, targetAgentIndex);
		diplomacyRuleInjected = diplomacyRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.DiplomacyRuleId, targetAgentIndex);
		worldMapPartyCommandRuleInjected = worldMapPartyCommandRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.WorldMapPartyCommandRuleId, targetAgentIndex);
		nobleGatheringRuleInjected = nobleGatheringRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.NobleGatheringRuleId, targetAgentIndex);
		marriageRuleInjected = marriageRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MarriageRuleId, targetAgentIndex);
		relayRuleInjected = relayRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.SceneRelayRuleId, targetAgentIndex);
		Logger.Log("ShoutBehavior", "[UnifiedPostprocess] setup chain=" + resolvedChainName
			+ " preprocessHits=" + ((preprocessRuleHits == null || preprocessRuleHits.Count == 0) ? "(none)" : string.Join(",", preprocessRuleHits))
			+ " kingdom_vassalage_hit=" + kingdomVassalagePreprocessHit
			+ " kingdom_vassalage_injected=" + kingdomVassalageRuleInjected
			+ " kingdom_agenda_injected=" + customPolicyAgendaRuleInjected
			+ " diplomacy_injected=" + diplomacyRuleInjected
			+ " independent_clan_peace_resident=" + independentClanPeaceResident);
		if (kingdomVassalageRuleInjected)
		{
			VassalageDiagnosticLog.Event("postprocess.scene.start", new Dictionary<string, object>
			{
				["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
				["targetCharacterId"] = targetCharacter?.StringId ?? "",
				["targetAgentIndex"] = targetAgentIndex,
				["npcName"] = npcName ?? "",
				["playerText"] = VassalageDiagnosticLog.Preview(playerText, 1000),
				["historyTextLen"] = (historyText ?? "").Length,
				["replyText"] = VassalageDiagnosticLog.Preview(replyText, 2000),
				["entityPostprocessContextLen"] = (entityPostprocessContext ?? "").Length,
				["preprocessHits"] = preprocessRuleHits
			});
		}
		bool excludeSceneMoveRule = AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission();
		bool inspectionSlaughterRuleAvailable = (sceneMechanismRules ?? new List<PostprocessRuleEntry>())
			.Any(rule => string.Equals(
				(rule?.Tag ?? "").Trim(),
				TroopInspectionPrisonerSlaughterProfile.ActionTag,
				StringComparison.OrdinalIgnoreCase));
		bool noblePrisonerExecutionRuleAvailable = (sceneMechanismRules ?? new List<PostprocessRuleEntry>())
			.Any(rule => string.Equals(
				(rule?.Tag ?? "").Trim(),
				NoblePrisonerEscortBehavior.ExecuteActionTag,
				StringComparison.OrdinalIgnoreCase));
		if (sceneMechanismRuleInjected || excludeSceneMoveRule)
		{
			text = StripSceneMechanismActionTagsForScene(text);
		}
		if (excludeSceneMoveRule && !inspectionSlaughterRuleAvailable && !noblePrisonerExecutionRuleAvailable)
		{
			sceneMechanismRuleInjected = false;
			sceneMechanismRules = null;
			sceneSummonTargets = null;
			sceneGuideTargets = null;
		}
		else if (excludeSceneMoveRule)
		{
			sceneMechanismRuleInjected = true;
			sceneMechanismRules = sceneMechanismRules
				.Where(rule =>
				{
					string tag = (rule?.Tag ?? "").Trim();
					return string.Equals(
						tag,
						TroopInspectionPrisonerSlaughterProfile.ActionTag,
						StringComparison.OrdinalIgnoreCase)
						|| string.Equals(
							tag,
							NoblePrisonerEscortBehavior.ExecuteActionTag,
							StringComparison.OrdinalIgnoreCase);
				})
				.ToList();
			sceneSummonTargets = null;
			sceneGuideTargets = null;
		}
		sceneMechanismRuleInjected = sceneMechanismRuleInjected
			&& AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.SceneMechanismRuleId, targetAgentIndex);
		if (!AIConfigHandler.CanUseAuxiliaryActionPostprocess())
		{
			if (Regex.Matches(text ?? "", "\\[ACTION:MOOD:[^\\]]+\\]", RegexOptions.IgnoreCase).Count <= 0 && !string.IsNullOrWhiteSpace(AIConfigHandler.ActionPostprocessFallbackMoodTag))
			{
				text = (text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
			}
			return new SceneActionPostprocessWorkItem(text.Trim());
		}
		string actionPostprocessSystemPrompt = AIConfigHandler.ActionPostprocessSystemPrompt;
		string actionPostprocessUserPromptTemplate = AIConfigHandler.ActionPostprocessUserPromptTemplate;
		if (string.IsNullOrWhiteSpace(actionPostprocessSystemPrompt) || string.IsNullOrWhiteSpace(actionPostprocessUserPromptTemplate))
		{
			return new SceneActionPostprocessWorkItem((text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim());
		}
		bool transactionPostprocessEnabled = rewardRuleInjected || loanRuleInjected || persistentAdpDebtRuleInjected;
		bool exposeAllRewardItems = rewardRuleInjected && RequestsAllOrdinaryAssetsForPostprocess(playerText, historyText);
		bool exposeAllFixedAssets = rewardRuleInjected && RequestsAllFixedAssetsForPostprocess(playerText, historyText);
		List<PostprocessRuleEntry> duelRules = duelRuleInjected ? AIConfigHandler.DuelPostprocessRules : null;
		List<PostprocessRuleEntry> persistentAdpDebtRules = persistentAdpDebtRuleInjected ? BuildPersistentAdpDebtPostprocessRules() : null;
		List<PostprocessRuleEntry> transactionRules = transactionPostprocessEnabled ? MergePostprocessRulesForScene(rewardRuleInjected ? AIConfigHandler.RewardPostprocessRules : null, loanRuleInjected ? AIConfigHandler.LoanPostprocessRules : null, persistentAdpDebtRules) : null;
		List<PostprocessRuleEntry> kingdomRules = null;
		if (kingdomServiceRuleInjected)
		{
			kingdomRules = MergePostprocessRulesForScene(kingdomServiceRules, AIConfigHandler.BuildRuntimeKingdomServicePostprocessRules() ?? new List<PostprocessRuleEntry>());
		}
		List<PostprocessRuleEntry> royalRules = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.RoyalActionRuleId, targetAgentIndex)
			? (AIConfigHandler.BuildRuntimeRoyalPostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>())
			: new List<PostprocessRuleEntry>();
		List<PostprocessRuleEntry> vassalageRules = kingdomVassalageRuleInjected ? (VassalageBehavior.BuildRuntimeVassalagePostprocessRulesForExternal(targetHero, targetCharacter) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> lordsHallRules = lordsHallRuleInjected ? MergePostprocessRulesForScene(AIConfigHandler.GetGuardrailRulePostprocessRules("lords_hall_access"), AIConfigHandler.BuildRuntimeLordsHallAccessPostprocessRules() ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> meetingReleaseRules = meetingReleaseRuleInjected ? MergePostprocessRulesForScene(AIConfigHandler.GetGuardrailRulePostprocessRules("encounter_release_player"), LordEncounterBehavior.BuildMeetingPlayerReleasePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject)) : null;
		List<PostprocessRuleEntry> vanillaIssueRules = vanillaIssueRuleInjected ? (VanillaIssueOfferBridge.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> heroJoinPartyRules = heroJoinPartyRuleInjected ? (AIConfigHandler.BuildRuntimeHeroJoinPartyPostprocessRules(!ShouldSuppressHeroJoinPartyPostprocessForScene(targetHero ?? targetCharacter?.HeroObject), targetHero ?? targetCharacter?.HeroObject, entityPostprocessContext) ?? new List<PostprocessRuleEntry>()) : null;
		if (heroJoinPartyRuleInjected && (heroJoinPartyRules == null || heroJoinPartyRules.Count == 0))
		{
			heroJoinPartyRuleInjected = false;
		}
		List<PostprocessRuleEntry> mechanismRules = sceneMechanismRuleInjected
			? (excludeSceneMoveRule
				? (sceneMechanismRules ?? new List<PostprocessRuleEntry>())
				: MergePostprocessRulesForScene(
					AIConfigHandler.GetGuardrailRulePostprocessRules("scene_mechanism_actions"),
					sceneMechanismRules ?? new List<PostprocessRuleEntry>()))
			: null;
		List<PostprocessRuleEntry> nobleExecutionOrderRules = replyIsDirectPlayerResponse
			? NoblePrisonerExecutionOrderBehavior.BuildRuntimePostprocessRules(
				targetHero ?? targetCharacter?.HeroObject,
				targetAgentIndex)
			: new List<PostprocessRuleEntry>();
		List<PostprocessRuleEntry> partyTransferRules = partyTransferRuleInjected ? (AIConfigHandler.GetGuardrailRulePostprocessRules("party_transfer") ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> voteDealRules = voteDealRuleInjected ? VoteDealBehavior.BuildAgendaVotePostprocessRulesForExternal() : null;
		List<PostprocessRuleEntry> customPolicyAgendaRules = customPolicyAgendaRuleInjected ? TeamModuleServices.Policy.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) : null;
		if (customPolicyAgendaRuleInjected && customPolicyAgendaRules.Count == 0)
		{
			customPolicyAgendaRuleInjected = false;
		}
		List<PostprocessRuleEntry> diplomacyRules = diplomacyRuleInjected ? BuildRuntimeDiplomacyPostprocessRulesForScene(targetHero, targetCharacter) : null;
		List<PostprocessRuleEntry> proposeAgendaRules = null;
		List<PostprocessRuleEntry> worldMapPartyCommandRules = worldMapPartyCommandRuleInjected ? (WorldMapPartyCommandBehavior.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject, targetCharacter, targetAgentIndex) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> nobleGatheringRules = nobleGatheringRuleInjected ? (TeamModuleServices.Gathering.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>()) : null;
		Hero marriageSpeaker = targetHero ?? targetCharacter?.HeroObject;
		List<PostprocessRuleEntry> marriageRuntimeRules = marriageRuleInjected ? (RomanceSystemBehavior.Instance?.BuildRuntimeMarriagePostprocessRulesForExternal(marriageSpeaker) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> marriageRules = marriageRuleInjected ? marriageRuntimeRules : null;
		Settlement siegeSurrenderSettlement = null;
		BattleSideEnum siegeSurrenderSide = BattleSideEnum.None;
		string siegeSurrenderSideLabel = "";
		bool allowEncounterSurrender = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.EncounterSurrenderRuleId, targetAgentIndex);
		bool siegeSurrenderPostprocessEnabled = allowEncounterSurrender
			&& IsNativeConversationPostprocessChain(resolvedChainName)
			&& TryResolveNativeConversationSiegeSurrenderContext(targetHero, targetCharacter, targetAgentIndex, out siegeSurrenderSettlement, out siegeSurrenderSide, out siegeSurrenderSideLabel);
		List<PostprocessRuleEntry> siegeSurrenderRules = BuildSiegeSurrenderPostprocessRulesForNativeConversation(siegeSurrenderPostprocessEnabled, siegeSurrenderSettlement, siegeSurrenderSide);
		bool npcSurrenderPostprocessEnabled = allowEncounterSurrender
			&& !siegeSurrenderPostprocessEnabled
			&& IsNpcSurrenderPostprocessContext();
		List<PostprocessRuleEntry> npcSurrenderRules = BuildNpcSurrenderPostprocessRulesForScene(npcSurrenderPostprocessEnabled);
		bool siegeInterventionPostprocessEnabled = siegeInterventionRuleInjected && AfGcczShoutBridge.ShouldContinuePostprocess(siegeInterventionRuleInjected, preprocessRuleHits);
		List<PostprocessRuleEntry> siegeInterventionRules = TeamModuleServices.Siege.BuildPostprocessRules(
			siegeInterventionPostprocessEnabled,
			targetAgentIndex,
			replyIsDirectPlayerResponse,
			playerText);
		List<PostprocessRuleEntry> relayRules = BuildAutoGroupRelayPostprocessRulesForScene(relayRuleInjected, relaySingleFramedNpc);
		List<PostprocessRuleEntry> intimacyRules = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.IntimacyRuleId, targetAgentIndex)
			? SexualConceptionBehavior.BuildRuntimePostprocessRules(targetHero ?? targetCharacter?.HeroObject, resolvedChainName)
			: new List<PostprocessRuleEntry>();
		bool siegeInterventionExclusive = siegeInterventionPostprocessEnabled && AfGcczShoutBridge.ShouldUseExclusivePostprocessRuleRouting(targetAgentIndex);
		// Global fact-detection rule (no topic): only when this request already
		// runs and the reply has stage prose the local parser cannot resolve.
		IReadOnlyList<string> sceneActionOfferedKeys = offerSceneActionDirective && !siegeInterventionExclusive
			? TryOfferSceneActionDirective(targetAgentIndex, replyText)
			: null;
		List<PostprocessRuleEntry> sceneActionRules = BuildSceneActionDirectiveRules(sceneActionOfferedKeys);
		if (sceneActionRules.Count == 0)
		{
			sceneActionOfferedKeys = null;
		}
		List<PostprocessRuleEntry> mergedRules = siegeInterventionExclusive
			? MergePostprocessRulesForScene(siegeInterventionRules, nobleExecutionOrderRules)
			: MergePostprocessRulesForScene(duelRules, transactionRules, kingdomRules, royalRules, vassalageRules, lordsHallRules, meetingReleaseRules, vanillaIssueRules, heroJoinPartyRules, mechanismRules, nobleExecutionOrderRules, partyTransferRules, voteDealRules, customPolicyAgendaRules, diplomacyRules, worldMapPartyCommandRules, nobleGatheringRules, marriageRules, proposeAgendaRules, siegeSurrenderRules, npcSurrenderRules, siegeInterventionRules, relayRules, intimacyRules, sceneActionRules);
		bool royalPostprocessRuleInjected = (royalRules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), "[ACTION:KING_ABDICATE_TO_PLAYER]", StringComparison.OrdinalIgnoreCase));
		bool vassalagePostprocessRuleInjected = (vassalageRules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => (x?.Tag ?? "").StartsWith("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase));
		int annexationRuleCount = (mergedRules ?? new List<PostprocessRuleEntry>()).Count((PostprocessRuleEntry x) => (x?.Tag ?? "").StartsWith("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase));
		bool kingdomAnnexPostprocessRuleInjected = annexationRuleCount > 0;
		if (kingdomVassalageRuleInjected)
		{
			VassalageDiagnosticLog.Event("postprocess.scene.rules", new Dictionary<string, object>
			{
				["vassalageRuleCount"] = vassalageRules?.Count ?? 0,
				["vassalageTags"] = (vassalageRules ?? new List<PostprocessRuleEntry>()).Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList(),
				["mergedRuleCount"] = mergedRules?.Count ?? 0,
				["mergedTags"] = (mergedRules ?? new List<PostprocessRuleEntry>()).Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList()
			});
		}
		Logger.Log("ShoutBehavior", "[UnifiedPostprocess] requested chain=" + resolvedChainName + " duel=" + duelRuleInjected + " reward=" + rewardRuleInjected + " loan=" + loanRuleInjected + " persistentAdpDebt=" + persistentAdpDebtRuleInjected + " kingdom=" + kingdomServiceRuleInjected + " royalEligible=" + royalPostprocessEligible + " royalRule=" + royalPostprocessRuleInjected + " vassalage=" + kingdomVassalageRuleInjected + " annexation=" + kingdomAnnexPostprocessRuleInjected + " VASSALAGE_rule_injected=" + vassalagePostprocessRuleInjected + " KINGDOM_ANNEX_rule_injected=" + kingdomAnnexPostprocessRuleInjected + " royalRuleCount=" + (royalRules?.Count ?? 0) + " vassalageRuleCount=" + (vassalageRules?.Count ?? 0) + " annexationRuleCount=" + annexationRuleCount + " lordsHall=" + lordsHallRuleInjected + " meetingRelease=" + meetingReleaseRuleInjected + " vanillaIssue=" + vanillaIssueRuleInjected + " heroJoin=" + heroJoinPartyRuleInjected + " sceneMechanism=" + sceneMechanismRuleInjected + " partyTransfer=" + partyTransferRuleInjected + " voteDeal=" + voteDealRuleInjected + " diplomacy=" + diplomacyRuleInjected + " worldMap=" + worldMapPartyCommandRuleInjected + " nobleGathering=" + nobleGatheringRuleInjected + " marriage=" + marriageRuleInjected + " intimacy=" + (intimacyRules?.Count > 0) + " siegeSurrender=" + siegeSurrenderPostprocessEnabled + " siegeSurrenderSide=" + (siegeSurrenderSideLabel ?? "") + " siegeSurrenderSettlement=" + (siegeSurrenderSettlement?.StringId ?? "") + " npcSurrender=" + npcSurrenderPostprocessEnabled + " siegeIntervention=" + siegeInterventionPostprocessEnabled + " relay=" + relayRuleInjected + " siegeSurrenderRule=" + HasSiegeSurrenderPostprocessRule(siegeSurrenderRules) + " mergedHasSiegeSurrender=" + HasSiegeSurrenderPostprocessRule(mergedRules) + " npcSurrenderRule=" + HasNpcSurrenderPostprocessRule(npcSurrenderRules) + " mergedHasNpcSurrender=" + HasNpcSurrenderPostprocessRule(mergedRules) + " preprocessHits=" + ((preprocessRuleHits == null || preprocessRuleHits.Count == 0) ? "(none)" : string.Join(",", preprocessRuleHits)) + " mergedRules=" + ((mergedRules == null || mergedRules.Count == 0) ? "(none; mood postprocess still runs)" : string.Join(",", mergedRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		if (kingdomServiceRuleInjected)
		{
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] kingdom_rules=" + ((kingdomRules == null || kingdomRules.Count == 0) ? "（无）" : string.Join(",", kingdomRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))) + " merged_rules=" + ((mergedRules == null || mergedRules.Count == 0) ? "（无）" : string.Join(",", mergedRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		}
		string text20 = string.IsNullOrWhiteSpace(npcName) ? (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "") : npcName;
		string text2 = NormalizePlayerNameForScenePostprocess(string.IsNullOrWhiteSpace(historyText) ? "（无）" : historyText.Trim(), text20);
		string text3 = BuildPostprocessRuleTextForScene(mergedRules);
		string text4 = BuildPostprocessRuleTextForScene(AIConfigHandler.ActionPostprocessMoodRules);
		string text5 = "（无）";
		string text6 = "（无）";
		string text7 = "（无）";
		string marriagePlayerCandidates = null;
		string marriageTargetCandidates = null;
		string runtimeContext = "（无）";
		List<RewardSystemBehavior.RewardItemInfo> rewardOptions = null;
		List<RewardSystemBehavior.RewardItemInfo> rewardAllOptions = null;
		int rewardAvailableGold = 0;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferTroopOptions = null;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferPrisonerOptions = null;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferAllTroopOptions = null;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null;
		List<MyBehavior.SettlementTransferPromptEntry> settlementTransferNpcOptions = null;
		List<MyBehavior.SettlementTransferPromptEntry> settlementTransferAllNpcOptions = null;
		MentionedWorldEntities promptListMentions = AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal();
		int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
		if (transactionPostprocessEnabled)
		{
			if (RewardSystemBehavior.Instance != null)
			{
				try
				{
					text6 = RewardSystemBehavior.Instance.BuildVisibleEquipmentPostprocessListForAI(Hero.MainHero, promptListMentions, promptListMax);
				}
				catch
				{
					text6 = "赤身裸体";
				}
				try
				{
					if (targetHero != null)
					{
						if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, targetHero, targetCharacter, -1, out rewardAllOptions))
						{
							rewardAllOptions = RewardSystemBehavior.Instance.BuildHeroRewardPostprocessItems(targetHero);
							PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, targetHero, targetCharacter, -1, rewardAllOptions);
						}
					if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope, targetHero, targetCharacter, -1, out rewardOptions))
					{
						rewardOptions = PromptListRetrievalService.FilterNpcRewardItemsForAssetTransfer(rewardAllOptions, promptListMentions, promptListMax);
					}
					if (exposeAllRewardItems)
					{
						rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
					}
						rewardAvailableGold = RewardSystemBehavior.Instance.GetRewardPostprocessGoldForHero(targetHero);
						text5 = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
						text7 = NormalizePlayerNameForScenePostprocess(RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero), text20);
					}
					else if (targetCharacter != null)
					{
						if (TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out var party))
						{
							rewardAllOptions = RewardSystemBehavior.Instance.BuildPartyRewardPostprocessItems(party);
							PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PartyRewardItemsAllSnapshotScope, null, targetCharacter, -1, rewardAllOptions);
						rewardOptions = PromptListRetrievalService.FilterRewardItems(rewardAllOptions, promptListMentions, promptListMax);
						if (exposeAllRewardItems)
						{
							rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
						}
							rewardAvailableGold = RewardSystemBehavior.Instance.GetPartyTradeGoldForExternal(party);
							text5 = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
							text7 = "（非hero野外部队没有个人赊账账本；如果要归还已经实际收到的物品或第纳尔，请只从上面的部队库存与资金中输出 GIVE 标签。）";
						}
						else
						{
							if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, targetCharacter, -1, out rewardAllOptions))
							{
								rewardAllOptions = RewardSystemBehavior.Instance.BuildSettlementMerchantPostprocessItems(targetCharacter);
								PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, targetCharacter, -1, rewardAllOptions);
							}
						if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsSnapshotScope, null, targetCharacter, -1, out rewardOptions))
						{
							rewardOptions = PromptListRetrievalService.FilterRewardItems(rewardAllOptions, promptListMentions, promptListMax);
						}
						if (exposeAllRewardItems)
						{
							rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
						}
							rewardAvailableGold = RewardSystemBehavior.Instance.GetSettlementMarketTradeGold(Settlement.CurrentSettlement);
							text5 = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
							text7 = NormalizePlayerNameForScenePostprocess(RewardSystemBehavior.Instance.BuildSettlementMerchantDebtHintForAI(targetCharacter), text20);
						}
					}
				}
				catch
				{
					text5 = "（无）";
					text7 = "（无）";
					rewardOptions = null;
					rewardAllOptions = null;
				}
			}
		}
		else if (duelRuleInjected)
		{
			text5 = BuildDuelPostprocessItemListForScene(duelStakeOptions);
			try
			{
				if (RewardSystemBehavior.Instance != null && Hero.MainHero != null)
				{
					text6 = RewardSystemBehavior.Instance.BuildVisibleEquipmentPostprocessListForAI(Hero.MainHero, promptListMentions, promptListMax);
				}
			}
			catch
			{
				text6 = "赤身裸体";
			}
		}
		if (partyTransferRuleInjected)
		{
			try
			{
				List<MyBehavior.PartyTransferPromptEntry> list = MyBehavior.BuildPartyTransferPromptEntriesForExternal(targetHero, targetCharacter, targetAgentIndex);
				int num2 = ResolvePartyTransferRecruitMaxTierForScene(targetHero, targetCharacter);
				bool hasTroopSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferTroopOptions);
				bool hasPrisonerSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferPrisonerOptions);
				bool hasAllTroopSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferAllTroopOptions);
				bool hasAllPrisonerSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferAllPrisonerOptions);
				bool wildernessNonHeroPartyTransfer = MyBehavior.IsWildernessNonHeroPartyTransferEligibleForExternal(targetHero, targetCharacter, targetAgentIndex);
				IEnumerable<MyBehavior.PartyTransferPromptEntry> troopOptions = (num2 > 0) ? list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcTroops && Math.Max(0, x.Character?.Tier ?? 0) > 0 && Math.Max(0, x.Character?.Tier ?? 0) <= num2) : Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>();
				IEnumerable<MyBehavior.PartyTransferPromptEntry> volunteerOptions = list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers);
				if (!hasTroopSnapshot || !hasPrisonerSnapshot)
				{
					if (!hasTroopSnapshot)
					{
						partyTransferTroopOptions = BuildDisplayIndexedPartyTransferEntriesForScene(troopOptions.Concat(volunteerOptions));
						partyTransferTroopOptions = PromptListRetrievalService.FilterPartyTransferEntries(partyTransferTroopOptions, promptListMentions, promptListMax, isPrisoner: false);
						PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferTroopOptions);
					}
					if (!hasPrisonerSnapshot)
					{
						partyTransferPrisonerOptions = BuildDisplayIndexedPartyTransferEntriesForScene(list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners));
						partyTransferPrisonerOptions = PromptListRetrievalService.FilterPartyTransferEntries(partyTransferPrisonerOptions, promptListMentions, promptListMax, isPrisoner: true);
						PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferPrisonerOptions);
					}
				}
				if (!hasAllTroopSnapshot)
				{
					IEnumerable<MyBehavior.PartyTransferPromptEntry> allTroopOptions = wildernessNonHeroPartyTransfer
						? troopOptions.Concat(volunteerOptions)
						: list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && (x.Section == MyBehavior.PartyTransferEntrySection.NpcTroops || x.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers));
					partyTransferAllTroopOptions = BuildDisplayIndexedPartyTransferEntriesForScene(allTroopOptions);
					PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferAllTroopOptions);
				}
				if (!hasAllPrisonerSnapshot)
				{
					partyTransferAllPrisonerOptions = BuildDisplayIndexedPartyTransferEntriesForScene(list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners));
					PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferAllPrisonerOptions);
				}
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildPartyTransferNpcPostprocessListForScene(partyTransferTroopOptions, partyTransferPrisonerOptions, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions));
			}
			catch
			{
				partyTransferTroopOptions = null;
				partyTransferPrisonerOptions = null;
				partyTransferAllTroopOptions = null;
				partyTransferAllPrisonerOptions = null;
			}
		}
		if (rewardRuleInjected)
		{
			try
			{
				List<MyBehavior.SettlementTransferPromptEntry> list2 = MyBehavior.BuildSettlementTransferPromptEntriesForExternal(targetHero, targetCharacter);
				if (!PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, out settlementTransferAllNpcOptions))
				{
					settlementTransferAllNpcOptions = BuildDisplayIndexedSettlementTransferEntriesForScene(list2.Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.NpcFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)));
					PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, settlementTransferAllNpcOptions);
				}
				if (!PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, out settlementTransferNpcOptions))
				{
					settlementTransferNpcOptions = BuildDisplayIndexedSettlementTransferEntriesForScene(list2.Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.NpcFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)));
					settlementTransferNpcOptions = PromptListRetrievalService.FilterSettlementTransferEntries(settlementTransferNpcOptions, promptListMentions, promptListMax);
					PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, settlementTransferNpcOptions);
				}
				if (exposeAllFixedAssets)
				{
					settlementTransferNpcOptions = settlementTransferAllNpcOptions;
				}
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSettlementTransferPostprocessListForScene(settlementTransferNpcOptions, settlementTransferAllNpcOptions));
			}
				catch
				{
					settlementTransferNpcOptions = null;
					settlementTransferAllNpcOptions = null;
				}
		}
		if (sceneMechanismRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSceneMechanismTargetListForPostprocess(sceneSummonTargets, sceneGuideTargets));
		}
		if (voteDealRuleInjected || customPolicyAgendaRuleInjected || worldMapPartyCommandRuleInjected || heroJoinPartyRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, entityPostprocessContext);
		}
		if (worldMapPartyCommandRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(targetHero, targetCharacter, targetAgentIndex));
		}
		if (diplomacyRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, DiplomacyBehavior.BuildDiplomacyPostprocessContext(targetHero ?? targetCharacter?.HeroObject));
		}
		if (marriageRuleInjected && RomanceSystemBehavior.Instance != null)
		{
			marriagePlayerCandidates = RomanceSystemBehavior.Instance.BuildMarriagePostprocessPlayerCandidatesBlockForExternal(marriageSpeaker);
			marriageTargetCandidates = RomanceSystemBehavior.Instance.BuildMarriagePostprocessTargetCandidatesBlockForExternal(marriageSpeaker);
		}
		if (nobleGatheringRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, TeamModuleServices.Gathering.BuildPostprocessContextForExternal(targetHero ?? targetCharacter?.HeroObject));
		}
		if (siegeSurrenderPostprocessEnabled)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSiegeSurrenderPostprocessContextForNativeConversation(siegeSurrenderSettlement, siegeSurrenderSideLabel));
		}
		if (siegeInterventionPostprocessEnabled)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, TeamModuleServices.Siege.BuildPostprocessContext(
				siegeInterventionPostprocessEnabled,
				targetAgentIndex,
				replyIsDirectPlayerResponse,
				replyIsDirectPlayerResponse ? playerText : string.Empty));
		}
		if (relayRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSceneRelayTargetListForPostprocess(relayCandidates, targetAgentIndex, relayPrimaryTargetAgentIndex));
		}
		string text8 = AIConfigHandler.BuildActionPostprocessSystemPrompt(text3, text4, text20, text5, text6, text7, marriagePlayerCandidates, marriageTargetCandidates);
		string latestReplyBlock = replyIsDirectPlayerResponse
			? AIConfigHandler.BuildActionPostprocessLatestReplyBlock(playerText, text, text20, text2)
			: AIConfigHandler.BuildActionPostprocessLatestReplyBlock("", text, text20, null);
		string text9 = BuildSceneActionPostprocessUserPrompt(actionPostprocessUserPromptTemplate, text3, text20, text2, latestReplyBlock, text5, text6, text7, marriagePlayerCandidates, marriageTargetCandidates, runtimeContext);
		text9 = AfGcczShoutBridge.AppendTownPostprocessDecisionContract(text9, AfGcczShoutBridge.ShouldUseTownPostprocessDecisionContract(), mergedRules);
		if (NativeConversationDetachedPromptParityLoggingEnabled && detachedMainPromptSections != null)
		{
			try
			{
				LegacyNativePromptParity.LegacyNativePromptParityResult postprocessParity = LegacyNativePromptParity.ComparePostprocessBlocks(text8, text9, 5000, "legacy-native-postprocess");
				DetachedInteractionPromptSections atomicBundle = LegacyNativePromptParity.BuildAtomicBundle(
					detachedMainPromptSections,
					postprocessParity.PostprocessSections);
				Logger.Log("ShoutBehavior", "[NativeDetachedPromptParity] " + postprocessParity.ToDiagnosticString()
					+ " atomicMainSystemSections=" + atomicBundle.Main.SystemSections.Count
					+ " atomicPostSystemSections=" + atomicBundle.Postprocess.SystemSections.Count);
			}
			catch (Exception ex)
			{
				// Keep the old postprocess request as the fail-open fallback.
				Logger.Log("ShoutBehavior", "[NativeDetachedPromptParity] postprocess comparison failed open: " + ex.Message);
			}
		}
		return new SceneActionPostprocessWorkItem(text8, text9, text, content =>
		{
			string text10 = duelRuleInjected ? NormalizeDuelPostprocessTagsForScene(content, duelStakeOptions, targetHero) : "";
			string text11 = transactionPostprocessEnabled ? NormalizeRewardPostprocessTagsForScene(content, rewardOptions, rewardAllOptions, rewardRuleInjected ? settlementTransferNpcOptions : null, rewardRuleInjected, rewardAvailableGold) : "";
			if (persistentAdpDebtRuleInjected && !rewardRuleInjected && !loanRuleInjected)
			{
				text11 = KeepOnlyPersistentAdpDebtTags(text11);
			}
			string text12 = kingdomServiceRuleInjected ? NormalizeKingdomServicePostprocessTagsForScene(content, kingdomRules) : "";
			string royalTags = royalPostprocessRuleInjected ? NormalizeKingdomServicePostprocessTagsForScene(content, royalRules) : "";
			string vassalageTags = kingdomVassalageRuleInjected ? NormalizeVassalagePostprocessTagsForScene(content, vassalageRules) : "";
			string diplomacyTags = (diplomacyRules != null && diplomacyRules.Count > 0) ? NormalizeDiplomacyPostprocessTagsForScene(content, diplomacyRules) : "";
			string annexationTags = diplomacyTags;
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] result chain=" + resolvedChainName
				+ " rawHasVASSALAGE=" + ContainsVassalageActionTagForLog(content)
				+ " normalizedHasVASSALAGE=" + ContainsVassalageActionTagForLog(vassalageTags)
				+ " rawHasKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(content)
				+ " normalizedHasKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(diplomacyTags)
				+ " vassalageTags=" + (string.IsNullOrWhiteSpace(vassalageTags) ? "(none)" : vassalageTags.Replace("\r", "").Replace("\n", "\\n"))
				+ " annexationTags=" + (string.IsNullOrWhiteSpace(annexationTags) ? "(none)" : annexationTags.Replace("\r", "").Replace("\n", "\\n")));
			if (kingdomVassalageRuleInjected)
			{
				VassalageDiagnosticLog.Event("postprocess.scene.result", new Dictionary<string, object>
				{
					["rawContent"] = VassalageDiagnosticLog.Preview(content, 4000),
					["normalizedVassalageTags"] = vassalageTags,
					["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
					["targetCharacterId"] = targetCharacter?.StringId ?? "",
					["targetAgentIndex"] = targetAgentIndex
				});
			}
			string text13 = lordsHallRuleInjected ? NormalizeLordsHallAccessPostprocessTagsForScene(content, lordsHallRules) : "";
			string text14 = meetingReleaseRuleInjected ? NormalizeEncounterReleasePostprocessTagsForScene(content, meetingReleaseRules) : "";
			string text15 = vanillaIssueRuleInjected ? NormalizeVanillaIssuePostprocessTagsForScene(content, vanillaIssueRules) : "";
			string text16 = heroJoinPartyRuleInjected ? NormalizeHeroJoinPartyPostprocessTagsForScene(content, heroJoinPartyRules) : "";
			string text17 = sceneMechanismRuleInjected ? NormalizeSceneMechanismPostprocessTagsForScene(content, mechanismRules, sceneSummonTargets, sceneGuideTargets) : "";
			string nobleExecutionOrderTags = NoblePrisonerExecutionOrderBehavior.NormalizePostprocessTags(content, nobleExecutionOrderRules);
			string text18 = partyTransferRuleInjected ? NormalizePartyTransferPostprocessTagsForScene(content, partyTransferTroopOptions, partyTransferPrisonerOptions, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions) : "";
			string voteDealTags = (voteDealRules != null && voteDealRules.Count > 0) ? NormalizeVoteDealPostprocessTagsForScene(content, voteDealRules) : "";
			string customPolicyAgendaTags = customPolicyAgendaRuleInjected ? NormalizeCustomPolicyAgendaPostprocessTag(content, customPolicyAgendaRules) : "";
			if (customPolicyAgendaRuleInjected || CustomPolicyAgendaActionTagRegex.IsMatch(content ?? ""))
			{
				Logger.Log("ShoutBehavior", "[CustomPolicyAgendaPostprocess] chain=" + resolvedChainName + " RAW_TAG=" + CustomPolicyAgendaActionTagRegex.IsMatch(content ?? "") + " FINAL_TAG=" + !string.IsNullOrWhiteSpace(customPolicyAgendaTags));
			}
			string proposeAgendaTags = "";
			string worldMapPartyCommandTags = worldMapPartyCommandRuleInjected ? NormalizeWorldMapPartyCommandPostprocessTagsForScene(content) : "";
			string nobleGatheringTags = nobleGatheringRuleInjected ? TeamModuleServices.Gathering.NormalizeNobleGatheringPostprocessTagsForExternal(content) : "";
			string marriageTags = (marriageRuleInjected && RomanceSystemBehavior.Instance != null) ? RomanceSystemBehavior.Instance.NormalizeMarriagePostprocessTagsForExternal(content, marriageRules, marriageSpeaker) : "";
			string siegeSurrenderTags = NormalizeSiegeSurrenderPostprocessTagsForScene(content, siegeSurrenderRules, siegeSurrenderPostprocessEnabled);
			string npcSurrenderTags = NormalizeNpcSurrenderPostprocessTagsForScene(content, npcSurrenderRules, npcSurrenderPostprocessEnabled);
			string siegeInterventionTags = siegeInterventionPostprocessEnabled ? TeamModuleServices.Siege.NormalizePostprocessTags(siegeInterventionPostprocessEnabled, content, siegeInterventionRules) : "";
			string relayTags = relayRuleInjected ? NormalizeAutoGroupRelayPostprocessTagsForScene(content, relayCandidates, targetAgentIndex) : "";
			string intimacyTags = SexualConceptionBehavior.NormalizePostprocessTags(content, intimacyRules);
			string sceneActionTags = NormalizeSceneActionDirectiveTag(content, sceneActionOfferedKeys);
			if (sceneActionOfferedKeys != null)
			{
				Logger.Log("ShoutBehavior", "[SceneActionDirective] chain=" + resolvedChainName + " agent=" + targetAgentIndex
					+ " RAW_TAG=" + NpcReplyDirectiveTagV1.ContainsTag(content) + " FINAL_TAG=" + (string.IsNullOrWhiteSpace(sceneActionTags) ? "(none)" : sceneActionTags));
			}
			string text21 = siegeInterventionExclusive
				? MergeNormalizedPostprocessBlocksForScene(siegeInterventionTags, nobleExecutionOrderTags)
				: MergeNormalizedPostprocessBlocksForScene(text10, text11, text12, royalTags, vassalageTags, text13, text14, text15, text16, text17, nobleExecutionOrderTags, text18, voteDealTags, customPolicyAgendaTags, diplomacyTags, worldMapPartyCommandTags, nobleGatheringTags, marriageTags, proposeAgendaTags, siegeSurrenderTags, npcSurrenderTags, siegeInterventionTags, relayTags, intimacyTags, sceneActionTags);
			text21 = AfGcczShoutBridge.ValidateTownPostprocessDecision(text21);
			if (string.IsNullOrWhiteSpace(text21))
			{
				text21 = AIConfigHandler.ActionPostprocessFallbackMoodTag;
			}
			MarkWeeklyMemoryMaterialTriggerForScene(targetHero, targetCharacter, text20, StripAutoGroupRelaySignal(text21), targetAgentIndex, rewardAllOptions ?? rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferAllNpcOptions ?? settlementTransferNpcOptions, partyTransferAllTroopOptions: partyTransferAllTroopOptions, partyTransferAllPrisonerOptions: partyTransferAllPrisonerOptions);
			string text22 = (text + "\n" + text21).Trim();
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] RAW=\n" + content + "\nFINAL=\n" + text22 + "\n");
			return text22;
		});
	}
}