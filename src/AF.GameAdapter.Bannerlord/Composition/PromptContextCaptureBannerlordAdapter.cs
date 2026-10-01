using System;
using System.Collections.Generic;
using System.Threading;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using WeeklyPromptSnapshot = AnimusForge.MyBehavior.WeeklyPromptSnapshot;

namespace AnimusForge;

// Full game-thread capture/effect sequence. Detached routing/retrieval and formatting remain in Prompt owners.
internal static class PromptContextCaptureBannerlordAdapter
{
	internal static void CapturePromptSections(PromptContextCaptureBannerlordPorts ports, PromptBuildRequest request, PromptRoutingResult routing, PromptRetrievalCapture retrieval, MentionedWorldEntities directPreprocessMentionedEntities, Hero targetHero, CharacterObject targetCharacter, WeeklyPromptSnapshot weeklyPromptSnapshot, out PromptContextFlags contextFlags, out PromptExtrasSections extrasSections, out PromptEntityCapture entityCapture, out MentionedWorldEntities mentionedEntities)
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
		bool partyTransferEligible = partyTransferHit && ports.IsPartyTransferEligible();
		contextFlags = PromptContextDecisions.ResolveFlags(routing,
			ports.HasDuelRuntimeTarget(),
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
		ports.LogStage("semantic_done", "duel=" + flag + " reward=" + flag3 + " loan=" + flag4 + " worldMap=" + worldMapPartyCommandHit + " partyTransfer=" + partyTransferHit, true);
		// Mentions were fully resolved in step 2 (routing); step 3 only clones the detached set.
		mentionedEntities = (retrieval?.AuxiliaryMentions ?? directPreprocessMentionedEntities).Clone();
		ports.LogStage("mentions_done", "hasMentions=" + (mentionedEntities != null && !mentionedEntities.IsEmpty) + " directCount=" + (directPreprocessMentionedEntities.Entities?.Count ?? 0), true);
		string loreContext = "";
		ports.LogStage("lore_start", "prefetched=" + request.HasPrefetchedLore, true);
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
		ports.LogStage("lore_done", "source=" + loreCtxSource + " loreLen=" + ((loreContext ?? "").Length), true);
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
			extrasSections.DuelResultLine = PromptExtrasComposer.BuildDuelResultLine(playerWon, ports.BuildPlayerDisplayName(targetHero, null, -1));
		}
		bool includeDuelStakeContext = contextFlags.IncludeDuelStakeContext;
		bool playerWonLastDuelForRule = contextFlags.PlayerWonLastDuel;
		if (targetHero != null && !string.IsNullOrEmpty(targetHero.StringId))
		{
			string playerDisplayName2 = ports.BuildPlayerDisplayName(targetHero, null, -1);
			if (ports.WasRecentlyDefeated(targetHero.StringId))
			{
				extrasSections.VanillaBattleDefeatLine = PromptExtrasComposer.BuildVanillaBattleDefeatLine(playerDisplayName2);
			}
			if (ports.WasRecentlyReleased(targetHero.StringId))
			{
				extrasSections.ReleasedPrisonerLine = PromptExtrasComposer.BuildReleasedPrisonerLine(playerDisplayName2);
			}
			extrasSections.ActivePrisonerStatusLine = ports.BuildPrisonerStatus(targetHero);
		}
		ports.LogStage("relationship_blocks_done", null, true);
		extrasSections.FeastAttendanceContext = TeamModuleServices.Gathering.BuildFeastAttendanceContext(targetHero);
		extrasSections.ClarificationHint = value;
		extrasSections.HeroArmyRuntimeFact = ports.BuildHeroArmyFact();
		extrasSections.PlayerArmyRuntimeFact = ports.BuildPlayerArmyFact();
		extrasSections.ResidentRecentActions = ports.BuildResidentRecentActions();
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
		ports.LogStage("world_runtime_done", null, true);
		ports.LogStage("triggered_rules_start", "suppressDynamic=" + suppressDynamicRuleAndLore, true);
		string value8 = allowRulePreprocess ? ports.BuildTriggeredRules(contextFlags) : "";
		ports.LogStage("triggered_rules_done", "ruleLen=" + ((value8 ?? "").Length), true);
		ports.LogStage("weekly_short_start", "", false);
		// Bulletin mode: build the fact snapshot once here so the short, exclusion and full layers share it.
		if (weeklyPromptSnapshot == null && ports.IsWorldBulletinEnabled() && TWParallel.IsMainThread())
		{
			weeklyPromptSnapshot = ports.CaptureWorldBulletinSnapshot();
		}
		bool excludeNpcShortReport2 = ports.ShouldExcludeWeeklyShortReport(value8, weeklyPromptSnapshot);
		FreezeWatchdog.Mark("ShoutPromptContext.weekly_short_exclusion_done", "excludeNpcKingdom=" + excludeNpcShortReport2 + " thread=" + Thread.CurrentThread.ManagedThreadId);
		extrasSections.WeeklyShortReports = ports.BuildWeeklyShortReports(excludeNpcShortReport2, weeklyPromptSnapshot);
		ports.LogStage("weekly_short_done", "shortLen=" + ((extrasSections.WeeklyShortReports ?? "").Length), true);
		ports.LogStage("policy_context_start", "", false);
		extrasSections.ActivePolicyContext = AfGcczShoutBridge.IsActive()
			? string.Empty
			: TeamModuleServices.Policy.BuildActivePolicyDialogueContextForExternal(targetHero, targetCharacter, kingdomIdOverride);
		ports.LogStage("policy_context_done", "policyLen=" + ((extrasSections.ActivePolicyContext ?? "").Length), true);
		extrasSections.TriggeredRuleInstructions = value8;
		ports.LogStage("weekly_full_start", "", false);
		extrasSections.WeeklyFullReports = ports.BuildWeeklyFullReports(value8, weeklyPromptSnapshot);
		extrasSections.LoreContext = loreContext;
		ports.LogStage("weekly_full_lore_append_done", "fullLen=" + ((extrasSections.WeeklyFullReports ?? "").Length), true);
		entityCapture = null;
		if (!suppressDynamicRuleAndLore)
		{
			bool includeResidentKingdomEntities = PromptRuleIdPolicy.ShouldIncludeResidentKingdomEntities(flag6, auxiliaryRuleHitIds);
			Hero entityContextHero = targetHero ?? targetCharacter?.HeroObject;
			bool includeResidentPlayerEntities = ports.ObserverKnowsPlayer();
			HashSet<string> entityRetrievalRuleIds = PromptExtrasComposer.BuildEntityRetrievalRuleIds(auxiliaryRuleHitIds, flag7, flag8, partyTransferHit, worldMapPartyCommandHit);
			ports.LogStage("entity_context_start", "rules=" + string.Join(",", entityRetrievalRuleIds), true);
			WorldEntityPromptContext entityPromptContext = WorldEntityRetrievalService.BuildPromptContext(mentionedEntities, ports.BuildPlayerDisplayName(entityContextHero, targetCharacter, targetAgentIndex), entityContextHero, includeResidentKingdomEntities, entityRetrievalRuleIds, input, includeResidentPlayerEntities, retrieval?.EntityCapture, retrieval?.EntityMatches);
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
			ports.LogStage("entity_context_done", "hasContent=" + entityCapture.HasContent, true);
		}
		bool includeMarriageCandidates = PromptContextDecisions.ShouldCaptureMarriageContext(targetHero != null, marriageHit);
		RomanceSystemBehavior.SetMarriagePostprocessContextEnabled(targetHero, includeMarriageCandidates);
		contextFlags.UseRewardContext = flag7;
		contextFlags.IsLoanContext = flag8;
		contextFlags.UseDuelContext = flag2;
	}
}

// Request-local live leaf capabilities; no host reference or whole-capture callback.
internal sealed class PromptContextCaptureBannerlordPorts
{
    internal Func<bool> IsPartyTransferEligible, HasDuelRuntimeTarget, IsWorldBulletinEnabled, ObserverKnowsPlayer;
    internal Func<string, bool> WasRecentlyDefeated, WasRecentlyReleased;
    internal Func<Hero, CharacterObject, int, string> BuildPlayerDisplayName;
    internal Func<Hero, string> BuildPrisonerStatus;
    internal Func<string> BuildHeroArmyFact, BuildPlayerArmyFact, BuildResidentRecentActions;
    internal Func<PromptContextFlags, string> BuildTriggeredRules;
    internal Func<WeeklyPromptSnapshot> CaptureWorldBulletinSnapshot;
    internal Func<string, WeeklyPromptSnapshot, bool> ShouldExcludeWeeklyShortReport;
    internal Func<bool, WeeklyPromptSnapshot, string> BuildWeeklyShortReports;
    internal Func<string, WeeklyPromptSnapshot, string> BuildWeeklyFullReports;
    internal Action<string, string, bool> LogStage;
}
