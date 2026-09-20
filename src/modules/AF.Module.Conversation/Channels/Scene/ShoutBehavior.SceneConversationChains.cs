using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace AnimusForge;

public partial class ShoutBehavior : CampaignBehaviorBase
{
	private sealed class ImmediateSceneReactionRequest
	{
		public long RequestId;

		public long RuntimeGeneration;

		public int SceneHistorySessionId;

		public Mission SourceMission;

		public int TargetAgentIndex = -1;

		public NpcDataPacket TargetNpc;

		public List<NpcDataPacket> AllNpcData;

		public bool SuppressStare;

		public string FactText;

		public bool RunSiegeReactionPostprocess;

		public Func<bool> CanStillPublish;

		public Action OnNoSpeech;

		public Action<bool> OnCompleted;

		public List<object> Messages;

		public int MaxTokens;

		public float Temperature = 0.35f;

		public bool UsesCompactTownOrdinaryChain;
	}

	private async Task<string> GenerateGroupConversationTurnLineAsync(NpcDataPacket speakerNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, string playerText, string extraFact, string commonCandidatesPrompt, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string sceneMechanismPromptSectionBase, List<string> patienceStatusLines, bool multiNpcScene, int minTokens, int maxTokens)
	{
		try
		{
			Stopwatch turnSw = Stopwatch.StartNew();
			if (speakerNpc == null || allNpcData == null || allNpcData.Count == 0)
			{
				return "";
			}
			Logger.Log("Logic", "[MemoryPerf] group_turn_fallback_start agent=" + speakerNpc.AgentIndex + " npc=" + (GetSceneNpcHistoryNameForPrompt(speakerNpc) ?? "") + " multi=" + multiNpcScene + " candidates=" + allNpcData.Count);
			ApplySceneLocalDisambiguatedNames(allNpcData);
			await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { speakerNpc }, resolvedHeroes ?? new Dictionary<int, Hero>());
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == speakerNpc.AgentIndex);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return "";
			}
			CharacterObject characterObject = agent.Character as CharacterObject;
			Hero hero = null;
			if (speakerNpc.IsHero && resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(speakerNpc.AgentIndex, out hero);
			}
			string kingdomIdOverride = TryGetKingdomIdOverrideFromAgent(agent);
			PrecomputedShoutRagContext precomputed = null;
			bool hasPrecomputed = precomputedContexts != null && precomputedContexts.TryGetValue(speakerNpc.AgentIndex, out precomputed);
			string loreContext = (hasPrecomputed && precomputed != null) ? (precomputed.LoreContext ?? "") : "";
			string fullExtra = string.IsNullOrWhiteSpace(loreContext) ? null : loreContext;
			if (!string.IsNullOrWhiteSpace(extraFact))
			{
				fullExtra = ((fullExtra == null) ? extraFact : (fullExtra + "\n" + extraFact));
			}
			List<string> preprocessExcludedRuleIds = BuildPreprocessExcludedRuleIdsForCurrentInteraction(hero, characterObject, speakerNpc.AgentIndex, speakerNpc.IsHero, sceneSummonTargets, sceneGuideTargets, speakerNpc, allNpcData, playerText);
			Task<string> persistedHeroHistoryTask = StartPrecomputedPersistedHistoryContextTask(speakerNpc.AgentIndex, playerText, resolvedHeroes, precomputedContexts, "group_turn_fallback");
			Stopwatch preprocessSw = Stopwatch.StartNew();
			MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(hero, playerText, fullExtra, speakerNpc.CultureId ?? "neutral", hasAnyHero: speakerNpc.IsHero, targetCharacter: characterObject, kingdomIdOverride: kingdomIdOverride, targetAgentIndex: speakerNpc.AgentIndex, usePrefetchedLoreContext: hasPrecomputed && precomputed != null && precomputed.HasLoreContext, prefetchedLoreContext: precomputed?.LoreContext, preprocessExcludedRuleIds: preprocessExcludedRuleIds);
			preprocessSw.Stop();
			Logger.Log("Logic", "[MemoryPerf] group_turn_fallback_preprocess_done agent=" + speakerNpc.AgentIndex + " hero=" + (hero?.StringId ?? "") + " ms=" + Math.Round(preprocessSw.Elapsed.TotalMilliseconds, 2) + " hits=" + (((ctx?.PreprocessRuleIds) == null || ctx.PreprocessRuleIds.Count == 0) ? "(none)" : string.Join(",", ctx.PreprocessRuleIds)) + " extrasLen=" + ((ctx?.Extras ?? "").Length));
			StringBuilder local = new StringBuilder();
			local.Append(commonCandidatesPrompt ?? "");
			string scenePatienceInstruction = "";
			string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
			string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
			SplitSceneExtraSections(baseExtrasWithoutTrust, out var miscExtrasSection, out var ruleExtrasSection, out var knowledgeExtrasSection);
			bool partyTransferTopicSelected = HasPartyTransferRuleContext(baseExtras);
			string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(speakerNpc);
			string sceneMechanismPromptSection = BuildSceneMechanismPromptSection(null, null, null, sceneFollowControlInstruction, speakerNpc);
			if (!string.IsNullOrWhiteSpace(sceneMechanismPromptSectionBase))
			{
				sceneMechanismPromptSection = string.IsNullOrWhiteSpace(sceneMechanismPromptSection) ? sceneMechanismPromptSectionBase.Trim() : (sceneMechanismPromptSectionBase.Trim() + "\n" + sceneMechanismPromptSection);
			}
			string persistedHeroHistory = await AwaitPrecomputedPersistedHistoryContextAsync(speakerNpc.AgentIndex, playerText, resolvedHeroes, precomputedContexts, persistedHeroHistoryTask, "group_turn_fallback");
			string privateRecentWindowSection = "";
			string persistedWithoutRecentWindow = "";
			SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
			bool includeInventorySummary = ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext);
			bool includeTradePricing = includeInventorySummary;
			string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(speakerNpc, hero, allNpcData, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities);
			string roleRuntimeContext = BuildSceneUserRuntimeContextForSingle(speakerNpc, hero, allNpcData, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities);
			string singleReplyPlayerName = GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(singleReplyPlayerName))
			{
				singleReplyPlayerName = "玩家";
			}
			string taskSystemBlock = BuildSceneSingleNpcTaskSystemBlock(GetSceneNpcHistoryNameForPrompt(speakerNpc), multiNpcScene, minTokens, maxTokens, singleReplyPlayerName);
			string systemRuleBlock = BuildSceneSystemRuleBlock(ruleExtrasSection, sceneMechanismPromptSection);
			if (HasPreprocessRuleHit(ctx?.PreprocessRuleIds, "worldmap_party_command") || HasInjectedRuleBlockForPostprocess(BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock), "worldmap_party_command"))
			{
				roleRuntimeContext = AppendPostprocessContextBlockForScene(roleRuntimeContext, WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(hero, characterObject, speakerNpc.AgentIndex));
			}
			string layeredPrompt = BuildSceneCompositeUserBlock("", roleTopIntro, taskSystemBlock, ctx?.PreprocessExcludedRuleBlock);
			layeredPrompt = AppendPlayerCustomPromptRuleToSystemPrompt(layeredPrompt);
			string currentAfefFactBlock = BuildCurrentAfefFactPromptBlock(extraFact);
			Stopwatch uncompressedSw = Stopwatch.StartNew();
			List<ConversationMessage> persistentMemoryRoleMessages = BuildUncompressedMemoryRoleMessagesForPrompt(hero, speakerNpc.AgentIndex);
			uncompressedSw.Stop();
			Logger.Log("Logic", "[MemoryPerf] group_turn_fallback_uncompressed_done agent=" + speakerNpc.AgentIndex + " hero=" + (hero?.StringId ?? "") + " messages=" + ((persistentMemoryRoleMessages == null) ? 0 : persistentMemoryRoleMessages.Count) + " ms=" + Math.Round(uncompressedSw.Elapsed.TotalMilliseconds, 2));
			List<object> messages = BuildStrictSceneMessagesForNpc(speakerNpc.AgentIndex, layeredPrompt, new string[9] { privateRecentWindowSection, persistedWithoutRecentWindow, roleRuntimeContext, local.ToString().Trim(), currentAfefFactBlock, trustBlock, miscExtrasSection, scenePatienceInstruction, BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock) }, persistentHistoryMessages: persistentMemoryRoleMessages);
			Logger.Log("Logic", "[MemoryPerf] group_turn_fallback_prompt_ready agent=" + speakerNpc.AgentIndex + " hero=" + (hero?.StringId ?? "") + " messages=" + messages.Count + " persistedChars=" + ((persistedHeroHistory ?? "").Length) + " privateChars=" + ((privateRecentWindowSection ?? "").Length) + " oldCompressedChars=" + ((persistedWithoutRecentWindow ?? "").Length));
			Stopwatch apiSw = Stopwatch.StartNew();
			string text = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(messages, 5000, promptRetryOnError: true);
			text = LlmVisibleReplyNormalizer.NormalizeComplete(text);
			apiSw.Stop();
			Logger.Log("Logic", "[MemoryPerf] group_turn_fallback_api_done agent=" + speakerNpc.AgentIndex + " hero=" + (hero?.StringId ?? "") + " outputLen=" + ((text ?? "").Length) + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " elapsedMs=" + Math.Round(turnSw.Elapsed.TotalMilliseconds, 2));
			if (string.IsNullOrWhiteSpace(text) || text.StartsWith("（错误") || text.StartsWith("（程序错误") || text.StartsWith("（API请求失败") || text.StartsWith("（API响应格式错误"))
			{
				return "";
			}
			string text2 = StripNpcNamePrefixSafely((text ?? "").Replace("\r", "").Trim(), 30);
			text2 = StripLeakedPromptContentForShout(text2);
			text2 = StripActionTagsForSceneSpeech(text2);
			return text2.Trim();
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[GroupConversationTurn] prompt failed: " + ex.Message);
			return "";
		}
	}

	private async Task<string> GetPassiveNpcResponse(NpcDataPacket data, string sceneDesc, string inputActionText, string precalculatedLore, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes)
	{
		try
		{
			if (data == null)
			{
				return "（没说话）";
			}
			ApplySceneLocalDisambiguatedNames(allNpcData);
			Hero hero = null;
			if (data.IsHero && resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(data.AgentIndex, out hero);
			}
			using (Logger.BeginTrace("shout_passive", hero?.StringId, data.Name))
			{
				Logger.Obs("ShoutPassive", "start", new Dictionary<string, object>
				{
					["npc"] = data.Name ?? "",
					["agentIndex"] = data.AgentIndex,
					["inputLen"] = (inputActionText ?? "").Length,
					["isHero"] = data.IsHero
				});
				string cultureId = data.CultureId ?? "neutral";
				string loreContext = precalculatedLore ?? "";
				string fullExtra = (string.IsNullOrWhiteSpace(loreContext) ? null : loreContext);
				Agent passiveAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == data.AgentIndex);
				CharacterObject passiveCharacter = passiveAgent?.Character as CharacterObject;
				string passiveKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(passiveAgent);
				List<string> preprocessExcludedRuleIds = BuildPreprocessExcludedRuleIdsForCurrentInteraction(hero, passiveCharacter, data.AgentIndex, data.IsHero, sceneSpeakerNpc: data, sceneCandidates: allNpcData, currentPlayerText: inputActionText);
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(hero, inputActionText, fullExtra, cultureId, hasAnyHero: data.IsHero, targetCharacter: passiveCharacter, kingdomIdOverride: passiveKingdomIdOverride, targetAgentIndex: data.AgentIndex, usePrefetchedLoreContext: !string.IsNullOrWhiteSpace(loreContext), prefetchedLoreContext: loreContext, preprocessExcludedRuleIds: preprocessExcludedRuleIds);
				DuelSettings settings = DuelSettings.GetSettings();
				GetSceneReplyLengthLimits(settings, out var minTokens, out var maxTokens);
				StringBuilder sysPrompt = new StringBuilder();
				string npcName = GetSceneNpcHistoryNameForPrompt(data);
				string presentNpcListBlock = BuildScenePresentNpcListBlockForPrompt(allNpcData, data, resolvedHeroes, useAllegianceLabel: true);
				if (!string.IsNullOrWhiteSpace(presentNpcListBlock))
				{
					sysPrompt.AppendLine(presentNpcListBlock);
				}
				string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(allNpcData);
				if (!string.IsNullOrWhiteSpace(sceneSummonClosureInstruction))
				{
					sysPrompt.AppendLine(sceneSummonClosureInstruction);
				}
				string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(data);
				if (!string.IsNullOrWhiteSpace(sceneFollowControlInstruction))
				{
					sysPrompt.AppendLine(sceneFollowControlInstruction);
				}
				bool passiveMultiNpcScene = allNpcData != null && allNpcData.Count((NpcDataPacket npc) => npc != null) > 1;
				if (!string.IsNullOrWhiteSpace(inputActionText))
				{
					sysPrompt.AppendLine("[玩家动作] " + inputActionText);
				}
				string scenePatienceInstruction = "";
				if (passiveMultiNpcScene)
				{
					string playerNameForPrompt = GetPlayerDisplayNameForShout();
					if (string.IsNullOrWhiteSpace(playerNameForPrompt))
					{
						playerNameForPrompt = "玩家";
					}

				}
				Stopwatch swPrompt = Stopwatch.StartNew();
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
				SplitSceneExtraSections(baseExtrasWithoutTrust, out var miscExtrasSection, out var ruleExtrasSection, out var knowledgeExtrasSection);
				bool partyTransferTopicSelected = HasPartyTransferRuleContext(baseExtras);
				bool includeTradePricing = ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext);
				string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(data, hero, allNpcData, includeTradePricing: includeTradePricing, partyTransferTopicSelected: partyTransferTopicSelected, promptMentions: ctx?.MentionedEntities);
				string roleRuntimeContext = BuildSceneUserRuntimeContextForSingle(data, hero, allNpcData, includeTradePricing: includeTradePricing, partyTransferTopicSelected: partyTransferTopicSelected, promptMentions: ctx?.MentionedEntities);
				string playerNameForLength = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForLength))
				{
					playerNameForLength = "玩家";
				}
				string taskSystemBlock = BuildSceneSingleNpcTaskSystemBlock(npcName, passiveMultiNpcScene, minTokens, maxTokens, playerNameForLength);
				string playerNameForTask = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForTask))
				{
					playerNameForTask = "玩家";
				}
				string taskPreamble = "你是【站在你旁边的人】中的NPC角色,可能是多个人。你们的唯一任务是：根据下方提供的角色信息、场景信息和对话历史，以NPC身份直接回复" + playerNameForTask + "的对话。\n禁止生成任何【】章节标题或格式说明。";
				string systemRuleBlock = BuildSceneSystemRuleBlock(ruleExtrasSection, null);
				if (HasPreprocessRuleHit(ctx?.PreprocessRuleIds, "worldmap_party_command") || HasInjectedRuleBlockForPostprocess(BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock), "worldmap_party_command"))
				{
					roleRuntimeContext = AppendPostprocessContextBlockForScene(roleRuntimeContext, WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(hero, passiveCharacter, data.AgentIndex));
				}
				string layeredPrompt = BuildSceneCompositeUserBlock("", roleTopIntro, taskPreamble, taskSystemBlock, ctx?.PreprocessExcludedRuleBlock);
				layeredPrompt = AppendPlayerCustomPromptRuleToSystemPrompt(layeredPrompt);
				swPrompt.Stop();
				int fixedChars = 0;
				int deltaChars = CountPromptChars(layeredPrompt);
				int totalChars = CountPromptChars(layeredPrompt);
				Logger.Log("ShoutBehavior(被动)", $"[PromptLayer] fixedChars={fixedChars} deltaChars={deltaChars} totalChars={totalChars}");
				Logger.Obs("Prompt", "compose", new Dictionary<string, object>
				{
					["mode"] = "shout_passive",
					["fixedChars"] = fixedChars,
					["deltaChars"] = deltaChars,
					["totalChars"] = totalChars,
					["buildMs"] = Math.Round(swPrompt.Elapsed.TotalMilliseconds, 2)
				});
				Logger.Metric("prompt.compose.shout_passive", ok: true, swPrompt.Elapsed.TotalMilliseconds);
				StringBuilder historyDump = null;
				List<string> historyLines = null;
				lock (_historyLock)
				{
					if (_publicConversationHistory.Count > 0)
					{
						historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, data.AgentIndex, npcName, passiveMultiNpcScene);
						if (historyLines != null && historyLines.Count > 0)
						{
							historyDump = new StringBuilder();
							historyDump.AppendLine($">>> History(Public viewerAgentIndex={data.AgentIndex}) count={historyLines.Count}");
							for (int i = 0; i < historyLines.Count; i++)
							{
								historyDump.AppendLine(historyLines[i]);
							}
						}
					}
				}
				if (historyDump != null)
				{
					Logger.Log("ShoutBehavior(被动)", historyDump.ToString());
				}
				string persistedHeroHistory = BuildPersistedHeroHistoryContext(data.AgentIndex, inputActionText, resolvedHeroes);
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					Logger.Log("ShoutBehavior(被动)", $"[HistoryBridge] heroAgentIndex={data.AgentIndex} chars={persistedHeroHistory.Length}");
				}
				string privateRecentWindowSection = "";
				string persistedWithoutRecentWindow = "";
				SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
				List<ConversationMessage> persistentMemoryRoleMessages = BuildUncompressedMemoryRoleMessagesForPrompt(data.AgentIndex, resolvedHeroes);
				List<object> messages = BuildStrictSceneMessagesForNpc(data.AgentIndex, layeredPrompt, new string[8] { privateRecentWindowSection, persistedWithoutRecentWindow, roleRuntimeContext, sysPrompt.ToString().Trim(), trustBlock, miscExtrasSection, scenePatienceInstruction, BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock) }, new string[1] { string.IsNullOrWhiteSpace(inputActionText) ? "" : ("【当前触发】\n" + inputActionText.Trim()) }, currentInputAlreadyRecorded: true, persistentHistoryMessages: persistentMemoryRoleMessages);
				Stopwatch swApi = Stopwatch.StartNew();
				string output = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(messages, 5000, promptRetryOnError: true);
				output = LlmVisibleReplyNormalizer.NormalizeComplete(output);
				swApi.Stop();
				bool ok = !string.IsNullOrWhiteSpace(output) && !output.StartsWith("（错误") && !output.StartsWith("（程序错误") && !output.StartsWith("（API请求失败") && !output.StartsWith("（API响应格式错误");
				Logger.Obs("API", "complete", new Dictionary<string, object>
				{
					["mode"] = "shout_passive",
					["ok"] = ok,
					["latencyMs"] = Math.Round(swApi.Elapsed.TotalMilliseconds, 2),
					["resultLen"] = (output ?? "").Length
				});
				Logger.Metric("api.shout_passive", ok, swApi.Elapsed.TotalMilliseconds);

				if (!ok)
				{
					_mainThreadActions.Enqueue(delegate
					{
						InformationManager.DisplayMessage(new InformationMessage("[被动回应失败] " + output, new Color(1f, 0.3f, 0.3f)));
					});
					return "（没说话）";
				}

				return output;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] GetPassiveNpcResponse 异常: " + ex.Message);
			Logger.Metric("api.shout_passive", ok: false);
			return "（没说话）";
		}
	}

	private async Task HandleGroupResponse(string playerText, List<NpcDataPacket> allNpcData, string sceneDesc, NpcDataPacket primaryNpc, string extraFact, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch, SceneShoutConversationScope conversationScope, List<NpcDataPacket> framedNpcData)
	{
		try
		{
			ApplySceneLocalDisambiguatedNames(allNpcData);
			using (Logger.BeginTrace("shout_group", null, primaryNpc?.Name))
			{
				Logger.Obs("ShoutGroup", "start", new Dictionary<string, object>
				{
					["primaryNpc"] = primaryNpc?.Name ?? "",
					["inputLen"] = (playerText ?? "").Length,
					["nearbyNpcCount"] = allNpcData?.Count ?? 0
				});
				bool usePerHeroIndependentRequests = true;
				if (usePerHeroIndependentRequests)
				{
					await HandleGroupResponsePerHeroIndependent(playerText, allNpcData, sceneDesc, primaryNpc, extraFact, precomputedContexts, resolvedHeroes, conversationEpoch, conversationScope, framedNpcData);
					return;
				}
				DuelSettings settings = DuelSettings.GetSettings();
				List<NpcDataPacket> speakingCandidates = BuildGroupSpeakingCandidates(allNpcData, primaryNpc);
				List<string> patienceStatusLines = new List<string>();
				List<NpcDataPacket> speakableCandidates = new List<NpcDataPacket>();
				foreach (NpcDataPacket npc in speakingCandidates)
				{
					if (npc != null)
					{
						bool canSpeak = true;
						string statusLine = "";
						bool hasStatus;
						if (npc.IsHero)
						{
							Hero hero = null;
							if (resolvedHeroes != null) resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
							hasStatus = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out statusLine, out canSpeak);
						}
						else
						{
							hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc.UnnamedKey, npc.Name, GetSceneNpcPatienceNameForPrompt(npc), out statusLine, out canSpeak);
						}
						if (hasStatus && !string.IsNullOrWhiteSpace(statusLine))
						{
							patienceStatusLines.Add(statusLine);
						}
						if (canSpeak)
						{
							speakableCandidates.Add(npc);
						}
					}
				}
				speakingCandidates = speakableCandidates;
				if (speakingCandidates.Count == 0)
				{
					_mainThreadActions.Enqueue(delegate
					{
						AnimusForgeQuickInfo.Show("周围的人明显不想继续聊下去。");
					});
					return;
				}
				await EnsurePersonaForCandidatesAsync(speakingCandidates, resolvedHeroes);
				StringBuilder sysPrompt = new StringBuilder();
				Hero contextHero = null;
				try
				{
					if (primaryNpc != null && primaryNpc.IsHero && resolvedHeroes != null)
					{
						resolvedHeroes.TryGetValue(primaryNpc.AgentIndex, out contextHero);
					}
					if (contextHero == null)
					{
						NpcDataPacket heroNpc = speakingCandidates.FirstOrDefault((NpcDataPacket npcDataPacket) => npcDataPacket?.IsHero ?? false);
						if (heroNpc != null && resolvedHeroes != null)
						{
							resolvedHeroes.TryGetValue(heroNpc.AgentIndex, out contextHero);
						}
					}
				}
				catch
				{
				}
				string cultureId = ((primaryNpc != null) ? primaryNpc.CultureId : "neutral");
				bool hasAnyHero = speakingCandidates.Any((NpcDataPacket npcDataPacket) => npcDataPacket?.IsHero ?? false);
				int contextAgentIndex = ((primaryNpc != null) ? primaryNpc.AgentIndex : ((speakingCandidates.Count > 0) ? speakingCandidates[0].AgentIndex : (-1)));
				Agent contextAgent = ((contextAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == contextAgentIndex) : null);
				CharacterObject contextCharacter = contextAgent?.Character as CharacterObject;
				string contextKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(contextAgent);
				PrecomputedShoutRagContext contextPrecomputed = null;
				bool hasContextPrecomputed = precomputedContexts != null && precomputedContexts.TryGetValue(contextAgentIndex, out contextPrecomputed);
				List<SceneSummonPromptTarget> sceneSummonTargets = BuildSceneSummonPromptTargets(speakingCandidates, resolvedHeroes);
				int sceneGuideFirstPromptId = ((sceneSummonTargets != null && sceneSummonTargets.Count > 0) ? sceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
				List<SceneGuidePromptTarget> sceneGuideTargets = BuildSceneGuidePromptTargets(firstPromptId: sceneGuideFirstPromptId);
				List<string> preprocessExcludedRuleIds = BuildPreprocessExcludedRuleIdsForCurrentInteraction(contextHero, contextCharacter, contextAgentIndex, hasAnyHero, sceneSummonTargets, sceneGuideTargets, primaryNpc, speakingCandidates, playerText);
				int preferredPersistedHistorySourceIndex = -1;
				if (primaryNpc != null && speakingCandidates.Any((NpcDataPacket npcDataPacket) => npcDataPacket != null && npcDataPacket.AgentIndex == primaryNpc.AgentIndex))
				{
					preferredPersistedHistorySourceIndex = primaryNpc.AgentIndex;
				}
				else if (speakingCandidates.Count > 0)
				{
					preferredPersistedHistorySourceIndex = speakingCandidates[0].AgentIndex;
				}
				if (preferredPersistedHistorySourceIndex == -1)
				{
					NpcDataPacket heroNpcForHistory = speakingCandidates.FirstOrDefault((NpcDataPacket x) => x != null && x.IsHero);
					if (heroNpcForHistory != null)
					{
						preferredPersistedHistorySourceIndex = heroNpcForHistory.AgentIndex;
					}
				}
				Task<string> persistedHeroHistoryTask = (preferredPersistedHistorySourceIndex == -1) ? Task.FromResult("") : StartPrecomputedPersistedHistoryContextTask(preferredPersistedHistorySourceIndex, playerText, resolvedHeroes, precomputedContexts, "group_compose");
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(contextHero, playerText, extraFact, cultureId, hasAnyHero, targetCharacter: contextCharacter, kingdomIdOverride: contextKingdomIdOverride, targetAgentIndex: contextAgentIndex, usePrefetchedLoreContext: hasContextPrecomputed && contextPrecomputed != null && contextPrecomputed.HasLoreContext, prefetchedLoreContext: contextPrecomputed?.LoreContext, preprocessExcludedRuleIds: preprocessExcludedRuleIds);
				string presentNpcListBlock = BuildScenePresentNpcListBlockForPrompt(speakingCandidates, primaryNpc, resolvedHeroes);
				if (!string.IsNullOrWhiteSpace(presentNpcListBlock))
				{
					sysPrompt.AppendLine(presentNpcListBlock);
				}
				string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(speakingCandidates);
				string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(primaryNpc);
				string sceneMechanismPromptSection = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, sceneFollowControlInstruction, primaryNpc);
				string scenePatienceInstruction = "";
				GetSceneReplyLengthLimits(settings, out var minTokens, out var maxTokens);
				sysPrompt.AppendLine("【群体对话规则】");
				string playerNameForPrompt = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForPrompt))
				{
					playerNameForPrompt = "玩家";
				}
				Stopwatch swPrompt = Stopwatch.StartNew();
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
				SplitSceneExtraSections(baseExtrasWithoutTrust, out var miscExtrasSection, out var ruleExtrasSection, out var knowledgeExtrasSection);
				bool partyTransferTopicSelected = HasPartyTransferRuleContext(baseExtras);
				string roleTopIntro = BuildSceneSystemTopPromptIntroForGroup(speakingCandidates, resolvedHeroes, partyTransferTopicSelected);
				string roleRuntimeContext = BuildSceneUserRuntimeContextForGroup(speakingCandidates, resolvedHeroes, partyTransferTopicSelected: partyTransferTopicSelected);
				string playerNameForTask = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForTask))
				{
					playerNameForTask = "玩家";
				}
				string taskPreamble = "你是【站在你旁边的人】中的NPC角色,可能是多个人。你们的唯一任务是：根据下方提供的角色信息、场景信息和对话历史，以NPC身份直接回复" + playerNameForTask + "的对话。";
				string taskLength = BuildReplyLengthInstruction(minTokens, maxTokens);
				string systemRuleBlock = BuildSceneSystemRuleBlock(ruleExtrasSection, sceneMechanismPromptSection);
				string layeredPrompt = BuildSceneCompositeUserBlock("", roleTopIntro, taskPreamble, taskLength, ctx?.PreprocessExcludedRuleBlock);
				layeredPrompt = AppendPlayerCustomPromptRuleToSystemPrompt(layeredPrompt);
				swPrompt.Stop();
				int fixedChars = 0;
				int deltaChars = CountPromptChars(layeredPrompt);
				int totalChars = CountPromptChars(layeredPrompt);
				Logger.Log("ShoutBehavior(主动)", $"[PromptLayer] fixedChars={fixedChars} deltaChars={deltaChars} totalChars={totalChars}");
				Logger.Obs("Prompt", "compose", new Dictionary<string, object>
				{
					["mode"] = "shout_group",
					["fixedChars"] = fixedChars,
					["deltaChars"] = deltaChars,
					["totalChars"] = totalChars,
					["buildMs"] = Math.Round(swPrompt.Elapsed.TotalMilliseconds, 2),
					["speakingCandidates"] = speakingCandidates.Count
				});
				Logger.Metric("prompt.compose.shout_group", ok: true, swPrompt.Elapsed.TotalMilliseconds);
				List<object> messages = new List<object>
				{
					new
					{
						role = "system",
						content = layeredPrompt
					}
				};
				int historySourceIndex = -1;
				if (primaryNpc != null && speakingCandidates.Any((NpcDataPacket npcDataPacket) => npcDataPacket != null && npcDataPacket.AgentIndex == primaryNpc.AgentIndex))
				{
					historySourceIndex = primaryNpc.AgentIndex;
				}
				else if (speakingCandidates.Count > 0)
				{
					historySourceIndex = speakingCandidates[0].AgentIndex;
				}
				StringBuilder historyDump = null;
				List<string> historyLines = null;
				if (historySourceIndex != -1)
				{
					lock (_historyLock)
					{
						if (_publicConversationHistory.Count > 0)
						{
							historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, historySourceIndex, (primaryNpc != null) ? GetSceneNpcHistoryNameForPrompt(primaryNpc) : "", useNpcNameAddress: true);
							if (historyLines != null && historyLines.Count > 0)
							{
								historyDump = new StringBuilder();
								historyDump.AppendLine($">>> History(viewerAgentIndex={historySourceIndex}) count={historyLines.Count}");
								for (int i = 0; i < historyLines.Count; i++)
								{
									historyDump.AppendLine(historyLines[i]);
								}
							}
						}
					}
				}
				if (historyDump != null)
				{
					Logger.Log("ShoutBehavior(主动)", historyDump.ToString());
				}
				int persistedHistorySourceIndex = preferredPersistedHistorySourceIndex;
				if (persistedHistorySourceIndex == -1)
				{
					NpcDataPacket npcDataPacket2 = speakingCandidates.FirstOrDefault((NpcDataPacket x) => x != null && x.IsHero);
					if (npcDataPacket2 != null)
					{
						persistedHistorySourceIndex = npcDataPacket2.AgentIndex;
					}
				}
				string persistedHeroHistory = ((persistedHistorySourceIndex == -1) ? "" : await AwaitPrecomputedPersistedHistoryContextAsync(persistedHistorySourceIndex, playerText, resolvedHeroes, precomputedContexts, persistedHeroHistoryTask, "group_compose"));
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					Logger.Log("ShoutBehavior(主动)", $"[HistoryBridge] heroAgentIndex={persistedHistorySourceIndex} chars={persistedHeroHistory.Length}");
				}
				string privateRecentWindowSection = "";
				string persistedWithoutRecentWindow = "";
				SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
				string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
				string sceneHistoryUserBlock = BuildSceneHistoryUserBlock(scenePublicHistorySection, privateRecentWindowSection, persistedWithoutRecentWindow);
				messages[0] = new
				{
					role = "system",
					content = layeredPrompt
				};
				string playerNameForUser = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForUser))
				{
					playerNameForUser = "玩家";
				}
				string currentAfefFactBlock = BuildCurrentAfefFactPromptBlock(extraFact);
				string userMessage = BuildSceneCompositeUserBlock(sceneHistoryUserBlock, roleRuntimeContext, sysPrompt.ToString().Trim(), currentAfefFactBlock, trustBlock, miscExtrasSection, scenePatienceInstruction, knowledgeExtrasSection, systemRuleBlock, "现在请你直接以【站在你旁边的人】中角色的身份回复" + playerNameForUser + "。直接输出对话内容，格式为'角色名: 对话内容'，每人一行。\n禁止生成任何新的【】章节标题、编号规则列表、格式说明或回复要求。禁止替" + playerNameForUser + "说话或编造" + playerNameForUser + "的台词。\n立即开始输出角色对话：");
				messages.Add(new
				{
					role = "user",
					content = userMessage
				});
				Logger.Log("ShoutBehavior(主动)", ">>> Prompt:\n" + layeredPrompt);
				Logger.Log("ShoutBehavior(主动)", ">>> 玩家: " + playerText);
				FloatingTextManager ftm = FloatingTextManager.Instance;
				ftm.BeginStream(speakingCandidates);
				List<NpcDataPacket> capturedAllNpcData = allNpcData;
				List<SceneSummonPromptTarget> capturedSceneSummonTargets = CloneSceneSummonPromptTargets(sceneSummonTargets);
				List<SceneGuidePromptTarget> capturedSceneGuideTargets = CloneSceneGuidePromptTargets(sceneGuideTargets);
				bool hasAnyQueuedLine = false;
				ftm.OnPartialLineUpdated = null;
				ftm.OnNewLineReady = delegate(NpcDataPacket npcDataPacket, string content)
				{
					if (npcDataPacket != null && !string.IsNullOrWhiteSpace(content))
					{
						hasAnyQueuedLine = true;
						EnqueueSpeechLine(npcDataPacket, content.Trim(), capturedAllNpcData, skipHistory: false, suppressStare: false, capturedSceneSummonTargets, capturedSceneGuideTargets);
					}
				};
				bool streamCompleted = false;
				bool streamFailed = false;
				string streamError = null;
				string streamFinalText = null;
				bool firstChunkSeen = false;
				Stopwatch swApi = Stopwatch.StartNew();
				double firstChunkMs = -1.0;
				LlmVisibleReplyNormalizer.StreamFilter visibleReplyFilter = new LlmVisibleReplyNormalizer.StreamFilter();
				await LegacyShoutNetworkGateway.SendLegacyMessagesStreamAsync(messages, 5000, delegate(string delta)
				{
					if (!firstChunkSeen)
					{
						firstChunkSeen = true;
						firstChunkMs = swApi.Elapsed.TotalMilliseconds;
						Logger.Obs("API", "first_chunk", new Dictionary<string, object>
						{
							["mode"] = "shout_group_stream",
							["firstChunkMs"] = Math.Round(firstChunkMs, 2),
							["deltaLen"] = (delta ?? "").Length
						});
					}
					try
					{
						string visibleDelta = visibleReplyFilter.Push(delta);
						if (!string.IsNullOrEmpty(visibleDelta))
						{
							ftm.AppendChunk(visibleDelta);
						}
					}
					catch
					{
					}
				}, delegate(string full)
				{
					string finalDelta = visibleReplyFilter.Complete(full ?? "");
					if (!string.IsNullOrEmpty(finalDelta))
					{
						ftm.AppendChunk(finalDelta);
					}
					streamFinalText = visibleReplyFilter.NormalizedText ?? "";
					streamCompleted = true;
				}, delegate(string err)
				{
					streamError = err ?? "（未知流式错误）";
					streamFailed = true;
				});
				swApi.Stop();
				if (streamFailed)
				{
					Logger.Log("ShoutBehavior(主动)", "<<< 流式错误: " + streamError);
					_mainThreadActions.Enqueue(delegate
					{
						InformationManager.DisplayMessage(new InformationMessage("[场景喊话] " + streamError, new Color(1f, 0.3f, 0.3f)));
					});
				}
				ftm.EndStream();
				ftm.OnPartialLineUpdated = null;
				ftm.OnNewLineReady = null;
				if (streamCompleted)
				{
					Logger.Log("ShoutBehavior(主动)", "<<< AI回复完成(流式):\n" + streamFinalText);
				}
				Logger.Obs("API", "complete", new Dictionary<string, object>
				{
					["mode"] = "shout_group_stream",
					["ok"] = !streamFailed,
					["error"] = streamError ?? "",
					["latencyMs"] = Math.Round(swApi.Elapsed.TotalMilliseconds, 2),
					["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
					["resultLen"] = (streamFinalText ?? "").Length,
					["hasAnyQueuedLine"] = hasAnyQueuedLine
				});
				Logger.Metric("api.shout_group.stream", !streamFailed, swApi.Elapsed.TotalMilliseconds);
				NpcDataPacket fallbackNpc;
				Agent liveAgent;
				CharacterObject co = default(CharacterObject);
				int num;
				string c;
				if (!hasAnyQueuedLine)
				{
					string fullText = streamFinalText ?? "";
					fallbackNpc = null;
					if (primaryNpc != null)
					{
						fallbackNpc = primaryNpc;
					}
					else if (speakingCandidates != null && speakingCandidates.Count > 0)
					{
						fallbackNpc = speakingCandidates[0];
					}
					if (fallbackNpc != null)
					{
						c = StripNpcNamePrefixSafely((fullText ?? "").Trim(), 20);
						if (!IsDetailedSceneSpeechPromptEnabled())
						{
							c = StripStageDirectionsForPassiveShout(c);
						}
						c = (c ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(c))
						{
							liveAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == fallbackNpc.AgentIndex);
							if (liveAgent != null)
							{
								BasicCharacterObject character = liveAgent.Character;
								co = character as CharacterObject;
								if (co != null)
								{
									num = ((co.HeroObject != null) ? 1 : 0);
									goto IL_1cba;
								}
							}
							num = 0;
							goto IL_1cba;
						}
					}
				}
				goto IL_1de3;
				IL_1de3:
				Logger.Obs("ShoutGroup", "done", new Dictionary<string, object>
				{
					["streamFailed"] = streamFailed,
					["hasAnyQueuedLine"] = hasAnyQueuedLine,
					["speakingCandidates"] = speakingCandidates.Count
				});
				goto end_IL_0086;
				IL_1cba:
				if (num != 0)
				{
					MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(co.HeroObject, ref c);
				}
				else
				{
					MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(fallbackNpc.UnnamedKey, fallbackNpc.Name, ref c);
				}
				c = StripLeakedPromptContentForShout(c);
				if (string.IsNullOrWhiteSpace(c))
				{
					return;
				}
				ShowNpcSpeechOutput(fallbackNpc, liveAgent, c);
				RecordResponseForAllNearbySafe(capturedAllNpcData, fallbackNpc.AgentIndex, fallbackNpc.Name, c);
				PersistNpcSpeechToNamedHeroes(fallbackNpc.AgentIndex, fallbackNpc.Name, c, capturedAllNpcData);
				goto IL_1de3;
				end_IL_0086:;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("ShoutBehavior", "[ERROR] " + ex2.Message);
			Logger.Obs("ShoutGroup", "error", new Dictionary<string, object>
			{
				["message"] = ex2.Message,
				["type"] = ex2.GetType().Name
			});
			Logger.Metric("api.shout_group.stream", ok: false);
		}
	}

	private async Task HandleGroupResponsePerHeroIndependent(string playerText, List<NpcDataPacket> allNpcData, string sceneDesc, NpcDataPacket primaryNpc, string extraFact, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch, SceneShoutConversationScope conversationScope, List<NpcDataPacket> framedNpcData)
	{
		long sceneReplyGeneration = SaveRuntimeGuard.CaptureGeneration();
		int sceneReplySessionId = Volatile.Read(ref _sceneHistorySessionId);
		HashSet<int> engagedAgentIndices = new HashSet<int>();
		if (primaryNpc != null && primaryNpc.AgentIndex >= 0)
		{
			engagedAgentIndices.Add(primaryNpc.AgentIndex);
		}
		try
		{
			ApplySceneLocalDisambiguatedNames(allNpcData);
			if (!IsSceneConversationEpochCurrent(conversationEpoch))
			{
				return;
			}
			if (allNpcData == null)
			{
				allNpcData = new List<NpcDataPacket>();
			}
			List<NpcDataPacket> speakableCandidates = BuildGroupSpeakingCandidates(allNpcData, primaryNpc);
			Dictionary<int, NpcDataPacket> audienceByAgentIndex = new Dictionary<int, NpcDataPacket>();
			for (int i = 0; i < speakableCandidates.Count; i++)
			{
				NpcDataPacket npc = speakableCandidates[i];
				if (npc != null && npc.AgentIndex >= 0 && !audienceByAgentIndex.ContainsKey(npc.AgentIndex))
				{
					audienceByAgentIndex.Add(npc.AgentIndex, npc);
				}
			}
			DuelSettings settings = DuelSettings.GetSettings();
			GetSceneReplyLengthLimits(settings, out var minTokens, out var maxTokens);
			string lastSpeakerOutputText = "";
			List<string> roundNpcVisibleTexts = new List<string>();
			HashSet<int> roundNpcSpeakerIndices = new HashSet<int>();
			List<NpcDataPacket> actionPromptParticipants = (framedNpcData != null && framedNpcData.Count > 0) ? framedNpcData : new List<NpcDataPacket> { primaryNpc };
			List<SceneSummonPromptTarget> sceneSummonTargets = BuildSceneSummonPromptTargets(actionPromptParticipants, resolvedHeroes);
			int sceneGuideFirstPromptId = ((sceneSummonTargets != null && sceneSummonTargets.Count > 0) ? sceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
			List<SceneGuidePromptTarget> sceneGuideTargets = BuildSceneGuidePromptTargets(firstPromptId: sceneGuideFirstPromptId);
			string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(actionPromptParticipants);
			string sceneMechanismPromptSectionBase = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, null, primaryNpc);
			bool multiNpcScene = speakableCandidates.Count > 1;
			bool relaySingleFramedNpc = framedNpcData != null && framedNpcData.Count == 1;
			int primaryTargetAgentIndex = primaryNpc?.AgentIndex ?? (-1);
			NpcDataPacket currentSpeaker = await RunNativeConversationMainThreadFuncAsync("scene_primary_first_validate", primaryNpc?.Name, primaryTargetAgentIndex, () => ResolveLiveSceneRelayTarget(conversationScope, audienceByAgentIndex, resolvedHeroes, conversationEpoch, primaryTargetAgentIndex), (NpcDataPacket)null);
			if (currentSpeaker == null)
			{
				List<NpcDataPacket> failedPrimaryParticipants = primaryNpc == null ? new List<NpcDataPacket>() : new List<NpcDataPacket> { primaryNpc };
				await RunNativeConversationMainThreadFuncAsync("scene_primary_first_release", primaryNpc?.Name, primaryTargetAgentIndex, delegate
				{
					if (!SaveRuntimeGuard.IsCurrentGeneration(sceneReplyGeneration)
						|| sceneReplySessionId != Volatile.Read(ref _sceneHistorySessionId)
						|| !IsSceneConversationEpochCurrent(conversationEpoch)) return false;
					ReleaseSceneConversationConstraints(failedPrimaryParticipants, primaryTargetAgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
					return true;
				}, fallback: false);
				QueueSceneInfoMessage("异色主对象当前无法回应", new Color(0.75f, 0.75f, 0.75f), conversationEpoch, AutoGroupRelayNegativeSoundEvent);
				return;
			}
			engagedAgentIndices.Add(currentSpeaker.AgentIndex);
			HashSet<int> personaPreparedAgentIndices = new HashSet<int>();
			bool firstTurn = true;
			bool battleSpeechClaimedRound = false;
			bool useTownResponseBudget = AfGcczShoutBridge.ShouldUseTownNpcResponseBudgetForExternal();
			int remainingTurns = useTownResponseBudget
				? Math.Min(SiegeNpcResponseEventBudget.MaxPendingRequests, speakableCandidates.Count)
				: Math.Min(AUTO_GROUP_CHAT_MAX_LINES, speakableCandidates.Count);
			string responseEventId = "player_utterance:" + conversationEpoch;
			while (currentSpeaker != null && remainingTurns-- > 0)
			{
				Stopwatch turnSw = Stopwatch.StartNew();
				string turnHeroId = "";
				try
				{
					if (currentSpeaker.IsHero && resolvedHeroes != null && resolvedHeroes.TryGetValue(currentSpeaker.AgentIndex, out var turnHero) && turnHero != null)
					{
						turnHeroId = (turnHero.StringId ?? "").Trim();
					}
				}
				catch
				{
					turnHeroId = "";
				}
				Logger.Log("Logic", "[MemoryPerf] group_turn_start agent=" + currentSpeaker.AgentIndex + " npc=" + (GetSceneNpcHistoryNameForPrompt(currentSpeaker) ?? "") + " hero=" + turnHeroId + " multi=" + multiNpcScene + " candidates=" + ((speakableCandidates ?? new List<NpcDataPacket>()).Count) + " firstTurn=" + firstTurn + " remainingAfterDecrement=" + remainingTurns);
				if (!IsSceneConversationEpochCurrent(conversationEpoch))
				{
					return;
				}
				NpcDataPacket validatedCurrentSpeaker = await RunNativeConversationMainThreadFuncAsync("scene_relay_turn_validate", currentSpeaker.Name, currentSpeaker.AgentIndex, () => ResolveLiveSceneRelayTarget(conversationScope, audienceByAgentIndex, resolvedHeroes, conversationEpoch, currentSpeaker.AgentIndex), (NpcDataPacket)null);
				if (validatedCurrentSpeaker == null)
				{
					break;
				}
				currentSpeaker = validatedCurrentSpeaker;
				if (useTownResponseBudget
					&& !AfGcczShoutBridge.TryClaimNpcResponseForExternal(
						responseEventId,
						currentSpeaker.AgentIndex,
						firstTurn ? SiegeNpcResponseEventOrigin.DirectPlayerReply : SiegeNpcResponseEventOrigin.PlayerUtterance,
						Math.Max(0, speakableCandidates.Count - 1),
						pendingRequestCount: 0,
						source: "group_shout_independent",
						out _))
				{
					break;
				}
				engagedAgentIndices.Add(currentSpeaker.AgentIndex);
				if (personaPreparedAgentIndices.Add(currentSpeaker.AgentIndex))
				{
					await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { currentSpeaker }, resolvedHeroes);
				}
				List<NpcDataPacket> engagedParticipants = speakableCandidates.Where((NpcDataPacket npc) => npc != null && engagedAgentIndices.Contains(npc.AgentIndex)).ToList();
				List<Agent> engagedAgents = new List<Agent>(engagedParticipants.Count);
				for (int engagedIndex = 0; engagedIndex < engagedParticipants.Count; engagedIndex++)
				{
					if (conversationScope.TryGetEntry(engagedParticipants[engagedIndex].AgentIndex, out var engagedEntry) && engagedEntry.AgentReference != null)
					{
						engagedAgents.Add(engagedEntry.AgentReference);
					}
				}
				await RunNativeConversationMainThreadFuncAsync("scene_relay_hold_participants", currentSpeaker.Name, currentSpeaker.AgentIndex, delegate
				{
					if (!SaveRuntimeGuard.IsCurrentGeneration(sceneReplyGeneration)
						|| sceneReplySessionId != Volatile.Read(ref _sceneHistorySessionId)
						|| !IsSceneConversationEpochCurrent(conversationEpoch)) return false;
					ActivateMultiSceneMovementSuppression(engagedAgentIndices);
					HoldSceneConversationAgents(engagedAgents);
					return true;
				}, fallback: false);
				SceneRelayEligibilitySnapshot turnEligibility = await RunNativeConversationMainThreadFuncAsync("scene_relay_turn_candidates", currentSpeaker.Name, currentSpeaker.AgentIndex, () => BuildSceneRelayEligibilitySnapshot(conversationScope, audienceByAgentIndex, resolvedHeroes, conversationEpoch), new SceneRelayEligibilitySnapshot());
				List<string> patienceStatusLines = turnEligibility.PatienceStatusLines;
				string cleaned = "";
				Hero speakingHero = null;
				CharacterObject npcCharacter = null;
				string scenePublicHistorySection = "";
				string scenePrivateRecentWindowSection = "";
				bool duelRuleInjected = false;
				bool rewardRuleInjected = false;
				bool loanRuleInjected = false;
				bool kingdomServiceRuleInjected = false;
				bool kingdomVassalageRuleInjected = false;
				bool kingdomAnnexationRuleInjected = false;
				bool lordsHallRuleInjected = false;
				bool meetingReleaseRuleInjected = false;
				bool vanillaIssueRuleInjected = false;
				bool heroJoinPartyRuleInjected = false;
				bool sceneMechanismRuleInjected = false;
				bool genericSceneMechanismRuleInjected = false;
				bool noblePrisonerExecutionRuleInjected = false;
				bool partyTransferRuleInjected = false;
				bool voteDealRuleInjected = false;
				bool customPolicyAgendaRuleInjected = false;
				bool diplomacyRuleInjected = false;
				bool worldMapPartyCommandRuleInjected = false;
				bool siegeInterventionRuleInjected = false;
				string historyFullText = "";
				List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions = null;
				List<PostprocessRuleEntry> kingdomServicePostprocessRules = null;
				List<PostprocessRuleEntry> sceneMechanismPostprocessRules = null;
				List<string> postprocessPreprocessHits = new List<string>();
				string postprocessEntityContext = "";
				bool endRequested = false;
				int resolvedRelayTargetAgentIndex = -1;
				// 多人接力的每一轮都必须走完整的前处理、正文和后处理链路，避免后续 NPC 降级到短请求体。
				if (currentSpeaker != null)
				{
					Stopwatch promptSw = Stopwatch.StartNew();
					Hero contextHero = null;
					if (currentSpeaker.IsHero && resolvedHeroes != null)
					{
						resolvedHeroes.TryGetValue(currentSpeaker.AgentIndex, out contextHero);
					}
					string cultureId = currentSpeaker.CultureId ?? "neutral";
					PrecomputedShoutRagContext precomputed = null;
					bool hasPrecomputed = precomputedContexts != null && precomputedContexts.TryGetValue(currentSpeaker.AgentIndex, out precomputed);
					string loreContext = (hasPrecomputed && precomputed != null) ? (precomputed.LoreContext ?? "") : "";
					string fullExtra = string.IsNullOrWhiteSpace(loreContext) ? null : loreContext;
					if (!string.IsNullOrWhiteSpace(extraFact))
					{
						fullExtra = ((fullExtra == null) ? extraFact : (fullExtra + "\n" + extraFact));
					}
					Agent npcAgent = conversationScope.TryGetEntry(currentSpeaker.AgentIndex, out var currentSpeakerEntry) ? currentSpeakerEntry.AgentReference : null;
					npcCharacter = npcAgent?.Character as CharacterObject;
					speakingHero = npcCharacter?.HeroObject ?? contextHero;
					string npcKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(npcAgent);
					List<string> preprocessExcludedRuleIds = BuildPreprocessExcludedRuleIdsForCurrentInteraction(contextHero, npcCharacter, currentSpeaker.AgentIndex, currentSpeaker.IsHero, sceneSummonTargets, sceneGuideTargets, currentSpeaker, speakableCandidates, playerText);
					Task<string> persistedHeroHistoryTask = StartPrecomputedPersistedHistoryContextTask(currentSpeaker.AgentIndex, playerText, resolvedHeroes, precomputedContexts, "group_turn");
					Stopwatch preprocessSw = Stopwatch.StartNew();
					MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(contextHero, playerText, fullExtra, cultureId, hasAnyHero: currentSpeaker.IsHero, targetCharacter: npcCharacter, kingdomIdOverride: npcKingdomIdOverride, targetAgentIndex: currentSpeaker.AgentIndex, usePrefetchedLoreContext: hasPrecomputed && precomputed != null && precomputed.HasLoreContext, prefetchedLoreContext: precomputed?.LoreContext, preprocessExcludedRuleIds: preprocessExcludedRuleIds);
					preprocessSw.Stop();
					postprocessPreprocessHits = ctx?.PreprocessRuleIds ?? new List<string>();
					postprocessEntityContext = ctx?.EntityPostprocessContext ?? "";
					Logger.Log("Logic", "[MemoryPerf] group_turn_preprocess_done agent=" + currentSpeaker.AgentIndex + " hero=" + (speakingHero?.StringId ?? turnHeroId ?? "") + " ms=" + Math.Round(preprocessSw.Elapsed.TotalMilliseconds, 2) + " hits=" + ((postprocessPreprocessHits == null || postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits)) + " extrasLen=" + ((ctx?.Extras ?? "").Length));
					StringBuilder local = new StringBuilder();
					string presentNpcListBlock = BuildScenePresentNpcListBlockForPrompt(speakableCandidates, currentSpeaker, resolvedHeroes);
					if (!string.IsNullOrWhiteSpace(presentNpcListBlock))
					{
						local.AppendLine(presentNpcListBlock);
					}
					string scenePatienceInstruction = "";
					string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
					string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
					SplitSceneExtraSections(baseExtrasWithoutTrust, out var miscExtrasSection, out var ruleExtrasSection, out var knowledgeExtrasSection);
					string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(currentSpeaker);
					string sceneMechanismPromptSection = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, sceneFollowControlInstruction, currentSpeaker);
					List<string> historyLines = null;
					lock (_historyLock)
					{
						if (_publicConversationHistory.Count > 0)
						{
							historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, currentSpeaker.AgentIndex, GetSceneNpcHistoryNameForPrompt(currentSpeaker), multiNpcScene);
						}
					}
					scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
					string persistedHeroHistory = await AwaitPrecomputedPersistedHistoryContextAsync(currentSpeaker.AgentIndex, playerText, resolvedHeroes, precomputedContexts, persistedHeroHistoryTask, "group_turn");
					string privateRecentWindowSection = "";
					string persistedWithoutRecentWindow = "";
					SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
					scenePrivateRecentWindowSection = privateRecentWindowSection;
					string singleReplyPlayerName = GetPlayerDisplayNameForShout();
					if (string.IsNullOrWhiteSpace(singleReplyPlayerName))
					{
						singleReplyPlayerName = "玩家";
					}
					bool includeInventorySummary = ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext);
					bool includeTradePricing = includeInventorySummary;
					bool partyTransferTopicSelected = HasPartyTransferRuleContext(baseExtras);
					string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(currentSpeaker, contextHero, speakableCandidates, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities);
					string roleRuntimeContext = BuildSceneUserRuntimeContextForSingle(currentSpeaker, contextHero, speakableCandidates, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities);
					string systemRuleBlock = BuildSceneSystemRuleBlock(ruleExtrasSection, sceneMechanismPromptSection);
					string combinedRuleInspectionBlock = BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock);
					string currentAfefFactBlock = BuildCurrentAfefFactPromptBlock(extraFact);
					string sceneDynamicUserBlock = BuildSceneCompositeUserBlock("", roleRuntimeContext, local.ToString().Trim(), currentAfefFactBlock, trustBlock, miscExtrasSection, scenePatienceInstruction);
					duelRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "duel");
					rewardRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "reward");
					loanRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "loan");
					kingdomServiceRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "kingdom_service");
					kingdomVassalageRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "kingdom_vassalage");
					kingdomAnnexationRuleInjected = false;
					lordsHallRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "lords_hall_access");
					meetingReleaseRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "encounter_release_player");
					vanillaIssueRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "vanilla_issue");
					heroJoinPartyRuleInjected = kingdomServiceRuleInjected;
					genericSceneMechanismRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "scene_mechanism_actions");
					noblePrisonerExecutionRuleInjected = firstTurn
						&& HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "noble_prisoner_execution");
					if (!CanUseSceneMechanismPostprocessForSpeaker(currentSpeaker.AgentIndex)
						|| (AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission() && !firstTurn))
					{
						genericSceneMechanismRuleInjected = false;
					}
					sceneMechanismRuleInjected = genericSceneMechanismRuleInjected || noblePrisonerExecutionRuleInjected;
					partyTransferRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "party_transfer");
					voteDealRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "kingdom_agenda");
					customPolicyAgendaRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, CustomPolicyAgendaPostprocessRuleId);
					diplomacyRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "diplomacy");
					worldMapPartyCommandRuleInjected = HasInjectedRuleBlockForPostprocess(combinedRuleInspectionBlock, "worldmap_party_command");
					if (worldMapPartyCommandRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "worldmap_party_command"))
					{
						roleRuntimeContext = AppendPostprocessContextBlockForScene(roleRuntimeContext, WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(speakingHero, npcCharacter, currentSpeaker.AgentIndex));
						sceneDynamicUserBlock = BuildSceneCompositeUserBlock("", roleRuntimeContext, local.ToString().Trim(), currentAfefFactBlock, trustBlock, miscExtrasSection, scenePatienceInstruction);
					}
					siegeInterventionRuleInjected = AfGcczShoutBridge.ShouldRunPostprocessFromPrompt(combinedRuleInspectionBlock, postprocessPreprocessHits);
					bool duelRuleInjectedBeforeEligibility = duelRuleInjected;
					string duelPostprocessBlockedReason = "";
					if (duelRuleInjected && !CanInjectDuelPostprocessRule(ctx, speakingHero, currentSpeaker.AgentIndex, playerText, out duelPostprocessBlockedReason))
					{
						duelRuleInjected = false;
					}
					Logger.Log("ShoutBehavior", "[RuleInjectionDebug] stage=scene_prompt npc=" + GetSceneNpcHistoryNameForPrompt(currentSpeaker) + " hero=" + (speakingHero?.StringId ?? "null") + " character=" + (npcCharacter?.StringId ?? "null") + " ctxDuel=" + ((ctx != null && ctx.UseDuelContext) ? "True" : "False") + " ctxQualified=" + ((ctx != null && ctx.IsQualified) ? "True" : "False") + " ctxReward=" + ((ctx != null && ctx.UseRewardContext) ? "True" : "False") + " ctxLoan=" + ((ctx != null && ctx.IsLoanContext) ? "True" : "False") + " duelInjectedRaw=" + duelRuleInjectedBeforeEligibility + " duelInjected=" + duelRuleInjected + " duelPostprocessBlockedReason=" + (duelPostprocessBlockedReason ?? "") + " rewardInjected=" + rewardRuleInjected + " loanInjected=" + loanRuleInjected + " kingdomServiceInjected=" + kingdomServiceRuleInjected + " kingdomVassalageInjected=" + kingdomVassalageRuleInjected + " kingdomAnnexationInjected=" + kingdomAnnexationRuleInjected + " worldMapInjected=" + worldMapPartyCommandRuleInjected + " siegeInterventionInjected=" + siegeInterventionRuleInjected + " systemRuleLen=" + ((systemRuleBlock ?? "").Length) + " combinedRuleLen=" + ((combinedRuleInspectionBlock ?? "").Length));
					if (kingdomServiceRuleInjected)
					{
						string text31 = (speakingHero?.StringId ?? "").Trim();
						string text32 = (npcCharacter?.StringId ?? speakingHero?.CharacterObject?.StringId ?? "").Trim();
						string text33 = (text32 ?? "").Trim().ToLowerInvariant();
						string text34 = "";
						try
						{
							text34 = (speakingHero?.Clan?.Kingdom?.StringId ?? npcCharacter?.HeroObject?.Clan?.Kingdom?.StringId ?? "").Trim();
						}
						catch
						{
							text34 = "";
						}
						using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
						try
						{
							AIConfigHandler.SetGuardrailRuntimeTargetKingdom(text34);
							AIConfigHandler.SetGuardrailRuntimeTargetHero(text31);
							AIConfigHandler.SetGuardrailRuntimeTargetCharacter(text32);
							AIConfigHandler.SetGuardrailRuntimeTargetTroop(text33);
							kingdomServicePostprocessRules = AIConfigHandler.BuildRuntimeKingdomServicePostprocessRules() ?? new List<PostprocessRuleEntry>();
						}
						finally
						{
							AIConfigHandler.SetGuardrailRuntimeTargetKingdom("");
							AIConfigHandler.SetGuardrailRuntimeTargetHero("");
							AIConfigHandler.SetGuardrailRuntimeTargetCharacter("");
							AIConfigHandler.SetGuardrailRuntimeTargetTroop("");
						}
						Logger.Log("ShoutBehavior", "[KingdomServicePostprocess] foreground_rules=" + ((kingdomServicePostprocessRules.Count == 0) ? "（无）" : string.Join(",", kingdomServicePostprocessRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
					}
					if (sceneMechanismRuleInjected)
					{
						sceneMechanismPostprocessRules = BuildRuntimeSceneMechanismPostprocessRulesForScene(
							currentSpeaker,
							sceneSummonTargets,
							sceneGuideTargets,
							includeGenericRules: genericSceneMechanismRuleInjected);
					}
					string taskSystemBlock = BuildSceneSingleNpcTaskSystemBlock(GetSceneNpcHistoryNameForPrompt(currentSpeaker), multiNpcScene, minTokens, maxTokens, singleReplyPlayerName);
					string layeredPrompt = BuildSceneCompositeUserBlock("", roleTopIntro, taskSystemBlock, ctx?.PreprocessExcludedRuleBlock);
					layeredPrompt = AppendPlayerCustomPromptRuleToSystemPrompt(layeredPrompt);
					if (duelRuleInjected && speakingHero != null && RewardSystemBehavior.Instance != null)
					{
						duelStakeOptions = RewardSystemBehavior.Instance.BuildDuelStakeOptionsForAI(speakingHero);
					}
					Stopwatch uncompressedSw = Stopwatch.StartNew();
					List<ConversationMessage> persistentMemoryRoleMessages = BuildUncompressedMemoryRoleMessagesForPrompt(speakingHero, currentSpeaker.AgentIndex);
					uncompressedSw.Stop();
					Logger.Log("Logic", "[MemoryPerf] group_turn_uncompressed_done agent=" + currentSpeaker.AgentIndex + " hero=" + (speakingHero?.StringId ?? turnHeroId ?? "") + " messages=" + ((persistentMemoryRoleMessages == null) ? 0 : persistentMemoryRoleMessages.Count) + " ms=" + Math.Round(uncompressedSw.Elapsed.TotalMilliseconds, 2));
					List<object> messages = BuildStrictSceneMessagesForNpc(currentSpeaker.AgentIndex, layeredPrompt, new string[4] { privateRecentWindowSection, persistedWithoutRecentWindow, sceneDynamicUserBlock, BuildSceneCompositeUserBlock("", knowledgeExtrasSection, systemRuleBlock) }, persistentHistoryMessages: persistentMemoryRoleMessages);
					promptSw.Stop();
					Logger.Log("Logic", "[MemoryPerf] group_turn_prompt_ready agent=" + currentSpeaker.AgentIndex + " hero=" + (speakingHero?.StringId ?? turnHeroId ?? "") + " messages=" + messages.Count + " persistedChars=" + ((persistedHeroHistory ?? "").Length) + " privateChars=" + ((privateRecentWindowSection ?? "").Length) + " oldCompressedChars=" + ((persistedWithoutRecentWindow ?? "").Length) + " sceneHistoryChars=" + ((scenePublicHistorySection ?? "").Length) + " dynamicChars=" + ((sceneDynamicUserBlock ?? "").Length) + " ruleChars=" + ((systemRuleBlock ?? "").Length) + " promptBuildMs=" + Math.Round(promptSw.Elapsed.TotalMilliseconds, 2));
					Stopwatch apiSw = Stopwatch.StartNew();
					string output = "";
					bool sceneShoutDetachedSubmitted = false;
					if (FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.ConversationGateway))
					{
						try
						{
							InteractionEnvelope envelope = await RunNativeConversationMainThreadFuncAsync(
								"scene_refactor_capture",
								currentSpeaker.Name,
								currentSpeaker.AgentIndex,
								() => LegacyInteractionSnapshotAdapters.CaptureSceneShout(playerText, currentSpeaker.AgentIndex, null, null),
								null).ConfigureAwait(false);
							if (envelope != null)
							{
								// Freeze this speaker's authoritative request; capture above only supplies identity.
								// In particular, current AFEF facts have already been consumed into messages.
								PromptPackage preparedMainPrompt = LegacyConfiguredChatGateway.BuildPromptPackage(messages, 5000, "legacy-scene-shout");
								LegacyInteractionPipelinePorts ports = CreateSceneShoutMainReplyPorts(preparedMainPrompt);
								ILlmGateway gateway = new LegacyShoutNetworkGateway();
								using (LegacyChannelInteractionFacade facade = LegacyInteractionSnapshotAdapters.CreateSceneShoutInteractionFacade(
									ports,
									gateway,
									text => envelope))
								{
									RuntimeConfigSnapshot configuration = CaptureSceneShoutRefactorConfigurationForExternal();
									string moduleId = LegacyInteractionSnapshotAdapters.NativeConversationModuleId;
									string providerId = configuration?.Providers?.Keys?.FirstOrDefault() ?? LegacyInteractionSnapshotAdapters.LegacyShoutNetworkProviderId;
									// The generation helper owns fallback after this boundary, even for an empty reply.
									sceneShoutDetachedSubmitted = true;
									DetachedInteractionHostResult hostResult = await GenerateSceneShoutMainReplyAsync(
										facade,
										configuration,
										moduleId,
										providerId,
										envelope,
										conversationEpoch,
										async () =>
										{
											string legOutput = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(messages, 5000, promptRetryOnError: true).ConfigureAwait(false);
											return LlmVisibleReplyNormalizer.NormalizeComplete(legOutput);
										},
										CancellationToken.None).ConfigureAwait(false);
									if (hostResult?.Status == InteractionStatus.CancelledAsStale)
									{
										return;
									}
									if (hostResult == null || (hostResult.Status != InteractionStatus.Executed && hostResult.Status != InteractionStatus.Succeeded))
									{
										Logger.Log("ShoutBehavior", "[SceneRefactor] terminal reply stopped error=" + (hostResult?.ErrorCode ?? "missing_host_result"));
										QueueSceneInfoMessage("本轮回应已停止，本轮尚未进入动作处理。", new Color(1f, 0.3f, 0.3f), conversationEpoch);
										break;
									}
									output = hostResult.VisibleReply ?? "";
								}
							}
						}
						catch (Exception ex)
						{
							Logger.Log("ShoutBehavior", "[SceneRefactor] cutover error=" + ex.Message);
							if (sceneShoutDetachedSubmitted)
							{
								QueueSceneInfoMessage("本轮回应发生错误，系统已停止后续动作处理。", new Color(1f, 0.3f, 0.3f), conversationEpoch);
								break;
							}
						}
					}
					if (!sceneShoutDetachedSubmitted)
					{
						output = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(messages, 5000, promptRetryOnError: true).ConfigureAwait(false);
						output = LlmVisibleReplyNormalizer.NormalizeComplete(output);
					}
					apiSw.Stop();
					Logger.Log("Logic", "[MemoryPerf] group_turn_api_done agent=" + currentSpeaker.AgentIndex + " hero=" + (speakingHero?.StringId ?? turnHeroId ?? "") + " outputLen=" + ((output ?? "").Length) + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " elapsedMs=" + Math.Round(turnSw.Elapsed.TotalMilliseconds, 2));
					if (!IsSceneConversationEpochCurrent(conversationEpoch))
					{
						return;
					}
					if (!string.IsNullOrWhiteSpace(output) && (output.StartsWith("（错误") || output.StartsWith("（程序错误") || output.StartsWith("（API请求失败") || output.StartsWith("（API响应格式错误")))
					{
						Logger.Log("ShoutBehavior(主动)", "<<< API错误: " + output);
						_mainThreadActions.Enqueue(delegate
						{
							InformationManager.DisplayMessage(new InformationMessage("[场景喊话] " + output, new Color(1f, 0.3f, 0.3f)));
						});
						break;
					}
					historyFullText = StripNpcNamePrefixSafely((output ?? "").Replace("\r", "").Trim(), 30);
					historyFullText = StripLeakedPromptContentForShout(historyFullText);
					cleaned = historyFullText;
					cleaned = StripStageDirectionsForPassiveShout(cleaned);
				}
				else
				{
					historyFullText = await GenerateGroupConversationTurnLineAsync(currentSpeaker, speakableCandidates, resolvedHeroes, precomputedContexts, playerText, extraFact, BuildScenePresentNpcListBlockForPrompt(speakableCandidates, currentSpeaker, resolvedHeroes), sceneSummonTargets, sceneGuideTargets, sceneMechanismPromptSectionBase, patienceStatusLines, multiNpcScene, minTokens, maxTokens);
					cleaned = historyFullText;
					if (!IsSceneConversationEpochCurrent(conversationEpoch))
					{
						return;
					}
				}
				cleaned = StripLeakedPromptContentForShout(cleaned);
				cleaned = StripStageDirectionsForPassiveShout(cleaned);
				endRequested = ContainsAutoGroupEndSignal(cleaned);
				int relayTargetAgentIndex = -1;
				bool relayRequested = false;
				bool relayPostprocessSelected = false;
				List<NpcDataPacket> relayCandidatesForNextTurn = new List<NpcDataPacket>();
				NpcDataPacket nextSpeaker = null;
				Agent currentSpeakerAgent = conversationScope.TryGetEntry(currentSpeaker.AgentIndex, out var postReplySpeakerEntry) ? postReplySpeakerEntry.AgentReference : null;
				bool battleSpeechClaimedReply = BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(currentSpeakerAgent);
				bool battleSpeechPerformanceActive = BattleSpeechApiV1.ShouldSuppressOrdinarySceneFollowups(Mission.Current);
				bool suppressBattleSpeechFollowups = battleSpeechClaimedReply || battleSpeechPerformanceActive;
				battleSpeechClaimedRound |= suppressBattleSpeechFollowups;
				bool flag9 = endRequested && IsAgentFollowingPlayerBySceneCommand(currentSpeakerAgent);
				bool flag10 = endRequested && TryGetSceneSummonConversationSessionForAgentIndex(currentSpeaker.AgentIndex) != null;
				cleaned = PrepareSceneMainReplySpeechText(cleaned, flag9, flag10);
				if (!string.IsNullOrWhiteSpace(cleaned))
				{
					lastSpeakerOutputText = cleaned;
					string historyText = PrepareSceneHistorySpeechText(string.IsNullOrWhiteSpace(historyFullText) ? cleaned : historyFullText);
					if (!string.IsNullOrWhiteSpace(historyText))
					{
						roundNpcVisibleTexts.Add(historyText);
						if (currentSpeaker.AgentIndex >= 0)
						{
							roundNpcSpeakerIndices.Add(currentSpeaker.AgentIndex);
						}
						bool historyRecorded = await RecordSceneReplyHistoryOnMainThreadAsync(
							currentSpeaker, allNpcData, speakingHero, npcCharacter, historyText,
							sceneReplyGeneration, sceneReplySessionId, conversationEpoch).ConfigureAwait(false);
						if (!historyRecorded)
						{
							if (!IsSceneConversationEpochCurrent(conversationEpoch)) return;
							break;
						}
					}
					bool duelPostprocessSelected = duelRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "duel");
					bool rewardPostprocessSelected = rewardRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "reward");
					bool loanPostprocessSelected = loanRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "loan");
					bool kingdomServicePostprocessSelected = kingdomServiceRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "kingdom_service");
					bool kingdomVassalagePostprocessSelected = kingdomVassalageRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "kingdom_vassalage");
					bool kingdomAnnexationPostprocessSelected = false;
					bool lordsHallPostprocessSelected = lordsHallRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "lords_hall_access");
					bool meetingReleasePostprocessSelected = meetingReleaseRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "encounter_release_player");
					bool vanillaIssuePostprocessSelected = vanillaIssueRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "vanilla_issue");
					bool heroJoinPartyPostprocessSelected = kingdomServicePostprocessSelected;
					bool genericSceneMechanismPostprocessSelected = (genericSceneMechanismRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "scene_mechanism_actions"))
						&& CanUseSceneMechanismPostprocessForSpeaker(currentSpeaker.AgentIndex)
						&& (!AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission() || firstTurn);
					bool noblePrisonerExecutionPostprocessSelected = firstTurn && noblePrisonerExecutionRuleInjected;
					bool sceneMechanismPostprocessSelected = genericSceneMechanismPostprocessSelected || noblePrisonerExecutionPostprocessSelected;
					bool partyTransferPostprocessSelected = partyTransferRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "party_transfer");
					bool voteDealPostprocessSelected = voteDealRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "kingdom_agenda");
					bool replyIsDirectPlayerResponse = firstTurn;
					bool customPolicyAgendaPostprocessSelected = replyIsDirectPlayerResponse && (customPolicyAgendaRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, CustomPolicyAgendaPostprocessRuleId));
					bool independentClanPeaceResident = replyIsDirectPlayerResponse
						&& DiplomacyBehavior.CanUseIndependentClanPeaceForExternal(speakingHero, npcCharacter);
					bool diplomacyPostprocessSelected = independentClanPeaceResident
						|| ((diplomacyRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "diplomacy"))
							&& DiplomacyBehavior.CanUseDiplomacyActionPostprocessForExternal(speakingHero, npcCharacter));
					bool worldMapPartyCommandPostprocessSelected = worldMapPartyCommandRuleInjected || HasPreprocessRuleHit(postprocessPreprocessHits, "worldmap_party_command");
					bool nobleGatheringPostprocessSelected = HasPreprocessRuleHit(postprocessPreprocessHits, "noble_gathering");
					bool marriagePostprocessSelected = HasPreprocessRuleHit(postprocessPreprocessHits, "marriage");
					bool proposeAgendaPostprocessSelected = false;
					bool persistentAdpDebtPostprocessSelected = HasPreprocessRuleHit(postprocessPreprocessHits, PersistentAdpDebtPostprocessRuleId);
					bool siegeInterventionPostprocessSelected = AfGcczShoutBridge.ShouldContinuePostprocess(siegeInterventionRuleInjected, postprocessPreprocessHits);
					bool directNoblePrisonerConversation = replyIsDirectPlayerResponse
						&& noblePrisonerExecutionRuleInjected;
					if (directNoblePrisonerConversation)
					{
						siegeInterventionPostprocessSelected = false;
					}
					if (replyIsDirectPlayerResponse && !directNoblePrisonerConversation)
					{
						bool directSceneCommandHandled = await RunNativeConversationMainThreadFuncAsync(
							"scene_direct_command", currentSpeaker.Name, currentSpeaker.AgentIndex, () =>
							{
								if (SaveRuntimeGuard.IsStale(sceneReplyGeneration, "scene_direct_command")
									|| sceneReplySessionId != Volatile.Read(ref _sceneHistorySessionId)
									|| !IsSceneConversationEpochCurrent(conversationEpoch)
									|| !IsNativeConversationResponseTargetAvailableForActionDispatch(currentSpeaker.AgentIndex, speakingHero, npcCharacter, out _))
								{
									return false;
								}
								return AfGcczShoutBridge.TryProcessDirectSceneCommand(currentSpeaker.AgentIndex, playerText, replyIsDirectPlayerResponse, out bool handled) && handled;
							}, false).ConfigureAwait(false);
						if (directSceneCommandHandled)
						{
							siegeInterventionPostprocessSelected = false;
							Logger.Log("ShoutBehavior", "[SceneConversation] GCCZ direct scene command handled npc=" + (speakingHero?.StringId ?? currentSpeaker?.Name ?? "unknown"));
						}
					}
					bool npcSurrenderPostprocessSelected = IsNpcSurrenderPostprocessContext();
					// 场景喊话同样按国王身份常驻排队，不依赖本轮 preprocess hits。
					bool royalPostprocessSelected = AIConfigHandler.CanUseAuxiliaryActionPostprocess()
						&& AIConfigHandler.IsRoyalAbdicationPostprocessTargetForExternal(speakingHero ?? npcCharacter?.HeroObject);
					if (!suppressBattleSpeechFollowups && !endRequested && remainingTurns > 0)
					{
						SceneRelayEligibilitySnapshot nextEligibility = await RunNativeConversationMainThreadFuncAsync("scene_relay_next_candidates", currentSpeaker.Name, currentSpeaker.AgentIndex, () => BuildSceneRelayEligibilitySnapshot(conversationScope, audienceByAgentIndex, resolvedHeroes, conversationEpoch), new SceneRelayEligibilitySnapshot());
						relayCandidatesForNextTurn = nextEligibility.Candidates
							.Where(candidate => candidate != null
								&& candidate.AgentIndex != currentSpeaker.AgentIndex
								&& !roundNpcSpeakerIndices.Contains(candidate.AgentIndex))
							.ToList();
					}
				relayPostprocessSelected = !suppressBattleSpeechFollowups && !endRequested && remainingTurns > 0 && relayCandidatesForNextTurn.Count > 0;
				bool flag11 = BattleSpeechFrameworkV2.ShouldQueueOrdinaryScenePostprocess(suppressBattleSpeechFollowups, duelPostprocessSelected || rewardPostprocessSelected || loanPostprocessSelected || persistentAdpDebtPostprocessSelected || kingdomServicePostprocessSelected || kingdomVassalagePostprocessSelected || kingdomAnnexationPostprocessSelected || lordsHallPostprocessSelected || meetingReleasePostprocessSelected || vanillaIssuePostprocessSelected || heroJoinPartyPostprocessSelected || sceneMechanismPostprocessSelected || partyTransferPostprocessSelected || voteDealPostprocessSelected || customPolicyAgendaPostprocessSelected || diplomacyPostprocessSelected || worldMapPartyCommandPostprocessSelected || nobleGatheringPostprocessSelected || marriagePostprocessSelected || proposeAgendaPostprocessSelected || siegeInterventionPostprocessSelected || npcSurrenderPostprocessSelected || royalPostprocessSelected || relayPostprocessSelected);
				Logger.Log("ShoutBehavior", "[RuleInjectionDebug] stage=scene_queue npc=" + GetSceneNpcHistoryNameForPrompt(currentSpeaker) + " battleSpeechClaimed=" + battleSpeechClaimedReply + " duelInjected=" + duelRuleInjected + " rewardInjected=" + rewardRuleInjected + " loanInjected=" + loanRuleInjected + " persistentAdpDebtSelected=" + persistentAdpDebtPostprocessSelected + " kingdomServiceInjected=" + kingdomServiceRuleInjected + " kingdomVassalageInjected=" + kingdomVassalageRuleInjected + " kingdomAnnexationInjected=" + kingdomAnnexationRuleInjected + " lordsHallInjected=" + lordsHallRuleInjected + " meetingReleaseInjected=" + meetingReleaseRuleInjected + " vanillaIssueInjected=" + vanillaIssueRuleInjected + " heroJoinPartyInjected=" + heroJoinPartyRuleInjected + " sceneMechanismInjected=" + sceneMechanismRuleInjected + " partyTransferInjected=" + partyTransferRuleInjected + " voteDealInjected=" + voteDealRuleInjected + " customPolicyAgendaSelected=" + customPolicyAgendaPostprocessSelected + " diplomacyInjected=" + diplomacyRuleInjected + " independentClanPeaceResident=" + independentClanPeaceResident + " worldMapInjected=" + worldMapPartyCommandRuleInjected + " nobleGatheringSelected=" + nobleGatheringPostprocessSelected + " marriageSelected=" + marriagePostprocessSelected + " proposeAgendaSelected=" + proposeAgendaPostprocessSelected + " siegeInterventionSelected=" + siegeInterventionPostprocessSelected + " npcSurrenderSelected=" + npcSurrenderPostprocessSelected + " royalSelected=" + royalPostprocessSelected + " relaySelected=" + relayPostprocessSelected + " replyIsDirectPlayerResponse=" + replyIsDirectPlayerResponse + " preprocessHits=" + ((postprocessPreprocessHits == null || postprocessPreprocessHits.Count == 0) ? "(none)" : string.Join(",", postprocessPreprocessHits)) + " queueDeferred=" + flag11 + " replyLen=" + cleaned.Length);
					float dynamicTimeoutSeconds = ResolveDynamicSceneConversationTimeoutSeconds(playerText, roundNpcVisibleTexts, roundNpcSpeakerIndices.Count, Math.Max(1, speakableCandidates.Count));
					bool speechQueued = await QueueSceneMainReplyOnMainThreadAsync(
						currentSpeaker, allNpcData, speakingHero, npcCharacter, cleaned,
						sceneReplyGeneration, sceneReplySessionId, conversationEpoch,
						sceneSummonTargets, sceneGuideTargets, flag11,
						remainingTurns > 0 ? (-1f) : dynamicTimeoutSeconds,
						Math.Max(1, engagedAgentIndices.Count), playerText, replyIsDirectPlayerResponse).ConfigureAwait(false);
					if (!speechQueued) break;
					if (flag11)
					{
						string replyForPostprocess = string.IsNullOrWhiteSpace(historyText) ? cleaned : historyText;
						Task<int> postprocessTask = QueueDeferredScenePostprocessActions(currentSpeaker, allNpcData, speakingHero, npcCharacter, scenePrivateRecentWindowSection, scenePublicHistorySection, playerText, replyForPostprocess, duelPostprocessSelected, rewardPostprocessSelected, loanPostprocessSelected, kingdomServicePostprocessSelected, kingdomVassalagePostprocessSelected, kingdomAnnexationPostprocessSelected, lordsHallPostprocessSelected, meetingReleasePostprocessSelected, vanillaIssuePostprocessSelected, heroJoinPartyPostprocessSelected, sceneMechanismPostprocessSelected, partyTransferPostprocessSelected, voteDealPostprocessSelected, diplomacyPostprocessSelected, worldMapPartyCommandPostprocessSelected, marriagePostprocessSelected, siegeInterventionPostprocessSelected, duelStakeOptions, kingdomServicePostprocessRules, sceneMechanismPostprocessRules, conversationEpoch, sceneSummonTargets, sceneGuideTargets, postprocessEntityContext, replyIsDirectPlayerResponse, preprocessRuleHits: postprocessPreprocessHits, relayRuleInjected: relayPostprocessSelected, relayCandidates: relayCandidatesForNextTurn, relayPrimaryTargetAgentIndex: primaryNpc?.AgentIndex ?? (-1), relaySingleFramedNpc: relaySingleFramedNpc, customPolicyAgendaRuleInjected: customPolicyAgendaPostprocessSelected, expectedRuntimeGeneration: sceneReplyGeneration, expectedSceneSessionId: sceneReplySessionId);
						if (relayPostprocessSelected)
						{
							// Release the processing flag while keeping the input gate: queued speech
							// and action-only lines must run before a relay can complete.
							await WaitForScenePostprocessGateAsync("scene_relay").ConfigureAwait(false);
							relayTargetAgentIndex = postprocessTask.IsCompleted ? await postprocessTask.ConfigureAwait(false) : -1;
							if (relayTargetAgentIndex == DeferredPostprocessTargetUnavailableResult)
							{
								Logger.Log("ShoutBehavior", "[SceneRelay] stopped because the current postprocess target became unavailable agent=" + currentSpeaker.AgentIndex);
								relayRequested = false;
							}
							else if (relayTargetAgentIndex < 0)
							{
								QueueSceneInfoMessage("没有人愿意作为下一个发言者", new Color(0.75f, 0.75f, 0.75f), conversationEpoch, AutoGroupRelayNegativeSoundEvent);
								relayRequested = false;
							}
							else if (relayTargetAgentIndex == currentSpeaker.AgentIndex)
							{
								Logger.Log("ShoutBehavior", "[SceneRelay] current speaker selected itself; relay stopped agent=" + currentSpeaker.AgentIndex);
								relayRequested = false;
							}
							else
							{
								relayRequested = relayTargetAgentIndex >= 0;
							}
						}
					}
				}
				else
				{
					lastSpeakerOutputText = "";
				}
				if (!string.IsNullOrWhiteSpace(cleaned) && relayRequested)
				{
					nextSpeaker = await RunNativeConversationMainThreadFuncAsync("scene_relay_next_validate", currentSpeaker.Name, relayTargetAgentIndex, () => ResolveLiveSceneRelayTarget(conversationScope, audienceByAgentIndex, resolvedHeroes, conversationEpoch, relayTargetAgentIndex), (NpcDataPacket)null);
					resolvedRelayTargetAgentIndex = nextSpeaker?.AgentIndex ?? (-1);
				}
			if (relayPostprocessSelected && !string.IsNullOrWhiteSpace(cleaned))
			{
				if (resolvedRelayTargetAgentIndex >= 0)
				{
					QueueSceneInfoMessage(BuildAutoGroupRelayThinkingInfoMessage(nextSpeaker), new Color(1f, 0.95f, 0.25f), conversationEpoch, AutoGroupRelayPositiveSoundEvent);
				}
			}
				if (endRequested || resolvedRelayTargetAgentIndex < 0 || nextSpeaker == null)
				{
					break;
				}
				currentSpeaker = nextSpeaker;
				firstTurn = false;
			}
			if (battleSpeechClaimedRound)
			{
				await RunNativeConversationMainThreadFuncAsync("battle_speech_followup_suppression", primaryNpc?.Name, primaryNpc?.AgentIndex ?? (-1), delegate
				{
					if (!SaveRuntimeGuard.IsCurrentGeneration(sceneReplyGeneration)
						|| sceneReplySessionId != Volatile.Read(ref _sceneHistorySessionId)
						|| !IsSceneConversationEpochCurrent(conversationEpoch)) return false;
					SuppressOrdinarySceneFollowupsForBattleSpeech(speakableCandidates);
					return true;
				}, fallback: false);
			}
			else
			{
				List<NpcDataPacket> finalEngagedParticipants = speakableCandidates.Where((NpcDataPacket npc) => npc != null && engagedAgentIndices.Contains(npc.AgentIndex)).ToList();
				await RunNativeConversationMainThreadFuncAsync("scene_relay_idle_timeout", primaryNpc?.Name, primaryNpc?.AgentIndex ?? (-1), delegate
				{
					if (!SaveRuntimeGuard.IsCurrentGeneration(sceneReplyGeneration)
						|| sceneReplySessionId != Volatile.Read(ref _sceneHistorySessionId)
						|| !IsSceneConversationEpochCurrent(conversationEpoch)) return false;
					PrepareAutoGroupParticipantsForIdleTimeout(finalEngagedParticipants, lastSpeakerOutputText, playerText, roundNpcVisibleTexts, roundNpcSpeakerIndices.Count);
					return true;
				}, fallback: false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] HandleGroupResponsePerHeroIndependent: " + ex.Message);
			if (IsSceneConversationEpochCurrent(conversationEpoch))
			{
				List<NpcDataPacket> failedEngagedParticipants = (allNpcData ?? new List<NpcDataPacket>()).Where((NpcDataPacket npc) => npc != null && engagedAgentIndices.Contains(npc.AgentIndex)).ToList();
				await RunNativeConversationMainThreadFuncAsync("scene_relay_failure_release", primaryNpc?.Name, primaryNpc?.AgentIndex ?? (-1), delegate
				{
					if (!SaveRuntimeGuard.IsCurrentGeneration(sceneReplyGeneration)
						|| sceneReplySessionId != Volatile.Read(ref _sceneHistorySessionId)
						|| !IsSceneConversationEpochCurrent(conversationEpoch)) return false;
					ReleaseSceneConversationConstraints(failedEngagedParticipants, primaryNpc?.AgentIndex ?? (-1), stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
					return true;
				}, fallback: false);
			}
		}
	}

	private bool TryBeginImmediateSceneReactionGeneration(int targetAgentIndex, out long requestId, out string suppressReason)
	{
		requestId = 0L;
		suppressReason = "";
		if (targetAgentIndex < 0)
		{
			suppressReason = "invalid_agent";
			return false;
		}
		float currentTime = 0f;
		try
		{
			currentTime = Mission.Current?.CurrentTime ?? 0f;
		}
		catch
		{
			currentTime = 0f;
		}
		lock (_immediateSceneReactionGateLock)
		{
			if (_immediateSceneReactionActiveRequestIds.ContainsKey(targetAgentIndex))
			{
				suppressReason = "in_flight";
				return false;
			}
			if (_immediateSceneReactionLastStartedMissionTime.TryGetValue(targetAgentIndex, out var lastStarted) && currentTime - lastStarted < ImmediateSceneReactionCooldownSeconds)
			{
				suppressReason = $"cooldown elapsed={Math.Max(0f, currentTime - lastStarted):0.###}s";
				return false;
			}
			requestId = Interlocked.Increment(ref _immediateSceneReactionRequestSequence);
			_immediateSceneReactionActiveRequestIds[targetAgentIndex] = requestId;
			_immediateSceneReactionLastStartedMissionTime[targetAgentIndex] = currentTime;
			return true;
		}
	}

	private bool FinishImmediateSceneReactionGeneration(int targetAgentIndex, long requestId)
	{
		if (targetAgentIndex < 0 || requestId <= 0L)
		{
			return false;
		}
		try
		{
			lock (_immediateSceneReactionGateLock)
			{
				_pendingImmediateSceneReactionRequests.Remove(requestId);
				if (_immediateSceneReactionActiveRequestIds.TryGetValue(targetAgentIndex, out var activeRequestId) && activeRequestId == requestId)
				{
					_immediateSceneReactionActiveRequestIds.Remove(targetAgentIndex);
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private bool RegisterImmediateSceneReactionRequest(ImmediateSceneReactionRequest request)
	{
		if (request == null || request.RequestId <= 0L || request.TargetAgentIndex < 0)
		{
			return false;
		}
		lock (_immediateSceneReactionGateLock)
		{
			if (_immediateSceneReactionActiveRequestIds.TryGetValue(request.TargetAgentIndex, out var activeRequestId) && activeRequestId == request.RequestId)
			{
				_pendingImmediateSceneReactionRequests[request.RequestId] = request;
				return true;
			}
		}
		return false;
	}

	private bool TryTakeImmediateSceneReactionRequest(long requestId, out ImmediateSceneReactionRequest request)
	{
		request = null;
		if (requestId <= 0L)
		{
			return false;
		}
		lock (_immediateSceneReactionGateLock)
		{
			if (!_pendingImmediateSceneReactionRequests.TryGetValue(requestId, out request) || request == null)
			{
				request = null;
				return false;
			}
			_pendingImmediateSceneReactionRequests.Remove(requestId);
			return true;
		}
	}

	private void InvokeImmediateSceneReactionNoSpeechFallback(Action onNoSpeech)
	{
		if (onNoSpeech == null)
		{
			return;
		}
		try
		{
			onNoSpeech();
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] Immediate reaction no-speech fallback failed: " + ex.Message);
		}
	}

	private void QueueImmediateSceneReactionNoSpeechFallback(Action onNoSpeech)
	{
		if (onNoSpeech == null)
		{
			return;
		}
		_mainThreadActions.Enqueue(delegate
		{
			InvokeImmediateSceneReactionNoSpeechFallback(onNoSpeech);
		});
	}

	private void QueueImmediateSceneReactionCompletion(Action<bool> onCompleted, bool generated)
	{
		if (onCompleted == null)
		{
			return;
		}
		_mainThreadActions.Enqueue(delegate
		{
			try
			{
				onCompleted(generated);
			}
			catch (Exception ex)
			{
				Logger.Log("ShoutBehavior", "[WARN] Immediate reaction completion callback failed: " + ex.Message);
			}
		});
	}

	private static bool CanPublishImmediateSceneReaction(Func<bool> canStillPublish)
	{
		if (canStillPublish == null)
		{
			return true;
		}
		try
		{
			return canStillPublish();
		}
		catch
		{
			return false;
		}
	}

	private bool TriggerImmediateSceneBehaviorReaction(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds = -1f, bool skipSceneFactRecord = false, bool returnSceneSummonOnTimeout = false, Action onNoSpeech = null, bool runSiegeReactionPostprocess = false, Func<bool> canStillPublish = null, Action<bool> onCompleted = null)
	{
		if (!IsBannerlordMainThreadForNativeActions())
		{
			_mainThreadActions.Enqueue(delegate
			{
				TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds, skipSceneFactRecord, returnSceneSummonOnTimeout, onNoSpeech, runSiegeReactionPostprocess, canStillPublish, onCompleted);
			});
			return true;
		}
		LlmRetryPrompt.CaptureMainThreadContext();
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (string.IsNullOrWhiteSpace(factText) || targetAgentIndex < 0 || agents == null)
		{
			InvokeImmediateSceneReactionNoSpeechFallback(onNoSpeech);
			QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
			return false;
		}
		bool immediateSceneReactionGateEntered = false;
		long requestId = 0L;
		try
		{
			List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>();
			Agent agent = nearbyNPCAgents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive()) ?? agents.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				InvokeImmediateSceneReactionNoSpeechFallback(onNoSpeech);
				QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
				return false;
			}
			if (!suppressStare)
			{
				ResetStaringForActiveInteraction(nearbyNPCAgents, agent);
			}
			List<NpcDataPacket> list = (from a in nearbyNPCAgents
				select ShoutUtils.ExtractNpcData(a) into d
				where d != null
				select d).ToList();
			NpcDataPacket npcDataPacket = list.FirstOrDefault(d => d != null && d.AgentIndex == targetAgentIndex) ?? ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket == null)
			{
				InvokeImmediateSceneReactionNoSpeechFallback(onNoSpeech);
				QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
				return false;
			}
			if (!TryBeginImmediateSceneReactionGeneration(targetAgentIndex, out requestId, out var suppressReason))
			{
				Logger.Log("ShoutBehavior", $"[ImmediateSceneReaction] skipped targetAgentIndex={targetAgentIndex} reason={suppressReason}");
				InvokeImmediateSceneReactionNoSpeechFallback(onNoSpeech);
				QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
				return false;
			}
			immediateSceneReactionGateEntered = true;
			if (IsAgentHostileToMainAgent(agent))
			{
				_activeInteractionSessions.Remove(targetAgentIndex);
				_pendingInteractionTimeoutArms.Remove(targetAgentIndex);
				RefreshHostileCombatAgentAutonomy(agent);
			}
			else
			{
				TrackPlayerInteraction(npcDataPacket, list?.Count ?? 0, postSpeechLeaveSeconds, returnSceneSummonOnTimeout);
			}
			if (!list.Any(d => d != null && d.AgentIndex == targetAgentIndex))
			{
				list.Insert(0, npcDataPacket);
			}
			if (!skipSceneFactRecord)
			{
				RecordExtraFactToSceneHistory(factText, list);
			}
			if (persistHeroPrivateHistory
				&& agent.Character is CharacterObject characterObject
				&& characterObject.HeroObject != null
				&& AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(characterObject.HeroObject, characterObject, targetAgentIndex))
			{
				MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, factText);
			}
			Dictionary<int, Hero> dictionary = new Dictionary<int, Hero>();
			foreach (NpcDataPacket item in list)
			{
				if (item != null && item.IsHero)
				{
					Hero hero = ResolveHeroFromAgentIndex(item.AgentIndex);
					if (hero != null)
					{
						dictionary[item.AgentIndex] = hero;
					}
				}
			}
			if (!TryPrepareImmediateSceneReactionRequest(mission, requestId, npcDataPacket, list, dictionary, suppressStare, factText, runSiegeReactionPostprocess, canStillPublish, out var request))
			{
				FinishImmediateSceneReactionGeneration(targetAgentIndex, requestId);
				QueueImmediateSceneReactionNoSpeechFallback(onNoSpeech);
				QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
				immediateSceneReactionGateEntered = false;
				return false;
			}
			request.OnNoSpeech = onNoSpeech;
			request.OnCompleted = onCompleted;
			if (!RegisterImmediateSceneReactionRequest(request))
			{
				FinishImmediateSceneReactionGeneration(targetAgentIndex, requestId);
				QueueImmediateSceneReactionNoSpeechFallback(onNoSpeech);
				QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
				immediateSceneReactionGateEntered = false;
				return false;
			}
			StartImmediateSceneReactionBackgroundRequest(request.RequestId, request.TargetAgentIndex, request.Messages, request.MaxTokens, request.Temperature);
			immediateSceneReactionGateEntered = false;
			return true;
		}
		catch (Exception ex)
		{
			if (immediateSceneReactionGateEntered)
			{
				FinishImmediateSceneReactionGeneration(targetAgentIndex, requestId);
				QueueImmediateSceneReactionNoSpeechFallback(onNoSpeech);
				QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
			}
			else
			{
				InvokeImmediateSceneReactionNoSpeechFallback(onNoSpeech);
				QueueImmediateSceneReactionCompletion(onCompleted, generated: false);
			}
			Logger.Log("ShoutBehavior", "[ERROR] TriggerImmediateSceneBehaviorReaction failed: " + ex.Message);
			return false;
		}
	}

	private bool TryPrepareImmediateSceneReactionRequest(Mission sourceMission, long requestId, NpcDataPacket targetNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes, bool suppressStare, string factText, bool runSiegeReactionPostprocess, Func<bool> canStillPublish, out ImmediateSceneReactionRequest request)
	{
		request = null;
		if (targetNpc == null || allNpcData == null || allNpcData.Count == 0)
		{
			return false;
		}
		if (sourceMission == null || !ReferenceEquals(Mission.Current, sourceMission) || !CanPublishImmediateSceneReaction(canStillPublish))
		{
			return false;
		}
		List<NpcDataPacket> list = CloneNpcDataSnapshot(allNpcData);
		ApplySceneLocalDisambiguatedNames(list);
		NpcDataPacket npcDataPacket = list.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == targetNpc.AgentIndex) ?? CloneNpcDataPacket(targetNpc);
		if (npcDataPacket == null)
		{
			return false;
		}
		targetNpc = npcDataPacket;
		allNpcData = list;
		Hero contextHero = null;
		if (targetNpc.IsHero && resolvedHeroes != null)
		{
			resolvedHeroes.TryGetValue(targetNpc.AgentIndex, out contextHero);
		}
		Agent npcAgent = sourceMission.Agents?.FirstOrDefault(a => a != null && a.Index == targetNpc.AgentIndex);
		if (!CanAgentParticipateInSceneSpeech(npcAgent))
		{
			return false;
		}
		CharacterObject npcCharacter = npcAgent.Character as CharacterObject;
		if (contextHero == null)
		{
			contextHero = npcCharacter?.HeroObject;
		}
		PopulateImmediateSceneReactionPersonaOnMainThread(targetNpc, contextHero);
		DuelSettings settings = DuelSettings.GetSettings();
		GetSceneReplyLengthLimits(settings, out var minTokens, out var maxTokens);
		bool useCompactTownOrdinaryChain = runSiegeReactionPostprocess
			&& AfGcczShoutBridge.ShouldUseCompactOrdinaryReaction(contextHero, npcCharacter, targetNpc.AgentIndex);
		if (useCompactTownOrdinaryChain)
		{
			return TryPrepareCompactTownOrdinaryReactionRequest(
				sourceMission,
				requestId,
				targetNpc,
				allNpcData,
				suppressStare,
				factText,
				canStillPublish,
				contextHero,
				npcCharacter,
				minTokens,
				maxTokens,
				out request);
		}
		string npcKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(npcAgent);
		MyBehavior.ShoutPromptContext shoutPromptContext = MyBehavior.BuildShoutPromptContextForExternal(contextHero, "请直接根据刚刚发生的公开互动做出即时反应。", null, targetNpc.CultureId ?? "neutral", hasAnyHero: targetNpc.IsHero, targetCharacter: npcCharacter, kingdomIdOverride: npcKingdomIdOverride, targetAgentIndex: targetNpc.AgentIndex, suppressDynamicRuleAndLore: true);
		StringBuilder stringBuilder = new StringBuilder();
		string presentNpcListBlock = BuildScenePresentNpcListBlockForPrompt(allNpcData, targetNpc);
		if (!string.IsNullOrWhiteSpace(presentNpcListBlock))
		{
			stringBuilder.AppendLine(presentNpcListBlock);
		}
		string baseExtras = StripScenePersonaBlocks((shoutPromptContext?.Extras ?? "").Trim());
		string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
		bool gcczImmediatePromptExtras = HasGcczImmediatePromptExtras(baseExtras);
		SplitSceneExtraSections(baseExtrasWithoutTrust, out var miscExtrasSection, out var ruleExtrasSection, out var knowledgeExtrasSection);
		if (!gcczImmediatePromptExtras)
		{
			miscExtrasSection = "";
			ruleExtrasSection = "";
			knowledgeExtrasSection = "";
		}
		string systemRuleBlock = gcczImmediatePromptExtras ? BuildSceneSystemRuleBlock(ruleExtrasSection, null) : "";
		string gcczIdentityOverrideBlock = BuildGcczImmediateIdentityOverrideBlock(contextHero, npcCharacter, targetNpc.AgentIndex, baseExtras);
		bool partyTransferTopicSelected = HasPartyTransferRuleContext(baseExtras);
		string text = BuildSceneCompositeUserBlock("", stringBuilder.ToString().Trim(), trustBlock, miscExtrasSection);
		text = BuildSceneCompositeUserBlock("", text, factText);
		string persistedHeroHistory = BuildPersistedHeroHistoryContext(targetNpc.AgentIndex, "", resolvedHeroes);
		string privateRecentWindowSection = "";
		string persistedWithoutRecentWindow = "";
		SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
		string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(targetNpc, contextHero, new List<NpcDataPacket> { targetNpc }, partyTransferTopicSelected: partyTransferTopicSelected);
		string roleRuntimeContext = BuildCompactSceneUserRuntimeContextForShortReply(targetNpc, contextHero, new List<NpcDataPacket> { targetNpc }, partyTransferTopicSelected: partyTransferTopicSelected);
		string layeredPrompt = AppendPlayerCustomPromptRuleToSystemPrompt(roleTopIntro);
		layeredPrompt = BuildSceneCompositeUserBlock("", BuildSceneCompositeUserBlock("", gcczIdentityOverrideBlock, systemRuleBlock), layeredPrompt);
		List<ConversationMessage> persistentMemoryRoleMessages = BuildUncompressedMemoryRoleMessagesForPrompt(contextHero, npcCharacter, targetNpc, targetNpc.AgentIndex);
		List<object> messages = BuildStrictSceneMessagesForNpc(targetNpc.AgentIndex, layeredPrompt, new string[3] { privateRecentWindowSection, persistedWithoutRecentWindow, BuildSceneCompositeUserBlock("", roleRuntimeContext, knowledgeExtrasSection, text) }, new string[1] { "请只根据你当前可见的场景消息、你自己的身份、处境和性格，回复一段发言，" + BuildSimpleDialogueReplyLengthInstruction(minTokens, maxTokens) + "，只输出你嘴里说出的话，不要描述你的行为和思考。" }, suppressReplyFormatInstruction: true, persistentHistoryMessages: persistentMemoryRoleMessages);
		request = new ImmediateSceneReactionRequest
		{
			RequestId = requestId,
			RuntimeGeneration = SaveRuntimeGuard.CurrentGeneration,
			SceneHistorySessionId = Volatile.Read(ref _sceneHistorySessionId),
			SourceMission = sourceMission,
			TargetAgentIndex = targetNpc.AgentIndex,
			TargetNpc = targetNpc,
			AllNpcData = allNpcData,
			SuppressStare = suppressStare,
			FactText = factText ?? string.Empty,
			RunSiegeReactionPostprocess = runSiegeReactionPostprocess,
			CanStillPublish = canStillPublish,
			Messages = new List<object>(messages),
			MaxTokens = maxTokens,
			Temperature = 0.35f,
			UsesCompactTownOrdinaryChain = false
		};
		return true;
	}

	private void StartImmediateSceneReactionBackgroundRequest(long requestId, int targetAgentIndex, List<object> messages, int maxTokens, float temperature)
	{
		if (requestId <= 0L || targetAgentIndex < 0 || messages == null || messages.Count == 0)
		{
			CompleteImmediateSceneReactionOnMainThread(requestId, requestSucceeded: false, response: "", error: "invalid_background_request");
			return;
		}
		try
		{
			_ = Task.Run(delegate
			{
				bool requestSucceeded = false;
				string response = "";
				string error = "";
				try
				{
					requestSucceeded = AIConfigHandler.TryCallAuxiliarySimpleDialogue(messages, maxTokens, temperature, out response, out error);
					if (!requestSucceeded)
					{
						Logger.Log("ShoutBehavior", "[ImmediateSceneReaction] auxiliary_simple_dialogue failed: " + (error ?? ""));
					}
				}
				catch (Exception ex)
				{
					error = ex.Message ?? "background_request_exception";
					Logger.Log("ShoutBehavior", "[ERROR] ImmediateSceneReaction background request failed: " + error);
				}
				try
				{
					_mainThreadActions.Enqueue(delegate
					{
						CompleteImmediateSceneReactionOnMainThread(requestId, requestSucceeded, response, error);
					});
				}
				catch (Exception ex2)
				{
					Logger.Log("ShoutBehavior", "[ERROR] ImmediateSceneReaction completion queue failed: " + ex2.Message);
					FinishImmediateSceneReactionGeneration(targetAgentIndex, requestId);
				}
			});
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] ImmediateSceneReaction background start failed: " + ex.Message);
			CompleteImmediateSceneReactionOnMainThread(requestId, requestSucceeded: false, response: "", error: ex.Message);
		}
	}

	private void CompleteImmediateSceneReactionOnMainThread(long requestId, bool requestSucceeded, string response, string error)
	{
		if (!IsBannerlordMainThreadForNativeActions())
		{
			_mainThreadActions.Enqueue(delegate
			{
				CompleteImmediateSceneReactionOnMainThread(requestId, requestSucceeded, response, error);
			});
			return;
		}
		if (!TryTakeImmediateSceneReactionRequest(requestId, out var request) || request == null)
		{
			return;
		}
		if (!IsImmediateSceneReactionRuntimeCurrent(request))
		{
			Logger.Log("ShoutBehavior", "[ImmediateSceneReaction] discarded stale completion request=" + requestId + " targetAgentIndex=" + request.TargetAgentIndex);
			FinishImmediateSceneReactionGeneration(request.TargetAgentIndex, request.RequestId);
			QueueImmediateSceneReactionCompletion(request.OnCompleted, generated: false);
			return;
		}
		bool generated = false;
		try
		{
			if (!requestSucceeded || string.IsNullOrWhiteSpace(response))
			{
				Logger.Log("ShoutBehavior", "[ImmediateSceneReaction] no reply request=" + requestId + " targetAgentIndex=" + request.TargetAgentIndex + " error=" + (error ?? ""));
			}
			else
			{
				generated = TryPublishImmediateSceneReactionOnMainThread(request, response);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] ImmediateSceneReaction main-thread publish failed request=" + requestId + " targetAgentIndex=" + request.TargetAgentIndex + " error=" + ex.Message);
		}
		finally
		{
			FinishImmediateSceneReactionGeneration(request.TargetAgentIndex, request.RequestId);
			if (!generated)
			{
				QueueImmediateSceneReactionNoSpeechFallback(request.OnNoSpeech);
			}
			QueueImmediateSceneReactionCompletion(request.OnCompleted, generated);
		}
	}

	private bool IsImmediateSceneReactionRuntimeCurrent(ImmediateSceneReactionRequest request)
	{
		if (request == null || request.SourceMission == null)
		{
			return false;
		}
		if (!ReferenceEquals(Mission.Current, request.SourceMission))
		{
			return false;
		}
		if (!SaveRuntimeGuard.IsCurrentGeneration(request.RuntimeGeneration))
		{
			return false;
		}
		return true;
	}

	private bool CanPublishImmediateSceneReactionRequest(ImmediateSceneReactionRequest request)
	{
		return IsImmediateSceneReactionRuntimeCurrent(request)
			&& request.SceneHistorySessionId == Volatile.Read(ref _sceneHistorySessionId)
			&& CanPublishImmediateSceneReaction(request.CanStillPublish);
	}

	private bool TryPublishImmediateSceneReactionOnMainThread(ImmediateSceneReactionRequest request, string response)
	{
		if (!CanPublishImmediateSceneReactionRequest(request))
		{
			Logger.Log("ShoutBehavior", "[ImmediateSceneReaction] discarded stale response request=" + request.RequestId + " targetAgentIndex=" + request.TargetAgentIndex);
			return false;
		}
		Agent npcAgent = request.SourceMission.Agents?.FirstOrDefault((Agent agent) => agent != null && agent.Index == request.TargetAgentIndex);
		if (!CanAgentParticipateInSceneSpeech(npcAgent))
		{
			Logger.Log("ShoutBehavior", "[ImmediateSceneReaction] discarded unavailable target request=" + request.RequestId + " targetAgentIndex=" + request.TargetAgentIndex);
			return false;
		}
		CharacterObject npcCharacter = npcAgent.Character as CharacterObject;
		Hero contextHero = npcCharacter?.HeroObject;
		string text = (response ?? "").Replace("\r", "").Trim();
		if (request.RunSiegeReactionPostprocess)
		{
			try
			{
				text = request.UsesCompactTownOrdinaryChain
					? TryRunCompactTownOrdinaryAmbientPostprocess(
						contextHero,
						npcCharacter,
						request.TargetAgentIndex,
						request.FactText ?? string.Empty,
						text)
					: TryRunSceneUnifiedActionPostprocess(
					contextHero,
					npcCharacter,
					request.TargetAgentIndex,
					request.TargetNpc?.Name,
					request.FactText ?? string.Empty,
					request.FactText ?? string.Empty,
					text,
					duelRuleInjected: false,
					rewardRuleInjected: false,
					loanRuleInjected: false,
					kingdomServiceRuleInjected: false,
					kingdomVassalageRuleInjected: false,
					kingdomAnnexationRuleInjected: false,
					lordsHallRuleInjected: false,
					meetingReleaseRuleInjected: false,
					vanillaIssueRuleInjected: false,
					heroJoinPartyRuleInjected: false,
					sceneMechanismRuleInjected: false,
					partyTransferRuleInjected: false,
					voteDealRuleInjected: false,
					diplomacyRuleInjected: false,
					worldMapPartyCommandRuleInjected: false,
					marriageRuleInjected: false,
					duelStakeOptions: null,
					kingdomServiceRules: null,
					sceneMechanismRules: null,
					sceneSummonTargets: null,
					sceneGuideTargets: null,
					siegeInterventionRuleInjected: true,
					replyIsDirectPlayerResponse: false,
					chainName: "siege_ambient_witness_reaction");
				TeamModuleServices.Siege.TryProcessActionTags(
					contextHero,
					npcCharacter,
					request.TargetAgentIndex,
					ref text,
					out _,
					replyIsDirectPlayerResponse: false,
					playerText: request.FactText);
			}
			catch (Exception ex)
			{
				Logger.Log("ShoutBehavior", "[SiegeAmbientWitnessPostprocess] failed: " + ex.Message);
			}
		}
		text = Regex.Replace(text, "\\[(?:ACTION:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|FOL|STP)\\]", "", RegexOptions.IgnoreCase).Trim();
		text = StripNpcNamePrefixSafely(text, 30);
		text = StripLeakedPromptContentForShout(text);
		string fullHistoryText = PrepareSceneHistorySpeechText(text);
		text = StripStageDirectionsForPassiveShout(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!CanPublishImmediateSceneReactionRequest(request))
		{
			Logger.Log("ShoutBehavior", "[ImmediateSceneReaction] discarded stale processed response request=" + request.RequestId + " targetAgentIndex=" + request.TargetAgentIndex);
			return false;
		}
		if (!string.IsNullOrWhiteSpace(fullHistoryText))
		{
			RecordResponseForAllNearbySafe(request.AllNpcData, request.TargetAgentIndex, request.TargetNpc?.Name, fullHistoryText);
			PersistNpcSpeechToNamedHeroes(request.TargetAgentIndex, request.TargetNpc?.Name, fullHistoryText, request.AllNpcData);
		}
		EnqueueSpeechLine(request.TargetNpc, text, request.AllNpcData, skipHistory: true, suppressStare: request.SuppressStare, canStillPublish: request.CanStillPublish);
		return true;
	}

	private void PopulateImmediateSceneReactionPersonaOnMainThread(NpcDataPacket npc, Hero hero)
	{
		if (npc == null)
		{
			return;
		}
		try
		{
			if (npc.IsHero && hero != null)
			{
				MyBehavior.GetNpcPersonaForExternal(hero, out var personality, out var background);
				if (string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background))
				{
					BuildHeroPersonaFallback(hero, out var fallbackPersonality, out var fallbackBackground);
					personality = string.IsNullOrWhiteSpace(personality) ? fallbackPersonality : personality;
					background = string.IsNullOrWhiteSpace(background) ? fallbackBackground : background;
				}
				if (!string.IsNullOrWhiteSpace(personality))
				{
					npc.PersonalityDesc = personality.Trim();
				}
				if (!string.IsNullOrWhiteSpace(background))
				{
					npc.BackgroundDesc = background.Trim();
				}
				return;
			}
			string key = (npc.UnnamedKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(key) && ShoutUtils.TryGetUnnamedPersonaByKey(key, out var unnamedPersonality, out var unnamedBackground))
			{
				if (!string.IsNullOrWhiteSpace(unnamedPersonality))
				{
					npc.PersonalityDesc = unnamedPersonality.Trim();
				}
				if (!string.IsNullOrWhiteSpace(unnamedBackground))
				{
					npc.BackgroundDesc = unnamedBackground.Trim();
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ImmediateSceneReaction] persona snapshot failed agent=" + npc.AgentIndex + " error=" + ex.Message);
		}
	}
}
