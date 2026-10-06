using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public static class TextParser
{
	public static List<TextToken> Parse(string text, ILanguage currentLanguage)
	{
		List<TextToken> list = new List<TextToken>(text.Length);
		foreach (char c in text)
		{
			if (c != '\r')
			{
				bool cannotEndLineWithCharacter = currentLanguage.IsCharacterForbiddenAtEndOfLine(c);
				bool cannotStartLineWithCharacter = currentLanguage.IsCharacterForbiddenAtStartOfLine(c);
				switch (c)
				{
				case ' ':
					list.Add(TextToken.CreateEmptyCharacter());
					continue;
				case '\t':
					list.Add(TextToken.CreateTab());
					continue;
				case '\n':
					list.Add(TextToken.CreateNewLine());
					continue;
				case '\u00a0':
				case '\u2007':
				case '\u202f':
					list.Add(TextToken.CreateNonBreakingSpaceCharacter());
					continue;
				case '\u200b':
					list.Add(TextToken.CreateZeroWidthSpaceCharacter());
					continue;
				case '\u2060':
					list.Add(TextToken.CreateWordJoinerCharacter());
					continue;
				}
				TextToken textToken = TextToken.CreateCharacter(c);
				textToken.CannotEndLineWithCharacter = cannotEndLineWithCharacter;
				textToken.CannotStartLineWithCharacter = cannotStartLineWithCharacter;
				list.Add(textToken);
			}
		}
		return list;
	}
}
