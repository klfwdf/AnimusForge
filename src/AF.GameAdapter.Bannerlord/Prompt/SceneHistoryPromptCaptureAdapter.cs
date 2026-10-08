using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using TaleWorlds.Library;
using AnimusForge.SiegeAftermathIntervention;
using NativeConversationAdmission = AnimusForge.ShoutBehavior.NativeConversationAdmission;
using NativeConversationPreparationSnapshot = AnimusForge.ShoutBehavior.NativeConversationPreparationSnapshot;
using SceneSummonPromptTarget = AnimusForge.ShoutBehavior.SceneSummonPromptTarget;
using SceneGuidePromptTarget = AnimusForge.ShoutBehavior.SceneGuidePromptTarget;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
namespace AnimusForge.Refactor.Adapters;
internal sealed class SceneHistoryPromptCaptureAdapter
{
internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(Hero hero, int targetAgentIndex)
	{
		return BuildUncompressedMemoryRoleMessagesForPrompt(hero, null, null, targetAgentIndex);
	}

internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(Hero hero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex)
	{
		try
		{
			if (!AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(hero, targetCharacter, targetAgentIndex))
			{
				return new List<ConversationMessage>();
			}
			if (hero != null)
			{
				return MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(hero, targetAgentIndex, includeCurrentActiveSceneSession: false) ?? new List<ConversationMessage>();
			}
			NpcDataPacket resolvedNpc = npc;
			CharacterObject resolvedCharacter = targetCharacter;
			var agents = Mission.Current?.Agents;
			if ((resolvedNpc == null || resolvedCharacter == null) && targetAgentIndex >= 0 && agents != null)
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
				resolvedCharacter ??= agent?.Character as CharacterObject;
				resolvedNpc ??= ShoutUtils.ExtractNpcData(agent);
			}
			if (resolvedNpc != null && resolvedNpc.AgentIndex < 0 && targetAgentIndex >= 0)
			{
				resolvedNpc.AgentIndex = targetAgentIndex;
			}
			if (SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(resolvedNpc, null, resolvedCharacter, targetAgentIndex, out var memoryId, out var memoryName))
			{
				List<ConversationMessage> messages = MyBehavior.BuildNonHeroUncompressedMemoryRoleMessagesForExternal(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession: false) ?? new List<ConversationMessage>();
				// 读档后原生短期会话历史会清空，非 hero 必须从同一个 af_nonhero 记忆 ID 注入未压缩长期记忆。
				SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=uncompressed_inject agent=" + targetAgentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " messages=" + messages.Count);
				return messages;
			}
			SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=uncompressed_inject agent=" + targetAgentIndex + " memoryId= reason=resolve_failed messages=0 character=" + (resolvedCharacter?.StringId ?? "") + " npc=" + (resolvedNpc?.TroopId ?? resolvedNpc?.UnnamedKey ?? ""));
			return new List<ConversationMessage>();
		}
		catch (Exception ex)
		{
			SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=uncompressed_inject agent=" + targetAgentIndex + " memoryId= reason=exception messages=0 error=" + ex.Message);
			return new List<ConversationMessage>();
		}
	}

internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(int targetAgentIndex, Dictionary<int, Hero> resolvedHeroes)
	{
		try
		{
			Hero hero = null;
			if (resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(targetAgentIndex, out hero);
			}
			var agents = Mission.Current?.Agents;
			if (hero == null && targetAgentIndex >= 0 && agents != null)
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
				hero = (agent?.Character as CharacterObject)?.HeroObject;
				if (hero == null)
				{
					CharacterObject character = agent?.Character as CharacterObject;
					NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
					return BuildUncompressedMemoryRoleMessagesForPrompt(null, character, npc, targetAgentIndex);
				}
			}
			return BuildUncompressedMemoryRoleMessagesForPrompt(hero, targetAgentIndex);
		}
		catch
		{
			return new List<ConversationMessage>();
		}
	}

internal void AppendTargetedScenePlayerFact(string factText, int targetAgentIndex, bool triggerImmediateReaction, float postSpeechLeaveSeconds, Action<string,int,float> triggerReaction)
	{
		if (string.IsNullOrWhiteSpace(factText) || targetAgentIndex < 0)
		{
			return;
		}
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (!SceneShoutInputController.CanAgentParticipateInSceneSpeech(agent))
			{
				return;
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket == null)
			{
				return;
			}
			RecordExtraFactToSceneHistoryCaptured(factText, new List<NpcDataPacket> { npcDataPacket });
			if (triggerImmediateReaction)
			{
				triggerReaction(factText, targetAgentIndex, postSpeechLeaveSeconds);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] AppendTargetedScenePlayerFact failed: " + ex.Message);
		}
	}
internal void AppendTargetedSceneNpcFact(string factText, int targetAgentIndex, bool persistHeroPrivateHistory)
	{
		if (string.IsNullOrWhiteSpace(factText) || targetAgentIndex < 0)
		{
			return;
		}
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (!SceneShoutInputController.CanAgentParticipateInSceneSpeech(agent))
			{
				return;
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket == null)
			{
				return;
			}
			string text = factText.Replace("\r", " ").Replace("\n", " ").Trim();
			if (!text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal) && !text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal))
			{
				text = "[AFEF NPC行为补充] " + text;
			}
			RecordExtraFactToSceneHistoryCaptured(text, new List<NpcDataPacket> { npcDataPacket });
			if (persistHeroPrivateHistory
				&& agent.Character is CharacterObject { HeroObject: not null } characterObject
				&& AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(characterObject.HeroObject, characterObject, targetAgentIndex))
			{
				MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, text);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] AppendTargetedSceneNpcFact failed: " + ex.Message);
		}
	}
internal static List<string> GetAuxiliarySceneDialogueHistoryLinesForExternal(NativeConversationSessionOwner nativeSessions, Func<SceneHistoryPromptCaptureAdapter> currentScene, int targetAgentIndex, int maxLines = 6)
	{
		try
		{
			if (SceneShoutInputController.IsNativeConversationInputOpenForExternal())
			{
				List<string> nativeLines = GetNativeConversationAuxiliaryHistoryLinesForExternal(nativeSessions, currentScene, maxLines);
				if (nativeLines != null && nativeLines.Count > 0)
				{
					return nativeLines;
				}
			}
			return currentScene()?.CaptureAuxiliarySceneDialogueHistoryLines(targetAgentIndex, maxLines) ?? new List<string>();
		}
		catch
		{
			return new List<string>();
		}
	}

internal string BuildAutoGroupLoreContextForSpeaker(NpcDataPacket speakerNpc, Agent speakerAgent, CharacterObject speakerCharacter, Hero speakerHero, string kingdomIdOverride, List<string> visibleHistoryLines)
	{
		try
		{
			string inputText = ScenePromptMessageProjectionComposer.BuildRecentSceneLoreQueryText(visibleHistoryLines);
			string secondaryInput = speakerNpc == null || speakerNpc.AgentIndex < 0 ? "" : _history().LatestNpcUtterance(speakerNpc.AgentIndex);
			if (string.IsNullOrWhiteSpace(inputText))
			{
				inputText = secondaryInput;
				secondaryInput = null;
			}
			if (string.IsNullOrWhiteSpace(inputText))
			{
				return "";
			}
			SceneAgentIdentityPromptCaptureAdapter.LogShoutLorePrequery("auto_group_round", speakerAgent, speakerCharacter, kingdomIdOverride, inputText, secondaryInput);
			if (speakerHero != null)
			{
				return AIConfigHandler.GetLoreContext(inputText, speakerHero, secondaryInput);
			}
			if (speakerCharacter != null)
			{
				return AIConfigHandler.GetLoreContext(inputText, speakerCharacter, kingdomIdOverride, secondaryInput);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[AutoGroupChat] lore query failed: " + ex.Message);
		}
		return "";
	}

internal static string GetLatestNativeConversationNpcUtteranceForExternal(NativeConversationSessionOwner nativeSessions, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			string npcName = targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "";
			if (targetAgentIndex < 0)
			{
				targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			}
			string key = CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return "";
			}
			return nativeSessions.LatestNpcUtterance(key, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
		}
		catch
		{
		}
		return "";
	}
internal static void TryRecordNativeConversationCurrentDialogLine(NativeConversationSessionOwner nativeSessions, Func<SceneHistoryPromptCaptureAdapter> currentScene, Hero targetHero, CharacterObject targetCharacter, string npcName, string npcDisplayName, string currentNativeDialogText)
	{
		try
		{
			string normalizedLine = ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(currentNativeDialogText, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
			if (string.IsNullOrWhiteSpace(normalizedLine))
			{
				return;
			}
			int targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			bool shouldAppend = false;
			shouldAppend = nativeSessions.TryMarkDialog(key, normalizedLine);
			if (shouldAppend)
			{
				AppendNativeConversationSessionHistoryCaptured(nativeSessions, currentScene, targetHero, targetCharacter, npcName, string.IsNullOrWhiteSpace(npcDisplayName) ? npcName : npcDisplayName, normalizedLine, "npc", targetAgentIndex: targetAgentIndex);
			}
		}
		catch
		{
		}
	}
internal static Func<string> CaptureNativeConversationPersistedHistoryWork(NativeConversationSessionOwner nativeSessions, SceneAgentIdentityPromptCaptureAdapter identity, Hero targetHero, CharacterObject targetCharacter, string playerText, string currentNativeDialogText, bool includeCurrentActiveSceneSession, long generation)
    {
        if (!ShoutBehavior.IsBannerlordMainThreadForNativeActions()) return null;
        try
        {
            Hero hero = targetHero ?? targetCharacter?.HeroObject;
            Func<string> work;
            if (hero == null)
            {
                int agentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
                NpcDataPacket npc = identity.BuildNativeConversationNpcData(targetHero, targetCharacter);
                if (npc != null) npc.AgentIndex = agentIndex;
                string secondaryInput = ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(currentNativeDialogText, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
                if (string.IsNullOrWhiteSpace(secondaryInput))
                    secondaryInput = GetLatestNativeConversationNpcUtteranceForExternal(nativeSessions, targetHero, targetCharacter, agentIndex);
                if (!SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var memoryName))
                {
                    SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=history_context_request ok=0 reason=resolve_failed agent=" + agentIndex);
                    return () => "";
                }
                Func<string> captured = MyBehavior.CaptureHistoryContextWorkById(memoryId, memoryName, playerText, secondaryInput, includeCurrentActiveSceneSession, generation);
                if (captured == null) return null;
                work = () =>
                {
                    string context = (captured() ?? "").Trim();
                    SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=history_context_request ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " chars=" + context.Length + " includeCurrentSession=" + includeCurrentActiveSceneSession + " currentInputLen=" + ((playerText ?? "").Length) + " secondaryInputLen=" + ((secondaryInput ?? "").Length));
                    return context;
                };
            }
            else
            {
                string secondaryInput = ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(currentNativeDialogText, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
                if (string.IsNullOrWhiteSpace(secondaryInput))
                    secondaryInput = GetLatestNativeConversationNpcUtteranceForExternal(nativeSessions, targetHero, targetCharacter, SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter));
                work = MyBehavior.CaptureHistoryContextWorkForHero(hero, playerText, secondaryInput, includeCurrentActiveSceneSession, generation);
            }
            if (work == null) return null;
            // Preserve the old history-only failure fallback; the work itself contains no live identity lookup.
            return () => { try { return (work() ?? "").Trim(); } catch { return ""; } };
        }
        catch { return () => ""; }
    }
internal static string BuildWildernessNonHeroHistoryContextForPrompt(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, string currentInput, string secondaryInput, bool includeCurrentActiveSceneSession = false)
	{
		if (!SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var memoryName))
		{
			SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=history_context_request ok=0 reason=resolve_failed agent=" + agentIndex);
			return "";
		}
		string context = (MyBehavior.BuildNonHeroHistoryContextForExternal(memoryId, memoryName, 0, currentInput, secondaryInput, includeCurrentActiveSceneSession) ?? "").Trim();
		SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=history_context_request ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " chars=" + context.Length + " includeCurrentSession=" + includeCurrentActiveSceneSession + " currentInputLen=" + ((currentInput ?? "").Length) + " secondaryInputLen=" + ((secondaryInput ?? "").Length));
		return context;
	}
internal static void AppendWildernessNonHeroMemory(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, string playerText, string aiText, string extraFact, int sceneSessionId = -1)
	{
		if (!SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var memoryName))
		{
			SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=append_request ok=0 reason=resolve_failed agent=" + agentIndex + " playerLen=" + ((playerText ?? "").Length) + " aiLen=" + ((aiText ?? "").Length) + " factLen=" + ((extraFact ?? "").Length) + " sceneSession=" + sceneSessionId);
			return;
		}
		SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace("stage=append_request ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " playerLen=" + ((playerText ?? "").Length) + " aiLen=" + ((aiText ?? "").Length) + " factLen=" + ((extraFact ?? "").Length) + " sceneSession=" + sceneSessionId);
		if (sceneSessionId >= 0)
		{
			MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, playerText, aiText, extraFact, sceneSessionId);
		}
		else
		{
			MyBehavior.AppendExternalNonHeroDialogueHistory(memoryId, memoryName, playerText, aiText, extraFact);
		}
	}
internal static string BuildScenePublicHistorySection(List<string> sceneHistoryLines)
    {
        var lines = ScenePromptMessageProjectionComposer.FilterScenePublicHistoryLines(sceneHistoryLines);
        return ScenePromptMessageProjectionComposer.ComposeScenePublicHistorySection(lines, DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), DuelSettings.DailyConversationHistoryLineLimitMin, DuelSettings.DailyConversationHistoryLineLimitMax);
    }

internal static List<string> BuildNativeConversationUnifiedSceneHistoryLinesForPrompt(NativeConversationSessionOwner nativeSessions, Func<SceneHistoryPromptCaptureAdapter> currentScene, Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0)
	{
		List<string> lines = new List<string>();
		try
		{
			int historyLineLimit = NativeConversationSessionOwner.ResolveHistoryLineLimit(maxLines, DuelSettings.DailyConversationHistoryLineLimitMax, maxLines > 0 ? 0 : DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
			List<ConversationMessage> messages = new List<ConversationMessage>();
			if (targetAgentIndex >= 0 && currentScene() != null)
			{
				SceneHistoryMessageAssemblyOwner.AppendConversationMessages(messages, currentScene().CaptureNpcConversationHistory(targetAgentIndex));
			}
			SceneHistoryMessageAssemblyOwner.AppendConversationMessages(messages, BuildNativeConversationSessionHistoryMessages(nativeSessions, targetHero, targetCharacter, npcName, targetAgentIndex, historyLineLimit));
			messages = SceneHistoryMessageAssemblyOwner.SortConversationMessagesByEventSequence(messages);
			string targetName = (npcName ?? "").Trim();
			HashSet<long> renderedEventSequences = new HashSet<long>();
			for (int i = 0; i < messages.Count; i++)
			{
				ConversationMessage message = messages[i];
				if (message.EventSequence > 0L && !renderedEventSequences.Add(message.EventSequence))
				{
					continue;
				}
				if (HistorySectionProjectionOwner.TryRenderSceneHistoryLine(message, null, out var rendered, targetAgentIndex, targetName, useNpcNameAddress: false, useSceneDistanceSpeechLabels: false, playerName: SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout()))
				{
					string text = (rendered ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						lines.Add(text);
					}
				}
			}
			lines = SceneHistoryMessageAssemblyOwner.KeepAfefFactsAndRecentHistoryLines(lines, Math.Max(DuelSettings.DailyConversationHistoryLineLimitMin, Math.Min(DuelSettings.DailyConversationHistoryLineLimitMax, historyLineLimit)));
		}
		catch
		{
		}
		return lines;
	}
internal static List<string> GetNativeConversationAuxiliaryHistoryLinesForExternal(NativeConversationSessionOwner nativeSessions, Func<SceneHistoryPromptCaptureAdapter> currentScene, int maxLines = 6)
	{
		try
		{
			if (!SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				return new List<string>();
			}
			int targetAgentIndex = SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			List<string> lines = BuildNativeConversationUnifiedSceneHistoryLinesForPrompt(nativeSessions, currentScene, targetHero, targetCharacter, npcName, targetAgentIndex, Math.Max(1, maxLines));
			if (lines.Count <= maxLines)
			{
				return lines;
			}
			return lines.Skip(Math.Max(0, lines.Count - maxLines)).ToList();
		}
		catch
		{
			return new List<string>();
		}
	}
internal static List<ConversationMessage> BuildNativeConversationSessionHistoryMessagesForAgent(NativeConversationSessionOwner nativeSessions, int targetAgentIndex, int maxLines = 0)
	{
		try
		{
			var agents = Mission.Current?.Agents;
			if (targetAgentIndex < 0 || agents == null)
			{
				return new List<ConversationMessage>();
			}
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (agent == null)
			{
				return new List<ConversationMessage>();
			}
			CharacterObject character = agent.Character as CharacterObject;
			Hero hero = character?.HeroObject;
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			string npcName = (npc != null) ? SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(npc) : (hero?.Name?.ToString() ?? character?.Name?.ToString() ?? "");
			return BuildNativeConversationSessionHistoryMessages(nativeSessions, hero, character, npcName, targetAgentIndex, maxLines, npc);
		}
		catch
		{
			return new List<ConversationMessage>();
		}
	}
internal static List<string> BuildNativeConversationSceneHistoryLinesForPrompt(NativeConversationSessionOwner nativeSessions, Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0)
	{
		List<string> lines = new List<string>();
		try
		{
			string targetName = (npcName ?? "").Trim();
			int historyLineLimit = NativeConversationSessionOwner.ResolveHistoryLineLimit(maxLines, DuelSettings.DailyConversationHistoryLineLimitMax, maxLines > 0 ? 0 : DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
			List<ConversationMessage> messages = BuildNativeConversationSessionHistoryMessages(nativeSessions, targetHero, targetCharacter, targetName, targetAgentIndex, historyLineLimit);
			for (int i = 0; i < messages.Count; i++)
			{
				if (HistorySectionProjectionOwner.TryRenderSceneHistoryLine(messages[i], null, out var rendered, targetAgentIndex, targetName, useNpcNameAddress: false, useSceneDistanceSpeechLabels: false, playerName: SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout()))
				{
					string text = (rendered ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						lines.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return lines;
	}
internal static bool HasNativeConversationSceneDialogueHistoryForPrompt(Func<SceneHistoryPromptCaptureAdapter> currentScene, int targetAgentIndex)
	{
		try
		{
			if (currentScene() == null)
			{
				return false;
			}
			List<ConversationMessage> messages = currentScene().CaptureNpcConversationHistory(targetAgentIndex);
			return messages != null && messages.Any(SceneHistoryProjectionOwner.IsSceneConversationTurn);
		}
		catch
		{
			return false;
		}
	}
internal static bool ShouldIncludeCurrentSceneSessionInNativePersistedHistory(Func<SceneHistoryPromptCaptureAdapter> currentScene, int targetAgentIndex)
	{
		try
		{
			if (Mission.Current?.Scene == null || !ShoutUtils.IsInValidScene())
			{
				return false;
			}
			return !HasNativeConversationSceneDialogueHistoryForPrompt(currentScene, targetAgentIndex);
		}
		catch
		{
			return false;
		}
	}
internal static int ClampMemoryPromptHour(int hour)
	{
		if (hour < 0)
		{
			return PersonaIdentityPromptCaptureAdapter.GetCurrentMemoryGameHourForExternal();
		}
		if (hour > 23)
		{
			return 23;
		}
		return hour;
	}
internal static bool TryConvertAfefFactToStrictChatMessage(ConversationMessage msg, int npcAgentIndex, bool isCurrent, out object chatMessage)
	{
		chatMessage = null;
		if (msg == null)
		{
			return false;
		}
		string text = SceneHistoryMessageAssemblyOwner.NormalizeSceneHistoryPromptLineContent(msg.Content);
		if (string.IsNullOrWhiteSpace(text) || !SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(text, out var afefFactLine))
		{
			return false;
		}
		chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", SceneHistoryMessageAssemblyOwner.PrefixConversationMessageForPrompt(CaptureSceneHistoryMessageContext(-1, true), msg, "AFEF", SceneHistoryMessageAssemblyOwner.BuildScopedAfefFactLineForPrompt(afefFactLine, isCurrent)));
		return true;
	}
internal static bool ShouldDeferExtraFactPersistenceUntilAfterSceneReply(string extraFact)
	{
		string text = (extraFact ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.Contains("看了看 总值为")
			&& text.Contains("暂未交付给你")
			&& text.Contains("证明了他有这些东西");
	}
internal static bool IsDetailedSceneSpeechPromptEnabled()
	{
		try
		{
			return DuelSettings.GetSettings()?.UseDetailedSceneSpeechPrompt == true;
		}
		catch
		{
			return false;
		}
	}
internal static bool ShouldPreserveSceneAsteriskActions()
	{
		try
		{
			return DuelSettings.GetSettings()?.PreserveSceneAsteriskActions == true;
		}
		catch
		{
			return false;
		}
	}
internal static string GetStrictScenePlayerDisplayName()
	{
		string text = (SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "玩家" : text;
	}
internal static string BuildStrictSceneMessagesSystemPrompt(string systemPrompt, bool suppressReplyFormatInstruction = false)
	{
		string text = (systemPrompt ?? "").Trim();
		string value = "【messages说明】在下面的对话消息里，assistant 只代表你自己过去说过的话；role=user 里的系统事实和规则必须严格遵守。如果 AFEF 事实前有【当下行为】，表示本轮刚刚真实发生；如果前有【过往行为】，只表示历史上已经发生过，不代表玩家本轮又交付了一次。玩家口头声称给了相同财物，必须以本轮是否存在对应【当下行为】AFEF事实为准。如果附加规则和开头的规则冲突，优先遵循开头的规则";
		bool disableBuiltInReplyFormatPrompt = DuelSettings.IsBuiltInSceneReplyFormatPromptDisabled();
		string instruction = "";
		if (!disableBuiltInReplyFormatPrompt)
		{
			AnimusForge.Refactor.Modules.ScenePromptMessageProjectionComposer.TryExtractReplyFormatInstruction(ref text, out instruction);
		}
		if (suppressReplyFormatInstruction)
		{
			instruction = "";
		}
		string text2 = string.IsNullOrWhiteSpace(instruction) ? value : (value + "\n" + instruction);
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		string text3 = SceneAgentIdentityPromptCaptureAdapter.BuildPlayerCustomPromptRuleBlock();
		if (!string.IsNullOrWhiteSpace(text3) && text.StartsWith(text3, StringComparison.Ordinal))
		{
			return SceneAgentIdentityPromptCaptureAdapter.JoinPromptSections(text3, text2, text.Substring(text3.Length));
		}
		return text2 + "\n" + text;
	}
internal static SceneHistoryMessageContext CaptureSceneHistoryMessageContext(int npcAgentIndex, bool useDistance)
    {
        string date;
        try { date = CampaignTime.Now.ToString(); } catch { date = "当前日期"; }
        return new SceneHistoryMessageContext
        {
            ViewerAgentIndex = npcAgentIndex, ViewerHeroId = SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHeroIdFromAgentIndex(npcAgentIndex),
            PlayerName = GetStrictScenePlayerDisplayName(), GameDate = date,
            GameHour = PersonaIdentityPromptCaptureAdapter.GetCurrentMemoryGameHourForExternal(), Scene = SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel(),
            UseDistanceLabels = useDistance,
            SpeechTextOptions = new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions())
        };
    }
internal List<string> CaptureVisibleSceneHistoryLinesForPrompt(int viewerAgentIndex, string viewerName, bool useDistanceLabels)
    {
        lock (_history().Gate)
        {
            if (_history().PublicCount == 0) return null;
            return _history().CaptureVisiblePublicLines(viewerAgentIndex, SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHeroIdFromAgentIndex(viewerAgentIndex),
                viewerName, useDistanceLabels, SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout(), DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
        }
    }
internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc(int npcAgentIndex, string systemPrompt, IEnumerable<string> prefixUserSections, IEnumerable<string> suffixUserSections = null, bool currentInputAlreadyRecorded = true, string currentPlayerInput = null, int maxHistoryMessages = 0, bool suppressReplyFormatInstruction = false, IEnumerable<ConversationMessage> injectedHistoryMessages = null, bool includeSceneHistory = true, IEnumerable<ConversationMessage> persistentHistoryMessages = null, IEnumerable<ConversationMessage> pendingCurrentAfefFactMessages = null, bool useSceneDistanceSpeechLabels = true)
	{

        int limit = NativeConversationSessionOwner.ResolveHistoryLineLimit(maxHistoryMessages, DuelSettings.DailyConversationHistoryLineLimitMax, maxHistoryMessages > 0 ? 0 : DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
        string strictSystem = BuildStrictSceneMessagesSystemPrompt(systemPrompt, suppressReplyFormatInstruction);
        var pending = new List<ConversationMessage>();
        SceneHistoryMessageAssemblyOwner.AppendConversationMessages(pending, pendingCurrentAfefFactMessages);
        pending.AddRange(_pendingFacts.Consume(npcAgentIndex));
        ConversationMessage current = null;
        if (!currentInputAlreadyRecorded && !string.IsNullOrWhiteSpace(currentPlayerInput))
            current = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
            {
                Role = "user", Content = currentPlayerInput.Trim(), SpeakerName = GetStrictScenePlayerDisplayName(),
                SpeakerAgentIndex = -1, TargetAgentIndex = npcAgentIndex,
                PlayerDistanceMeters = SceneAgentIdentityPromptCaptureAdapter.GetPlayerDistanceToAgentForScenePrompt(npcAgentIndex)
            });
        return new SceneHistoryMessageAssemblyInput
        {
            SystemPrompt = strictSystem,
            PrefixUserSections = prefixUserSections?.ToArray(), SuffixUserSections = suffixUserSections?.ToArray(),
            PendingCurrentFacts = SceneHistoryProjectionOwner.CapturePromptMessages(pending), SceneHistory = includeSceneHistory ? SceneHistoryProjectionOwner.CapturePromptMessages(CaptureNpcConversationHistory(npcAgentIndex)) : null,
            InjectedHistory = SceneHistoryProjectionOwner.CapturePromptMessages(injectedHistoryMessages), PersistentHistory = SceneHistoryProjectionOwner.CapturePromptMessages(persistentHistoryMessages),
            CurrentInputMessage = current, HistoryLineLimit = limit,
            Context = CaptureSceneHistoryMessageContext(npcAgentIndex, useSceneDistanceSpeechLabels)
        };
	}
internal static string BuildPlayerMarriageFactForNpcListLine(Hero npcHero)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (npcHero == null || mainHero == null || npcHero == mainHero)
			{
				return "";
			}
			if (npcHero.Spouse == mainHero || mainHero.Spouse == npcHero)
			{
				string text = (npcHero.IsFemale ? "丈夫" : "妻子");
				string text2 = SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = "玩家";
				}
				return " | 与" + text2 + "的关系: 配偶（" + text2 + "是其" + text + "）";
			}
		}
		catch
		{
		}
		return "";
	}

    private readonly Func<SceneConversationHistoryOwner> _history;
    private readonly ScenePendingAfefFactsOwner _pendingFacts;
    private readonly NativeConversationSessionOwner _nativeSessions;
    private readonly Action<List<NpcDataPacket>> _firstMeeting, _revisit;
    private readonly Func<bool,bool> _flushDeferred;
    private readonly Func<SceneHistoryPromptCaptureAdapter> _ownCapture;
    internal SceneHistoryPromptCaptureAdapter(Func<SceneConversationHistoryOwner> history, ScenePendingAfefFactsOwner pendingFacts, NativeConversationSessionOwner nativeSessions,
        Action<List<NpcDataPacket>> firstMeeting = null, Action<List<NpcDataPacket>> revisit = null, Func<bool,bool> flushDeferred = null, PersistedHistoryCapturePorts persisted = null)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _pendingFacts = pendingFacts ?? throw new ArgumentNullException(nameof(pendingFacts));
        _nativeSessions = nativeSessions ?? throw new ArgumentNullException(nameof(nativeSessions));
        _persisted=persisted; _firstMeeting=firstMeeting; _revisit=revisit; _flushDeferred=flushDeferred; _ownCapture=()=>this;
    }
	internal static ConversationMessage StampConversationMessageWithCurrentMemoryContext(ConversationMessage message)
	{
		if (message == null)
		{
			return null;
		}
		if (message.EventSequence <= 0L)
		{
			message.EventSequence = SceneConversationHistoryOwner.NextEventSequence();
		}
		if (message.GameDayIndex < 0)
		{
			try
			{
				message.GameDayIndex = (int)CampaignTime.Now.ToDays;
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(message.GameDate))
		{
			try
			{
				message.GameDate = CampaignTime.Now.ToString();
			}
			catch
			{
			}
		}
		if (message.GameHour < 0)
		{
			message.GameHour = PersonaIdentityPromptCaptureAdapter.GetCurrentMemoryGameHourForExternal();
		}
		if (string.IsNullOrWhiteSpace(message.Scene))
		{
			message.Scene = SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel();
		}
		SceneAgentIdentityPromptCaptureAdapter.FillSceneMessageHeroIdentity(message);
		return message;
	}
internal void PromotePersonalizedExtraFactInScenePrivateHistoryCaptured(
		string personalizedExtraFact,
		int personalizedAgentIndex)
	{
		if (personalizedAgentIndex < 0
			|| !ScenePromptMessageProjectionComposer.ContainsPlayerCraftedAfefInspectionSuffix(personalizedExtraFact))
		{
			return;
		}
		string personalizedLine = ScenePromptMessageProjectionComposer.NormalizeSceneExtraFactForHistory(personalizedExtraFact);
		string sharedLine = ScenePromptMessageProjectionComposer.NormalizeSceneExtraFactForHistory(
			ScenePromptMessageProjectionComposer.StripPlayerCraftedAfefInspectionSuffix(personalizedExtraFact));
		if (string.IsNullOrWhiteSpace(personalizedLine)
			|| string.IsNullOrWhiteSpace(sharedLine)
			|| string.Equals(personalizedLine, sharedLine, StringComparison.Ordinal))
		{
			return;
		}
		lock (_history().Gate)
		{
            if (!_history().CapturePrivateFactForPromotion(personalizedAgentIndex, sharedLine, out int index, out ConversationMessage message)) return;
            message.Content = personalizedLine;
            message.TargetAgentIndex = personalizedAgentIndex;
            message.TargetHeroId = "";
            message.VisibleAgentIndices = new List<int> { personalizedAgentIndex };
            message.VisibleHeroIds = new List<string>();
            SceneAgentIdentityPromptCaptureAdapter.FillSceneMessageHeroIdentity(message);
            _history().ApplyPrivateFactPromotion(personalizedAgentIndex, index, sharedLine, message);
		}
	}
	internal bool PersistExtraFactToNamedHeroes(
		string extraFact,
		List<NpcDataPacket> nearbyData,
		int personalizedAgentIndex = -1,
		bool requireMemoryReceipt = false)
	{
		if (string.IsNullOrWhiteSpace(extraFact) || nearbyData == null)
		{
			return true;
		}
		string sharedExtraFact = personalizedAgentIndex >= 0
			? ScenePromptMessageProjectionComposer.StripPlayerCraftedAfefInspectionSuffix(extraFact)
			: extraFact;
		HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool accepted = true;
		if (personalizedAgentIndex >= 0)
		{
			NpcDataPacket personalizedTarget = nearbyData.FirstOrDefault(
				npc => npc != null && npc.AgentIndex == personalizedAgentIndex);
			accepted &= PersistExtraFactToNamedObserver(
				personalizedTarget,
				extraFact,
				persistedNonHeroIds,
				requireMemoryReceipt);
		}
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum == null || nearbyDatum.AgentIndex == personalizedAgentIndex)
			{
				continue;
			}
			accepted &= PersistExtraFactToNamedObserver(
				nearbyDatum,
				sharedExtraFact,
				persistedNonHeroIds,
				requireMemoryReceipt);
		}
		return accepted;
	}
	internal bool PersistExtraFactToNamedObserver(
		NpcDataPacket observer,
		string extraFact,
		HashSet<string> persistedNonHeroIds,
		bool requireMemoryReceipt)
	{
		if (observer == null || string.IsNullOrWhiteSpace(extraFact))
		{
			return true;
		}
		if (observer.IsHero)
		{
			Hero hero = SceneAgentIdentityPromptCaptureAdapter.ResolveHeroFromAgentIndex(observer.AgentIndex);
			if (hero != null)
			{
				if (requireMemoryReceipt)
					return MyBehavior.CommitDialogueHistoryWithScene(hero.StringId, false, hero.Name?.ToString(), null, null, extraFact, SceneConversationHistoryOwner.SessionId)?.HistoryWritten == true;
				MyBehavior.AppendExternalSceneDialogueHistory(
					hero,
					null,
					null,
					extraFact,
					SceneConversationHistoryOwner.SessionId);
			}
			return true;
		}
		if (SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(
			observer,
			null,
			null,
			observer.AgentIndex,
			out var memoryId,
			out var memoryName)
			&& (persistedNonHeroIds == null || persistedNonHeroIds.Add(memoryId)))
		{
			if (requireMemoryReceipt)
				return MyBehavior.CommitDialogueHistoryWithScene(memoryId, true, memoryName, null, null, extraFact, SceneConversationHistoryOwner.SessionId)?.HistoryWritten == true;
			MyBehavior.AppendExternalNonHeroSceneDialogueHistory(
				memoryId,
				memoryName,
				null,
				null,
				extraFact,
				SceneConversationHistoryOwner.SessionId);
		}
		return true;
	}
	internal void QueuePendingCurrentAfefFactForAgent(int targetAgentIndex, string fact)
	{
		if (targetAgentIndex < 0)
		{
			return;
		}
		string factLine = SceneHistoryMessageAssemblyOwner.NormalizeAfefFactLineForPrompt(fact);
		if (string.IsNullOrWhiteSpace(factLine))
		{
			return;
		}
		ConversationMessage message = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			Role = "system",
			Content = factLine,
			SpeakerName = "绯荤粺",
			SpeakerAgentIndex = -1,
			VisibleAgentIndices = new List<int> { targetAgentIndex }
		});
		_pendingFacts.Queue(targetAgentIndex, message);
	}
	internal void QueuePendingCurrentNativeAfefFactForKey(string key, string fact)
	{
		key = (key ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		string factLine = SceneHistoryMessageAssemblyOwner.NormalizeAfefFactLineForPrompt(fact);
		if (string.IsNullOrWhiteSpace(factLine))
		{
			return;
		}
		ConversationMessage message = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			Role = "system",
			Content = factLine,
			SpeakerName = "绯荤粺",
			SpeakerAgentIndex = -1
		});
		_nativeSessions.QueueFact(key, message);
	}

    internal static string CaptureNativeConversationNonHeroUnnamedKey(CharacterObject character, string npcName, int agentIndex, Func<int, MobileParty> resolveParty)
    {
        try
        {
            if (character?.HeroObject != null) return NativeHistoryIdentityProjectionOwner.BuildUnnamedKey(true, "", "", false, "", "", "");
            string troop = character?.StringId;
            string faction = "", leader = "";
            try
            {
                MobileParty party = resolveParty(agentIndex);
                if (party != null && party != MobileParty.MainParty) { faction = party.MapFaction?.StringId; leader = party.LeaderHero?.StringId; }
            }
            catch { faction = ""; leader = ""; }
            bool soldier = false; string culture = "", name = "";
            if (string.IsNullOrWhiteSpace(NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(troop)))
            {
                culture = character?.Culture?.StringId;
                try { soldier = character != null && character.IsSoldier; } catch { }
                name = npcName ?? character?.Name?.ToString() ?? "npc";
            }
            return NativeHistoryIdentityProjectionOwner.BuildUnnamedKey(false, troop, culture, soldier, name, faction, leader);
        }
        catch { return ""; }
    }
    internal static string CaptureNativeConversationHistoryKey(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null)
    {
        var input = new NativeHistoryIdentitySnapshot { AgentIndex = targetAgentIndex, NpcName = npcName };
        try
        {
            if (targetAgentIndex < 0 && npc?.AgentIndex >= 0) targetAgentIndex = npc.AgentIndex;
            input.AgentIndex = targetAgentIndex;
            Hero hero = targetHero ?? targetCharacter?.HeroObject;
            CharacterObject character = targetCharacter;
            var agents = Mission.Current?.Agents;
            if ((hero == null || character == null) && targetAgentIndex >= 0 && agents != null)
            {
                Agent agent = agents.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
                CharacterObject agentCharacter = agent?.Character as CharacterObject;
                if (character == null) character = agentCharacter;
                hero ??= agentCharacter?.HeroObject;
                npc ??= ShoutUtils.ExtractNpcData(agent);
            }
            if (hero != null) { input.HasHero = true; input.HeroId = hero.StringId; }
            else if (SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(npc, null, character, targetAgentIndex, out var memoryId, out _))
            { input.HasWildernessMemory = true; input.WildernessMemoryId = memoryId; }
            else
            {
                input.UnnamedKey = npc != null && !npc.IsHero ? (npc.UnnamedKey ?? "").Trim() : "";
                if (string.IsNullOrWhiteSpace(input.UnnamedKey)) input.UnnamedKey = CaptureNativeConversationNonHeroUnnamedKey(character, npcName, targetAgentIndex, SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMobileParty);
                input.CharacterId = character?.StringId;
            }
        }
        catch
        {
            input.CaptureFailed = true;
            input.FailureHeroId = targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "";
            input.FailureKey = targetCharacter?.StringId ?? npcName ?? "";
        }
        return NativeHistoryIdentityProjectionOwner.BuildKey(input);
    }
internal void RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(long eventSequence)
	{
		if (eventSequence <= 0L)
		{
			return;
		}
		try
		{
			lock (_history().Gate)
			{
				_history().RollbackPlayerEvent(eventSequence);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] scene-history rollback failed: " + ex.Message);
		}
	}
	internal static List<int> BuildVisibleAgentSnapshot(List<NpcDataPacket> nearbyData)
	{
		List<int> list = new List<int>();
		if (nearbyData == null || nearbyData.Count == 0)
		{
			return list;
		}
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < nearbyData.Count; i++)
		{
			NpcDataPacket npcDataPacket = nearbyData[i];
			if (npcDataPacket != null && npcDataPacket.AgentIndex >= 0 && hashSet.Add(npcDataPacket.AgentIndex))
			{
				list.Add(npcDataPacket.AgentIndex);
			}
		}
		return list;
	}
    internal static List<string> CaptureAndBuildVisibleSceneHistoryLines(List<ConversationMessage> history, int viewerAgentIndex, string targetNpcName = "", bool useNpcNameAddress = false)
    {
        if (history == null || history.Count == 0) return null;
        return SceneConversationHistoryOwner.BuildVisibleSceneHistoryLines(history, viewerAgentIndex, targetNpcName, useNpcNameAddress,
            SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHeroIdFromAgentIndex(viewerAgentIndex), SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout(), DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
    }
    internal List<ConversationMessage> CaptureNpcConversationHistory(int npcAgentIndex)
    {
        lock (_history().Gate)
        {
            return _history().CaptureNpc(npcAgentIndex, SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHeroIdFromAgentIndex(npcAgentIndex),
                LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions());
        }
    }
    internal List<string> CaptureAuxiliarySceneDialogueHistoryLines(int targetAgentIndex, int maxLines)
    {
        if (targetAgentIndex < 0 || maxLines <= 0) return new List<string>();
        lock (_history().Gate)
        {
            if (_history().PublicCount == 0) return new List<string>();
            return _history().CaptureVisiblePublicLines(targetAgentIndex, SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHeroIdFromAgentIndex(targetAgentIndex), "", false,
                SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout(), DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), maxLines) ?? new List<string>();
        }
    }
internal void AppendNativeConversationSessionLineToSceneHistoryCaptured(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, int targetAgentIndexOverride = -1, NpcDataPacket targetNpc = null, int playerTargetAgentIndexOverride = -1, string playerTargetNameOverride = null)
	{
		text = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			int targetAgentIndex = targetAgentIndexOverride >= 0 ? targetAgentIndexOverride : SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			if (targetAgentIndex < 0)
			{
				return;
			}
			string targetName = (npcName ?? "").Trim();
			try
			{
				NpcDataPacket npc = targetNpc;
				if (npc == null)
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
					npc = ShoutUtils.ExtractNpcData(agent);
				}
				string sceneName = (npc != null) ? SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(npc) : "";
				if (!string.IsNullOrWhiteSpace(sceneName))
				{
					targetName = sceneName;
				}
			}
			catch
			{
			}
			if (string.IsNullOrWhiteSpace(targetName))
			{
				targetName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			}
			string normalizedKind = (kind ?? "").Trim().ToLowerInvariant();
			int actualPlayerTargetAgentIndex = playerTargetAgentIndexOverride >= 0 ? playerTargetAgentIndexOverride : targetAgentIndex;
			string actualPlayerTargetName = (playerTargetNameOverride ?? "").Trim();
			if (string.IsNullOrWhiteSpace(actualPlayerTargetName))
			{
				actualPlayerTargetName = targetName;
			}
            float distance = normalizedKind == "npc" || normalizedKind == "fact" ? -1f : SceneAgentIdentityPromptCaptureAdapter.GetPlayerDistanceToAgentForScenePrompt(actualPlayerTargetAgentIndex);
            ConversationMessage message = StampConversationMessageWithCurrentMemoryContext(SceneHistoryProjectionOwner.BuildNativeSceneBridgeMessage(
                text, normalizedKind, speaker, targetName, targetAgentIndex, eventSequence, actualPlayerTargetAgentIndex, actualPlayerTargetName, distance,
                LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions()));
			if (string.IsNullOrWhiteSpace(message.Content))
			{
				return;
			}
			lock (_history().Gate)
			{
				_history().AppendPublic(message);
				_history().AppendNpc(targetAgentIndex, message);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] scene-history bridge failed: " + ex.Message);
		}
	}
    internal static void AppendNativeConversationSessionHistoryCaptured(NativeConversationSessionOwner nativeSessions, Func<SceneHistoryPromptCaptureAdapter> currentSceneCapture, Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, bool bridgeToSceneHistory = true, int targetAgentIndex = -1, NpcDataPacket npc = null, int playerTargetAgentIndex = -1, string playerTargetName = null, string capturedHistoryKey = null)
    {
        text = (text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            string key = capturedHistoryKey ?? CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
            if (string.IsNullOrWhiteSpace(key)) return;
            int day = 0; string date = ""; int hour = -1; string scene = "";
            try { day = (int)CampaignTime.Now.ToDays; date = CampaignTime.Now.ToString(); hour = PersonaIdentityPromptCaptureAdapter.GetCurrentMemoryGameHourForExternal(); scene = SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel(); } catch { }
            if (eventSequence <= 0L) eventSequence = SceneConversationHistoryOwner.NextEventSequence();
            nativeSessions.Append(key, NativeHistoryIdentityProjectionOwner.BuildEntry(eventSequence, day, date, hour, scene, npcName, speaker, text, kind, targetAgentIndex, playerTargetAgentIndex, playerTargetName));
            if (bridgeToSceneHistory)
                currentSceneCapture()?.AppendNativeConversationSessionLineToSceneHistoryCaptured(targetHero, targetCharacter, npcName, speaker, text, kind, eventSequence, targetAgentIndex, npc, playerTargetAgentIndex, playerTargetName);
        }
        catch { }
    }
	internal static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(NativeConversationSessionOwner nativeSessions, Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, int maxLines = 0, NpcDataPacket npc = null, string capturedHistoryKey = null)
	{
		try
		{
			string key = capturedHistoryKey ?? CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			return nativeSessions.Snapshot(key, NativeConversationSessionOwner.ResolveHistoryLineLimit(maxLines, DuelSettings.DailyConversationHistoryLineLimitMax, maxLines > 0 ? 0 : DuelSettings.GetDailyConversationHistoryLineLimitForExternal()));
		}
		catch
		{
			return new List<AnimusForgeDialogueHistoryEntry>();
		}
	}
	internal static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(NativeConversationSessionOwner nativeSessions, Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0, NpcDataPacket npc = null, string capturedHistoryKey = null)
	{
		try
		{
			var entries = GetNativeConversationSessionHistorySnapshot(nativeSessions, targetHero, targetCharacter, npcName, targetAgentIndex, maxLines, npc, capturedHistoryKey);
			var distances = new Dictionary<int, float>();
			foreach (var entry in entries)
			{
				if (entry == null || string.IsNullOrWhiteSpace(entry.Text) || string.Equals((entry.Kind ?? "").Trim(), "fact", StringComparison.OrdinalIgnoreCase) || string.Equals((entry.Kind ?? "").Trim(), "npc", StringComparison.OrdinalIgnoreCase)) continue;
				int index = entry.TargetAgentIndex >= 0 ? entry.TargetAgentIndex : targetAgentIndex;
				if (!distances.ContainsKey(index)) distances[index] = SceneAgentIdentityPromptCaptureAdapter.GetPlayerDistanceToAgentForScenePrompt(index);
			}
			return NativeConversationSessionOwner.ProjectHistoryMessages(entries, npcName, targetAgentIndex, distances);
		}
		catch { return new List<ConversationMessage>(); }
	}
	internal List<ConversationMessage> ConsumePendingCurrentNativeAfefFactMessagesForPrompt(string key)
	{
		key = (key ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return new List<ConversationMessage>();
		}
		return _nativeSessions.ConsumeFacts(key);
	}

    internal sealed class NativePreparationPorts
    {
        internal readonly NativeAdmissionApplicationAdapter Admissions;
        internal readonly NativeConversationSessionOwner Sessions;
        internal readonly SceneAgentIdentityPromptCaptureAdapter Identity;
        internal readonly Func<List<NpcDataPacket>, Dictionary<int,Hero>, List<SceneSummonPromptTarget>> Summon;
        internal readonly Func<Agent,int,List<SceneGuidePromptTarget>> Guide;
        internal readonly Func<Hero,CharacterObject,int,bool,List<SceneSummonPromptTarget>,List<SceneGuidePromptTarget>,NpcDataPacket,List<NpcDataPacket>,string,List<string>> Excluded;
        internal NativePreparationPorts(NativeAdmissionApplicationAdapter admissions, NativeConversationSessionOwner sessions, SceneAgentIdentityPromptCaptureAdapter identity,
            Func<List<NpcDataPacket>,Dictionary<int,Hero>,List<SceneSummonPromptTarget>> summon, Func<Agent,int,List<SceneGuidePromptTarget>> guide,
            Func<Hero,CharacterObject,int,bool,List<SceneSummonPromptTarget>,List<SceneGuidePromptTarget>,NpcDataPacket,List<NpcDataPacket>,string,List<string>> excluded)
        {
            Admissions=admissions ?? throw new ArgumentNullException(nameof(admissions));
            Sessions=sessions ?? throw new ArgumentNullException(nameof(sessions));
            Identity=identity ?? throw new ArgumentNullException(nameof(identity));
            Summon=summon ?? throw new ArgumentNullException(nameof(summon));
            Guide=guide ?? throw new ArgumentNullException(nameof(guide));
            Excluded=excluded ?? throw new ArgumentNullException(nameof(excluded));
        }
    }
internal static NativeConversationPreparationSnapshot CaptureNativeConversationPreparation(NativePreparationPorts ports,
        NativeConversationAdmission admission, Hero targetHero, CharacterObject targetCharacter,
        string npcName, string routingInput, out string reason)
    {
        if (!ShoutBehavior.IsBannerlordMainThreadForNativeActions())
        {
            reason = "main_thread_required";
            return null;
        }
        if (!ports.Admissions.IsNativeConversationAdmissionCurrent(admission, out reason)) return null;
        // Same existing builders and inputs; no LLM/recall task may be started inside this capture.
		int nativeTargetAgentIndex = admission.AgentIndex;
		NpcDataPacket npc = ports.Identity.BuildNativeConversationNpcData(targetHero, targetCharacter);
		npc.AgentIndex = nativeTargetAgentIndex;
		string nativeTargetLog = targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? "unknown";
		List<NpcDataPacket> presentNpcs = new List<NpcDataPacket> { npc };
		string cultureId = npc.CultureId ?? "neutral";
		bool hadNativeConversationSessionHistoryBeforeTurn = HasNativeConversationSessionHistory(ports.Sessions, targetHero, targetCharacter, npcName, nativeTargetAgentIndex, npc);
		string nativeMeetingTauntRuleBlock = "";
		PartyBase nativeMeetingTauntParty = null;
		if (targetHero == null)
		{
			ConversationActionBoundaryBannerlordAdapter.TryResolveNativeConversationMeetingTauntParty(targetHero, targetCharacter, nativeTargetAgentIndex, out nativeMeetingTauntParty);
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
		List<SceneSummonPromptTarget> nativeSceneSummonTargets = (nativeTargetAgentIndex >= 0) ? ports.Summon(presentNpcs, nativeResolvedHeroes) : null;
		int nativeSceneGuideFirstPromptId = ((nativeSceneSummonTargets != null && nativeSceneSummonTargets.Count > 0) ? nativeSceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
		Agent nativeTargetAgent = (nativeTargetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == nativeTargetAgentIndex) : null;
		List<SceneGuidePromptTarget> nativeSceneGuideTargets = (nativeTargetAgentIndex >= 0) ? ports.Guide(nativeTargetAgent, nativeSceneGuideFirstPromptId) : null;
		List<string> preprocessExcludedRuleIds = ports.Excluded(targetHero, targetCharacter, nativeTargetAgentIndex, npc.IsHero, nativeSceneSummonTargets, nativeSceneGuideTargets, npc, presentNpcs, routingInput);
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
internal static bool HasNativeConversationSessionHistory(NativeConversationSessionOwner sessions, Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
		try
		{
			string key = CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}
			return sessions.HasHistory(key);
		}
		catch
		{
			return false;
		}
	}

internal void RecordExtraFactToSceneHistoryCaptured(string extraFact, List<NpcDataPacket> nearbyData)
	{
		if (string.IsNullOrWhiteSpace(extraFact) || nearbyData == null)
		{
			return;
		}
		string text = ScenePromptMessageProjectionComposer.NormalizeSceneExtraFactForHistory(extraFact);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		long eventSequence = SceneConversationHistoryOwner.NextEventSequence();
		lock (_history().Gate)
		{
			_history().AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "system",
				Content = text,
				SpeakerName = "系统",
				SpeakerAgentIndex = -1,
				VisibleAgentIndices = visibleAgentIndices
			}));
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				_history().AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "system",
					Content = text,
					SpeakerName = "系统",
					SpeakerAgentIndex = -1,
					VisibleAgentIndices = visibleAgentIndices
				}));
			}
		}
		AppendSceneEventToNativeSharedHistoryForTargets(nearbyData, "系统", text, "fact", eventSequence);
	}
internal void AppendActionAfefFactToSceneHistoryInOrderCaptured(int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory = true)
	{
		if (targetAgentIndex < 0)
		{
			return;
		}
		string factLine = ConversationSpeechTextRules.NormalizeNativeConversationFactLineForPrompt(fact, "玩家动作");
		if (string.IsNullOrWhiteSpace(factLine))
		{
			return;
		}
		long eventSequence = SceneConversationHistoryOwner.NextEventSequence();
		ConversationMessage publicMessage = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			EventSequence = eventSequence,
			Role = "system",
			Content = factLine,
			SpeakerName = "系统",
			SpeakerAgentIndex = -1,
			VisibleAgentIndices = new List<int> { targetAgentIndex }
		});
		ConversationMessage privateMessage = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			EventSequence = eventSequence,
			Role = "system",
			Content = factLine,
			SpeakerName = "系统",
			SpeakerAgentIndex = -1,
			VisibleAgentIndices = new List<int> { targetAgentIndex }
		});
		lock (_history().Gate)
		{
			_history().AppendPair(targetAgentIndex, publicMessage, privateMessage);
		}
		if (mirrorToNativeSharedHistory)
		{
			AppendSceneEventToNativeSharedHistory(ResolveSceneNpcDataForSharedHistory(targetAgentIndex), "玩家动作", factLine, "fact", eventSequence);
		}
	}
internal bool RecordPlayerMessageCaptured(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "", Dictionary<int, Agent> audienceAgentsByIndex = null, bool requireMemoryReceipt = false)
	{
		// Must happen first so the dynamic AFEF fact sits directly above the player's current scene utterance.
		_firstMeeting(nearbyData);
		_revisit(nearbyData);
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		string text2 = SceneAgentIdentityPromptCaptureAdapter.ResolveSceneTargetNameForPrompt(primaryTargetAgentIndex, primaryTargetName, nearbyData);
		float playerDistanceMeters = SceneAgentIdentityPromptCaptureAdapter.GetPlayerDistanceToAgentForScenePrompt(primaryTargetAgentIndex);
		long eventSequence = SceneConversationHistoryOwner.NextEventSequence();
		lock (_history().Gate)
		{
			_history().AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "user",
				Content = text,
				SpeakerName = "你",
				SpeakerAgentIndex = -1,
				TargetAgentIndex = primaryTargetAgentIndex,
				TargetName = text2,
				PlayerDistanceMeters = playerDistanceMeters,
				VisibleAgentIndices = visibleAgentIndices
			}));
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				_history().AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "user",
					Content = text,
					SpeakerName = "你",
					SpeakerAgentIndex = -1,
					TargetAgentIndex = primaryTargetAgentIndex,
					TargetName = text2,
					PlayerDistanceMeters = playerDistanceMeters,
					VisibleAgentIndices = visibleAgentIndices
				}));
			}
		}
		AppendSceneEventToNativeSharedHistoryForTargets(nearbyData, SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout(), text, "player", eventSequence, primaryTargetAgentIndex, text2, audienceAgentsByIndex);
		try
		{
			return PersistPlayerMessageToNamedHeroes(text, nearbyData, primaryTargetAgentIndex, text2, audienceAgentsByIndex, requireMemoryReceipt);
		}
		catch
		{
			return false;
		}
	}
internal bool RecordResponseForAllNearbySafeCaptured(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt = false)
	{
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		string text = SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHistorySpeakerNameForPrompt(speakerAgentIndex, speakerName, nearbyData);
		long eventSequence = SceneConversationHistoryOwner.NextEventSequence();
		lock (_history().Gate)
		{
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				_history().AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "assistant",
					Content = "[" + text + "]: " + response,
					SpeakerName = text,
					SpeakerAgentIndex = speakerAgentIndex,
					VisibleAgentIndices = visibleAgentIndices
				}));
				_history().PruneNpcFacts(agentIndex);
			}
			_history().AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "assistant",
				Content = response,
				SpeakerName = text,
				SpeakerAgentIndex = speakerAgentIndex,
				VisibleAgentIndices = visibleAgentIndices
			}));
			_history().PrunePublicFacts();
		}
		NpcDataPacket speakerNpc = nearbyData?.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == speakerAgentIndex) ?? ResolveSceneNpcDataForSharedHistory(speakerAgentIndex, speakerName);
		AfGcczShoutBridge.RecordOrdinarySpeakerUtterance(speakerNpc, response);
		AppendSceneEventToNativeSharedHistory(speakerNpc, text, response, "npc", eventSequence);
		return _flushDeferred(requireMemoryReceipt);
	}
internal void RecordSystemFactForNearbySafeCaptured(List<NpcDataPacket> nearbyData, string factText)
	{
		string text = (factText ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		bool isAfefFact = SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(text, out var afefFactLine);
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		List<NpcDataPacket> afefPendingTargets = isAfefFact ? new List<NpcDataPacket>() : null;
		long eventSequence = SceneConversationHistoryOwner.NextEventSequence();
		lock (_history().Gate)
		{
			_history().AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "system",
				Content = text,
				SpeakerName = "system",
				SpeakerAgentIndex = -1,
				VisibleAgentIndices = visibleAgentIndices
			}));
			if (nearbyData == null)
			{
				return;
			}
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (nearbyDatum == null)
				{
					continue;
				}
				int agentIndex = nearbyDatum.AgentIndex;
				_history().AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "system",
					Content = text,
					SpeakerName = "system",
					SpeakerAgentIndex = -1,
					VisibleAgentIndices = visibleAgentIndices
				}));
				if (isAfefFact)
				{
					afefPendingTargets.Add(nearbyDatum);
				}
			}
		}
		if (afefPendingTargets != null)
		{
			foreach (NpcDataPacket target in afefPendingTargets)
			{
				if (target == null)
				{
					continue;
				}
				QueuePendingCurrentAfefFactForAgent(target.AgentIndex, afefFactLine);
				QueuePendingCurrentNativeAfefFactForSceneTarget(target, afefFactLine);
			}
		}
		AppendSceneEventToNativeSharedHistoryForTargets(nearbyData, "系统", text, "fact", eventSequence);
		try
		{
			if (nearbyData != null)
			{
				HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (NpcDataPacket nearbyDatum in nearbyData)
				{
					if (nearbyDatum != null && !nearbyDatum.IsHero && SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(nearbyDatum, null, null, nearbyDatum.AgentIndex, out var memoryId, out var memoryName) && persistedNonHeroIds.Add(memoryId))
					{
						MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, null, null, text, SceneConversationHistoryOwner.SessionId);
					}
				}
			}
		}
		catch
		{
		}
	}
internal void AppendSceneEventToNativeSharedHistory(NpcDataPacket targetNpc, string speaker, string text, string kind, long eventSequence, int playerTargetAgentIndex = -1, string playerTargetName = null, Agent targetAgentOverride = null)
	{
		text = (text ?? "").Replace("\r", "").Trim();
		if (targetNpc == null || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			Agent targetAgent = targetAgentOverride != null && targetAgentOverride.Index == targetNpc.AgentIndex ? targetAgentOverride : null;
			CharacterObject character = targetAgent?.Character as CharacterObject;
			Hero hero = character?.HeroObject;
			if (hero == null && targetAgent == null)
			{
				hero = SceneAgentIdentityPromptCaptureAdapter.ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
				character = hero?.CharacterObject;
			}
			if (character == null && targetNpc.AgentIndex >= 0)
			{
				targetAgent ??= Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetNpc.AgentIndex && a.IsActive());
				character = targetAgent?.Character as CharacterObject;
				hero ??= character?.HeroObject;
			}
			string npcName = SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(targetNpc);
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (hero?.Name?.ToString() ?? character?.Name?.ToString() ?? targetNpc.Name ?? "NPC").Trim();
			}
			AppendNativeConversationSessionHistoryCaptured(_nativeSessions, _ownCapture, hero, character, npcName, speaker, text, kind, eventSequence, bridgeToSceneHistory: false, targetAgentIndex: targetNpc.AgentIndex, npc: targetNpc, playerTargetAgentIndex: playerTargetAgentIndex, playerTargetName: playerTargetName);
		}
		catch (Exception ex)
		{
			Logger.Log("SharedConversationHistory", "[WARN] scene->native append failed: " + ex.Message);
		}
	}
internal void AppendSceneEventToNativeSharedHistoryForTargets(IEnumerable<NpcDataPacket> targets, string speaker, string text, string kind, long eventSequence, int playerTargetAgentIndex = -1, string playerTargetName = null, Dictionary<int, Agent> audienceAgentsByIndex = null)
	{
		if (targets == null)
		{
			return;
		}
		HashSet<string> writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (NpcDataPacket target in targets)
		{
			if (target == null)
			{
				continue;
			}
			string key = target.AgentIndex.ToString();
			if (!writtenKeys.Add(key))
			{
				continue;
			}
			Agent targetAgent = null;
			audienceAgentsByIndex?.TryGetValue(target.AgentIndex, out targetAgent);
			AppendSceneEventToNativeSharedHistory(target, speaker, text, kind, eventSequence, playerTargetAgentIndex, playerTargetName, targetAgent);
		}
	}
internal NpcDataPacket ResolveSceneNpcDataForSharedHistory(int agentIndex, string fallbackName = "")
	{
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			if (npc != null)
			{
				return npc;
			}
		}
		catch
		{
		}
		if (agentIndex < 0 && string.IsNullOrWhiteSpace(fallbackName))
		{
			return null;
		}
		return new NpcDataPacket
		{
			AgentIndex = agentIndex,
			Name = string.IsNullOrWhiteSpace(fallbackName) ? "NPC" : fallbackName.Trim(),
			IsHero = false,
			CultureId = "neutral"
		};
	}
internal bool PersistPlayerMessageToNamedHeroes(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex, string primaryTargetName, Dictionary<int, Agent> audienceAgentsByIndex, bool requireMemoryReceipt = false)
	{
		if (string.IsNullOrWhiteSpace(text) || nearbyData == null || nearbyData.Count == 0)
		{
			return true;
		}
		HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool accepted = true;
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum != null && nearbyDatum.IsHero)
			{
				Agent agent = null;
				if (audienceAgentsByIndex == null || !audienceAgentsByIndex.TryGetValue(nearbyDatum.AgentIndex, out agent))
				{
					agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == nearbyDatum.AgentIndex);
				}
				if (agent != null
					&& agent.Character is CharacterObject co
					&& co.HeroObject != null
					&& AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(co.HeroObject, co, nearbyDatum.AgentIndex))
				{
					if (requireMemoryReceipt)
						accepted &= MyBehavior.CommitDialogueHistoryWithScene(co.HeroObject.StringId, false, co.HeroObject.Name?.ToString(), text, null, null, SceneConversationHistoryOwner.SessionId, primaryTargetAgentIndex, primaryTargetName)?.HistoryWritten == true;
					else
						MyBehavior.AppendExternalSceneDialogueHistory(co.HeroObject, text, null, null, SceneConversationHistoryOwner.SessionId, primaryTargetAgentIndex, primaryTargetName);
				}
			}
			else if (nearbyDatum != null && SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(nearbyDatum, null, null, nearbyDatum.AgentIndex, out var memoryId, out var memoryName) && persistedNonHeroIds.Add(memoryId))
			{
				if (requireMemoryReceipt)
					accepted &= MyBehavior.CommitDialogueHistoryWithScene(memoryId, true, memoryName, text, null, null, SceneConversationHistoryOwner.SessionId, primaryTargetAgentIndex, primaryTargetName)?.HistoryWritten == true;
				else
					MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, text, null, null, SceneConversationHistoryOwner.SessionId, primaryTargetAgentIndex, primaryTargetName);
			}
		}
		return accepted;
	}
internal void QueuePendingCurrentNativeAfefFactForSceneTarget(NpcDataPacket targetNpc, string fact)
	{
		if (targetNpc == null || string.IsNullOrWhiteSpace(fact))
		{
			return;
		}
		try
		{
			Hero hero = SceneAgentIdentityPromptCaptureAdapter.ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
			CharacterObject character = hero?.CharacterObject;
			if (character == null && targetNpc.AgentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetNpc.AgentIndex && a.IsActive());
				character = agent?.Character as CharacterObject;
				hero ??= character?.HeroObject;
			}
			string npcName = SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(targetNpc);
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (hero?.Name?.ToString() ?? character?.Name?.ToString() ?? targetNpc.Name ?? "NPC").Trim();
			}
			string key = CaptureNativeConversationHistoryKey(hero, character, npcName, targetNpc.AgentIndex, targetNpc);
			if (!string.IsNullOrWhiteSpace(key))
			{
				QueuePendingCurrentNativeAfefFactForKey(key, fact);
			}
		}
		catch
		{
		}
	}

internal static string BuildSceneFirstMeetingNpcFactSection(Hero hero, Func<Hero,string> capture)
    { return ScenePromptMessageProjectionComposer.BuildSceneFirstMeetingNpcFactSection(capture(hero)); }
internal static string BuildSceneFirstMeetingNpcFactSection(int agentIndex, Dictionary<int,Hero> resolvedHeroes, Func<Hero,string> capture)
    {
        if (agentIndex < 0 || resolvedHeroes == null || !resolvedHeroes.TryGetValue(agentIndex, out var value)) return "";
        return BuildSceneFirstMeetingNpcFactSection(value, capture);
    }

    internal sealed class PersistedHistoryCapturePorts
    {
        internal readonly ConversationGameThreadDispatcher Dispatcher;
        internal readonly Func<bool> IsCurrentOwner;
        internal readonly Func<int> Epoch;
        internal readonly Func<SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts> Memory;
        internal PersistedHistoryCapturePorts(ConversationGameThreadDispatcher dispatcher, Func<bool> isCurrentOwner,
            Func<int> epoch, Func<SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts> memory)
        { Dispatcher=dispatcher; IsCurrentOwner=isCurrentOwner; Epoch=epoch; Memory=memory; }
    }
    private readonly PersistedHistoryCapturePorts _persisted;
    // Metadata only: the original request DTO remains the sole text/task cache.
    private readonly ConditionalWeakTable<ShoutBehavior.PrecomputedShoutRagContext, PersistedHistoryBinding> _historyBindings = new();
    private readonly ConditionalWeakTable<Task<string>, PersistedHistoryStamp> _historyTaskBindings = new();
    private Task<string> BindPersistedHistoryTask(Task<string> task, PersistedHistoryStamp stamp)
    { _historyTaskBindings.Add(task,stamp); return task; }
    private sealed class PersistedHistoryBinding { internal PersistedHistoryStamp Stamp; }
    private PersistedHistoryStamp ReadPersistedHistoryBinding(ShoutBehavior.PrecomputedShoutRagContext value)
    { return Volatile.Read(ref _historyBindings.GetValue(value, static _ => new PersistedHistoryBinding()).Stamp); }
    private sealed class PersistedHistoryStamp
    {
        internal long Generation;
        internal int Epoch, Session;
        internal string Input;
        internal Hero Hero;
    }
    private PersistedHistoryStamp CapturePersistedHistoryStamp(string input, int agent, Dictionary<int,Hero> heroes)
    {
        Hero hero=null; if(heroes!=null) heroes.TryGetValue(agent,out hero);
        return new PersistedHistoryStamp { Generation=SaveRuntimeGuard.CaptureGeneration(), Epoch=_persisted.Epoch(),
            Session=Volatile.Read(ref SceneConversationHistoryOwner.SessionId), Input=input, Hero=hero };
    }
    private bool IsPersistedHistoryCurrent(PersistedHistoryStamp stamp) => stamp!=null && _persisted.IsCurrentOwner()
        && SaveRuntimeGuard.IsCurrentGeneration(stamp.Generation) && _persisted.Epoch()==stamp.Epoch
        && Volatile.Read(ref SceneConversationHistoryOwner.SessionId)==stamp.Session;
    private PersistedHistoryStamp BindPersistedHistory(ShoutBehavior.PrecomputedShoutRagContext value,
        int agent, string input, Dictionary<int,Hero> heroes)
    {
        Hero hero=null; if(heroes!=null) heroes.TryGetValue(agent,out hero);
        PersistedHistoryBinding binding=_historyBindings.GetValue(value, static _ => new PersistedHistoryBinding());
        PersistedHistoryStamp old=Volatile.Read(ref binding.Stamp);
        if(old!=null && IsPersistedHistoryCurrent(old) && string.Equals(old.Input,input,StringComparison.Ordinal)
            && ReferenceEquals(old.Hero,hero)) return old;
        PersistedHistoryStamp current=CapturePersistedHistoryStamp(input,agent,heroes);
        value.PersistedHistoryContext=""; value.HasPersistedHistoryContext=false; value.PersistedHistoryContextTask=null;
        Volatile.Write(ref binding.Stamp,current); return current;
    }
    private Func<string> CapturePersistedHistoryWork(int agentIndex, PersistedHistoryStamp stamp)
    {
        if(!TWParallel.IsMainThread()) throw new InvalidOperationException("Persisted scene history capture requires the game thread.");
        if(!IsPersistedHistoryCurrent(stamp)) return null;
        Agent agent=Mission.Current?.Agents?.FirstOrDefault(a=>a!=null && a.Index==agentIndex && a.IsActive());
        CharacterObject character=agent?.Character as CharacterObject;
        Hero hero=stamp.Hero;
        if(!AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(hero,character,agentIndex)) return ()=>"";
        string secondary=agentIndex<0 ? "" : _history().LatestNpcUtterance(agentIndex);
        if(hero!=null) return SharedPromptCaptureBannerlordAdapter.CaptureHistoryContextWorkForHero(
            _persisted.Memory,hero,stamp.Input,secondary,false,stamp.Generation);
        NpcDataPacket npc=ShoutUtils.ExtractNpcData(agent);
        if(!SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(npc,null,character,agentIndex,out string id,out string name)) return ()=>"";
        return SharedPromptCaptureBannerlordAdapter.CaptureHistoryContextWorkById(_persisted.Memory,id,name,
            stamp.Input,secondary,false,stamp.Generation);
    }
    private async Task<string> RunPersistedHistoryWorkAsync(int agentIndex, PersistedHistoryStamp stamp,
        ShoutBehavior.PrecomputedShoutRagContext value=null)
    {
        Func<string> work=await _persisted.Dispatcher.RunAsync<Func<string>>("persisted_history_capture", "scene", agentIndex,
            ()=>CapturePersistedHistoryWork(agentIndex,stamp),null).ConfigureAwait(false);
        bool Current() => IsPersistedHistoryCurrent(stamp) && (value==null || ReferenceEquals(ReadPersistedHistoryBinding(value),stamp));
        if(work==null || !Current()) return "";
        string result=await Task.Run(()=>Current() ? (work() ?? "").Trim() : "").ConfigureAwait(false);
        return Current() ? result : "";
    }
    internal string BuildPersistedHeroHistoryContext(int agentIndex, string currentInput, Dictionary<int,Hero> resolvedHeroes)
    {
        try { return RunPersistedHistoryWorkAsync(agentIndex,CapturePersistedHistoryStamp(currentInput,agentIndex,resolvedHeroes)).GetAwaiter().GetResult(); }
        catch { return ""; }
    }

internal string GetOrBuildPrecomputedPersistedHistoryContext(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, ShoutBehavior.PrecomputedShoutRagContext> precomputedContexts)
	{
		try
		{
			Stopwatch sw = Stopwatch.StartNew();
			if (agentIndex < 0)
			{
				return "";
			}
			if (precomputedContexts != null)
			{
                lock (precomputedContexts)
                {
				if (!precomputedContexts.TryGetValue(agentIndex, out var value) || value == null)
				{
					value = new ShoutBehavior.PrecomputedShoutRagContext();
					precomputedContexts[agentIndex] = value;
				}
				PersistedHistoryStamp stamp = BindPersistedHistory(value, agentIndex, currentInput, resolvedHeroes);
                    if (value.HasPersistedHistoryContext)
				{
					string cached = (value.PersistedHistoryContext ?? "").Trim();
					sw.Stop();
					Logger.Log("Logic", "[MemoryPerf] persisted_history_cache_hit agent=" + agentIndex + " chars=" + cached.Length + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
					return cached;
				}
				value.PersistedHistoryContext = RunPersistedHistoryWorkAsync(agentIndex, stamp, value).GetAwaiter().GetResult();
				value.HasPersistedHistoryContext = !string.IsNullOrWhiteSpace(value.PersistedHistoryContext);
				string built = (value.PersistedHistoryContext ?? "").Trim();
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] persisted_history_cache_miss agent=" + agentIndex + " chars=" + built.Length + " hasValue=" + value.HasPersistedHistoryContext + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
				return built;

                }
}
			string direct = BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes);
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] persisted_history_direct agent=" + agentIndex + " chars=" + ((direct ?? "").Trim().Length) + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return direct;
		}
		catch
		{
			return "";
		}
	}
internal Task<string> StartPrecomputedPersistedHistoryContextTask(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, ShoutBehavior.PrecomputedShoutRagContext> precomputedContexts, string reason)
	{
		try
		{
			if (agentIndex < 0)
			{
				return Task.FromResult("");
			}
			if (precomputedContexts != null)
			{
				ShoutBehavior.PrecomputedShoutRagContext value = null;
				lock (precomputedContexts)
				{
					if (!precomputedContexts.TryGetValue(agentIndex, out value) || value == null)
					{
						value = new ShoutBehavior.PrecomputedShoutRagContext();
						precomputedContexts[agentIndex] = value;
					}
					PersistedHistoryStamp stamp = BindPersistedHistory(value, agentIndex, currentInput, resolvedHeroes);
                    if (value.HasPersistedHistoryContext)
					{
						string cached = (value.PersistedHistoryContext ?? "").Trim();
						Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=cached chars=" + cached.Length);
						return BindPersistedHistoryTask(Task.FromResult(cached), stamp);
					}
					if (value.PersistedHistoryContextTask != null)
					{
						Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=reuse_task");
						return value.PersistedHistoryContextTask;
					}
					value.PersistedHistoryContextTask = BindPersistedHistoryTask(RunPersistedHistoryWorkAsync(agentIndex, stamp, value), stamp);
					Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=task");
					return value.PersistedHistoryContextTask;
				}
			}
			Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=direct_task");
			PersistedHistoryStamp directStamp=CapturePersistedHistoryStamp(currentInput, agentIndex, resolvedHeroes);
            return BindPersistedHistoryTask(RunPersistedHistoryWorkAsync(agentIndex, directStamp), directStamp);
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[MemoryPerf] parallel_history_start_failed reason=" + (reason ?? "") + " agent=" + agentIndex + " error=" + ex.Message);
			return Task.FromResult("");
		}
	}
internal async Task<string> AwaitPrecomputedPersistedHistoryContextAsync(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, ShoutBehavior.PrecomputedShoutRagContext> precomputedContexts, Task<string> startedTask, string reason)
	{
		if (startedTask == null)
		{
			return GetOrBuildPrecomputedPersistedHistoryContext(agentIndex, currentInput, resolvedHeroes, precomputedContexts);
		}
		Stopwatch sw = Stopwatch.StartNew();
		try
		{
			string built = ((await startedTask) ?? "").Trim();
            if (!_historyTaskBindings.TryGetValue(startedTask, out var taskStamp) || !IsPersistedHistoryCurrent(taskStamp)) return "";
			if (precomputedContexts != null && agentIndex >= 0)
			{
				lock (precomputedContexts)
				{
					if (!precomputedContexts.TryGetValue(agentIndex, out var value) || value == null) return "";
                    PersistedHistoryStamp stamp = ReadPersistedHistoryBinding(value);
                    Hero hero = null; if (resolvedHeroes != null) resolvedHeroes.TryGetValue(agentIndex, out hero);
                    if (!ReferenceEquals(stamp,taskStamp) || !IsPersistedHistoryCurrent(stamp) || !string.Equals(stamp.Input,currentInput,StringComparison.Ordinal)
                        || !ReferenceEquals(stamp.Hero,hero)
                        || (value.PersistedHistoryContextTask != null && !ReferenceEquals(value.PersistedHistoryContextTask,startedTask))) return "";
                    value.PersistedHistoryContext = built;
					value.HasPersistedHistoryContext = !string.IsNullOrWhiteSpace(built);
					if (object.ReferenceEquals(value.PersistedHistoryContextTask, startedTask))
					{
						value.PersistedHistoryContextTask = null;
					}
				}
			}
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] parallel_history_join reason=" + (reason ?? "") + " agent=" + agentIndex + " chars=" + built.Length + " hasValue=" + !string.IsNullOrWhiteSpace(built) + " waitMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return built;
		}
		catch (Exception ex)
		{
			sw.Stop();
			if (precomputedContexts != null && agentIndex >= 0)
			{
				try
				{
					lock (precomputedContexts)
					{
						if (precomputedContexts.TryGetValue(agentIndex, out var value) && value != null && object.ReferenceEquals(value.PersistedHistoryContextTask, startedTask))
						{
							value.PersistedHistoryContextTask = null;
						}
					}
				}
				catch
				{
				}
			}
			Logger.Log("Logic", "[MemoryPerf] parallel_history_join_failed reason=" + (reason ?? "") + " agent=" + agentIndex + " waitMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " error=" + ex.Message);
			return "";
		}
	}
internal bool PersistNpcSpeechToNamedHeroes(int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData, bool requireMemoryReceipt = false)
	{
		if (string.IsNullOrWhiteSpace(response) || nearbyData == null || nearbyData.Count == 0)
		{
			return true;
		}
		string text = SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHistorySpeakerNameForPrompt(speakerAgentIndex, speakerName, nearbyData);
		HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool accepted = true;
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum == null || !nearbyDatum.IsHero)
			{
				if (nearbyDatum != null && SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(nearbyDatum, null, null, nearbyDatum.AgentIndex, out var memoryId, out var memoryName) && persistedNonHeroIds.Add(memoryId))
				{
					string aiText = nearbyDatum.AgentIndex == speakerAgentIndex ? response : "[场景喊话] " + text + ": " + response;
					if (requireMemoryReceipt)
						accepted &= MyBehavior.CommitDialogueHistoryWithScene(memoryId, true, memoryName, null, aiText, null, Volatile.Read(ref SceneConversationHistoryOwner.SessionId))?.HistoryWritten == true;
					else
						MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, null, aiText, null, Volatile.Read(ref SceneConversationHistoryOwner.SessionId));
				}
				continue;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == nearbyDatum.AgentIndex);
			if (agent != null
				&& agent.Character is CharacterObject co
				&& co.HeroObject != null
				&& AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(co.HeroObject, co, nearbyDatum.AgentIndex))
			{
				string aiText = nearbyDatum.AgentIndex == speakerAgentIndex ? response : "[场景喊话] " + text + ": " + response;
				if (requireMemoryReceipt)
					accepted &= MyBehavior.CommitDialogueHistoryWithScene(co.HeroObject.StringId, false, co.HeroObject.Name?.ToString(), null, aiText, null, Volatile.Read(ref SceneConversationHistoryOwner.SessionId))?.HistoryWritten == true;
				else
					MyBehavior.AppendExternalSceneDialogueHistory(co.HeroObject, null, aiText, null, Volatile.Read(ref SceneConversationHistoryOwner.SessionId));
			}
		}
		return accepted;
	}
}
