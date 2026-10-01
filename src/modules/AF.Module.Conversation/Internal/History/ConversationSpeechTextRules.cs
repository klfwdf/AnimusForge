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
		if (!text2.EndsWith("】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说)", StringComparison.Ordinal))
		{
			return false;
		}
		return text2.IndexOf("商铺可用财富与物品", StringComparison.Ordinal) > 1 || text2.IndexOf("当前可用财富与物品", StringComparison.Ordinal) > 1 || text2.IndexOf("携带的所有物资和财富", StringComparison.Ordinal) > 1;
	}

internal static bool IsLeakedPromptLineForShout(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("必须遵守【", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("必须严格遵守", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【对话历史】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【历史对话记录】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【近期对话窗口】", StringComparison.Ordinal) || text.StartsWith("【最近对话历史】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【玩家与", StringComparison.Ordinal) && text.EndsWith("的近期对话】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【当前场景公共对话与互动】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【长期记忆摘要】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【当前对话】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【回复要求】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【输出要求】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【指令】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【请严格按照格式输出】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【常驻发言格式】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【群体对话规则】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("你的回复格式必须严格按照以下执行", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("（你的内心思考内容", StringComparison.Ordinal))
		{
			return true;
		}
		if (string.Equals(text, "*你的动作内容*", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【附加规则", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("现在，请根据以上所有信息", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("请根据以上所有信息", StringComparison.Ordinal))
		{
			return true;
		}
		if (Regex.IsMatch(text, "^\\d+\\.\\s*必须", RegexOptions.CultureInvariant))
		{
			return true;
		}
		if (IsNpcInventorySummaryHeader(text))
		{
			return true;
		}
		if (text.StartsWith("Gold:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("第纳尔:", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("InventoryItems:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.StartsWith("库存物品：", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("BattleEquipment:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.IndexOf("的私人战斗装备:", StringComparison.Ordinal) >= 0)
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
		if (text.StartsWith("【玩家触发了（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【以下是关于（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【触发相关话题/背景】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("[RELAY:接力编号]", StringComparison.Ordinal) || text.StartsWith("这是一场多人聊天", StringComparison.Ordinal) || text.StartsWith("其他人也要说话", StringComparison.Ordinal) || text.StartsWith("如果你觉得下一位还必须接话", StringComparison.Ordinal) || text.StartsWith("若你觉得在你这句之后", StringComparison.Ordinal) || text.StartsWith("如果你觉得这句之后还值得继续聊下去", StringComparison.Ordinal))
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
			"这是一场多人聊天",
			"其他人也要说话",
			"如果你觉得下一位还必须接话",
			"若你觉得在你这句之后",
			"如果你觉得这句之后还值得继续聊下去",
			"[RELAY:接力编号]",
			"接力编号必须从【站在你旁边的人】",
			"id 必须从【站在你旁边的人】",
			"你不能选你自己",
			"才能让其他人发言"
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

internal static string SanitizeSceneSpeechText(string text, ConversationSpeechTextOptions options)
	{
		string text2 = LlmVisibleReplyNormalizer.NormalizeComplete(text);
		text2 = StripLeakedPromptContentForShout(text2);
		text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
		text2 = StripStageDirectionsForPassiveShout(text2, options);
		text2 = ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[RELAY\\s*:[^\\]\\r\\n]+\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		return text2.Trim(' ', '\t', '\r', '\n', '[', ']', ':');
	}

internal static string PrepareSceneHistorySpeechText(string text)
	{
		string text2 = StripLeakedPromptContentForShout(text);
		text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
		text2 = ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
		text2 = StripAutoGroupRelaySignal(text2);
		text2 = StripAutoGroupStopSignal(text2);
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		return text2.Trim(' ', '\t', '\r', '\n', '[', ']', ':');
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
			string[] prefixes = new string[4] { "【当下行为】", "【过往行为】", "[当下行为]", "[过往行为]" };
			foreach (string prefix in prefixes)
			{
				if (value.StartsWith(prefix, StringComparison.Ordinal))
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
		if (text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			return text;
		}
		string speakerText = (speaker ?? "").Trim();
		if (speakerText.IndexOf("NPC", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "[AFEF NPC行为补充] " + text;
		}
		return "[AFEF玩家行为补充] " + text;
	}

internal static string NormalizeNativeConversationVisibleTextKey(string text, ConversationSpeechTextOptions options)
	{
		return SanitizeSceneSpeechText(ShoutUtils.StripNamePrefixedLineSafely((text ?? "").Replace("\r", "").Trim(), 30), options).Trim();
	}

internal static string NormalizeNativeConversationHistoryTextForPostprocess(string text)
	{
		string value = ShoutUtils.StripNamePrefixedLineSafely((text ?? "").Replace("\r", "").Trim(), 30);
		return PrepareSceneHistorySpeechText(value);
	}
}
