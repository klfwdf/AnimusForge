using System.Text.RegularExpressions;
using AnimusForge.Refactor.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Modules;

internal static class ScenePromptMessageProjectionComposer
{
internal static bool HasPartyTransferRuleContext(string extras)
	{
		return !string.IsNullOrWhiteSpace(extras) && extras.IndexOf("【附加规则:party_transfer】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static string BuildNpcInitiatedOpeningPersistentFactText(string extraFact)
	{
		string factBody = StripAfefPrefixForPromptSection(extraFact);
		return string.IsNullOrWhiteSpace(factBody) ? "" : ("[AFEF NPC行为补充] " + factBody);
	}

internal static string BuildRecentSceneLoreQueryText(List<string> historyLines, int maxLines = 3, int maxChars = 280)
	{
		if (historyLines == null || historyLines.Count == 0)
		{
			return "";
		}
		List<string> list = new List<string>();
		for (int num = historyLines.Count - 1; num >= 0 && list.Count < Math.Max(1, maxLines); num--)
		{
			string text = NormalizeSceneHistoryLineForLoreQuery(historyLines[num]);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		list.Reverse();
		string text2 = string.Join("\n", list).Trim();
		if (text2.Length > maxChars)
		{
			text2 = text2.Substring(text2.Length - maxChars).Trim();
		}
		return text2;
	}

internal static string PrepareSceneMainReplySpeechText(string text, bool stopFollowing, bool endSummon)
	{
		// Main text cannot authorize domain actions. Only the already-validated Scene
		// end-of-conversation controls may be restored for the existing speech owner.
		string speechText = ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(ConversationSpeechTextRules.StripAutoGroupStopSignal(ConversationSpeechTextRules.StripAutoGroupRelaySignal(text)));
		if (stopFollowing) speechText = (speechText + " [STP]").Trim();
		if (endSummon) speechText = (speechText + " [END]").Trim();
		return speechText;
	}
internal static bool IsAuxiliaryPureDialogueSceneMessage(ConversationMessage msg)
	{
		string text = (msg?.Role ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.Equals("assistant", StringComparison.OrdinalIgnoreCase) || text.Equals("user", StringComparison.OrdinalIgnoreCase);
	}
internal static List<string> FilterScenePublicHistoryLines(List<string> sceneHistoryLines)
{
		List<string> lines = new List<string>();
		if (sceneHistoryLines != null)
		{
			foreach (string sceneHistoryLine in sceneHistoryLines)
			{
				string text = (sceneHistoryLine ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && !ConversationSpeechTextRules.IsLeakedPromptLineForShout(text))
				{
					lines.Add(text);
				}
			}
		}
return lines;
}
internal static string ComposeScenePublicHistorySection(List<string> lines, int historyLimit, int minimum, int maximum)
{
		lines = SceneHistoryMessageAssemblyOwner.KeepAfefFactsAndRecentHistoryLines(lines, Math.Max(minimum, Math.Min(maximum, historyLimit)));
		return (lines.Count == 0) ? "【当前场景公共对话与互动】\n无" : ("【当前场景公共对话与互动】\n" + string.Join("\n", lines));
	}
internal static string BuildSceneNpcListLineForPrompt(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "- 名字: 未知 | 性别: 未知 | 身份: 未知身份";
		}
		string text = npc.IsHero ? ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(npc) : ConversationActionPostprocessOwner.GetSceneNpcGivenNameForPrompt(npc);
		return "- 名字: " + text + " | 性别: " + (npc.IsFemale ? "女" : "男") + " | 身份: " + ConversationActionPostprocessOwner.GetSceneNpcListIdentityForPrompt(npc);
	}
internal static string BuildDistanceToCurrentNpcForNpcListLine(NpcDataPacket npc, NpcDataPacket currentNpc)
	{
		if (npc == null || currentNpc == null || !npc.HasScenePosition || !currentNpc.HasScenePosition)
		{
			return " | 距离你: 未知";
		}
		float deltaX = npc.ScenePositionX - currentNpc.ScenePositionX;
		float deltaY = npc.ScenePositionY - currentNpc.ScenePositionY;
		float deltaZ = npc.ScenePositionZ - currentNpc.ScenePositionZ;
		float distanceMeters = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
		if (float.IsNaN(distanceMeters) || float.IsInfinity(distanceMeters))
		{
			return " | 距离你: 未知";
		}
		return " | 距离你: " + distanceMeters.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "米";
	}
internal static string BuildSceneNonHeroNamingNoteForPrompt(IEnumerable<NpcDataPacket> npcs)
	{
		if (npcs == null || !npcs.Any((NpcDataPacket npc) => npc != null && !npc.IsHero))
		{
			return "";
		}
		return "【非HeroNPC命名说明】：【站在你旁边的人】里的“名字”是非HeroNPC的个人名字；在【当前场景公共对话与互动】等历史里，会使用“身份+名字”的写法，例如“帝国女镇民利娅”，两者指向同一人。";
	}

internal static string BuildProactiveSceneOpeningFactText(string extraFact, string promptText)
	{
		List<string> parts = new List<string>();
		string factBody = StripAfefPrefixForPromptSection(extraFact);
		if (!string.IsNullOrWhiteSpace(factBody))
		{
			parts.Add(factBody);
		}
		string promptBody = (promptText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(promptBody))
		{
			parts.Add(promptBody + " 这是NPC主动发起本轮对话的事实补充，不是玩家说出的台词。");
		}
		return parts.Count == 0 ? "" : ("[AFEF NPC行为补充] " + string.Join(" ", parts).Trim());
	}
internal static string BuildNpcInitiatedOpeningUserText(string extraFact, string promptText)
	{
		List<string> sections = new List<string>();
		string factBody = StripAfefPrefixForPromptSection(extraFact);
		if (!string.IsNullOrWhiteSpace(factBody))
		{
			sections.Add("【当下事实】\n[AFEF NPC行为补充] " + factBody);
		}
		string promptBody = (promptText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(promptBody))
		{
			sections.Add("【本轮行为请求】\n" + promptBody);
		}
		if (sections.Count == 0)
		{
			return "";
		}
		return "【NPC主动发起本轮对话】\n" + string.Join("\n\n", sections)
			+ "\n\n以上 role=user 内容是行为与事实指令，不是玩家说出的台词。";
	}

    internal static string BuildNativeConversationStreamingVisibleText(string text, ConversationSpeechTextOptions options)
    {
        string visible = LlmVisibleReplyNormalizer.NormalizeStreamingPreview(text);
        visible = (visible ?? "").Replace("\r", "").TrimStart();
        if (string.IsNullOrWhiteSpace(visible)) return "...";
        visible = ShoutUtils.StripNamePrefixedLineSafely(visible, 30);
        visible = ConversationSpeechTextRules.StripLeakedPromptContentForShout(visible);
        visible = ConversationSpeechTextRules.StripStageDirectionsForPassiveShout(visible, options);
        visible = ConversationSpeechTextRules.SanitizeSceneSpeechText(visible, options);
        return string.IsNullOrWhiteSpace(visible) ? "..." : visible.Trim();
    }
internal static bool IsSceneWeeklyFullReportHeader(string line)
{
	string text = (line ?? "").Trim();
	return text.StartsWith("【NPC所属王国完整周报】", StringComparison.Ordinal) || text.StartsWith("【世界完整周报】", StringComparison.Ordinal) || text.StartsWith("【周边相关王国完整周报】", StringComparison.Ordinal);
}

internal static string FormatSceneRuleSection(string text)
{
	if (string.IsNullOrWhiteSpace(text))
	{
		return "";
	}
	string[] lines = (text ?? "").Replace("\r", "").Split('\n');
	List<string> blocks = new List<string>();
	StringBuilder current = new StringBuilder();
	for (int i = 0; i < lines.Length; i++)
	{
		string line = (lines[i] ?? "").TrimEnd();
		string trimmed = line.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			continue;
		}
		bool startsNewBlock = trimmed.StartsWith("【附加规则:", StringComparison.Ordinal) || string.Equals(trimmed, "【说明】你不必提到附加规则内的内容，除非有人问起。", StringComparison.Ordinal);
		if (startsNewBlock && current.Length > 0)
		{
			blocks.Add(current.ToString().Trim());
			current.Clear();
		}
		if (current.Length > 0)
		{
			current.AppendLine();
		}
		current.Append(trimmed);
	}
	if (current.Length > 0)
	{
		blocks.Add(current.ToString().Trim());
	}
	return string.Join("\n\n", blocks.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

internal static string FormatSceneKnowledgeSection(string text)
{
	if (string.IsNullOrWhiteSpace(text))
	{
		return "";
	}
	string[] lines = (text ?? "").Replace("\r", "").Split('\n');
	List<string> output = new List<string>();
	StringBuilder currentBlock = new StringBuilder();
	void FlushCurrentBlock()
	{
		if (currentBlock.Length > 0)
		{
			string value = currentBlock.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(value))
			{
				output.Add(value);
			}
			currentBlock.Clear();
		}
	}
	for (int i = 0; i < lines.Length; i++)
	{
		string trimmed = (lines[i] ?? "").Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			continue;
		}
		if (string.Equals(trimmed, "参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal))
		{
			FlushCurrentBlock();
			output.Add(trimmed);
			continue;
		}
		if (trimmed.StartsWith("【以下是关于（", StringComparison.Ordinal))
		{
			FlushCurrentBlock();
		}
		if (trimmed.StartsWith("【玩家外貌信息（常驻）】", StringComparison.Ordinal))
		{
			FlushCurrentBlock();
		}
		if (currentBlock.Length > 0)
		{
			currentBlock.AppendLine();
		}
		currentBlock.Append(trimmed);
	}
	FlushCurrentBlock();
	return string.Join("\n\n", output.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

internal static void SplitSceneExtraSections(string text, out string miscSection, out string ruleSection, out string knowledgeSection)
{
	miscSection = "";
	ruleSection = "";
	knowledgeSection = "";
	if (string.IsNullOrWhiteSpace(text))
	{
		return;
	}
	string[] lines = (text ?? "").Replace("\r", "").Split('\n');
	List<string> miscLines = new List<string>();
	List<string> ruleLines = new List<string>();
	List<string> knowledgeLines = new List<string>();
	bool inRuleSection = false;
	bool inKnowledgeSection = false;
	for (int i = 0; i < lines.Length; i++)
	{
		string rawLine = lines[i] ?? "";
		string trimmed = rawLine.Trim();
		if (!inKnowledgeSection && (string.Equals(trimmed, "参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal) || trimmed.StartsWith("【以下是关于（", StringComparison.Ordinal) || trimmed.StartsWith("【玩家外貌信息（常驻）】", StringComparison.Ordinal)))
		{
			inRuleSection = false;
			inKnowledgeSection = true;
		}
		else if (!inKnowledgeSection && (trimmed.StartsWith("【附加规则:", StringComparison.Ordinal) || string.Equals(trimmed, "【说明】你不必提到附加规则内的内容，除非有人问起。", StringComparison.Ordinal)))
		{
			inRuleSection = true;
		}
		else if (inRuleSection && IsSceneWeeklyFullReportHeader(trimmed))
		{
			inRuleSection = false;
		}
		if (inKnowledgeSection)
		{
			knowledgeLines.Add(rawLine);
		}
		else if (inRuleSection)
		{
			ruleLines.Add(rawLine);
		}
		else
		{
			miscLines.Add(rawLine);
		}
	}
	miscSection = string.Join("\n", miscLines).Trim();
	ruleSection = FormatSceneRuleSection(string.Join("\n", ruleLines));
	knowledgeSection = FormatSceneKnowledgeSection(string.Join("\n", knowledgeLines));
}

internal static string BuildSceneSystemRuleBlock(string ruleSection, string sceneMechanismPromptSection)
{
	string text = FormatSceneRuleSection(ruleSection);
	string mechanism = FormatSceneRuleSection(sceneMechanismPromptSection);
	bool hasSceneMechanismRuleMarker = !string.IsNullOrWhiteSpace(text) && text.IndexOf("【附加规则:scene_mechanism_actions】", StringComparison.OrdinalIgnoreCase) >= 0;
	if (!string.IsNullOrWhiteSpace(mechanism) && hasSceneMechanismRuleMarker)
	{
		bool allowAppendWithoutMarker = false;
		text = string.IsNullOrWhiteSpace(text) ? mechanism : InjectSceneMechanismPromptSection(text, mechanism, allowAppendWithoutMarker);
		text = FormatSceneRuleSection(text);
	}
	else if (!string.IsNullOrWhiteSpace(mechanism)
		&& mechanism.IndexOf(TroopInspectionPrisonerSlaughterProfile.ActionTag, StringComparison.OrdinalIgnoreCase) >= 0
		&& (string.IsNullOrWhiteSpace(text)
			|| text.IndexOf(TroopInspectionPrisonerSlaughterProfile.ActionTag, StringComparison.OrdinalIgnoreCase) < 0))
	{
		string inspectionSlaughterInstruction = string.Join(
			"\n",
			mechanism
				.Replace("\r", "")
				.Split('\n')
				.Where(line => (line ?? "").IndexOf(
					TroopInspectionPrisonerSlaughterProfile.ActionTag,
					StringComparison.OrdinalIgnoreCase) >= 0))
			.Trim();
		if (!string.IsNullOrWhiteSpace(inspectionSlaughterInstruction))
		{
			string inspectionSlaughterRule = "【附加规则:scene_mechanism_actions】\n"
				+ inspectionSlaughterInstruction;
			text = string.IsNullOrWhiteSpace(text)
				? inspectionSlaughterRule
				: (text + "\n" + inspectionSlaughterRule);
			text = FormatSceneRuleSection(text);
		}
	}
	else if (!string.IsNullOrWhiteSpace(mechanism)
		&& mechanism.IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) >= 0
		&& (string.IsNullOrWhiteSpace(text)
			|| text.IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) < 0))
	{
		string executionInstruction = string.Join(
			"\n",
			mechanism
				.Replace("\r", "")
				.Split('\n')
				.Where(line => (line ?? "").IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) >= 0))
			.Trim();
		if (!string.IsNullOrWhiteSpace(executionInstruction))
		{
			string executionRule = "【附加规则:noble_prisoner_execution】\n" + executionInstruction;
			text = string.IsNullOrWhiteSpace(text) ? executionRule : (text + "\n" + executionRule);
			text = FormatSceneRuleSection(text);
		}
	}
	return text;
}

	internal static string InjectSceneMechanismPromptSection(string prompt, string mechanismSection, bool allowAppendWithoutMarker = false)
	{
		string text = (prompt ?? "").Trim();
		string text2 = (mechanismSection ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		const string marker = "【附加规则:scene_mechanism_actions】";
		int num = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			if (!allowAppendWithoutMarker)
			{
				return text;
			}
			return string.IsNullOrWhiteSpace(text) ? text2 : (text + "\n" + text2);
		}
		int num2 = text.IndexOf("【附加规则:", num + marker.Length, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return text.TrimEnd() + "\n" + text2;
		}
		return text.Substring(0, num2).TrimEnd() + "\n" + text2 + "\n" + text.Substring(num2).TrimStart();
	}

internal static string BuildSceneHistoryUserBlock(string scenePublicHistorySection, string privateRecentWindowSection, string persistedWithoutRecentWindow)
{
	List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
		{
			list.Add(HistorySectionProjectionOwner.FormatSceneHistorySection(scenePublicHistorySection));
		}
		if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
		{
			list.Add(HistorySectionProjectionOwner.FormatSceneHistorySection(privateRecentWindowSection));
		}
		if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
		{
			list.Add(HistorySectionProjectionOwner.FormatSceneHistorySection(persistedWithoutRecentWindow));
	}
	return string.Join("\n\n", list.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

internal static string BuildSimpleDialogueReplyLengthInstruction(int minTokens, int maxTokens)
{
	int num = Math.Max(1, minTokens);
	int num2 = Math.Max(num, maxTokens);
	return (num == num2) ? $"你实际要说的话，此处约{num2}字。" : $"你实际要说的话，此处不得少于{num}字，最多{num2}字。";
}

internal static bool TryExtractReplyFormatInstruction(ref string prompt, out string instruction)
{
	instruction = "";
	string text = (prompt ?? "").Replace("\r", "");
	if (string.IsNullOrWhiteSpace(text))
	{
		prompt = "";
		return false;
	}
	string[] array = text.Split('\n');
	for (int i = 0; i <= array.Length - 4; i++)
	{
		if (!string.Equals(array[i].Trim(), "你的回复格式必须严格按照以下执行，其中内心思考要用()括起来，动作要用**括起来：", StringComparison.Ordinal))
		{
			continue;
		}
		if (!array[i + 1].Trim().StartsWith("（你的内心思考内容", StringComparison.Ordinal) || !string.Equals(array[i + 2].Trim(), "*你的动作内容*", StringComparison.Ordinal) || !array[i + 3].Trim().StartsWith("你实际要说的话，此处", StringComparison.Ordinal))
		{
			continue;
		}
		int instructionLineCount = (i + 4 < array.Length && array[i + 4].Trim().StartsWith("[RELAY:接力编号]", StringComparison.Ordinal)) ? 5 : 4;
		List<string> list = array.ToList();
		instruction = string.Join("\n", list.GetRange(i, instructionLineCount)).Trim();
		list.RemoveRange(i, instructionLineCount);
		prompt = string.Join("\n", list).Trim();
		return true;
	}
	for (int i = 0; i <= array.Length - 3; i++)
	{
		if (!string.Equals(array[i].Trim(), "你的回复格式必须严格按照以下执行，动作要用**括起来：", StringComparison.Ordinal))
		{
			continue;
		}
		if (!string.Equals(array[i + 1].Trim(), "*你的动作内容*", StringComparison.Ordinal) || !array[i + 2].Trim().StartsWith("你实际要说的话，此处", StringComparison.Ordinal))
		{
			continue;
		}
		int instructionLineCount = (i + 3 < array.Length && array[i + 3].Trim().StartsWith("[RELAY:接力编号]", StringComparison.Ordinal)) ? 4 : 3;
		List<string> list = array.ToList();
		instruction = string.Join("\n", list.GetRange(i, instructionLineCount)).Trim();
		list.RemoveRange(i, instructionLineCount);
		prompt = string.Join("\n", list).Trim();
		return true;
	}
	prompt = text.Trim();
	return false;
}

	internal static string TrimPrivateRecentWindowForActionPostprocess(string privateRecentWindowSection, int maxTurns = 5)
	{
		string text = (privateRecentWindowSection ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Split('\n');
		int num = Math.Max(1, maxTurns);
		int num2 = 0;
		int num3 = 0;
		for (int i = array.Length - 1; i >= 0; i--)
		{
			string text2 = (array[i] ?? "").Trim();
			if (IsActionPostprocessPlayerTurnLine(text2))
			{
				num2++;
				if (num2 >= num)
				{
					num3 = i;
					break;
				}
			}
		}
		if (num2 < num)
		{
			num3 = 0;
		}
		List<string> list = new List<string>();
		string text3 = array.Length > 0 ? (array[0] ?? "").Trim() : "";
		if (HistorySectionProjectionOwner.IsPrivateRecentWindowHeader(text3) && num3 > 0)
		{
			list.Add(text3);
		}
		for (int j = num3; j < array.Length; j++)
		{
			string text4 = (array[j] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text4) && (list.Count == 0 || !string.Equals(list[list.Count - 1], text4, StringComparison.Ordinal)))
			{
				list.Add(text4);
			}
		}
		return string.Join("\n", list).Trim();
	}

	internal static bool ContainsAfefFactMarkerForActionPostprocess(string line)
	{
		string text = ConversationSpeechTextRules.StripAfefPromptScopeLabel((line ?? "").Trim());
		return text.IndexOf("[AFEF玩家行为补充]", StringComparison.Ordinal) >= 0
			|| text.IndexOf("[AFEF NPC行为补充]", StringComparison.Ordinal) >= 0
			|| text.IndexOf("【AFEF玩家行为补充】", StringComparison.Ordinal) >= 0
			|| text.IndexOf("【AFEF NPC行为补充】", StringComparison.Ordinal) >= 0;
	}

	internal static bool IsActionPostprocessHistoryHeaderLine(string line)
	{
		string text = (line ?? "").Trim();
		return text.StartsWith("【", StringComparison.Ordinal) && text.EndsWith("】", StringComparison.Ordinal);
	}

	internal static string StripAfefFactLinesForActionPostprocess(string historySection)
	{
		string text = (historySection ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		List<string> kept = new List<string>();
		bool hasContent = false;
		string[] lines = text.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			string line = (lines[i] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(line) || ContainsAfefFactMarkerForActionPostprocess(line))
			{
				continue;
			}
			kept.Add(line);
			if (!IsActionPostprocessHistoryHeaderLine(line) && !string.Equals(line, "无", StringComparison.Ordinal))
			{
				hasContent = true;
			}
		}
		return hasContent ? string.Join("\n", kept).Trim() : "";
	}

	internal static bool IsActionPostprocessPlayerTurnLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.StartsWith("【", StringComparison.Ordinal) || text.StartsWith("——", StringComparison.Ordinal))
		{
			return false;
		}
		int num = text.IndexOfAny(new char[2] { ':', '：' });
		string text2 = (num >= 0) ? text.Substring(0, num).Trim() : text;
		return text2.Equals("玩家", StringComparison.OrdinalIgnoreCase) || text2.Equals("你", StringComparison.OrdinalIgnoreCase) || text2.EndsWith("对你说", StringComparison.Ordinal) || text2.EndsWith("对NPC说", StringComparison.Ordinal);
	}

	internal static string BuildSingleNpcSceneReplyInstruction(string npcName, bool hasMultiplePresentNpcs, string capturedPlayerName)
	{
		string text = (npcName ?? "").Trim();
		string text2 = capturedPlayerName;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		if (hasMultiplePresentNpcs)
		{
			return "请只以" + text + "的身份回复，并结合role=user中其他人刚才说过的话来回应，不要各说各的。绝对不可以代替他人说话";
		}
		return text + "现在正在与" + text2 + "单独交谈。请只以" + text + "的身份回应" + text2 + "。绝对不可以代替他人说话";
	}

internal static string BuildReplyLengthInstruction(int minTokens, int maxTokens, bool formatDisabled, bool innerThoughtDisabled, int thoughtMinTokens)
{
	if (formatDisabled)
	{
		return "";
	}
	int num = Math.Max(1, minTokens);
	int num2 = Math.Max(num, maxTokens);
	int num3 = thoughtMinTokens;
	string text = (num == num2) ? $"你实际要说的话，此处约{num2}字。" : $"你实际要说的话，此处不得少于{num}字，最多{num2}字。";
	if (innerThoughtDisabled)
	{
		return "你的回复格式必须严格按照以下执行，动作要用**括起来：\n*你的动作内容*(注意不要分段）\n" + text;
	}
	return $"你的回复格式必须严格按照以下执行，其中内心思考要用()括起来，动作要用**括起来：\n（你的内心思考内容，不少于{num3}字，重点是思考#4 role=user及往后的对话历史）\n*你的动作内容*\n" + text;
}

internal static string BuildSceneSingleNpcTaskSystemBlock(string npcName, bool hasMultiplePresentNpcs, int minTokens, int maxTokens, string playerNameForLength, bool formatDisabled, bool innerThoughtDisabled, int thoughtMinTokens, string capturedPlayerName)
{
	if (formatDisabled)
	{
		return "";
	}
	StringBuilder stringBuilder = new StringBuilder();
	stringBuilder.AppendLine(BuildSingleNpcSceneReplyInstruction(npcName, hasMultiplePresentNpcs, capturedPlayerName));
	stringBuilder.Append(BuildReplyLengthInstruction(minTokens, maxTokens, formatDisabled, innerThoughtDisabled, thoughtMinTokens));
	return stringBuilder.ToString().Trim();
}
	internal const string PlayerCraftedAfefInspectionSuffix = "*你的智识分辨出来了该物品由玩家亲手制造*";

	internal static bool ContainsPlayerCraftedAfefInspectionSuffix(string extraFact)
	{
		return !string.IsNullOrWhiteSpace(extraFact)
			&& extraFact.IndexOf(
				PlayerCraftedAfefInspectionSuffix,
				StringComparison.Ordinal) >= 0;
	}

	internal static string StripPlayerCraftedAfefInspectionSuffix(string extraFact)
	{
		return (extraFact ?? "").Replace(
			PlayerCraftedAfefInspectionSuffix,
			"");
	}

	internal static string NormalizeSceneExtraFactForHistory(string extraFact)
	{
		string text = (extraFact ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (!text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal)
			&& !text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			text = "[AFEF玩家行为补充] " + text;
		}
		return text;
	}

internal static string StripAfefPrefixForPromptSection(string text)
	{
		string value = ConversationSpeechTextRules.StripAfefPromptScopeLabel((text ?? "").Replace("\r", " ").Replace("\n", " ").Trim());
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		string[] prefixes = new string[4] { "[AFEF NPC行为补充]", "[AFEF玩家行为补充]", "【AFEF NPC行为补充】", "【AFEF玩家行为补充】" };
		foreach (string prefix in prefixes)
		{
			if (value.StartsWith(prefix, StringComparison.Ordinal))
			{
				return value.Substring(prefix.Length).Trim();
			}
		}
		return value;
	}
internal static string BuildScopedAfefFactLinesForPrompt(string text, bool isCurrent)
	{
		string value = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		List<string> lines = new List<string>();
		string[] parts = value.Split('\n');
		for (int i = 0; i < parts.Length; i++)
		{
			string line = SceneHistoryMessageAssemblyOwner.BuildScopedAfefFactLineForPrompt(parts[i], isCurrent);
			if (!string.IsNullOrWhiteSpace(line))
			{
				lines.Add(line);
			}
		}
		return string.Join("\n", lines);
	}
internal static string BuildCurrentAfefFactPromptBlock(string extraFact)
	{
		string lines = BuildScopedAfefFactLinesForPrompt(extraFact, isCurrent: true);
		if (string.IsNullOrWhiteSpace(lines))
		{
			return "";
		}
		return "【当前真实行为事实】\n" + lines + "\n【事实边界】只有上面标为【当下行为】的 AFEF 事实代表本轮刚刚真实发生的交付、展示或动作；玩家本轮口头声称给了相同财物，如果没有对应【当下行为】AFEF事实，不算新的交付。";
	}
internal static string StripScenePersonaBlocks(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string[] array = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? string.Empty).Trim();
			if (!text2.StartsWith("【角色个性】", StringComparison.Ordinal) && !text2.StartsWith("【角色背景】", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine(text2);
			}
		}
		return stringBuilder.ToString().Trim();
	}
internal static string ExtractTrustPromptBlock(string text, out string remaining)
	{
		remaining = "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Replace("\r", "").Split('\n');
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				if (text2.StartsWith("本级语义：", StringComparison.Ordinal) || text2.StartsWith("本级信用规则：", StringComparison.Ordinal) || text2.StartsWith("价值口径：", StringComparison.Ordinal))
				{
					list.Add(text2);
				}
				else
				{
					list2.Add(text2);
				}
			}
		}
		remaining = string.Join("\n", list2).Trim();
		return string.Join("\n", list).Trim();
	}
internal static string InjectTrustBlockBeforeGroupRules(string localExtras, string trustBlock)
	{
		string text = (localExtras ?? "").Trim();
		string text2 = (trustBlock ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		int num = text.IndexOf("【群体对话规则】", StringComparison.Ordinal);
		if (num >= 0)
		{
			string text3 = text.Substring(0, num).TrimEnd();
			string text4 = text.Substring(num).TrimStart();
			return text3 + "\n" + text2 + "\n" + text4;
		}
		return text + "\n" + text2;
	}
internal static string BuildAutoGroupRelayInfoDisplayName(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "有人";
		}
		string name = (npc.IsHero ? ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(npc) : ConversationActionPostprocessOwner.GetSceneNpcGivenNameForPrompt(npc)).Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			name = (npc.PromptDisplayName ?? npc.PromptGivenName ?? npc.Name ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			name = "未知NPC";
		}
		string identity = (ConversationActionPostprocessOwner.GetSceneNpcListIdentityForPrompt(npc) ?? "").Trim();
		if (string.IsNullOrWhiteSpace(identity) || identity.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 || string.Equals(identity, name, StringComparison.OrdinalIgnoreCase))
		{
			return identity.Length > 0 ? identity : name;
		}
		return identity + name;
	}
internal static bool ContainsLiteralKeywordHit(string input, List<string> keywords)
	{
		string text = (input ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text) || keywords == null || keywords.Count <= 0)
		{
			return false;
		}
		for (int i = 0; i < keywords.Count; i++)
		{
			string text2 = (keywords[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && text.Contains(text2.ToLowerInvariant()))
			{
				return true;
			}
		}
		return false;
	}
internal static bool ContainsAutoGroupEndSignal(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.IndexOf("[END]", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[STP]", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase) >= 0;
	}
internal static bool TryParseAutoGroupRelayTargetAgentIndex(string text, out int relayTargetAgentIndex)
	{
		relayTargetAgentIndex = -1;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Match match = Regex.Match(text, "\\[RELAY\\s*:\\s*(\\d+)\\]", RegexOptions.IgnoreCase);
		return match.Success && int.TryParse(match.Groups[1].Value, out relayTargetAgentIndex) && relayTargetAgentIndex >= 0;
	}
internal static string BuildAutoGroupChatReplyInstruction(string npcName, List<NpcDataPacket> allNpcData)
	{
		string text = (npcName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		string text2 = string.Join("、", (allNpcData ?? new List<NpcDataPacket>()).Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0)
			.Select(SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt)
			.Where((string name) => !string.IsNullOrWhiteSpace(name) && !string.Equals(name.Trim(), text, StringComparison.Ordinal))
			.Where((string name) => !string.IsNullOrWhiteSpace(name))
			.Distinct()
			.Take(5));
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "周围的人";
		}
		return text + "现在正在和" + text2 + "继续聊天，玩家暂时没有插话。请只以" + text + "的身份，接续上面的对话历史和【本轮接力话题】中上一位的发言，可以回应在场的人或补充自己的看法，不必重新向玩家回答同一个问题。可以认同、质疑或追问，但不要重复相同的问候、奉承或照抄别人的结论，也不要代替任何人说话。";
	}
internal static string NormalizeSceneHistoryLineForLoreQuery(string line)
	{
		string text = (line ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text) || ConversationSpeechTextRules.IsLeakedPromptLineForShout(text))
		{
			return "";
		}
		if (text.StartsWith("[系统事实] ", StringComparison.Ordinal))
		{
			text = text.Substring("[系统事实] ".Length).Trim();
		}
		return text;
	}
internal static MyBehavior.ShoutPromptContext CreateEmptyNativeConversationPromptContext()
	{
		return new MyBehavior.ShoutPromptContext
		{
			Extras = "",
			EntityPostprocessContext = "",
			PreprocessRuleIds = new List<string>(),
			UseDuelContext = false,
			UseRewardContext = false,
			IsLoanContext = false,
			IsQualified = true
		};
	}
internal static bool IsNativeConversationPreprocessUnavailableTextForExternal(string text)
	{
		string value = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		return value.StartsWith("（API请求失败", StringComparison.Ordinal)
			&& value.IndexOf("原生对话前处理", StringComparison.Ordinal) >= 0;
	}
internal static bool HasInjectedRuleBlockForPostprocess(string instructions, string ruleId)
	{
		string text = (instructions ?? "").Trim();
		string text2 = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		return text.IndexOf("【附加规则:" + text2 + "】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static string BuildSceneFirstMeetingNpcFactSection(string text)
{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "【AFEF NPC行为补充】\n" + text.Trim();

}
internal static string BuildNearbyPresentNpcLineForPrompt(NpcDataPacket selfNpc, IEnumerable<NpcDataPacket> presentNpcs)
	{
		if (selfNpc == null || presentNpcs == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (NpcDataPacket presentNpc in presentNpcs)
		{
			if (presentNpc == null)
			{
				continue;
			}
			if (SceneAgentIdentityPromptCaptureAdapter.IsSameSceneNpcForPrompt(presentNpc, selfNpc))
			{
				continue;
			}
			string text = ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(presentNpc);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (presentNpc.IsHero)
			{
				string text2 = (presentNpc.RoleDesc ?? "").Trim();
				list.Add(string.IsNullOrWhiteSpace(text2) ? text : (text2 + text));
				continue;
			}
			list.Add(SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(presentNpc));
		}
		if (list.Count == 0)
		{
			return "";
		}
		return "并且你旁边还站着" + string.Join("，", list) + "。";
	}

internal static string ComposePlayerIdentityInfo(PersonaIdentityPromptCaptureAdapter.HeroIdentityInfoSnapshot snapshot)
    {
        if (snapshot == null) return "";
        int num=snapshot.Tier;
        string text=snapshot.Clan,factionName=snapshot.Faction,liegeName=snapshot.Liege,text3=snapshot.Title,heroCultureNameForPrompt=snapshot.Culture,text4=snapshot.Age,text5=snapshot.Equipment,value=snapshot.TradeSummary;
        bool includeRuleGatedFields=snapshot.IncludeRules,flag=snapshot.HasFaction,includeMarriageCandidates=snapshot.IncludeMarriage;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[player身份信息]");
		stringBuilder.AppendLine("玩家文化：" + heroCultureNameForPrompt);
		if (includeRuleGatedFields)
		{
			stringBuilder.AppendLine("玩家声望：" + PersonaIntroTextRules.GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
			if (flag)
			{
				stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt("玩家势力：", factionName, liegeName));
				stringBuilder.AppendLine("玩家身份：" + text3);
			}
			stringBuilder.AppendLine("玩家家族：" + text + $"（{Math.Max(0, num)} level，玩家是家族的族长）");
		}
		else if (flag)
		{
			stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt("玩家势力：", factionName, liegeName));
		}
		else
		{
			stringBuilder.AppendLine("提示：你感觉此人只是个普通人。");
		}
		stringBuilder.AppendLine("玩家年纪：" + text4);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine("玩家装备：" + text5);
			stringBuilder.AppendLine(value);
		}
		else
		{
			stringBuilder.AppendLine("玩家装备：" + text5);
		}
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = snapshot.MarriageMaxAge;
			stringBuilder.AppendLine("【玩家家族可婚配成员（允许已有配偶，事实清单）】");
			stringBuilder.AppendLine($"筛选口径：列出玩家家族内部基础适婚池（年龄 {PersonaIdentityPromptCaptureAdapter.MarriageCandidateMinAgeForPrompt}-{marriageCandidateMaxAgeSettingForPrompt}、非囚犯）。多配偶已允许，已有配偶不是拒绝理由；具体能否与对方成员成婚，仍以性别限制、年龄差、非重复婚姻和运行时婚姻规则为准。");
			stringBuilder.AppendLine(snapshot.MarriageCandidates);
		}
		return stringBuilder.ToString().Trim();
	}
internal static string ComposeNpcIdentityInfo(PersonaIdentityPromptCaptureAdapter.HeroIdentityInfoSnapshot snapshot)
    {
        if (snapshot == null) return "";
        int num=snapshot.Tier,num2=snapshot.Gold;
        string text=snapshot.Clan,factionName=snapshot.Faction,liegeName=snapshot.Liege,text3=snapshot.Title,heroCultureNameForPrompt=snapshot.Culture,text4=snapshot.ClanRole,text5=snapshot.Age,text6=snapshot.Equipment;
        bool includeMarriageCandidates=snapshot.IncludeMarriage;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[NPC身份信息]");
		stringBuilder.AppendLine("NPC文化：" + heroCultureNameForPrompt);
		stringBuilder.AppendLine("NPC声望：" + PersonaIntroTextRules.GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
		stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt("NPC势力：", factionName, liegeName));
		stringBuilder.AppendLine("NPC身份：" + text3);
		string npcPlayerRelationshipLine = snapshot.Kinship;
		if (!string.IsNullOrWhiteSpace(npcPlayerRelationshipLine))
		{
			stringBuilder.AppendLine(npcPlayerRelationshipLine);
		}
		if (!string.IsNullOrWhiteSpace(text4))
		{
			stringBuilder.AppendLine("NPC家族：" + text + $"（{Math.Max(0, num)} level，{text4}）");
		}
		else
		{
			stringBuilder.AppendLine("NPC家族：" + text + $"（{Math.Max(0, num)} level）");
		}
		if (snapshot.HasLeader)
		{
			if (snapshot.LeaderIsSelf)
			{
				stringBuilder.AppendLine("该家族族长：你本人");
			}
			else
			{
				stringBuilder.AppendLine("该家族族长：" + (snapshot.LeaderName));
			}
		}
		stringBuilder.AppendLine("NPC年纪：" + text5);
		stringBuilder.AppendLine("NPC存款：" + num2 + " 第纳尔");
		stringBuilder.AppendLine("NPC装备：" + text6);
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = snapshot.MarriageMaxAge;
			stringBuilder.AppendLine("【该家族可婚配成员（允许已有配偶，事实清单）】");
			stringBuilder.AppendLine($"筛选口径：列出该家族内部基础适婚池（年龄 {PersonaIdentityPromptCaptureAdapter.MarriageCandidateMinAgeForPrompt}-{marriageCandidateMaxAgeSettingForPrompt}、非囚犯）。多配偶已允许，已有配偶不是拒绝理由；具体能否与玩家家族成员成婚，仍以性别限制、年龄差、非重复婚姻和运行时婚姻规则为准。");
			stringBuilder.AppendLine(snapshot.MarriageCandidates);
		}
		if (!string.IsNullOrWhiteSpace(snapshot.InventorySummary))
        {
            stringBuilder.AppendLine(snapshot.InventoryHeader);
            stringBuilder.AppendLine(snapshot.InventorySummary);
        }
		return stringBuilder.ToString().Trim();
	}
}
