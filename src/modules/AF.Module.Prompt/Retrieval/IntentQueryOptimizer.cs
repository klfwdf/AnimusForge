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
		PromptRetrievalTexts.Raw("Raw_670d9743542c"), PromptRetrievalTexts.Raw("Raw_609bc0751d56"), PromptRetrievalTexts.Raw("Raw_ba7dbdda7204"), PromptRetrievalTexts.Raw("Raw_c2defdcad302"), "hi", "hello", "hey", PromptRetrievalTexts.Raw("Raw_ea8ea533443d"), PromptRetrievalTexts.Raw("Raw_42e5dbf9def6"), PromptRetrievalTexts.Raw("Raw_eddbaaf1382f"),
		PromptRetrievalTexts.Raw("Raw_29365f35b1ea"), PromptRetrievalTexts.Raw("Raw_677aad1a0ee4"), PromptRetrievalTexts.Raw("Raw_f6c71210522b"), PromptRetrievalTexts.Raw("Raw_a8a080f22176"), PromptRetrievalTexts.Raw("Raw_c7517eabd1a4"), PromptRetrievalTexts.Raw("Raw_22a82bc61fef"), PromptRetrievalTexts.Raw("Raw_44dd6014a532"), PromptRetrievalTexts.Raw("Raw_d59d3f60093b"), PromptRetrievalTexts.Raw("Raw_3427f58f0367"), PromptRetrievalTexts.Raw("Raw_2d68ccf36c8b"),
		PromptRetrievalTexts.Raw("Raw_f63579f508ad"), PromptRetrievalTexts.Raw("Raw_9dabd2ff0d07"), PromptRetrievalTexts.Raw("Raw_d2033138c3b3"), PromptRetrievalTexts.Raw("Raw_308f3f8b2f76"), PromptRetrievalTexts.Raw("Raw_c7d5119a60f6"), PromptRetrievalTexts.Raw("Raw_42be23d8344a"), PromptRetrievalTexts.Raw("Raw_1bf7ac688560"), PromptRetrievalTexts.Raw("Raw_d037f54b4193"), PromptRetrievalTexts.Raw("Raw_536edaf7cdd1"), PromptRetrievalTexts.Raw("Raw_821ea6fd74a7")
	};

	private static readonly string[] LeadingPhrases = new string[38]
	{
		PromptRetrievalTexts.Raw("Raw_2d68ccf36c8b"), PromptRetrievalTexts.Raw("Raw_f63579f508ad"), PromptRetrievalTexts.Raw("Raw_9dabd2ff0d07"), PromptRetrievalTexts.Raw("Raw_d2033138c3b3"), PromptRetrievalTexts.Raw("Raw_308f3f8b2f76"), PromptRetrievalTexts.Raw("Raw_670d9743542c"), PromptRetrievalTexts.Raw("Raw_609bc0751d56"), PromptRetrievalTexts.Raw("Raw_ba7dbdda7204"), PromptRetrievalTexts.Raw("Raw_c2defdcad302"), "hello",
		"hi", "hey", PromptRetrievalTexts.Raw("Raw_ea8ea533443d"), PromptRetrievalTexts.Raw("Raw_42e5dbf9def6"), PromptRetrievalTexts.Raw("Raw_eddbaaf1382f"), PromptRetrievalTexts.Raw("Raw_42be23d8344a"), PromptRetrievalTexts.Raw("Raw_c7d5119a60f6"), PromptRetrievalTexts.Raw("Raw_9f018fe48c5b"), PromptRetrievalTexts.Raw("Raw_77e53f095a76"), PromptRetrievalTexts.Raw("Raw_9c06ab11aa25"),
		PromptRetrievalTexts.Raw("Raw_95d4a33c6b12"), PromptRetrievalTexts.Raw("Raw_a13d4472a2e2"), PromptRetrievalTexts.Raw("Raw_42edaea0b1f8"), PromptRetrievalTexts.Raw("Raw_20e3fd8bb7d2"), PromptRetrievalTexts.Raw("Raw_4ecfbe2d3b5b"), PromptRetrievalTexts.Raw("Raw_b50cddeaeeb7"), PromptRetrievalTexts.Raw("Raw_ad1fda86453e"), PromptRetrievalTexts.Raw("Raw_a0fb6f2f390a"),
		PromptRetrievalTexts.Raw("Raw_6501f519f0b9"), PromptRetrievalTexts.Raw("Raw_39b4d254b443"), PromptRetrievalTexts.Raw("Raw_369a2a63eb6c"), PromptRetrievalTexts.Raw("Raw_e84d4b153710"), PromptRetrievalTexts.Raw("Raw_b6ce96340e0b"), PromptRetrievalTexts.Raw("Raw_3b888814e45a"), PromptRetrievalTexts.Raw("Raw_e83bd36bbb70"), PromptRetrievalTexts.Raw("Raw_82d9007350d7"),
		PromptRetrievalTexts.Raw("Raw_94af60cfaae2"), PromptRetrievalTexts.Raw("Raw_a57c6bc40758")
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
text2 = text2.TrimEnd(PromptRetrievalTexts.Raw("ModalParticles_Cn").ToCharArray());
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
