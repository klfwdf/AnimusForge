using System;
using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public class TextToken
{
	public enum TokenType
	{
		EmptyCharacter,
		ZeroWidthSpace,
		NonBreakingSpace,
		WordJoiner,
		NewLine,
		Tab,
		Character,
		Tag
	}

	public char Token { get; private set; }

	public TokenType Type { get; private set; }

	public RichTextTag Tag { get; private set; }

	public bool CannotStartLineWithCharacter { get; set; }

	public bool CannotEndLineWithCharacter { get; set; }

	private TextToken(TokenType type, char token)
	{
		Type = type;
		Token = token;
	}

	private TextToken(RichTextTag tag)
	{
		Type = TokenType.Tag;
		Tag = tag;
	}

	public static TextToken CreateEmptyCharacter()
	{
		return new TextToken(TokenType.EmptyCharacter, ' ');
	}

	public static TextToken CreateZeroWidthSpaceCharacter()
	{
		return new TextToken(TokenType.ZeroWidthSpace, '\0');
	}

	public static TextToken CreateNonBreakingSpaceCharacter()
	{
		return new TextToken(TokenType.NonBreakingSpace, ' ');
	}

	public static TextToken CreateWordJoinerCharacter()
	{
		return new TextToken(TokenType.WordJoiner, Convert.ToChar(8288));
	}

	public static TextToken CreateNewLine()
	{
		return new TextToken(TokenType.NewLine, '\n');
	}

	public static TextToken CreateTab()
	{
		return new TextToken(TokenType.Tab, '\t');
	}

	public static TextToken CreateCharacter(char character)
	{
		return new TextToken(TokenType.Character, character);
	}

	public static TextToken CreateTag(RichTextTag tag)
	{
		return new TextToken(tag);
	}

	public static TextToken CreateCharacterCannotEndLineWith(char character)
	{
		return new TextToken(TokenType.Character, character)
		{
			CannotEndLineWithCharacter = true
		};
	}

	public static TextToken CreateCharacterCannotStartLineWith(char character)
	{
		return new TextToken(TokenType.Character, character)
		{
			CannotStartLineWithCharacter = true
		};
	}

	public static List<TextToken> CreateTokenArrayFromWord(string word)
	{
		List<TextToken> list = new List<TextToken>();
		foreach (char character in word)
		{
			list.Add(CreateCharacter(character));
		}
		return list;
	}
}
