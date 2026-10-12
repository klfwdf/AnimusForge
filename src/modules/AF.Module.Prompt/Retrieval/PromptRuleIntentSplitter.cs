using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

internal static class PromptRuleIntentSplitter
{
	internal static List<string> Split(string input, int maxParts = IntentQueryOptimizer.MaxCombinedIntentCount)
	{
		List<string> list = new List<string>();
		try
		{
			string text = Normalize(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			List<string> list2 = new List<string>();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char c in text)
			{
				if (c == '。' || c == '！' || c == '!' || c == '？' || c == '?' || c == '；' || c == ';' || c == '，' || c == ',' || c == '、' || c == '\n' || c == '\r')
				{
					string text2 = stringBuilder.ToString().Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						list2.Add(text2);
					}
					stringBuilder.Clear();
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			string text3 = stringBuilder.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				list2.Add(text3);
			}
			if (list2.Count <= 0)
			{
				list2.Add(text);
			}
			List<string> list3 = new List<string>();
			string[] array = new string[13]
			{
				PromptRetrievalTexts.Raw("Raw_ca4f5ee82e28"), PromptRetrievalTexts.Raw("Raw_2e331dc332df"), PromptRetrievalTexts.Raw("Raw_3d90e73b8904"), PromptRetrievalTexts.Raw("Raw_65e02db56393"), PromptRetrievalTexts.Raw("Raw_8913b8b6e1ef"), PromptRetrievalTexts.Raw("Raw_c193ed92f77d"), PromptRetrievalTexts.Raw("Raw_5b77181bd648"), PromptRetrievalTexts.Raw("Raw_b9a6fa173c48"), PromptRetrievalTexts.Raw("Raw_83eec76b3911"), PromptRetrievalTexts.Raw("Raw_d5b0967855f4"),
				PromptRetrievalTexts.Raw("Raw_3e67a4cf9e15"), PromptRetrievalTexts.Raw("Raw_7e74d007fe67"), PromptRetrievalTexts.Raw("Raw_1c3b3b6448fd")
			};
			for (int j = 0; j < list2.Count; j++)
			{
				string text4 = (list2[j] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text4))
				{
					continue;
				}
				bool flag = false;
				foreach (string text5 in array)
				{
					int num = text4.IndexOf(text5, StringComparison.Ordinal);
					if (num > 1 && num < text4.Length - text5.Length - 1)
					{
						string text6 = text4.Substring(0, num).Trim();
						string text7 = text4.Substring(num + text5.Length).Trim();
						if (text6.Length >= 2)
						{
							list3.Add(text6);
						}
						if (text7.Length >= 2)
						{
							list3.Add(text7);
						}
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list3.Add(text4);
				}
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			list.Add(text);
			hashSet.Add(text);
			for (int l = 0; l < list3.Count; l++)
			{
				if (list.Count >= Math.Max(1, maxParts))
				{
					break;
				}
				string text8 = Normalize(list3[l]);
				if (!string.IsNullOrWhiteSpace(text8) && text8.Length >= 2 && hashSet.Add(text8))
				{
					list.Add(text8);
				}
			}
		}
		catch
		{
		}
		list = IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
		if (list.Count <= 0)
		{
			string text9 = Normalize(input);
			if (!string.IsNullOrWhiteSpace(text9))
			{
				list = IntentQueryOptimizer.OptimizeSplitIntents(new List<string> { text9 }, 1);
			}
		}
		return list;
	}

    private static string Normalize(string value)
    {
        string text = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
