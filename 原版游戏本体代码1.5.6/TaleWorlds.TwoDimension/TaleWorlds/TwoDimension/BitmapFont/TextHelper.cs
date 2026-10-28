using System;
using System.Collections.Generic;

namespace TaleWorlds.TwoDimension.BitmapFont;

internal static class TextHelper
{
	internal static int GetIndexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex(List<TextToken> tokens, int startIndex, ILanguage currentLanguage, bool canBreakInZeroWidthSpace = true)
	{
		if (!currentLanguage.DoesLanguageRequireSpaceForNewline())
		{
			for (int num = startIndex; num >= 1; num--)
			{
				if (!currentLanguage.IsCharacterForbiddenAtEndOfLine(tokens[num - 1].Token) && !currentLanguage.IsCharacterForbiddenAtStartOfLine(tokens[num].Token))
				{
					return num;
				}
			}
		}
		else
		{
			for (int num2 = startIndex; num2 >= 0; num2--)
			{
				if (tokens[num2].Type == TextToken.TokenType.EmptyCharacter)
				{
					return num2 + 1;
				}
				if (canBreakInZeroWidthSpace && tokens[num2].Type == TextToken.TokenType.ZeroWidthSpace)
				{
					return num2 + 1;
				}
			}
		}
		return -1;
	}

	internal static int GetIndexOfFirstAppropriateCharacterToMoveToNextLineForwardsFromIndex(List<TextToken> tokens, int startIndex, ILanguage currentLanguage, bool canBreakInZeroWidthSpace = true)
	{
		if (!currentLanguage.DoesLanguageRequireSpaceForNewline())
		{
			for (int i = startIndex; i < tokens.Count; i++)
			{
				if (i > 0 && !currentLanguage.IsCharacterForbiddenAtEndOfLine(tokens[i - 1].Token) && !currentLanguage.IsCharacterForbiddenAtStartOfLine(tokens[i].Token))
				{
					return i;
				}
			}
		}
		else
		{
			for (int j = startIndex; j < tokens.Count; j++)
			{
				if (tokens[j].Type == TextToken.TokenType.EmptyCharacter)
				{
					return j + 1;
				}
				if (canBreakInZeroWidthSpace && tokens[j].Type == TextToken.TokenType.ZeroWidthSpace)
				{
					return j + 1;
				}
			}
		}
		return -1;
	}

	internal static float GetTotalWordWidthBetweenIndices(int startIndex, int endIndex, List<TextToken> tokens, Func<TextToken, Font> getFontForToken, float extraPadding, float requiredFontSize)
	{
		float num = 0f;
		for (int i = startIndex; i < endIndex; i++)
		{
			Font font = getFontForToken(tokens[i]);
			if (font != null)
			{
				float num2 = requiredFontSize / (float)font.Size;
				num += font.GetCharacterWidth(tokens[i].Token, extraPadding) * num2;
			}
		}
		return num;
	}

	internal static bool IsTokenEqualToSeparatorChar(TextToken token, ILanguage currentLanguage)
	{
		if (token != null && token.Type == TextToken.TokenType.Character)
		{
			return token.Token == currentLanguage.GetLineSeperatorChar();
		}
		return false;
	}
}
