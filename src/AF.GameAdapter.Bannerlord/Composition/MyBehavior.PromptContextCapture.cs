using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
	private void CapturePromptSections(PromptBuildRequest request, PromptRoutingResult routing, PromptRetrievalCapture retrieval, MentionedWorldEntities directPreprocessMentionedEntities, Hero targetHero, CharacterObject targetCharacter, WeeklyPromptSnapshot weeklyPromptSnapshot, Stopwatch promptContextTotalSw, Stopwatch promptContextStageSw,
		out PromptContextFlags contextFlags, out PromptExtrasSections extrasSections, out PromptEntityCapture entityCapture, out MentionedWorldEntities mentionedEntities)
	{
		string input = request.Input;
		string kingdomIdOverride = request.KingdomIdOverride;
		int targetAgentIndex = request.TargetAgentIndex;
		bool hasAnyHero = request.HasAnyHero;
		bool suppressDynamicRuleAndLore = request.SuppressDynamicRuleAndLore;
		bool allowRulePreprocess = request.AllowRulePreprocess;
		string npcLastUtterance = request.NpcLastUtterance;
		bool isQualified = request.IsQualified;
		List<string> auxiliaryRuleHitIds = routing.AuxiliaryRuleHitIds;
		bool flag = routing.Duel.Hit;
		bool flag3 = routing.Reward.Hit;
		bool flag4 = routing.Loan.Hit;
		bool flag5 = routing.Surroundings.Hit;
		bool flag6 = routing.KingdomService.Hit;
		bool marriageHit = routing.Marriage.Hit;
		bool partyTransferHit = routing.PartyTransfer.Hit;
		bool worldMapPartyCommandHit = routing.WorldMapPartyCommand.Hit;
		float score = routing.Duel.Score;
		float score2 = routing.Reward.Score;
		float score3 = routing.Loan.Score;
		float score4 = routing.Surroundings.Score;
		bool persistentAdpDebtPostprocess = false;
		if (allowRulePreprocess && AIConfigHandler.LoanEnabled && !PromptRuleIdPolicy.IsExcluded(request.PreprocessExcludedRuleIds, "loan") && RewardSystemBehavior.Instance != null)
		{
			try
			{
				persistentAdpDebtPostprocess = RewardSystemBehavior.Instance.HasUnpaidDebtForInteraction(targetHero, targetCharacter);
			}
			catch
			{
				persistentAdpDebtPostprocess = false;
			}
		}
		// Pre-duel flags: the duel result is consumed later at its legacy position so the Reward
		// TrustPrompt decision still sees the un-promoted reward flag.
		bool partyTransferEligible = partyTransferHit && IsPartyTransferRuleEligible(targetHero, targetCharacter, targetAgentIndex);
		contextFlags = PromptContextDecisions.ResolveFlags(routing,
			HasDuelRuntimeTarget(targetHero, targetCharacter, targetAgentIndex),
			partyTransferEligible,
			AIConfigHandler.RewardEnabled, AIConfigHandler.LoanEnabled, persistentAdpDebtPostprocess, hasDuelResult: false, playerWonLastDuel: false);
		bool flag2 = contextFlags.UseDuelContext;
		bool flag7 = contextFlags.UseRewardContext;
		bool flag8 = contextFlags.IsLoanContext;
		string value = "";
		if (PromptContextDecisions.ShouldBuildClarificationHint(allowRulePreprocess, contextFlags, flag5))
		{
			value = AIConfigHandler.BuildGuardrailClarificationHint(input, flag, score, flag3, score2, flag4, score3, flag5, score4);
		}
		Logger.Log("Logic", PromptContextDecisions.DescribeSemanticTrigger(routing, npcLastUtterance, input, request.TargetDisplayName));
		Logger.Log("Logic", $"[RuleInjectionDebug] stage=semantic targetHero={(targetHero?.StringId ?? "null")} targetCharacter={(targetCharacter?.StringId ?? "null")} liveDuel={routing.LiveDuelSemanticHit} liveReward={routing.LiveRewardSemanticHit} liveLoan={routing.LiveLoanSemanticHit} auxRuleHits={PromptTopicRoutingStage.DescribeHits(auxiliaryRuleHitIds)} finalDuel={flag} finalReward={flag3} finalLoan={flag4} persistentAdpDebtPostprocess={persistentAdpDebtPostprocess} useDuelContext={flag2} qualified={isQualified} marriageHit={marriageHit} partyTransferHit={partyTransferHit} worldMapHit={worldMapPartyCommandHit}");
		LogShoutPromptContextStage("semantic_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "duel=" + flag + " reward=" + flag3 + " loan=" + flag4 + " worldMap=" + worldMapPartyCommandHit + " partyTransfer=" + partyTransferHit);
		// Mentions were fully resolved in step 2 (routing); step 3 only clones the detached set.
		mentionedEntities = (retrieval?.AuxiliaryMentions ?? directPreprocessMentionedEntities).Clone();
		LogShoutPromptContextStage("mentions_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "hasMentions=" + (mentionedEntities != null && !mentionedEntities.IsEmpty) + " directCount=" + (directPreprocessMentionedEntities.Entities?.Count ?? 0));
		string loreContext = "";
		LogShoutPromptContextStage("lore_start", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "prefetched=" + request.HasPrefetchedLore);
		PromptLoreSource loreSource = PromptContextDecisions.SelectLoreSource(suppressDynamicRuleAndLore, request.UsePrefetchedLoreContext, request.PrefetchedLoreContext, targetHero != null, targetCharacter != null);
		string loreCtxSource = PromptContextDecisions.DescribeLoreSource(loreSource, request.UsePrefetchedLoreContext, request.PrefetchedLoreContext);
		switch (loreSource)
		{
		case PromptLoreSource.Prefetched:
			loreContext = request.PrefetchedLoreContext ?? "";
			break;
		case PromptLoreSource.Hero:
			loreContext = AIConfigHandler.GetLoreContextWithCandidates(input, targetHero, npcLastUtterance, mentionedEntities, retrieval?.LoreCandidates, retrieval?.LoreRuleVersion ?? 0L);
			break;
		case PromptLoreSource.Character:
			loreContext = AIConfigHandler.GetLoreContextWithCandidates(input, targetCharacter, kingdomIdOverride, npcLastUtterance, mentionedEntities, retrieval?.LoreCandidates, retrieval?.LoreRuleVersion ?? 0L);
			break;
		}
		try
		{
			Logger.Log("LoreMatch", $"shout_prompt_lore_ctx source={loreCtxSource} heroId={(targetHero?.StringId ?? "null")} charId={(targetCharacter?.StringId ?? "null")} kingdomIdOverride={(kingdomIdOverride ?? "")}");
		}
		catch
		{
		}
		LogShoutPromptContextStage("lore_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "source=" + loreCtxSource + " loreLen=" + ((loreContext ?? "").Length));
		extrasSections = new PromptExtrasSections();
		var relationshipPlan = PromptContextDecisions.PlanRelationshipCapture(contextFlags,
            RewardSystemBehavior.Instance != null, targetHero != null, targetCharacter != null);
		if (relationshipPlan.CaptureLoan)
		{
				extrasSections.LoanDueDateReference = RewardSystemBehavior.Instance.BuildDueDateReferenceForAI();
				extrasSections.LoanDebtHint = RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero);
		}
        if (relationshipPlan.CaptureTrust)
        {
				extrasSections.TrustPrompt = RewardSystemBehavior.Instance.BuildTrustPromptForAI(targetHero);
		}
		if (relationshipPlan.CaptureMerchantDebt)
		{
			extrasSections.SettlementMerchantDebtHint = RewardSystemBehavior.Instance.BuildSettlementMerchantDebtHintForAI(targetCharacter);
		}
		bool playerWon = false;
		bool hasDuelResult = targetHero != null && DuelBehavior.TryConsumeLastDuelResult(targetHero, out playerWon);
		if (hasDuelResult)
		{
			contextFlags = PromptContextDecisions.ResolveFlags(routing, flag2, partyTransferEligible,
				AIConfigHandler.RewardEnabled, AIConfigHandler.LoanEnabled, persistentAdpDebtPostprocess, hasDuelResult: true, playerWonLastDuel: playerWon);
			flag7 = contextFlags.UseRewardContext;
			extrasSections.DuelResultLine = PromptExtrasComposer.BuildDuelResultLine(playerWon, BuildPlayerPublicDisplayNameForPrompt(targetHero));
		}
		bool includeDuelStakeContext = contextFlags.IncludeDuelStakeContext;
		bool playerWonLastDuelForRule = contextFlags.PlayerWonLastDuel;
		if (targetHero != null && !string.IsNullOrEmpty(targetHero.StringId))
		{
			string playerDisplayName2 = BuildPlayerPublicDisplayNameForPrompt(targetHero);
			if (_recentlyDefeatedByPlayer.Contains(targetHero.StringId))
			{
				extrasSections.VanillaBattleDefeatLine = PromptExtrasComposer.BuildVanillaBattleDefeatLine(playerDisplayName2);
			}
			if (_recentlyReleasedPrisoners.Contains(targetHero.StringId))
			{
				extrasSections.ReleasedPrisonerLine = PromptExtrasComposer.BuildReleasedPrisonerLine(playerDisplayName2);
			}
			extrasSections.ActivePrisonerStatusLine = BuildHeroPrisonerStatusPromptLineForExternal(targetHero);
		}
		LogShoutPromptContextStage("relationship_blocks_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex);
		extrasSections.FeastAttendanceContext = TeamModuleServices.Gathering.BuildFeastAttendanceContext(targetHero);
		extrasSections.ClarificationHint = value;
		extrasSections.HeroArmyRuntimeFact = BuildHeroArmyRuntimeFactForPrompt(targetHero);
		extrasSections.PlayerArmyRuntimeFact = BuildPlayerArmyRuntimeFactForPrompt(targetHero, targetCharacter, targetAgentIndex);
		extrasSections.ResidentRecentActions = BuildResidentRecentActionsPrompt(targetHero, targetCharacter, targetAgentIndex);
		if (flag5)
		{
			bool flag9 = false;
			CampaignVec2 pos;
			try
			{
				if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
				{
					flag9 = LordEncounterBehavior.TryGetSavedMainPartyPosition(out pos);
				}
				else
				{
					pos = MobileParty.MainParty.Position;
					flag9 = pos.IsValid();
				}
			}
			catch
			{
				flag9 = false;
				pos = default(CampaignVec2);
			}
			if (flag9)
			{
				extrasSections.NearbySettlementsDetail = ShoutUtils.BuildNearbySettlementsDetailForPrompt(pos, targetHero);
			}
		}
		LogShoutPromptContextStage("world_runtime_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex);
		LogShoutPromptContextStage("triggered_rules_start", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "suppressDynamic=" + suppressDynamicRuleAndLore);
		string value8 = allowRulePreprocess ? BuildTriggeredRuleInstructions(input, targetHero, flag2, isQualified, request.PlayerClanTier, flag7, flag8, flag5, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, npcLastUtterance, includeDuelStakeContext, playerWonLastDuelForRule, worldMapPartyCommandHit, request.ExcludedRuleIds, auxiliaryRuleHitIds, PromptRuleIdPolicy.IsExcluded(request.ExplicitExcludedRuleIds, "meeting_taunt"), retrieval?.FallbackExtraRuleHits) : "";
		LogShoutPromptContextStage("triggered_rules_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "ruleLen=" + ((value8 ?? "").Length));
		LogShoutPromptContextStage("weekly_short_start", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "", immediate: false);
		// Bulletin mode: build the fact snapshot once here so the short, exclusion and full layers share it.
		if (weeklyPromptSnapshot == null && IsWorldBulletinPublishingEnabled() && TWParallel.IsMainThread())
		{
			weeklyPromptSnapshot = CaptureWorldBulletinNpcSnapshot(targetHero, targetCharacter, kingdomIdOverride);
		}
		bool excludeNpcShortReport2 = ShouldExcludeNpcShortReportFromWeeklyShortLayer(value8, targetHero, targetCharacter, kingdomIdOverride, weeklyPromptSnapshot);
		FreezeWatchdog.Mark("ShoutPromptContext.weekly_short_exclusion_done", "excludeNpcKingdom=" + excludeNpcShortReport2 + " thread=" + Thread.CurrentThread.ManagedThreadId);
		extrasSections.WeeklyShortReports = BuildWeeklyShortReportsPromptBlock(targetHero, targetCharacter, kingdomIdOverride, excludeNpcShortReport2, weeklyPromptSnapshot);
		LogShoutPromptContextStage("weekly_short_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "shortLen=" + ((extrasSections.WeeklyShortReports ?? "").Length));
		LogShoutPromptContextStage("policy_context_start", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "", immediate: false);
		extrasSections.ActivePolicyContext = AfGcczShoutBridge.IsActive()
			? string.Empty
			: TeamModuleServices.Policy.BuildActivePolicyDialogueContextForExternal(targetHero, targetCharacter, kingdomIdOverride);
		LogShoutPromptContextStage("policy_context_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "policyLen=" + ((extrasSections.ActivePolicyContext ?? "").Length));
		extrasSections.TriggeredRuleInstructions = value8;
		LogShoutPromptContextStage("weekly_full_start", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "", immediate: false);
		extrasSections.WeeklyFullReports = BuildTriggeredWeeklyFullReportsPromptBlock(value8, targetHero, targetCharacter, kingdomIdOverride, weeklyPromptSnapshot);
		extrasSections.LoreContext = loreContext;
		LogShoutPromptContextStage("weekly_full_lore_append_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "fullLen=" + ((extrasSections.WeeklyFullReports ?? "").Length));
		entityCapture = null;
		if (!suppressDynamicRuleAndLore)
		{
			bool includeResidentKingdomEntities = PromptRuleIdPolicy.ShouldIncludeResidentKingdomEntities(flag6, auxiliaryRuleHitIds);
			Hero entityContextHero = targetHero ?? targetCharacter?.HeroObject;
			bool includeResidentPlayerEntities = DoesPlayerNotorietyObserverKnowPlayer(targetHero, targetCharacter, targetAgentIndex);
			HashSet<string> entityRetrievalRuleIds = PromptExtrasComposer.BuildEntityRetrievalRuleIds(auxiliaryRuleHitIds, flag7, flag8, partyTransferHit, worldMapPartyCommandHit);
			LogShoutPromptContextStage("entity_context_start", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "rules=" + string.Join(",", entityRetrievalRuleIds));
			WorldEntityPromptContext entityPromptContext = WorldEntityRetrievalService.BuildPromptContext(mentionedEntities, BuildPlayerPublicDisplayNameForPrompt(entityContextHero, targetCharacter, targetAgentIndex), entityContextHero, includeResidentKingdomEntities, entityRetrievalRuleIds, input, includeResidentPlayerEntities, retrieval?.EntityCapture, retrieval?.EntityMatches);
			entityCapture = new PromptEntityCapture
			{
				MainPromptBlock = entityPromptContext?.MainPromptBlock,
				PostprocessPromptBlock = entityPromptContext?.PostprocessPromptBlock,
				ExplicitMentionedKingdomIds = entityPromptContext?.ExplicitMentionedKingdomIds,
				HasContent = entityPromptContext != null && entityPromptContext.HasContent
			};
			if (entityCapture.HasContent)
			{
				Logger.Log("WorldEntityRetrieval", "entity_context matches=" + entityPromptContext.MatchCount + " residentKingdoms=" + includeResidentKingdomEntities + " residentPlayerEntities=" + includeResidentPlayerEntities + " mainLen=" + ((entityPromptContext.MainPromptBlock ?? "").Length) + " postLen=" + ((entityPromptContext.PostprocessPromptBlock ?? "").Length));
			}
			if (entityRetrievalRuleIds.Contains("kingdom_agenda"))
			{
				WorldEntityPromptContext agendaPromptContext = VoteDealBehavior.BuildUnifiedAgendaPromptContextForExternal(entityContextHero, mentionedEntities);
				entityCapture.AgendaHasContent = agendaPromptContext != null && agendaPromptContext.HasContent;
				entityCapture.AgendaMainPromptBlock = agendaPromptContext?.MainPromptBlock;
				entityCapture.AgendaPostprocessPromptBlock = agendaPromptContext?.PostprocessPromptBlock;
			}
			LogShoutPromptContextStage("entity_context_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "hasContent=" + entityCapture.HasContent);
		}
		bool includeMarriageCandidates = PromptContextDecisions.ShouldCaptureMarriageContext(targetHero != null, marriageHit);
		RomanceSystemBehavior.SetMarriagePostprocessContextEnabled(targetHero, includeMarriageCandidates);
		contextFlags.UseRewardContext = flag7;
		contextFlags.IsLoanContext = flag8;
		contextFlags.UseDuelContext = flag2;
	}
}
