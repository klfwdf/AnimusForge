using System.Text.RegularExpressions;

namespace TaleWorlds.Localization.TextProcessor;

internal class TokenDefinition
{
	private readonly Regex _regex;

	public TokenType TokenType { get; private set; }

	public int Precedence { get; private set; }

	public TokenDefinition(TokenType tokenType, string regexPattern, int precedence)
	{
		_regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		TokenType = tokenType;
		Precedence = precedence;
	}

	internal Match CheckMatch(string str, int beginIndex)
	{
		beginIndex = SkipWhiteSpace(str, beginIndex);
		Match match = _regex.Match(str, beginIndex);
		if (match.Success && match.Index == beginIndex)
		{
			return match;
		}
		return null;
	}

	private int SkipWhiteSpace(string str, int beginIndex)
	{
		int i = beginIndex;
		for (int length = str.Length; i < length && char.IsWhiteSpace(str[i]); i++)
		{
		}
		return i;
	}
}
