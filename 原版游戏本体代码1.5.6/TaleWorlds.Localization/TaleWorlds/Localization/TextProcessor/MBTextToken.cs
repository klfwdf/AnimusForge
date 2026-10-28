using System;

namespace TaleWorlds.Localization.TextProcessor;

[Serializable]
internal class MBTextToken
{
	internal TokenType TokenType { get; set; }

	public string Value { get; set; }

	internal MBTextToken(TokenType tokenType)
	{
		TokenType = tokenType;
		Value = string.Empty;
	}

	internal MBTextToken(TokenType tokenType, string value)
	{
		TokenType = tokenType;
		Value = value;
	}

	public MBTextToken Clone()
	{
		return new MBTextToken(TokenType, Value);
	}
}
