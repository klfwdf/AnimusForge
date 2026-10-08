using System.Text;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using System;
using TaleWorlds.Library;
using HistoryPromptSnapshot = AnimusForge.MyBehavior.HistoryPromptSnapshot;
using DialogueDay = AnimusForge.MyBehavior.DialogueDay;
using ShoutPromptContext = AnimusForge.MyBehavior.ShoutPromptContext;
using WeeklyPromptSnapshot = AnimusForge.MyBehavior.WeeklyPromptSnapshot;
using System.Diagnostics;
using System.Threading;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using SceneSummonPromptTarget = AnimusForge.ShoutBehavior.SceneSummonPromptTarget;
using SceneGuidePromptTarget = AnimusForge.ShoutBehavior.SceneGuidePromptTarget;
namespace AnimusForge.Refactor.Adapters;
internal sealed class SharedPromptCaptureBannerlordAdapter
{
    internal sealed class ExternalPromptBuildPorts
    {
        internal readonly RequestCapturePorts Capture;
        internal readonly Func<PromptBuildPhases,SharedPromptRoutingWork> Routing;
        internal readonly Func<PromptBuildPhases,Hero,CharacterObject,PromptContextCaptureBannerlordPorts> Context;
        internal ExternalPromptBuildPorts(RequestCapturePorts capture,Func<PromptBuildPhases,SharedPromptRoutingWork> routing,Func<PromptBuildPhases,Hero,CharacterObject,PromptContextCaptureBannerlordPorts> context)
        { Capture=capture??throw new ArgumentNullException(nameof(capture));Routing=routing??throw new ArgumentNullException(nameof(routing));Context=context??throw new ArgumentNullException(nameof(context)); }
    }
internal static ShoutPromptContext BuildShoutPromptContextForExternalInternal(ExternalPromptBuildPorts ports, Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, bool suppressDynamicRuleAndLore = false, bool usePrefetchedLoreContext = false, string prefetchedLoreContext = null, IEnumerable<string> excludedRuleIds = null, IEnumerable<string> preprocessExcludedRuleIds = null, IEnumerable<string> forcedPreprocessRuleIds = null, MentionedWorldEntities preprocessMentionedEntities = null, WeeklyPromptSnapshot weeklyPromptSnapshot = null)
	{
		PromptBuildPhases phases = BeginSharedPromptBuild(ports.Capture, targetHero, input, extraFact, cultureIdOverride, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, suppressDynamicRuleAndLore, usePrefetchedLoreContext, prefetchedLoreContext, excludedRuleIds, preprocessExcludedRuleIds, forcedPreprocessRuleIds, preprocessMentionedEntities);
		if (phases == null)
		{
			return CreateEmptyShoutPromptContext();
		}
		using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
		AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
		try
		{
			SharedPromptRoutingRuntime.Run(ports.Routing(phases));
			CaptureSharedKnowledgeSnapshot(phases, targetHero ?? targetCharacter?.HeroObject);
			RunSharedKnowledgeRetrieval(phases);
			return CompleteSharedPromptBuild(ports.Context(phases, targetHero, targetCharacter), phases, targetHero, targetCharacter, weeklyPromptSnapshot);
		}
		finally
		{
			AIConfigHandler.ClearGuardrailRuntimeTarget();
		}
	}

    private readonly SceneAgentIdentityPromptCaptureAdapter _identity;
    private readonly Func<List<NpcDataPacket>, Dictionary<int, Hero>, List<SceneSummonPromptTarget>> _summon;
    private readonly Func<Agent, int, List<SceneGuidePromptTarget>> _guide;
    private readonly Func<NpcDataPacket,List<SceneSummonPromptTarget>,List<SceneGuidePromptTarget>,List<PostprocessRuleEntry>> _mechanismRules;
    internal SharedPromptCaptureBannerlordAdapter(SceneAgentIdentityPromptCaptureAdapter identity,
        Func<List<NpcDataPacket>,Dictionary<int,Hero>,List<SceneSummonPromptTarget>> summon,
        Func<Agent,int,List<SceneGuidePromptTarget>> guide,
        Func<NpcDataPacket,List<SceneSummonPromptTarget>,List<SceneGuidePromptTarget>,List<PostprocessRuleEntry>> mechanismRules)
    {
        _identity=identity ?? throw new ArgumentNullException(nameof(identity));
        _summon=summon ?? throw new ArgumentNullException(nameof(summon));
        _guide=guide ?? throw new ArgumentNullException(nameof(guide));
        _mechanismRules=mechanismRules ?? throw new ArgumentNullException(nameof(mechanismRules));
    }
    internal SceneActionPostprocessWorkItem CaptureNativeDetachedPostprocessWork(
        Hero hero, CharacterObject character, int agentIndex, InteractionEnvelope envelope, RuleSelection selection, string reply)
    {
        var hits = (selection?.RuleIds ?? Array.Empty<string>()).ToList();
        bool Hit(string id) => ConversationActionPostprocessOwner.HasPreprocessRuleHit(hits, id);
        var npc = _identity.BuildNativeConversationNpcData(hero, character);
        npc.AgentIndex = agentIndex;
        var targets = new List<NpcDataPacket> { npc };
        var heroes = new Dictionary<int, Hero>();
        if (agentIndex >= 0 && hero != null) heroes[agentIndex] = hero;
        var summon = agentIndex >= 0 ? _summon(targets, heroes) : null;
        int guideStart = (summon != null && summon.Count > 0 ? summon.Max(item => item?.PromptId ?? 0) : 0) + 1;
        Agent agent = agentIndex >= 0 ? Mission.Current?.Agents?.FirstOrDefault(item => item != null && item.Index == agentIndex) : null;
        var guide = agentIndex >= 0 ? _guide(agent, guideStart) : null;
        bool mechanism = Hit("scene_mechanism_actions") && ConversationActionPostprocessOwner.CanUseSceneMechanismPostprocessForSpeaker(agentIndex);
        var mechanismRules = mechanism ? _mechanismRules(npc, summon, guide) : null;
        var stakes = Hit("duel") && hero != null ? RewardSystemBehavior.Instance?.BuildDuelStakeOptionsForAI(hero) : null;
        string history = string.Join("\n", envelope.History.Select(message => message.Role + ": " + message.Content));
        return ConversationActionPostprocessOwner.PrepareSceneUnifiedActionPostprocess(
            hero, character, agentIndex, SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(npc),
            envelope.Snapshot.PlayerText, history, reply,
            duelRuleInjected: Hit("duel"), rewardRuleInjected: Hit("reward"), loanRuleInjected: Hit("loan"),
            kingdomServiceRuleInjected: Hit("kingdom_service"), kingdomVassalageRuleInjected: Hit("kingdom_vassalage"),
            kingdomAnnexationRuleInjected: false, lordsHallRuleInjected: Hit("lords_hall_access"),
            meetingReleaseRuleInjected: Hit("encounter_release_player"), vanillaIssueRuleInjected: Hit("vanilla_issue"),
            heroJoinPartyRuleInjected: Hit("kingdom_service"), sceneMechanismRuleInjected: mechanism,
            partyTransferRuleInjected: Hit("party_transfer"), voteDealRuleInjected: Hit("kingdom_agenda"),
            diplomacyRuleInjected: Hit("diplomacy"), worldMapPartyCommandRuleInjected: Hit("worldmap_party_command"),
            marriageRuleInjected: Hit("marriage"), duelStakeOptions: stakes, kingdomServiceRules: null,
            sceneMechanismRules: mechanismRules, sceneSummonTargets: CapturePostprocessSummonTargets(summon), sceneGuideTargets: CapturePostprocessGuideTargets(guide),
            siegeInterventionRuleInjected: AfGcczShoutBridge.ShouldRunPostprocessForActiveScene(),
            replyIsDirectPlayerResponse: !string.IsNullOrWhiteSpace(envelope.Snapshot.PlayerText),
            preprocessRuleHits: hits, chainName: "native_conversation", detachedMainPromptSections: envelope.PromptSections,
            offerSceneActionDirective: true);
    }
	internal static List<PostprocessSummonTarget> CapturePostprocessSummonTargets(IEnumerable<SceneSummonPromptTarget> targets)
	{
		ConversationActionPostprocessOwner.RequireMainThread();
		return targets?.Where(x=>x!=null).Select(x=>new PostprocessSummonTarget { PromptId=x.PromptId,DisplayName=x.DisplayName,LocationCode=x.LocationCode,HasLocationCharacter=x.LocationCharacter!=null }).ToList();
	}
	internal static List<PostprocessGuideTarget> CapturePostprocessGuideTargets(IEnumerable<SceneGuidePromptTarget> targets)
	{
		ConversationActionPostprocessOwner.RequireMainThread();
		return targets?.Where(x=>x!=null).Select(x=>new PostprocessGuideTarget { PromptId=x.PromptId,DisplayName=x.DisplayName,LocationCode=x.LocationCode,HasLocationCharacter=x.LocationCharacter!=null }).ToList();
	}
	internal static void CaptureSharedKnowledgeSnapshot(PromptBuildPhases phases, Hero targetHero)
	{
		if (phases?.Retrieval == null || phases.Request.SuppressDynamicRuleAndLore)
		{
			return;
		}
		if (!phases.Request.HasPrefetchedLore)
		{
			phases.Retrieval.LoreSettings = KnowledgeLibraryBehavior.CapturePromptLoreSettings();
			phases.Retrieval.LoreRuleVersion = KnowledgeLibraryBehavior.PreparePromptLoreRetrieval(phases.Retrieval.AuxiliaryMentions);
		}
		try
		{
			phases.Retrieval.EntityCapture = WorldEntityRetrievalService.CaptureEntityCandidates(phases.Retrieval.AuxiliaryMentions, phases.Request.Input, targetHero);
			phases.Retrieval.EntityCandidates = phases.Retrieval.EntityCapture.Candidates;
			phases.Retrieval.EntityMaxInjectedEntities = phases.Retrieval.EntityCapture.MaxInjectedEntities;
		}
		catch (Exception ex)
		{
			phases.Retrieval.EntityCapture = null;
			phases.Retrieval.EntityCandidates = null;
			Logger.Log("WorldEntityRetrieval", "candidate_capture_failed: " + ex.Message);
		}
	}
	internal static PromptKnowledgeWorkInput CreateSharedKnowledgeWorkInput(PromptBuildPhases phases)
	{
		if (phases?.Retrieval == null || phases.Request.SuppressDynamicRuleAndLore)
		{
			return null;
		}
		bool needsFallbackExtraRules = phases.Request.AllowRulePreprocess && phases.Routing?.AuxiliaryRuleHitIds == null;
		return new PromptKnowledgeWorkInput
		{
			Mentions = phases.Retrieval.AuxiliaryMentions?.Clone(),
			Target = phases.Request.Target,
			Eligibility = phases.Request.Eligibility,
			LoreRuleVersion = phases.Retrieval.LoreRuleVersion,
			LoreSettings = phases.Retrieval.LoreSettings,
			HasPrefetchedLore = phases.Request.HasPrefetchedLore,
			EntityCandidates = phases.Retrieval.EntityCandidates,
			EntityMaxInjectedEntities = phases.Retrieval.EntityMaxInjectedEntities,
			Input = phases.Request.Input,
			NpcLastUtterance = phases.Request.NpcLastUtterance,
			NeedsFallbackExtraRules = needsFallbackExtraRules,
			ExtraRuleReturnCap = needsFallbackExtraRules ? AIConfigHandler.GuardrailRuleReturnCap : 0,
			ExcludedRuleIds = phases.Request.ExcludedRuleIds == null ? null : new HashSet<string>(phases.Request.ExcludedRuleIds, StringComparer.OrdinalIgnoreCase),
			GuardrailStickyTargetKey = phases.Request.GuardrailStickyTargetKey
		};
	}
	internal static PromptKnowledgeWorkResult RunSharedKnowledgeRetrieval(PromptKnowledgeWorkInput input)
	{
		PromptKnowledgeWorkResult result = new PromptKnowledgeWorkResult();
		if (input == null) return result;
		using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
		AIConfigHandler.ApplyGuardrailRuntimeTarget(input.Target, input.Eligibility);
		try
		{
		if (!input.HasPrefetchedLore)
		{
			result.LoreCandidates = KnowledgeLibraryBehavior.CollectPromptLoreCandidates(input.Mentions, input.LoreRuleVersion, input.LoreSettings);
		}
		if (input.EntityCandidates != null)
		{
			try
			{
				result.EntityMatches = WorldEntityRetrievalService.MatchDetachedCandidates(
					input.EntityCandidates, input.Mentions, input.Input, input.EntityMaxInjectedEntities);
			}
			catch (Exception ex)
			{
				Logger.Log("WorldEntityRetrieval", "candidate_match_failed: " + ex.Message);
			}
		}
		if (input.NeedsFallbackExtraRules)
		{
			result.FallbackExtraRuleHits = AIConfigHandler.GetMatchedExtraRuleHitsForWorker(
				input.Input, input.NpcLastUtterance, input.ExtraRuleReturnCap,
				input.ExcludedRuleIds, input.GuardrailStickyTargetKey);
		}
		return result;
		}
		finally
		{
			AIConfigHandler.ClearGuardrailRuntimeTarget();
		}
	}
	internal static void ApplySharedKnowledgeRetrieval(PromptBuildPhases phases, PromptKnowledgeWorkResult result)
	{
		if (phases?.Retrieval == null || result == null) return;
		phases.Retrieval.LoreCandidates = result.LoreCandidates;
		phases.Retrieval.EntityMatches = result.EntityMatches;
		phases.Retrieval.FallbackExtraRuleHits = result.FallbackExtraRuleHits;
	}
	internal static void RunSharedKnowledgeRetrieval(PromptBuildPhases phases)
	{
		ApplySharedKnowledgeRetrieval(phases, RunSharedKnowledgeRetrieval(CreateSharedKnowledgeWorkInput(phases)));
	}

    private const double ShoutPromptContextSlowStageMs = 1000.0;
    private const double ShoutPromptContextHardBudgetMs = 15000.0;
	internal static PromptRuntimeTargetBinding CreatePromptRuntimeTargetBinding(string kingdomId, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		return PromptRuntimeTargetBinding.Create(kingdomId, targetHero?.StringId, targetCharacter?.StringId, targetCharacter?.HeroObject?.StringId, targetHero != null, targetCharacter != null && targetCharacter.IsSoldier, targetAgentIndex);
	}
	internal static string ResolveBuiltInRuleStickyTargetKey(Hero targetHero, CharacterObject targetCharacter)
	{
		return BuiltInRuleStickyCarry.ResolveTargetKey(targetHero?.StringId, targetCharacter?.StringId, targetCharacter?.HeroObject?.StringId);
	}
	internal static ShoutPromptContext CreateEmptyShoutPromptContext()
	{
		return new ShoutPromptContext
		{
			Extras = "",
			EntityPostprocessContext = "",
			PreprocessRuleIds = new List<string>(),
			PreprocessExcludedRuleIds = new List<string>(),
			PreprocessExcludedRuleBlock = "",
			UseDuelContext = false,
			UseRewardContext = false,
			IsLoanContext = false,
			IsQualified = true
		};
	}
	internal static void LogShoutPromptContextStage(string stage, Stopwatch totalSw, Stopwatch stageSw, string targetId, int targetAgentIndex, string detail, bool immediate)
	{
		try
		{
			string safeStage = string.IsNullOrWhiteSpace(stage) ? "unknown" : stage.Trim();
			double stageMs = stageSw?.Elapsed.TotalMilliseconds ?? 0.0;
			double totalMs = totalSw?.Elapsed.TotalMilliseconds ?? 0.0;
			string target = string.IsNullOrWhiteSpace(targetId) ? "unknown" : targetId;
			string suffix = string.IsNullOrWhiteSpace(detail) ? "" : " " + detail.Trim();
			string message = "[NativePerf] prompt_context_stage stage=" + safeStage + " target=" + target + " agent=" + targetAgentIndex + " stageMs=" + Math.Round(stageMs, 2) + " totalMs=" + Math.Round(totalMs, 2) + suffix;
			Logger.Log("Logic", message);
			if (stageMs >= ShoutPromptContextSlowStageMs || totalMs >= ShoutPromptContextHardBudgetMs)
			{
				Logger.Log("Logic", "[NativePerf][WARN] prompt_context_slow_stage stage=" + safeStage + " target=" + target + " agent=" + targetAgentIndex + " stageMs=" + Math.Round(stageMs, 2) + " totalMs=" + Math.Round(totalMs, 2));
			}
			FreezeWatchdog.Mark("ShoutPromptContext." + safeStage, "target=" + target + " agent=" + targetAgentIndex + " stageMs=" + Math.Round(stageMs, 2) + " totalMs=" + Math.Round(totalMs, 2) + suffix, immediate: immediate);
			stageSw?.Restart();
		}
		catch
		{
		}
	}
	internal static void LogShoutPromptContextStage(string stage, Stopwatch totalSw, Stopwatch stageSw, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string detail = null, bool immediate = true)
	{
		LogShoutPromptContextStage(stage, totalSw, stageSw, targetHero?.StringId ?? targetCharacter?.StringId ?? targetCharacter?.HeroObject?.StringId, targetAgentIndex, detail, immediate);
	}
	internal static string AppendPlayerPartySharedResourcePrompt(string text, Hero targetHero, CharacterObject targetCharacter = null)
	{
		const string marker = "【队内资源共享限制】";
		if (string.IsNullOrWhiteSpace(text) || PromptRuleBlockText.Count(text) <= 0)
		{
			return text;
		}
		if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return text;
		}
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (!AIConfigHandler.IsPlayerPartyTradeLimitedTarget(hero))
		{
			return text;
		}
		string playerName = (PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(hero) ?? "").Trim();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		string prompt = marker + "由于你是" + playerName + "的队内成员，你和" + playerName + "的大部分资源都共享，例如定居点、工坊、商队和部队。正文中不要把部队转移或固定资产转移当作你与" + playerName + "之间需要谈判或执行的交易；如果被问到，应自然说明队内资源共享，不要提出部队转移或固定资产转移。";
		return text.TrimEnd() + Environment.NewLine + prompt;
	}
	internal static void ApplyPromptRuntimeAppendices(ShoutPromptContext shoutPromptContext, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string cultureIdOverride, Stopwatch promptContextTotalSw, Stopwatch promptContextStageSw)
	{
		LogShoutPromptContextStage("gccz_runtime_start", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex);
		AfGcczShoutBridge.AppendRuntimePromptToShoutContext(shoutPromptContext, targetHero, targetCharacter, targetAgentIndex, cultureIdOverride);
		LogShoutPromptContextStage("gccz_runtime_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "extrasLen=" + ((shoutPromptContext.Extras ?? "").Length));
		shoutPromptContext.Extras = AppendPlayerPartySharedResourcePrompt(shoutPromptContext.Extras, targetHero, targetCharacter);
		LogShoutPromptContextStage("shared_resource_done", promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex, "extrasLen=" + ((shoutPromptContext.Extras ?? "").Length));
		PromptExtrasMarkers extrasMarkers = PromptExtrasComposer.DetectMarkers(shoutPromptContext.Extras);
		bool extrasHasSiegeInterventionRule = AfGcczShoutBridge.HasInjectedRuleBlock(shoutPromptContext.Extras);
		Logger.Log("Logic", $"[RuleInjectionDebug] stage=extras targetHero={(targetHero?.StringId ?? "null")} targetCharacter={(targetCharacter?.StringId ?? "null")} extrasHasDuelRule={extrasMarkers.Duel} extrasHasRewardRule={extrasMarkers.Reward} extrasHasLoanRule={extrasMarkers.Loan} extrasHasWorldMapRule={extrasMarkers.WorldMap} extrasHasVanillaIssueRule={extrasMarkers.VanillaIssue} extrasHasVanillaIssueRuntimeBlock={extrasMarkers.VanillaIssueRuntimeBlock} extrasHasSiegeInterventionRule={extrasHasSiegeInterventionRule} extrasHasNpcMajorRule={extrasMarkers.NpcMajor} extrasHasResidentRecentActions={extrasMarkers.ResidentRecentActions} extrasLen={(shoutPromptContext.Extras ?? "").Length} useDuelContext={shoutPromptContext.UseDuelContext} useRewardContext={shoutPromptContext.UseRewardContext} useLoanContext={shoutPromptContext.IsLoanContext}");
	}
	internal static ShoutPromptContext CompleteSharedPromptBuild(PromptContextCaptureBannerlordPorts ports, PromptBuildPhases phases, Hero targetHero, CharacterObject targetCharacter, WeeklyPromptSnapshot weeklyPromptSnapshot)
	{
		PromptBuildRequest request = phases.Request;
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		int targetAgentIndex = request.TargetAgentIndex;
		ShoutPromptContext shoutPromptContext = CreateEmptyShoutPromptContext();
		shoutPromptContext.PreprocessExcludedRuleIds = phases.PreprocessExcludedRuleIds;
		shoutPromptContext.PreprocessExcludedRuleBlock = phases.PreprocessExcludedRuleBlock;
		using FreezeWatchdog.ScopeToken promptContextScope = FreezeWatchdog.Scope("ShoutPromptContext.Build");
		PromptContextFlags contextFlags;
		PromptExtrasSections extrasSections;
		PromptEntityCapture entityCapture;
		MentionedWorldEntities mentionedEntities;
		PromptContextCaptureBannerlordAdapter.CapturePromptSections(ports, request, phases.Routing, phases.Retrieval, phases.DirectPreprocessMentions, targetHero, targetCharacter, weeklyPromptSnapshot,
			out contextFlags, out extrasSections, out entityCapture, out mentionedEntities);
		shoutPromptContext.MentionedEntities = mentionedEntities.Clone();
		PromptAssembly assembly = PromptAssemblyStage.Assemble(extrasSections, entityCapture, phases.Routing, contextFlags, request.IsQualified, request.SuppressDynamicRuleAndLore, request.PreprocessExcludedRuleIds);
		shoutPromptContext.Extras = assembly.Extras;
		shoutPromptContext.EntityPostprocessContext = assembly.EntityPostprocessContext;
		shoutPromptContext.ExplicitMentionedKingdomIds = assembly.ExplicitMentionedKingdomIds;
		shoutPromptContext.UseDuelContext = assembly.UseDuelContext;
		shoutPromptContext.UseRewardContext = assembly.UseRewardContext;
		shoutPromptContext.IsLoanContext = assembly.IsLoanContext;
		shoutPromptContext.IsQualified = assembly.IsQualified;
		shoutPromptContext.PreprocessRuleIds = assembly.PreprocessRuleIds;
		LogShoutPromptContextStage("extras_assigned", phases.TotalStopwatch, phases.StageStopwatch, targetHero, targetCharacter, targetAgentIndex, "extrasLen=" + ((shoutPromptContext.Extras ?? "").Length) + " useRewardContext=" + assembly.UseRewardContext + " useLoanContext=" + assembly.IsLoanContext);
		LogShoutPromptContextStage("preprocess_ids_done", phases.TotalStopwatch, phases.StageStopwatch, targetHero, targetCharacter, targetAgentIndex,
			"ids=" + PromptTopicRoutingStage.DescribeHits(shoutPromptContext.PreprocessRuleIds, "(none)")
			+ " excluded=" + PromptTopicRoutingStage.DescribeHits(shoutPromptContext.PreprocessExcludedRuleIds, "(none)")
			+ " excludedBlockLen=" + ((shoutPromptContext.PreprocessExcludedRuleBlock ?? "").Length));
		ApplyPromptRuntimeAppendices(shoutPromptContext, targetHero, targetCharacter, targetAgentIndex, request.CultureId, phases.TotalStopwatch, phases.StageStopwatch);
		LogShoutPromptContextStage("complete", phases.TotalStopwatch, phases.StageStopwatch, targetHero, targetCharacter, targetAgentIndex, "extrasLen=" + ((shoutPromptContext.Extras ?? "").Length), immediate: true);
		return shoutPromptContext;
	}

    internal sealed class RequestCapturePorts
    {
        internal readonly Func<int> ReadCachedPlayerClanTier;
        internal readonly Func<Hero, CharacterObject, int, string> LatestUtterance;
        internal readonly Func<Hero, List<DialogueDay>> LoadHistory;
        internal RequestCapturePorts(Func<int> cachedTier, Func<Hero, CharacterObject, int, string> latestUtterance, Func<Hero, List<DialogueDay>> loadHistory)
        {
            ReadCachedPlayerClanTier = cachedTier ?? throw new ArgumentNullException(nameof(cachedTier));
            LatestUtterance = latestUtterance ?? throw new ArgumentNullException(nameof(latestUtterance));
            LoadHistory = loadHistory ?? throw new ArgumentNullException(nameof(loadHistory));
        }
    }

	internal static PromptBuildRequest CapturePromptBuildRequest(RequestCapturePorts ports, Hero targetHero, CharacterObject targetCharacter, string input, string extraFact, string cultureIdOverride, string kingdomIdOverride, int targetAgentIndex, bool hasAnyHero, bool suppressDynamicRuleAndLore, bool usePrefetchedLoreContext, string prefetchedLoreContext, IEnumerable<string> excludedRuleIds, IEnumerable<string> preprocessExcludedRuleIds, IEnumerable<string> forcedPreprocessRuleIds)
	{
		// These are topics removed from the candidate list before routing. They are
		// intentionally unrelated to topics the preprocessing LLM saw but did not select.
		PromptExclusionSets.Build(excludedRuleIds, preprocessExcludedRuleIds,
			set =>
			{
				PromptRuleCaptureBannerlordAdapter.AddPlayerCompanionOrFamilyRuleExclusionsForTarget(set, targetHero, targetCharacter);
				PromptRuleCaptureBannerlordAdapter.AddWorldMapCommandRuleExclusionForTarget(set, targetHero, targetCharacter, targetAgentIndex);
				PromptRuleCaptureBannerlordAdapter.AddSceneMoveRuleExclusionForCurrentMission(set);
				AfGcczShoutBridge.AddRuntimePreprocessRuleExclusions(set);
			},
			set => AfGcczShoutBridge.AddRuntimePreprocessRuleExclusions(set),
			out HashSet<string> explicitExcludedRuleIdSet, out HashSet<string> excludedRuleIdSet, out HashSet<string> preprocessExcludedRuleIdSet, out bool completeRuntimeExcludedRuleIds);
		string targetKingdomId = PromptRuleCaptureBannerlordAdapter.ResolveTargetKingdomIdForRules(targetHero, targetCharacter, kingdomIdOverride);
		bool bypassRulePreprocess = AfGcczShoutBridge.ShouldBypassPreprocessForActiveScene(targetAgentIndex);
		PromptRuntimeTargetBinding runtimeTarget = CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex);
		return new PromptBuildRequest
		{
			Input = input,
			ExtraFact = extraFact,
			CultureId = cultureIdOverride,
			KingdomIdOverride = kingdomIdOverride,
			TargetKingdomId = targetKingdomId,
			Target = runtimeTarget,
			Eligibility = AIConfigHandler.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTarget),
			TargetHeroId = targetHero?.StringId,
			TargetCharacterId = targetCharacter?.StringId,
			TargetDisplayName = targetHero?.Name?.ToString() ?? "某人",
			TargetAgentIndex = targetAgentIndex,
			HasAnyHero = hasAnyHero,
			HasTargetHero = targetHero != null,
			HasTargetCharacter = targetCharacter != null,
			SuppressDynamicRuleAndLore = suppressDynamicRuleAndLore,
			BypassRulePreprocess = bypassRulePreprocess,
			PlayerClanTier = PersonaIdentityPromptCaptureAdapter.ResolvePlayerClanTierForPrompt(ports.ReadCachedPlayerClanTier()),
			MinimumClanTier = DuelSettings.GetSettings()?.MinimumClanTier ?? 0,
			NpcLastUtterance = ports.LatestUtterance(targetHero, targetCharacter, targetAgentIndex),
			GuardrailSemanticContext = suppressDynamicRuleAndLore ? "" : PromptRuleCaptureBannerlordAdapter.BuildGuardrailSemanticContext(ports.LoadHistory, targetHero, extraFact),
			UsePrefetchedLoreContext = usePrefetchedLoreContext,
			PrefetchedLoreContext = prefetchedLoreContext,
			ExplicitExcludedRuleIds = explicitExcludedRuleIdSet,
			ExcludedRuleIds = excludedRuleIdSet,
			PreprocessExcludedRuleIds = preprocessExcludedRuleIdSet,
			CompleteRuntimeExcludedRuleIds = completeRuntimeExcludedRuleIds,
			ForcedPreprocessRuleIds = forcedPreprocessRuleIds,
			StickyTargetKey = ResolveBuiltInRuleStickyTargetKey(targetHero, targetCharacter),
			GuardrailStickyTargetKey = AIConfigHandler.CaptureGuardrailStickyTargetKey(runtimeTarget)
		};
	}
	internal static PromptBuildPhases BeginSharedPromptBuild(RequestCapturePorts ports, Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, bool suppressDynamicRuleAndLore, bool usePrefetchedLoreContext, string prefetchedLoreContext, IEnumerable<string> excludedRuleIds, IEnumerable<string> preprocessExcludedRuleIds, IEnumerable<string> forcedPreprocessRuleIds, MentionedWorldEntities preprocessMentionedEntities)
	{
		if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(extraFact))
		{
			return null;
		}
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		PromptBuildPhases phases = new PromptBuildPhases
		{
			DirectPreprocessMentions = preprocessMentionedEntities?.Clone() ?? new MentionedWorldEntities(),
			TotalStopwatch = Stopwatch.StartNew(),
			StageStopwatch = Stopwatch.StartNew()
		};
		LogShoutPromptContextStage("start", phases.TotalStopwatch, phases.StageStopwatch, targetHero, targetCharacter, targetAgentIndex, "inputLen=" + ((input ?? "").Length) + " extraLen=" + ((extraFact ?? "").Length) + " suppressDynamic=" + suppressDynamicRuleAndLore + " thread=" + Thread.CurrentThread.ManagedThreadId);
		PromptBuildRequest request = CapturePromptBuildRequest(ports, targetHero, targetCharacter, input, extraFact, cultureIdOverride, kingdomIdOverride, targetAgentIndex, hasAnyHero, suppressDynamicRuleAndLore, usePrefetchedLoreContext, prefetchedLoreContext, excludedRuleIds, preprocessExcludedRuleIds, forcedPreprocessRuleIds);
		phases.Request = request;
		if (!request.SuppressDynamicRuleAndLore && request.CompleteRuntimeExcludedRuleIds)
		{
			PromptExclusionSets.AddUnavailableConfiguredRules(request.PreprocessExcludedRuleIds, AIConfigHandler.GetConfiguredEnabledGuardrailRuleIdsForExternal(),
				id => AIConfigHandler.IsGuardrailRuleAvailableToPreprocessForExternal(id, request.HasAnyHero));
		}
		if (!request.SuppressDynamicRuleAndLore)
		{
			phases.PreprocessExcludedRuleIds = PromptExclusionSets.ToOrderedList(request.PreprocessExcludedRuleIds);
			phases.PreprocessExcludedRuleBlock = AIConfigHandler.BuildPreprocessExcludedRuleBlockForExternal(phases.PreprocessExcludedRuleIds);
		}
		LogShoutPromptContextStage("runtime_init_done", phases.TotalStopwatch, phases.StageStopwatch, targetHero, targetCharacter, targetAgentIndex, "semanticContextLen=" + ((request.GuardrailSemanticContext ?? "").Length) + " targetKingdom=" + (request.TargetKingdomId ?? ""));
		return phases;
	}

internal static List<string> RunCourierRulePreprocessInternal(RequestCapturePorts ports, Hero targetHero, string input, string extraFact, out MentionedWorldEntities mentionedEntities, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, IEnumerable<string> excludedRuleIds)
	{
		MyBehavior.CourierPreprocessRequest request = BeginCourierRulePreprocess(ports, targetHero, input, extraFact, targetCharacter, kingdomIdOverride, targetAgentIndex, excludedRuleIds);
		if (request == null)
		{
			mentionedEntities = new MentionedWorldEntities();
			return new List<string>();
		}
		using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
		AIConfigHandler.ApplyGuardrailRuntimeTarget(request.Target, request.Eligibility);
		try
		{
			return RunCourierRulePreprocessRetrieval(request, out mentionedEntities);
		}
		finally
		{
			AIConfigHandler.ClearGuardrailRuntimeTarget();
			AIConfigHandler.SetGuardrailSemanticContext("");
		}
	}
internal static MyBehavior.CourierPreprocessRequest BeginCourierRulePreprocess(RequestCapturePorts ports, Hero targetHero, string input, string extraFact, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, IEnumerable<string> excludedRuleIds)
	{
		HashSet<string> excludedRuleIdSet = PromptRuleIdPolicy.BuildRuleIdSet(excludedRuleIds);
		PromptRuleCaptureBannerlordAdapter.AddPlayerCompanionOrFamilyRuleExclusionsForTarget(excludedRuleIdSet, targetHero, targetCharacter);
		PromptRuleCaptureBannerlordAdapter.AddWorldMapCommandRuleExclusionForTarget(excludedRuleIdSet, targetHero, targetCharacter, targetAgentIndex);
		AfGcczShoutBridge.AddRuntimePreprocessRuleExclusions(excludedRuleIdSet, targetAgentIndex);
		PromptRuleIdPolicy.AddPreprocessOnlyResidentRuleExclusions(excludedRuleIdSet);
		if (AfGcczShoutBridge.ShouldBypassPreprocessForActiveScene(targetAgentIndex))
		{
			Logger.Log("CourierDelivery", "[Preprocess] skipped: active GCCZ siege aftermath scene uses unconditional postprocess routing.");
			return null;
		}
		string targetKingdomId = PromptRuleCaptureBannerlordAdapter.ResolveTargetKingdomIdForRules(targetHero, targetCharacter, kingdomIdOverride);
		PromptRuntimeTargetBinding runtimeTarget = CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex);
		return new MyBehavior.CourierPreprocessRequest
		{
			Input = input,
			TargetHeroId = targetHero?.StringId,
			TargetCharacterId = targetCharacter?.StringId,
			Target = runtimeTarget,
			Eligibility = AIConfigHandler.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTarget),
			ExcludedRuleIds = excludedRuleIdSet,
			GuardrailSemanticContext = PromptRuleCaptureBannerlordAdapter.BuildGuardrailSemanticContext(ports.LoadHistory, targetHero, extraFact),
			NpcLastUtterance = ports.LatestUtterance(targetHero, targetCharacter, targetAgentIndex)
		};
	}
internal static List<string> RunCourierRulePreprocessRetrieval(MyBehavior.CourierPreprocessRequest request, out MentionedWorldEntities mentionedEntities)
	{
		AIConfigHandler.SetGuardrailSemanticContext(request.GuardrailSemanticContext);
		List<GuardrailRuleHit> hits = AIConfigHandler.GetGuardrailSemanticRuleHitsForPreprocess(request.Input, request.NpcLastUtterance, AIConfigHandler.GuardrailRuleReturnCap, includeBuiltInRules: true, request.ExcludedRuleIds, out mentionedEntities);
		List<string> result = PromptRuleIdPolicy.OrderPreprocessHitIds(hits);
		Logger.Log("CourierDelivery", "[Preprocess] targetHero=" + (request.TargetHeroId ?? "null") + " targetCharacter=" + (request.TargetCharacterId ?? "null") + " npcRecall=" + (string.IsNullOrWhiteSpace(request.NpcLastUtterance) ? "off" : "on") + " hits=" + (result.Count == 0 ? "(none)" : string.Join(",", result)));
		return result;
	}

    internal sealed class HistoryWorkCapturePorts
    {
        internal Func<bool> IsCurrentOwner;
        internal Func<string,List<CompressedMemoryBlock>> LoadBlocks;
        internal Func<string,List<DailyMemoryDraft>> LoadDrafts;
        internal Func<string,string> Overview;
        internal Func<string,string,int,string,string,bool,MyBehavior.HistoryPromptSnapshot,string> BuildContext;
    }

internal static Func<string> CaptureHistoryContextWorkForHero(Func<HistoryWorkCapturePorts> readOwner, Hero hero, string currentInput,
        string secondaryInput, bool includeCurrentActiveSceneSession, long generation)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("History capture requires the game thread.");
        return CaptureHistoryContextWorkById(readOwner, CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero), hero?.Name?.ToString() ?? "NPC",
            currentInput, secondaryInput, includeCurrentActiveSceneSession, generation);
    }
internal static Func<string> CaptureHistoryContextWorkById(Func<HistoryWorkCapturePorts> readOwner, string memoryId, string memoryName,
        string currentInput, string secondaryInput, bool includeCurrentActiveSceneSession, long generation)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("History capture requires the game thread.");
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return null;
        HistoryWorkCapturePorts owner = readOwner();
        string id = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
        if (owner == null || !MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(id)) return () => "";
        if (!owner.IsCurrentOwner()) return null;

        int blockCount = owner.LoadBlocks(id).Count;
        List<DailyMemoryDraft> drafts = owner.LoadDrafts(id);
        int draftCount = drafts.Count;
        // Keep the original overview/sanitize order before projecting the blocks for recall.
        string overview = owner.Overview(id);
        string scene = SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel();
        List<CompressedMemoryBlock> blocks = owner.LoadBlocks(id);
        int finalCount = MemoryBusinessStateOwner.GetMemoryFinalInjectCountFromSettings();
        int candidateLimit = MemoryBusinessStateOwner.GetMemoryCandidateLimitFromSettings();
        HistoryPromptSnapshot snapshot = new HistoryPromptSnapshot
        {
            Generation = generation,
            GameDay = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe(),
            Scene = scene,
            FinalCount = finalCount,
            CandidateLimit = candidateLimit,
            PreprocessMode = MemoryBusinessStateOwner.GetMemoryPreprocessModeFromSettings(),
            BlockCount = blockCount,
            DraftCount = draftCount,
            Overview = overview,
            // Below this upper bound the existing recall path never requests embedding.
            RecallQuery = blocks.Count > Math.Max(finalCount, candidateLimit)
                ? MemoryRecallInputCaptureAdapter.BuildMemoryRecallQueryText(null, currentInput, secondaryInput, drafts, scene) : "",
            Blocks = blocks.Select(CopyHistoryRecallBlock).ToList()
        };
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return null;
        return () =>
        {
            if (!owner.IsCurrentOwner() || !SaveRuntimeGuard.IsCurrentGeneration(generation)) return "";
            return owner.BuildContext(id, memoryName, 0, currentInput, secondaryInput,
                includeCurrentActiveSceneSession, snapshot);
        };
    }
internal static CompressedMemoryBlock CopyHistoryRecallBlock(CompressedMemoryBlock block)
    {
        if (block == null) return null;
        // Only fields read by the existing recall/selection/render path belong in this projection.
        // In particular, AFEF must not retain a mutable list owned by the campaign.
        return new CompressedMemoryBlock
        {
            Id = block.Id,
            HeroId = block.HeroId,
            HeroName = block.HeroName,
            GameDayIndex = block.GameDayIndex,
            GameDate = block.GameDate,
            StartHour = block.StartHour,
            EndHour = block.EndHour,
            CreatedUtcTicks = block.CreatedUtcTicks,
            RichTitle = block.RichTitle,
            Summary = block.Summary,
            AfefLines = block.AfefLines == null ? null : new List<string>(block.AfefLines)
        };
    }
internal static string BuildHeroPrisonerStatusPromptLineForExternal(Hero hero)
	{
		try
		{
			if (hero == null || !hero.IsPrisoner)
			{
				return "";
			}
			PartyBase holder = null;
			try
			{
				holder = hero.PartyBelongedToAsPrisoner;
			}
			catch
			{
				holder = null;
			}
			string place = "";
			string captor = "";
			if (holder != null)
			{
				try
				{
					if (holder.IsSettlement && holder.Settlement != null)
					{
						Settlement settlement = holder.Settlement;
						string settlementName = (settlement.Name?.ToString() ?? settlement.StringId ?? "").Trim();
						string settlementType = settlement.IsCastle ? "城堡" : (settlement.IsTown ? "城镇" : (settlement.IsVillage ? "村庄" : "定居点"));
						if (!string.IsNullOrWhiteSpace(settlementName))
						{
							place = settlementName + "（" + settlementType + "）";
						}
						captor = (settlement.OwnerClan?.Leader?.Name?.ToString() ?? settlement.OwnerClan?.Name?.ToString() ?? settlement.MapFaction?.Name?.ToString() ?? "").Trim();
					}
					else if (holder.IsMobile && holder.MobileParty != null)
					{
						MobileParty party = holder.MobileParty;
						place = (party.Name?.ToString() ?? party.StringId ?? "一支队伍").Trim();
						captor = (party.LeaderHero?.Name?.ToString() ?? party.ActualClan?.Leader?.Name?.ToString() ?? party.ActualClan?.Name?.ToString() ?? party.MapFaction?.Name?.ToString() ?? "").Trim();
					}
				}
				catch
				{
				}
				if (string.IsNullOrWhiteSpace(captor))
				{
					try
					{
						captor = (holder.LeaderHero?.Name?.ToString() ?? holder.MapFaction?.Name?.ToString() ?? "").Trim();
					}
					catch
					{
						captor = "";
					}
				}
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("【俘虏处境】你现在是俘虏，");
			if (!string.IsNullOrWhiteSpace(place))
			{
				stringBuilder.Append("被关押在").Append(place);
			}
			else
			{
				stringBuilder.Append("正在被关押");
			}
			if (!string.IsNullOrWhiteSpace(captor))
			{
				stringBuilder.Append("，关押/控制你的人或势力是").Append(captor);
			}
			stringBuilder.Append("。这不是普通拜访、驻留或自由行军；你行动受限，不能随意离开，也不应声称自己仍能自由统领部队或处理外部事务。回应时必须承认自己被关押和被俘虏的事实。");
			return stringBuilder.ToString();
		}
		catch
		{
			return "";
		}
	}
}
