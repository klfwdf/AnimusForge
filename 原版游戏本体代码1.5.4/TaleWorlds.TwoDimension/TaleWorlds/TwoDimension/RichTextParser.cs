using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public class RichTextParser
{
	public static List<TextToken> Parse(string text)
	{
		int i = 0;
		List<TextToken> list = new List<TextToken>(text.Length);
		for (; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case ' ':
			case '\u3000':
				list.Add(TextToken.CreateEmptyCharacter());
				break;
			case '\t':
				list.Add(TextToken.CreateTab());
				break;
			case '\n':
				list.Add(TextToken.CreateNewLine());
				break;
			case '\u00a0':
			case '\u2007':
			case '\u202f':
				list.Add(TextToken.CreateNonBreakingSpaceCharacter());
				break;
			case '\u200b':
				list.Add(TextToken.CreateZeroWidthSpaceCharacter());
				break;
			case '\u2060':
				list.Add(TextToken.CreateWordJoinerCharacter());
				break;
			case '<':
			{
				int num = i;
				int num2 = -1;
				for (; i < text.Length; i++)
				{
					if (text[i] == '>')
					{
						num2 = i + 1;
						break;
					}
				}
				RichTextTag richTextTag = RichTextTagParser.Parse(text, num, num2);
				if (richTextTag.Type == RichTextTagType.TextAfterError)
				{
					if (num2 == -1)
					{
						list.AddRange(TextToken.CreateTokenArrayFromWord(text.Substring(num, text.Length - num)));
					}
					else if (num2 > num)
					{
						list.AddRange(TextToken.CreateTokenArrayFromWord(text.Substring(num, num2 - num)));
					}
				}
				list.Add(TextToken.CreateTag(richTextTag));
				break;
			}
			default:
				list.Add(TextToken.CreateCharacter(c));
				break;
			case '\r':
				break;
			}
		}
		return list;
	}
}
