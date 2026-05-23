using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library;

public class ProfanityChecker
{
	public enum ProfanityChechkerType
	{
		FalsePositive,
		FalseNegative
	}

	private readonly string[] ProfanityList;

	private readonly string[] AllowList;

	public ProfanityChecker(string[] profanityList, string[] allowList)
	{
		ProfanityList = profanityList;
		AllowList = allowList;
		for (int i = 0; i < ProfanityList.Length; i++)
		{
			ProfanityList[i] = ProfanityList[i].ToLower();
		}
		for (int j = 0; j < AllowList.Length; j++)
		{
			AllowList[j] = AllowList[j].ToLower();
		}
	}

	public bool IsProfane(string word)
	{
		if (string.IsNullOrEmpty(word) || word.Length == 0)
		{
			return false;
		}
		word = word.ToLower();
		if (AllowList.Contains(word))
		{
			return false;
		}
		return ProfanityList.Contains(word);
	}

	public bool ContainsProfanity(string text, ProfanityChechkerType checkType)
	{
		if (string.IsNullOrEmpty(text) || text.Length == 0)
		{
			return false;
		}
		List<string> list = new List<string>();
		string[] profanityList = ProfanityList;
		foreach (string text2 in profanityList)
		{
			if (text.Length >= text2.Length)
			{
				list.Add(text2);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		text = text.ToLower();
		switch (checkType)
		{
		case ProfanityChechkerType.FalsePositive:
			foreach (object item in new Regex(string.Format("(?:{0})", string.Join("|", list).Replace("$", "\\$"), RegexOptions.IgnoreCase)).Matches(text))
			{
				if (!AllowList.Contains(item))
				{
					return true;
				}
			}
			break;
		case ProfanityChechkerType.FalseNegative:
			foreach (object item2 in new Regex("\\w(?<!\\d)[\\w'-]*", RegexOptions.IgnoreCase).Matches(text))
			{
				string value = item2.ToString();
				if (ProfanityList.Contains(value) && !AllowList.Contains(value))
				{
					return true;
				}
			}
			break;
		}
		return false;
	}

	public string CensorText(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			string text2 = text.ToLower();
			StringBuilder stringBuilder = new StringBuilder(text);
			string[] array = text.Split(new char[1] { ' ' });
			for (int i = 0; i < array.Length; i++)
			{
				string text3 = array[i].ToLower();
				string[] profanityList = ProfanityList;
				foreach (string text4 in profanityList)
				{
					string text5 = text3;
					while (text3.Contains(text4) && !AllowList.Contains(text3))
					{
						string text6 = stringBuilder.ToString().ToLower();
						int num = text6.IndexOf(text4, StringComparison.Ordinal);
						if (num < 0)
						{
							num = text2.IndexOf(text4, StringComparison.Ordinal);
							text6.Substring(num, text4.Length);
						}
						int startIndex = text3.IndexOf(text4, StringComparison.Ordinal);
						text3 = text3.Remove(startIndex, text4.Length);
						for (int k = num; k < num + text4.Length; k++)
						{
							stringBuilder[k] = '*';
						}
					}
					text3 = text5;
				}
			}
			return stringBuilder.ToString();
		}
		return string.Empty;
	}
}
