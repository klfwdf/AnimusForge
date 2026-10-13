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
		return !string.IsNullOrWhiteSpace(extras) && extras.IndexOf(PromptComposerTexts.Raw("rule_party_transfer"), StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static string BuildNpcInitiatedOpeningPersistentFactText(string extraFact)
	{
		string factBody = StripAfefPrefixForPromptSection(extraFact);
		return string.IsNullOrWhiteSpace(factBody) ? "" : (PromptComposerTexts.Raw("scene_compat_001") + factBody);
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
		return (lines.Count == 0) ? PromptComposerTexts.Get("scene_live_002") : (PromptComposerTexts.Get("scene_live_003") + string.Join("\n", lines));
	}
internal static string BuildSceneNpcListLineForPrompt(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return PromptComposerTexts.Get("scene_live_004");
		}
		string text = npc.IsHero ? ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(npc) : ConversationActionPostprocessOwner.GetSceneNpcGivenNameForPrompt(npc);
		return PromptComposerTexts.Get("scene_npc_list_name") + text + PromptComposerTexts.Get("scene_npc_list_gender") + (npc.IsFemale ? PromptComposerTexts.Get("scene_npc_list_female") : PromptComposerTexts.Get("scene_npc_list_male")) + PromptComposerTexts.Get("scene_npc_list_identity") + ConversationActionPostprocessOwner.GetSceneNpcListIdentityForPrompt(npc);
	}
internal static string BuildDistanceToCurrentNpcForNpcListLine(NpcDataPacket npc, NpcDataPacket currentNpc)
	{
		if (npc == null || currentNpc == null || !npc.HasScenePosition || !currentNpc.HasScenePosition)
		{
			return PromptComposerTexts.Get("scene_live_010");
		}
		float deltaX = npc.ScenePositionX - currentNpc.ScenePositionX;
		float deltaY = npc.ScenePositionY - currentNpc.ScenePositionY;
		float deltaZ = npc.ScenePositionZ - currentNpc.ScenePositionZ;
		float distanceMeters = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
		if (float.IsNaN(distanceMeters) || float.IsInfinity(distanceMeters))
		{
			return PromptComposerTexts.Get("scene_live_010");
		}
		return PromptComposerTexts.Get("scene_npc_list_distance") + distanceMeters.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + PromptComposerTexts.Get("scene_npc_list_distance_unit");
	}
internal static string BuildSceneNonHeroNamingNoteForPrompt(IEnumerable<NpcDataPacket> npcs)
	{
		if (npcs == null || !npcs.Any((NpcDataPacket npc) => npc != null && !npc.IsHero))
		{
			return "";
		}
		return PromptComposerTexts.Get("scene_live_014");
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
			parts.Add(promptBody + PromptComposerTexts.Get("npc_initiated_fact_supplement"));
		}
		return parts.Count == 0 ? "" : (PromptComposerTexts.Raw("scene_compat_001") + string.Join(" ", parts).Trim());
	}
internal static string BuildNpcInitiatedOpeningUserText(string extraFact, string promptText)
	{
		List<string> sections = new List<string>();
		string factBody = MemorySystemFactProjection.ProjectBody(StripAfefPrefixForPromptSection(extraFact));
		if (!string.IsNullOrWhiteSpace(factBody))
		{
			sections.Add(PromptComposerTexts.Get("scene_live_017") + factBody);
		}
		string promptBody = (promptText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(promptBody))
		{
			sections.Add(PromptComposerTexts.Get("scene_live_018") + promptBody);
		}
		if (sections.Count == 0)
		{
			return "";
		}
		return PromptComposerTexts.Get("scene_live_019") + string.Join("\n\n", sections)
			+ PromptComposerTexts.Get("scene_live_020");
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
	return PromptComposerTexts.StartsWith(text, "weekly_report_npc_kingdom") || PromptComposerTexts.StartsWith(text, "weekly_report_world") || PromptComposerTexts.StartsWith(text, "weekly_report_nearby");
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
		bool startsNewBlock = PromptComposerTexts.StartsWith(trimmed, "rule_section_prefix") || string.Equals(trimmed, PromptComposerTexts.Raw("rule_section_note"), StringComparison.Ordinal);
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
		if (string.Equals(trimmed, PromptComposerTexts.Raw("knowledge_section_recall_prefix"), StringComparison.Ordinal))
		{
			FlushCurrentBlock();
			output.Add(trimmed);
			continue;
		}
		if (PromptComposerTexts.StartsWith(trimmed, "knowledge_section_about_prefix"))
		{
			FlushCurrentBlock();
		}
		if (PromptComposerTexts.StartsWith(trimmed, "knowledge_section_player_appearance"))
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
		if (!inKnowledgeSection && (string.Equals(trimmed, PromptComposerTexts.Raw("knowledge_section_recall_prefix"), StringComparison.Ordinal) || PromptComposerTexts.StartsWith(trimmed, "knowledge_section_about_prefix") || PromptComposerTexts.StartsWith(trimmed, "knowledge_section_player_appearance")))
		{
			inRuleSection = false;
			inKnowledgeSection = true;
		}
		else if (!inKnowledgeSection && (PromptComposerTexts.StartsWith(trimmed, "rule_section_prefix") || string.Equals(trimmed, PromptComposerTexts.Raw("rule_section_note"), StringComparison.Ordinal)))
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
	bool hasSceneMechanismRuleMarker = !string.IsNullOrWhiteSpace(text) && text.IndexOf(PromptComposerTexts.Raw("rule_scene_mechanism"), StringComparison.OrdinalIgnoreCase) >= 0;
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
			string inspectionSlaughterRule = PromptComposerTexts.Raw("scene_compat_035")
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
			string executionRule = PromptComposerTexts.Raw("scene_compat_036") + executionInstruction;
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
		string marker = PromptComposerTexts.Raw("rule_scene_mechanism");
		int num = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			if (!allowAppendWithoutMarker)
			{
				return text;
			}
			return string.IsNullOrWhiteSpace(text) ? text2 : (text + "\n" + text2);
		}
		int num2 = text.IndexOf(PromptComposerTexts.Raw("rule_section_prefix"), num + marker.Length, StringComparison.Ordinal);
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
	return num == num2 ? PromptComposerTexts.Format("scene_reply_speech_length_exact", ("max", num2))
		: PromptComposerTexts.Format("scene_reply_speech_length_range", ("min", num), ("max", num2));
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
		if (!PromptComposerTexts.IsLabel(array[i].Trim(), "scene_reply_format_strict"))
		{
			continue;
		}
		if (!PromptComposerTexts.StartsWith(array[i + 1].Trim(), "scene_reply_thought_prefix") || !PromptComposerTexts.IsLabel(array[i + 2].Trim(), "scene_reply_action_placeholder") || !PromptComposerTexts.StartsWith(array[i + 3].Trim(), "scene_reply_speech_prefix"))
		{
			continue;
		}
		int instructionLineCount = (i + 4 < array.Length && array[i + 4].Trim().StartsWith(PromptComposerTexts.Raw("scene_compat_039"), StringComparison.Ordinal)) ? 5 : 4;
		List<string> list = array.ToList();
		instruction = string.Join("\n", list.GetRange(i, instructionLineCount)).Trim();
		list.RemoveRange(i, instructionLineCount);
		prompt = string.Join("\n", list).Trim();
		return true;
	}
	for (int i = 0; i <= array.Length - 3; i++)
	{
		if (!PromptComposerTexts.IsLabel(array[i].Trim(), "scene_reply_format_no_thought"))
		{
			continue;
		}
		if (!(PromptComposerTexts.IsLabel(array[i + 1].Trim(), "scene_reply_action_placeholder") || PromptComposerTexts.IsLabel(array[i + 1].Trim(), "scene_reply_action_placeholder_no_break")) || !PromptComposerTexts.StartsWith(array[i + 2].Trim(), "scene_reply_speech_prefix"))
		{
			continue;
		}
		int instructionLineCount = (i + 3 < array.Length && array[i + 3].Trim().StartsWith(PromptComposerTexts.Raw("scene_compat_039"), StringComparison.Ordinal)) ? 4 : 3;
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
		return text.IndexOf(PromptComposerTexts.Raw("afef_player_action"), StringComparison.Ordinal) >= 0
			|| text.IndexOf(PromptComposerTexts.Raw("afef_npc_action"), StringComparison.Ordinal) >= 0
			|| text.IndexOf(PromptComposerTexts.Raw("afef_player_action_bracket"), StringComparison.Ordinal) >= 0
			|| text.IndexOf(PromptComposerTexts.Raw("afef_npc_action_bracket"), StringComparison.Ordinal) >= 0;
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
			if (!IsActionPostprocessHistoryHeaderLine(line) && !PromptComposerTexts.IsLabel(line, "history_section_none"))
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
		return text2.Equals(PromptComposerTexts.Raw("postprocess_player_name_placeholder"), StringComparison.OrdinalIgnoreCase) || text2.Equals(PromptComposerTexts.Raw("retrieval_remainder_you"), StringComparison.OrdinalIgnoreCase) || text2.EndsWith(PromptComposerTexts.Raw("player_says_generic"), StringComparison.Ordinal) || text2.EndsWith(PromptComposerTexts.Raw("player_says_to_npc"), StringComparison.Ordinal);
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
			text2 = PromptComposerTexts.Get("scene_live_050");
		}
		if (hasMultiplePresentNpcs)
		{
			return PromptComposerTexts.Get("scene_reply_multi_instruction_prefix") + text + PromptComposerTexts.Get("scene_reply_multi_instruction_suffix");
		}
		return text + PromptComposerTexts.Get("scene_reply_single_instruction") + text2 + PromptComposerTexts.Get("scene_reply_single_instruction_suffix") + text + PromptComposerTexts.Get("scene_reply_single_instruction_respond") + text2 + PromptComposerTexts.Get("scene_reply_single_instruction_no_replace");
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
	string text = BuildSimpleDialogueReplyLengthInstruction(num, num2);
	if (innerThoughtDisabled)
	{
		return PromptComposerTexts.Get("scene_reply_format_no_thought") + "\n" + PromptComposerTexts.Get("scene_reply_action_placeholder_no_break") + "\n" + text;
	}
	return PromptComposerTexts.Get("scene_reply_format_strict") + "\n" + PromptComposerTexts.Format("scene_reply_thought_placeholder", ("min", num3)) + "\n" + PromptComposerTexts.Get("scene_reply_action_placeholder") + "\n" + text;
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
	internal static string PlayerCraftedAfefInspectionSuffix => PromptComposerTexts.Raw("history_afef_inspection_suffix");

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
		if (!PromptComposerTexts.StartsWith(text, "afef_player_action")
			&& !PromptComposerTexts.StartsWith(text, "afef_npc_action"))
		{
			text = PromptComposerTexts.Raw("scene_compat_060") + text;
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
		string[] prefixes = new string[4] { PromptComposerTexts.Raw("afef_npc_action"), PromptComposerTexts.Raw("afef_player_action"), PromptComposerTexts.Raw("afef_npc_action_bracket"), PromptComposerTexts.Raw("afef_player_action_bracket") };
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
		return PromptComposerTexts.Get("scene_live_065") + lines + PromptComposerTexts.Get("scene_live_066");
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
			if (!PromptComposerTexts.StartsWith(text2, "scene_compat_067") && !PromptComposerTexts.StartsWith(text2, "scene_compat_068") && !string.IsNullOrWhiteSpace(text2))
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
				if (PromptComposerTexts.StartsWith(text2, "trust_semantic_level") || PromptComposerTexts.StartsWith(text2, "trust_credit_rule") || PromptComposerTexts.StartsWith(text2, "trust_value_standard"))
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
		int num = text.IndexOf(PromptComposerTexts.Raw("group_dialogue_rules"), StringComparison.Ordinal);
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
			return PromptComposerTexts.Get("scene_auto_group_someone");
		}
		string name = (npc.IsHero ? ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(npc) : ConversationActionPostprocessOwner.GetSceneNpcGivenNameForPrompt(npc)).Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			name = (npc.PromptDisplayName ?? npc.PromptGivenName ?? npc.Name ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			name = PromptComposerTexts.Get("scene_live_074");
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
			text2 = PromptComposerTexts.Get("scene_auto_group_surrounding");
		}
		return text + PromptComposerTexts.Get("scene_auto_group_instruction_prefix") + text2 + PromptComposerTexts.Get("scene_auto_group_instruction_suffix") + text + PromptComposerTexts.Get("scene_auto_group_instruction_identity");
	}
internal static string NormalizeSceneHistoryLineForLoreQuery(string line)
	{
		string text = (line ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text) || ConversationSpeechTextRules.IsLeakedPromptLineForShout(text))
		{
			return "";
		}
		if (PromptComposerTexts.StartsWith(text, "scene_compat_079"))
		{
			text = text.Substring(PromptComposerTexts.Raw("scene_compat_079").Length).Trim();
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
		return PromptComposerTexts.StartsWith(value, "scene_compat_081")
			&& value.IndexOf(PromptComposerTexts.Raw("scene_compat_082"), StringComparison.Ordinal) >= 0;
	}
internal static bool HasInjectedRuleBlockForPostprocess(string instructions, string ruleId)
	{
		string text = (instructions ?? "").Trim();
		string text2 = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		return text.IndexOf(PromptComposerTexts.Raw("rule_section_prefix") + text2 + "】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static string BuildSceneFirstMeetingNpcFactSection(string text)
{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return PromptComposerTexts.Raw("scene_compat_084") + text.Trim();

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
				string text2 = AdapterPromptCaptureTexts.PcapProjectKnown((presentNpc.RoleDesc ?? "").Trim());
				list.Add(string.IsNullOrWhiteSpace(text2) ? text : (text2 + text));
				continue;
			}
			list.Add(SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(presentNpc));
		}
		if (list.Count == 0)
		{
			return "";
		}
		return PromptComposerTexts.Get("scene_nearby_present_prefix") + string.Join("，", list) + "。";
	}

internal static string ComposePlayerIdentityInfo(PersonaIdentityPromptCaptureAdapter.HeroIdentityInfoSnapshot snapshot)
    {
        if (snapshot == null) return "";
        int num=snapshot.Tier;
        string text=snapshot.Clan,factionName=snapshot.Faction,liegeName=snapshot.Liege,text3=snapshot.Title,heroCultureNameForPrompt=snapshot.Culture,text4=snapshot.Age,text5=snapshot.Equipment,value=snapshot.TradeSummary;
        bool includeRuleGatedFields=snapshot.IncludeRules,flag=snapshot.HasFaction,includeMarriageCandidates=snapshot.IncludeMarriage;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_086"));
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_087") + heroCultureNameForPrompt);
		if (includeRuleGatedFields)
		{
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_088") + PersonaIntroTextRules.GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
			if (flag)
			{
				stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt(PromptComposerTexts.Get("scene_live_089"), factionName, liegeName));
				stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_090") + text3);
			}
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_091") + text + PromptComposerTexts.Format("scene_player_clan_suffix", ("TIER", Math.Max(0, num))));
		}
		else if (flag)
		{
			stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt(PromptComposerTexts.Get("scene_live_089"), factionName, liegeName));
		}
		else
		{
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_093"));
		}
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_094") + text4);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_095") + text5);
			stringBuilder.AppendLine(value);
		}
		else
		{
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_095") + text5);
		}
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = snapshot.MarriageMaxAge;
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_097"));
			stringBuilder.AppendLine(PromptComposerTexts.Format("scene_player_marriage_scope", ("MIN", PersonaIdentityPromptCaptureAdapter.MarriageCandidateMinAgeForPrompt), ("MAX", marriageCandidateMaxAgeSettingForPrompt)));
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
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_098"));
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_099") + heroCultureNameForPrompt);
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_100") + PersonaIntroTextRules.GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
		stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt(PromptComposerTexts.Get("scene_live_101"), factionName, liegeName));
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_102") + text3);
		string npcPlayerRelationshipLine = snapshot.Kinship;
		if (!string.IsNullOrWhiteSpace(npcPlayerRelationshipLine))
		{
			stringBuilder.AppendLine(npcPlayerRelationshipLine);
		}
		if (!string.IsNullOrWhiteSpace(text4))
		{
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_103") + text + $"（{Math.Max(0, num)} level，{text4}）");
		}
		else
		{
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_103") + text + $"（{Math.Max(0, num)} level）");
		}
		if (snapshot.HasLeader)
		{
			if (snapshot.LeaderIsSelf)
			{
				stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_105"));
			}
			else
			{
				stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_106") + (snapshot.LeaderName));
			}
		}
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_107") + text5);
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_108") + num2 + PromptComposerTexts.Get("scene_live_109"));
		stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_110") + text6);
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = snapshot.MarriageMaxAge;
			stringBuilder.AppendLine(PromptComposerTexts.Get("scene_live_111"));
			stringBuilder.AppendLine(PromptComposerTexts.Format("scene_npc_marriage_scope", ("MIN", PersonaIdentityPromptCaptureAdapter.MarriageCandidateMinAgeForPrompt), ("MAX", marriageCandidateMaxAgeSettingForPrompt)));
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
