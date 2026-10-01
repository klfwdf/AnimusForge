using System;
using System.Globalization;
using System.Text;

namespace AnimusForge;

public static class AnimusForgeTextInputSanitizer
{
	public const int MaxNativeConversationChars = 6000;
	public const int MaxShoutInputChars = 6000;
	public const int MaxCourierLetterChars = 12000;
	public const int MaxLongEditorChars = 60000;
	public const int MaxPolicyNameChars = 100;
	public const int MaxPolicyContentChars = 6000;

	private const int PreNormalizeScanMultiplier = 4;

	public static string SanitizeSingleLine(string text, int maxChars)
	{
		return Sanitize(text, allowNewLines: false, maxChars);
	}

	public static string SanitizeMultiline(string text, int maxChars)
	{
		return Sanitize(text, allowNewLines: true, maxChars);
	}

	public static string Sanitize(string text, bool allowNewLines, int maxChars)
	{
		if (string.IsNullOrEmpty(text) || maxChars == 0)
		{
			return string.Empty;
		}

		text = LimitScanInput(text, maxChars);
		text = text.Replace("\r\n", "\n").Replace('\r', '\n');
		try
		{
			text = text.Normalize(NormalizationForm.FormC);
		}
		catch (ArgumentException)
		{
		}

		int limit = maxChars > 0 ? maxChars : int.MaxValue;
		StringBuilder builder = new StringBuilder(Math.Min(text.Length, limit));
		for (int i = 0; i < text.Length && builder.Length < limit; i++)
		{
			char c = text[i];
			if (c == '<' || c == '>')
			{
				builder.Append(' ');
				continue;
			}
			if (c == '\n')
			{
				builder.Append(allowNewLines ? '\n' : ' ');
				continue;
			}
			if (c == '\t')
			{
				builder.Append(allowNewLines ? '\t' : ' ');
				continue;
			}
			if (IsSpaceLike(c))
			{
				builder.Append(' ');
				continue;
			}
			if (ShouldDrop(c))
			{
				continue;
			}
			if (char.IsControl(c))
			{
				continue;
			}
			builder.Append(c);
		}
		return builder.ToString();
	}

	public static string SanitizeCodePoint(int codePoint, bool allowNewLines)
	{
		if (codePoint < 0 || codePoint > char.MaxValue)
		{
			return string.Empty;
		}
		char c = (char)codePoint;
		return Sanitize(c.ToString(), allowNewLines, 1);
	}

	public static bool IsSafeEditableCodePoint(int codePoint, bool allowNewLines)
	{
		return !string.IsNullOrEmpty(SanitizeCodePoint(codePoint, allowNewLines));
	}

	public static int ResolveUnboundedMaxChars(int configuredMaxLength)
	{
		return configuredMaxLength >= 0 ? configuredMaxLength : MaxLongEditorChars;
	}

	private static string LimitScanInput(string text, int maxChars)
	{
		if (maxChars <= 0)
		{
			return text;
		}
		long scanLimit = (long)maxChars * PreNormalizeScanMultiplier;
		if (scanLimit > int.MaxValue)
		{
			scanLimit = int.MaxValue;
		}
		if (text.Length <= scanLimit)
		{
			return text;
		}
		return text.Substring(0, (int)scanLimit);
	}

	private static bool IsSpaceLike(char c)
	{
		switch (c)
		{
		case '\u00A0':
		case '\u1680':
		case '\u2000':
		case '\u2001':
		case '\u2002':
		case '\u2003':
		case '\u2004':
		case '\u2005':
		case '\u2006':
		case '\u2007':
		case '\u2008':
		case '\u2009':
		case '\u200A':
		case '\u202F':
		case '\u205F':
		case '\u3000':
			return true;
		default:
			return false;
		}
	}

	private static bool ShouldDrop(char c)
	{
		switch (c)
		{
		case '\u200B':
		case '\u200C':
		case '\u200D':
		case '\u2060':
		case '\uFEFF':
		case '\uFFFD':
			return true;
		}

		UnicodeCategory category = char.GetUnicodeCategory(c);
		return category == UnicodeCategory.Surrogate
			|| category == UnicodeCategory.PrivateUse
			|| category == UnicodeCategory.OtherNotAssigned
			|| category == UnicodeCategory.Format;
	}
}
