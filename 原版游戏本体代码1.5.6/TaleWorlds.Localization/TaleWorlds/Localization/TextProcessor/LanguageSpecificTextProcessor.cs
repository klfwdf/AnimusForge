using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TaleWorlds.Localization.TextProcessor;

public abstract class LanguageSpecificTextProcessor
{
	[ThreadStatic]
	private static List<int> _lowerMarkers;

	public abstract CultureInfo CultureInfoForLanguage { get; }

	private static List<int> LowerMarkers
	{
		get
		{
			if (_lowerMarkers == null)
			{
				_lowerMarkers = new List<int>();
			}
			return _lowerMarkers;
		}
	}

	public abstract void ProcessToken(string sourceText, ref int cursorPos, string token, StringBuilder outputString);

	public abstract void ClearTemporaryData();

	public LanguageSpecificTextProcessor()
	{
	}

	public string Process(string text)
	{
		if (text == null)
		{
			return null;
		}
		bool flag = false;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '{')
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return text;
		}
		List<int> lowerMarkers = _lowerMarkers;
		_lowerMarkers = null;
		StringBuilder stringBuilder = new StringBuilder();
		int i2 = 0;
		while (i2 < text.Length)
		{
			if (text[i2] != '{')
			{
				stringBuilder.Append(text[i2]);
				i2++;
				continue;
			}
			string token = ReadFirstToken(text, ref i2);
			if (IsPostProcessToken(token))
			{
				ProcessTokenInternal(text, ref i2, token, stringBuilder);
			}
		}
		ProcessLowerCaseMarkers(stringBuilder);
		_lowerMarkers = lowerMarkers;
		return stringBuilder.ToString();
	}

	private void ProcessTokenInternal(string sourceText, ref int cursorPos, string token, StringBuilder outputString)
	{
		CultureInfo cultureInfoForLanguage = CultureInfoForLanguage;
		char c = token[1];
		if (c == '^' && token.Length == 2)
		{
			int num = FindNextLetter(sourceText, cursorPos);
			if (num > cursorPos && num < sourceText.Length)
			{
				outputString.Append(sourceText.Substring(cursorPos, num - cursorPos));
			}
			if (num < sourceText.Length)
			{
				outputString.Append(char.ToUpper(sourceText[num], cultureInfoForLanguage));
				cursorPos = num + 1;
			}
		}
		else if (c == '_' && token.Length == 2)
		{
			int num2 = FindNextLetter(sourceText, cursorPos);
			if (num2 > cursorPos && num2 < sourceText.Length)
			{
				outputString.Append(sourceText.Substring(cursorPos, num2 - cursorPos));
			}
			if (num2 < sourceText.Length)
			{
				outputString.Append(char.ToLower(sourceText[num2], cultureInfoForLanguage));
				cursorPos = num2 + 1;
			}
		}
		else if (c == '%' && token.Length == 2)
		{
			LowerMarkers.Add(outputString.Length - 1);
		}
		else
		{
			ProcessToken(sourceText, ref cursorPos, token, outputString);
		}
	}

	private void ProcessLowerCaseMarkers(StringBuilder stringBuilder)
	{
		if (LowerMarkers.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < LowerMarkers.Count; i += 2)
		{
			int num = LowerMarkers[i];
			if (i + 1 < LowerMarkers.Count)
			{
				int num2 = LowerMarkers[i + 1];
				if (num != num2)
				{
					if (num > stringBuilder.Length)
					{
						num = -1;
					}
					int num3 = Math.Min(num2 - num, stringBuilder.Length - num - 1);
					string text = stringBuilder.ToString(num + 1, num3);
					stringBuilder = stringBuilder.Remove(num + 1, num3).Insert(num + 1, text.ToLower());
				}
			}
			else
			{
				if (num > stringBuilder.Length)
				{
					num = -1;
				}
				if (num + 1 < stringBuilder.Length)
				{
					string text2 = stringBuilder.ToString(num + 1, stringBuilder.Length - num - 1);
					stringBuilder = stringBuilder.Remove(num + 1, stringBuilder.Length - num - 1).Insert(num + 1, text2.ToLower());
				}
			}
		}
		LowerMarkers.Clear();
	}

	private static int FindNextLetter(string sourceText, int cursorPos)
	{
		int i = cursorPos;
		if (sourceText.Length > i + "<a style=\"Link.".Length && sourceText.Substring(i, "<a style=\"Link.".Length).Equals("<a style=\"Link."))
		{
			i += "<a style=\"Link.".Length;
			while (sourceText[i++] != '>')
			{
			}
		}
		for (; i < sourceText.Length; i++)
		{
			if (sourceText[i] == '<')
			{
				i += 2;
			}
			if (char.IsLetter(sourceText, i))
			{
				return i;
			}
		}
		return i;
	}

	private static bool IsPostProcessToken(string token)
	{
		if (token.Length > 1 && token[0] == '.')
		{
			return true;
		}
		return false;
	}

	private static string ReadFirstToken(string text, ref int i)
	{
		int num = i;
		while (i < text.Length && text[i] != '}')
		{
			i++;
		}
		int length = i - num - 1;
		if (i < text.Length)
		{
			i++;
		}
		return text.Substring(num + 1, length);
	}
}
