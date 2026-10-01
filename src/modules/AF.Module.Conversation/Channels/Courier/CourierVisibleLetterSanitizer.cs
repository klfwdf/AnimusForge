using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

/// <summary>
/// Removes role-play-only thoughts and actions from NPC text that will be shown
/// as courier letter prose. This is intentionally courier-specific: scene and
/// face-to-face dialogue keep their existing presentation rules.
/// </summary>
internal static class CourierVisibleLetterSanitizer
{
	public static string Clean(string text)
	{
		string value = text ?? "";
		if (value.Length == 0)
		{
			return value;
		}

		bool removedMarkup = false;
		if (ContainsRoundParenthesis(value))
		{
			value = RemoveRoundParenthesizedContent(value);
			removedMarkup = true;
		}
		if (value.IndexOf('*') >= 0)
		{
			value = RemoveAsteriskDelimitedContent(value);
			removedMarkup = true;
		}

		return removedMarkup ? NormalizeRemainingWhitespace(value) : value.Trim();
	}

	private static bool ContainsRoundParenthesis(string text)
	{
		return text.IndexOf('(') >= 0
			|| text.IndexOf(')') >= 0
			|| text.IndexOf('（') >= 0
			|| text.IndexOf('）') >= 0;
	}

	private static string RemoveRoundParenthesizedContent(string text)
	{
		StringBuilder result = new StringBuilder(text.Length);
		int depth = 0;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '(' || c == '（')
			{
				depth++;
				continue;
			}
			if (c == ')' || c == '）')
			{
				if (depth > 0)
				{
					depth--;
				}
				continue;
			}
			if (depth == 0)
			{
				result.Append(c);
			}
		}
		return result.ToString();
	}

	private static string RemoveAsteriskDelimitedContent(string text)
	{
		StringBuilder result = new StringBuilder(text.Length);
		int position = 0;
		while (position < text.Length)
		{
			int open = text.IndexOf('*', position);
			if (open < 0)
			{
				result.Append(text, position, text.Length - position);
				break;
			}

			result.Append(text, position, open - position);
			int contentStart = SkipAsteriskRun(text, open);
			int close = text.IndexOf('*', contentStart);
			if (close < 0)
			{
				// A lone marker is removed, but the following prose is retained.
				position = contentStart;
				continue;
			}

			position = SkipAsteriskRun(text, close);
		}
		return result.ToString();
	}

	private static int SkipAsteriskRun(string text, int position)
	{
		int index = position;
		while (index < text.Length && text[index] == '*')
		{
			index++;
		}
		return index;
	}

	private static string NormalizeRemainingWhitespace(string text)
	{
		string value = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
		value = Regex.Replace(value, "[ \\t]+\\n", "\n");
		value = Regex.Replace(value, "\\n[ \\t]+", "\n");
		value = Regex.Replace(value, "\\n{3,}", "\n\n");
		value = Regex.Replace(value, "[ \\t]{2,}", " ");
		return value.Trim();
	}
}
