using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal readonly struct ConversationSpeechTextOptions
{
    internal readonly bool Detailed;
    internal readonly bool PreserveAsterisk;
    internal ConversationSpeechTextOptions(bool detailed, bool preserveAsterisk) { Detailed = detailed; PreserveAsterisk = preserveAsterisk; }
}

// Pure text rules shared by channel adapters. Settings are captured before entry.
internal static class ConversationSpeechTextRules
{
internal static bool IsNpcInventorySummaryHeader(string text)
	{
		string text2 = (text ?? "").Trim();
		if (!text2.StartsWith("【", StringComparison.Ordinal))
		{
			return false;
		}
		if (!text2.EndsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0235"), StringComparison.Ordinal)
			&& !text2.EndsWith(PromptComposerTexts.Get("inventory_limit_suffix"), StringComparison.Ordinal))
		{
			return false;
		}
		return text2.IndexOf(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0236"), StringComparison.Ordinal) > 1 || text2.IndexOf(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0237"), StringComparison.Ordinal) > 1 || text2.IndexOf(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0238"), StringComparison.Ordinal) > 1
			|| text2.IndexOf(PromptComposerTexts.Get("inventory_shop_phrase"), StringComparison.OrdinalIgnoreCase) > 1
			|| text2.IndexOf(PromptComposerTexts.Get("inventory_current_phrase"), StringComparison.OrdinalIgnoreCase) > 1
			|| text2.IndexOf(PromptComposerTexts.Get("inventory_carried_phrase"), StringComparison.OrdinalIgnoreCase) > 1;
	}

internal static bool IsLeakedPromptLineForShout(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0239"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0240"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0241"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0242"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0243"), StringComparison.Ordinal) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0244"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0245"), StringComparison.Ordinal) && text.EndsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0246"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0247"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0248"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0249"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0250"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0251"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0252"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0253"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0254"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0255"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0256"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0257"), StringComparison.Ordinal))
		{
			return true;
		}
		if (string.Equals(text, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0258"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0259"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0260"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0261"), StringComparison.Ordinal))
		{
			return true;
		}
		if (Regex.IsMatch(text, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0262"), RegexOptions.CultureInvariant))
		{
			return true;
		}
		if (IsNpcInventorySummaryHeader(text))
		{
			return true;
		}
		if (text.StartsWith("Gold:", StringComparison.OrdinalIgnoreCase) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0263"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("InventoryItems:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0264"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("BattleEquipment:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.IndexOf(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0265"), StringComparison.Ordinal) >= 0)
		{
			return true;
		}
		if (text.IndexOf("|guidePrice=", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text.IndexOf("|lineValue=", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0266"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0267"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0268"), StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0269"), StringComparison.Ordinal) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0270"), StringComparison.Ordinal) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0271"), StringComparison.Ordinal) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0272"), StringComparison.Ordinal) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0273"), StringComparison.Ordinal) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0274"), StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}

internal static string StripLeakedPromptFragmentsForShout(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		string[] array = new string[10]
		{
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0275"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0276"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0277"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0278"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0279"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0280"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0281"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0282"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0283"),
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0284")
		};
		string[] array2 = text2.Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		for (int i = 0; i < array2.Length; i++)
		{
			string text3 = array2[i] ?? "";
			int num = -1;
			for (int j = 0; j < array.Length; j++)
			{
				int num2 = text3.IndexOf(array[j], StringComparison.Ordinal);
				if (num2 >= 0 && (num < 0 || num2 < num))
				{
					num = num2;
				}
			}
			if (num >= 0)
			{
				text3 = text3.Substring(0, num).TrimEnd();
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(text3);
		}
		return stringBuilder.ToString().Trim();
	}

internal static string StripLeakedPromptContentForShout(string text)
	{
		string text2 = StripLeakedPromptFragmentsForShout((text ?? "").Replace("\r", ""));
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		string[] array = text2.Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = (array[i] ?? "").Trim();
			text3 = ShoutUtils.StripConversationMetadataPrefix(text3);
			if (!string.IsNullOrWhiteSpace(text3) && !IsLeakedPromptLineForShout(text3))
			{
				stringBuilder.AppendLine(text3);
			}
		}
		return stringBuilder.ToString().Trim();
	}

internal static string StripStageDirectionsForPassiveShout(string text, ConversationSpeechTextOptions options)
	{
		if (options.Detailed)
		{
			return ((text ?? "").Replace("\r", "")).Trim();
		}
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = Regex.Replace(text2, "\\（.*?\\）", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\(.*?\\)", "", RegexOptions.Singleline);
		if (!options.PreserveAsterisk)
		{
			text2 = Regex.Replace(text2, "\\*\\*.*?\\*\\*", "", RegexOptions.Singleline);
			text2 = Regex.Replace(text2, "\\*.*?\\*", "", RegexOptions.Singleline);
		}
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		text2 = text2.Trim();
		text2 = text2.TrimStart('，', '。', '、', '；', '：', ',', ';', ':');
		return text2.Trim();
	}

internal static string SanitizeSceneSpeechText(string text, ConversationSpeechTextOptions options, bool cleanNpcReceipts = true)
	{
		string text2 = LocalTtsToneTag.Strip(LlmVisibleReplyNormalizer.NormalizeComplete(text));
		text2 = StripLeakedPromptContentForShout(text2);
		text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
		text2 = StripStageDirectionsForPassiveShout(text2, options);
		text2 = ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[RELAY\\s*:[^\\]\\r\\n]+\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		return (cleanNpcReceipts ? DiplomacyReplyTextRules.CleanNpcText(text2) : text2).Trim(' ', '\t', '\r', '\n', '[', ']', ':');
	}

internal static string PrepareSceneHistorySpeechText(string text)
	{
        text = LocalTtsToneTag.Strip(text);
		string text2 = StripLeakedPromptContentForShout(text);
		text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
		text2 = ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
		text2 = StripAutoGroupRelaySignal(text2);
		text2 = StripAutoGroupStopSignal(text2);
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		return DiplomacyReplyTextRules.CleanNpcText(text2).Trim(' ', '\t', '\r', '\n', '[', ']', ':');
	}

internal static string StripAutoGroupStopSignal(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		text2 = Regex.Replace(text2, "\\[NO_CONTINUE\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[END\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", "", RegexOptions.IgnoreCase);
		return text2.Trim();
	}

internal static string StripAutoGroupRelaySignal(string text)
	{
		return Regex.Replace((text ?? "").Replace("\r", ""), "\\[RELAY\\s*:[^\\]\\r\\n]+\\]", "", RegexOptions.IgnoreCase).Trim();
	}

internal static string StripAfefPromptScopeLabel(string text)
	{
		string value = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		bool changed;
		do
		{
			changed = false;
			string[] prefixes = new string[8]
			{
				ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0285"),
				ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0286"),
				ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0287"),
				ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0288"),
				"[Current action]",
				"[Past action]",
				"【Current action】",
				"【Past action】"
			};
			foreach (string prefix in prefixes)
			{
				if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					value = value.Substring(prefix.Length).Trim();
					changed = true;
				}
			}
		}
		while (changed);
		return value;
	}

internal static string NormalizeNativeConversationFactLineForPrompt(string text, string speaker)
	{
		text = StripAfefPromptScopeLabel((text ?? "").Trim());
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0289"), StringComparison.Ordinal) || text.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0290"), StringComparison.Ordinal))
		{
			return text;
		}
		string speakerText = (speaker ?? "").Trim();
		if (speakerText.IndexOf("NPC", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0291") + text;
		}
		return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0292") + text;
	}

internal static string NormalizeNativeConversationVisibleTextKey(string text, ConversationSpeechTextOptions options)
	{
		// Identity/deduplication also processes player rows; preserve the original key.
		return SanitizeSceneSpeechText(ShoutUtils.StripNamePrefixedLineSafely((text ?? "").Replace("\r", "").Trim(), 30), options, cleanNpcReceipts: false).Trim();
	}

internal static string NormalizeNativeConversationHistoryTextForPostprocess(string text)
	{
		string value = ShoutUtils.StripNamePrefixedLineSafely((text ?? "").Replace("\r", "").Trim(), 30);
		return PrepareSceneHistorySpeechText(value);
	}


internal static string SanitizeSceneSpeechTextForTts(string text)
	{
        text = LocalTtsToneTag.Strip(text);
		string text2 = ExtractContentSectionForTts((text ?? "").Replace("\r", ""));
		text2 = StripLeakedPromptContentForShout(text2);
		text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
		text2 = ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "<think\\b[^>]*>.*?</think>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\[REASONING\\].*?(?=\\[CONTENT\\]|$)", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[RELAY\\s*:[^\\]\\r\\n]+\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\（.*?\\）", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\(.*?\\)", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\*\\*.*?\\*\\*", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\*.*?\\*", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "(^|\\n)\\s*【[^】\\r\\n]{1,40}】", "$1");
		text2 = StripTtsNarrationLines(text2);
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		text2 = Regex.Replace(text2, "\\n{3,}", "\n\n");
		text2 = DiplomacyReplyTextRules.CleanNpcText(text2);
		text2 = text2.Trim(' ', '\t', '\r', '\n', '[', ']', ':', '：', '，', '。', ',', ';', '；');
		return text2.Trim();
	}

internal static string ExtractContentSectionForTts(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		int num = text2.LastIndexOf("[CONTENT]", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			text2 = text2.Substring(num + "[CONTENT]".Length);
		}
		return text2.Trim();
	}

internal static string StripTtsNarrationLines(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Replace("\r", "").Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (Regex.IsMatch(text2, "^(?:OUTPUT|INPUT|REQUEST_BODY|raw_response|reasoning_content|\\[?REASONING\\]?|\\[?CONTENT\\]?)[：:]", RegexOptions.IgnoreCase))
			{
				continue;
			}
			if (Regex.IsMatch(text2, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0293")))
			{
				continue;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(text2);
		}
		return stringBuilder.ToString().Trim();
	}
}
