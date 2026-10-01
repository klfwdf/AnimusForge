using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static AnimusForge.ShoutBehavior;
namespace AnimusForge;
// Synchronous caller-thread capture preserves the external hook's game-thread protocol.
internal sealed class SceneExternalPromptCaptureAdapter
{
    private readonly SceneExternalPromptCapturePorts _ports;
    internal SceneExternalPromptCaptureAdapter(SceneExternalPromptCapturePorts ports) { _ports = ports; }
    internal DetachedInteractionPromptSections Build(string playerText, int targetAgentIndex)
    {
        if (targetAgentIndex < 0) return DetachedInteractionPromptSections.Empty;
		Agent targetAgent = Mission.Current?.Agents?.FirstOrDefault(agent => agent != null && agent.Index == targetAgentIndex);
		NpcDataPacket targetNpc = targetAgent == null ? null : ShoutUtils.ExtractNpcData(targetAgent);
		if (targetNpc == null)
		{
			return DetachedInteractionPromptSections.Empty;
		}
		List<NpcDataPacket> presentNpcs = (ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>())
			.Select(agent => ShoutUtils.ExtractNpcData(agent))
			.Where(data => data != null)
			.ToList();
		if (!presentNpcs.Any(data => data.AgentIndex == targetAgentIndex))
		{
			presentNpcs.Insert(0, targetNpc);
		}
		Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
		foreach (NpcDataPacket data in presentNpcs)
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(item => item != null && item.Index == data.AgentIndex);
			Hero hero = (agent?.Character as CharacterObject)?.HeroObject;
			if (hero != null)
			{
				resolvedHeroes[data.AgentIndex] = hero;
			}
		}
		resolvedHeroes.TryGetValue(targetAgentIndex, out Hero targetHero);
		CharacterObject targetCharacter = targetAgent.Character as CharacterObject;
		List<SceneSummonPromptTarget> summonTargets = _ports.SummonTargets(presentNpcs, resolvedHeroes);
		int guidePromptId = (summonTargets ?? new List<SceneSummonPromptTarget>()).Count == 0
			? 1
			: summonTargets.Max(item => item?.PromptId ?? 0) + 1;
		List<SceneGuidePromptTarget> guideTargets = _ports.GuideTargets(targetAgent, guidePromptId);
		List<string> excludedRuleIds = _ports.BuildPreprocessExcludedRuleIdsForCurrentInteraction(
			targetHero,
			targetCharacter,
			targetAgentIndex,
			targetNpc.IsHero,
			summonTargets,
			guideTargets,
			targetNpc,
			presentNpcs,
			playerText);
		MyBehavior.ShoutPromptContext context = MyBehavior.BuildShoutPromptContextForExternal(
			targetHero,
			playerText,
			extraFact: null,
			targetNpc.CultureId ?? "neutral",
			hasAnyHero: targetNpc.IsHero,
			targetCharacter: targetCharacter,
			kingdomIdOverride: _ports.TryGetKingdomIdOverrideFromAgent(targetAgent),
			targetAgentIndex: targetAgentIndex,
			preprocessExcludedRuleIds: excludedRuleIds);
		string baseExtras = _ports.StripScenePersonaBlocks((context?.Extras ?? string.Empty).Trim());
		_ports.ExtractTrustPromptBlock(baseExtras, out string extrasWithoutTrust);
		_ports.SplitSceneExtraSections(extrasWithoutTrust, out string miscExtras, out string ruleExtras, out string knowledgeExtras);
		bool partyTransferSelected = _ports.HasPartyTransferRuleContext(baseExtras);
		bool includeInventory = context != null && (context.UseRewardContext || context.IsLoanContext);
		string roleTop = _ports.BuildSceneSystemTopPromptIntroForSingle(
			targetNpc,
			targetHero,
			presentNpcs,
			includeInventory,
			includeInventory,
			partyTransferSelected,
			context?.MentionedEntities);
		string roleRuntime = _ports.BuildSceneUserRuntimeContextForSingle(
			targetNpc,
			targetHero,
			presentNpcs,
			includeInventory,
			includeInventory,
			partyTransferSelected,
			context?.MentionedEntities);
		string presentBlock = _ports.BuildScenePresentNpcListBlockForPrompt(presentNpcs, targetNpc, resolvedHeroes);
		string mechanism = _ports.BuildSceneMechanismPromptSection(
			summonTargets,
			guideTargets,
				_ports.SummonClosure(presentNpcs),
			_ports.FollowControl(targetNpc),
			targetNpc);
		string ruleBlock = _ports.BuildSceneSystemRuleBlock(ruleExtras, mechanism);
		string dynamic = MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(string.Empty, roleRuntime, presentBlock, miscExtras);
		_ports.GetSceneReplyLengthLimits(DuelSettings.GetSettings(), out int minTokens, out int maxTokens);
		string playerName = _ports.GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		string layered = MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(
			string.Empty,
			roleTop,
			_ports.BuildSceneSingleNpcTaskSystemBlock(_ports.GetSceneNpcHistoryNameForPrompt(targetNpc), presentNpcs.Count > 1, minTokens, maxTokens, playerName),
			context?.PreprocessExcludedRuleBlock);
		layered = _ports.AppendPlayerCustomPromptRuleToSystemPrompt(layered);
		string persisted = _ports.BuildPersistedHeroHistoryContext(targetAgentIndex, playerText, resolvedHeroes);
		_ports.SplitPersistedHeroHistorySections(persisted, out string privateRecent, out string persistedWithoutRecent);
		List<string> sceneHistoryLines = null;
		sceneHistoryLines = _ports.CaptureVisibleSceneHistoryLinesForPrompt(targetAgentIndex, _ports.GetSceneNpcHistoryNameForPrompt(targetNpc), false);
		string scenePublicHistory = _ports.BuildScenePublicHistorySection(sceneHistoryLines);
		string mainHistory = MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(
			string.Empty,
			privateRecent,
			persistedWithoutRecent,
			dynamic,
			MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(string.Empty, knowledgeExtras, ruleBlock));
		string strictSystem = _ports.BuildStrictSceneMessagesSystemPrompt(layered, suppressReplyFormatInstruction: false);
		string postHistory = MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(string.Empty, privateRecent, persistedWithoutRecent, scenePublicHistory);
		string postRules = MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(string.Empty, knowledgeExtras, ruleBlock);
		string postUser = _ports.BuildSceneActionPostprocessUserPrompt(
			AIConfigHandler.ActionPostprocessUserPromptTemplate,
			postRules,
			_ports.GetSceneNpcHistoryNameForPrompt(targetNpc),
			postHistory,
			AIConfigHandler.BuildActionPostprocessLatestReplyBlock(playerText, string.Empty, _ports.GetSceneNpcHistoryNameForPrompt(targetNpc), postHistory),
			runtimeContext: context?.EntityPostprocessContext);
		return MainPromptMessageAssemblyOwner.BuildExternalScenePromptSections(strictSystem, mainHistory, AIConfigHandler.ActionPostprocessSystemPrompt, postHistory, postUser);
	}
}
