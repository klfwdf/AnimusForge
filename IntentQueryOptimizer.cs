using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class IntentQueryOptimizer
{
	internal const int MaxIntentCountPerSpeaker = 2;

	internal const int MaxCombinedIntentCount = MaxIntentCountPerSpeaker * 2;

	private static readonly string[] LowSignalExactPhrases = new string[30]
	{
		"你好", "您好", "哈喽", "嗨", "hi", "hello", "hey", "喂", "在吗", "在不在",
		"有人吗", "朋友", "兄弟", "老兄", "哥们", "姐妹", "大人", "阁下", "伙计", "你好朋友",
		"你好啊", "您好啊", "你好呀", "您好呀", "打扰了", "请问", "谢谢", "多谢", "好的", "嗯"
	};

	private static readonly string[] LeadingPhrases = new string[38]
	{
		"你好朋友", "你好啊", "您好啊", "你好呀", "您好呀", "你好", "您好", "哈喽", "嗨", "hello",
		"hi", "hey", "喂", "在吗", "在不在", "请问", "打扰了", "麻烦问一下", "想问一下", "想请教一下",
		"我想问", "我想知道", "我想请教", "能告诉我", "能不能告诉我", "能不能说说", "可以告诉我", "可以说说",
		"你知道不知道", "你知道吗", "你知道", "你可知道", "说说", "告诉我", "帮我说说", "麻烦说说",
		"顺便问一下", "顺带问一下"
	};

	private static readonly char[] TrimChars = new char[20]
	{
		' ', '\t', '\r', '\n', '，', '。', '！', '？', '；', '：',
		',', '.', '!', '?', ';', ':', '、', '~', '～', '…'
	};

	public static List<string> OptimizeSplitIntents(IEnumerable<string> intents, int maxCount)
	{
		List<string> list = new List<string>();
		try
		{
			if (intents == null)
			{
				return list;
			}
			int num = Math.Max(1, maxCount);
			List<string> list2 = new List<string>();
			foreach (string intent in intents)
			{
				string text = NormalizeWhitespace(intent);
				if (!string.IsNullOrWhiteSpace(text))
				{
					list2.Add(text);
				}
			}
			if (list2.Count <= 0)
			{
				return list;
			}
			List<string> list3 = new List<string>();
			foreach (string item in list2)
			{
				if (IsLowInformationIntent(item))
				{
					continue;
				}
				string item2 = BuildCanonicalIntent(item);
				if (string.IsNullOrWhiteSpace(item2))
				{
					continue;
				}
				bool flag = false;
				for (int i = 0; i < list3.Count; i++)
				{
					if (AreNearDuplicateCanonical(item2, list3[i]))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(item);
					list3.Add(item2);
					if (list.Count >= num)
					{
						break;
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool IsLowInformationIntent(string text)
	{
		string text2 = NormalizeWhitespace(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return true;
		}
		foreach (string lowSignalExactPhrase in LowSignalExactPhrases)
		{
			if (string.Equals(text2, lowSignalExactPhrase, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		string text3 = BuildCanonicalIntent(text2);
		if (text3.Length < 2)
		{
			return true;
		}
		if (text3.Length < 4 && ContainsOnlyLettersOrCjk(text3))
		{
			return true;
		}
		return false;
	}

	private static string BuildCanonicalIntent(string text)
	{
		string text2 = NormalizeWhitespace(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = text2.Trim(TrimChars);
		bool flag = true;
		while (flag && !string.IsNullOrWhiteSpace(text2))
		{
			flag = false;
			for (int i = 0; i < LeadingPhrases.Length; i++)
			{
				string text3 = LeadingPhrases[i];
				if (!string.IsNullOrWhiteSpace(text3) && text2.StartsWith(text3, StringComparison.OrdinalIgnoreCase))
				{
					text2 = text2.Substring(text3.Length).TrimStart(TrimChars);
					flag = true;
					break;
				}
			}
		}
		text2 = text2.TrimEnd('吗', '么', '嘛', '呢', '呀', '啊', '吧', '呗', '啦', '了');
		text2 = Regex.Replace(text2, "[\\s\\p{P}]+", "", RegexOptions.CultureInvariant);
		return text2.Trim();
	}

	private static bool AreNearDuplicateCanonical(string left, string right)
	{
		string text = left ?? "";
		string text2 = right ?? "";
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		if (string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string text3 = (text.Length >= text2.Length) ? text : text2;
		string text4 = (text.Length < text2.Length) ? text : text2;
		if (text4.Length < 6)
		{
			return false;
		}
		if (text3.IndexOf(text4, StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}
		return (double)text4.Length / (double)text3.Length >= 0.78;
	}

	private static bool ContainsOnlyLettersOrCjk(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (!char.IsLetter(c) && !IsCjk(c))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsCjk(char ch)
	{
		return (ch >= '\u4E00' && ch <= '\u9FFF') || (ch >= '\u3400' && ch <= '\u4DBF');
	}

	private static string NormalizeWhitespace(string text)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		bool flag = false;
		for (int i = 0; i < text2.Length; i++)
		{
			char c = text2[i];
			if (char.IsWhiteSpace(c))
			{
				if (!flag)
				{
					stringBuilder.Append(' ');
					flag = true;
				}
			}
			else
			{
				stringBuilder.Append(c);
				flag = false;
			}
		}
		return stringBuilder.ToString().Trim();
	}
}
