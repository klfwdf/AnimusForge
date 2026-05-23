using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaleWorlds.Library;

namespace TaleWorlds.Localization.TextProcessor;

internal class Tokenizer
{
	private readonly TokenDefinition[] _tokenDefinitions;

	public Tokenizer()
	{
		_tokenDefinitions = new TokenDefinition[44]
		{
			new TokenDefinition(TokenType.ConditionSeperator, "{\\?}", 1),
			new TokenDefinition(TokenType.ConditionStarter, "{\\?", 1),
			new TokenDefinition(TokenType.ConditionFinalizer, "{\\\\\\?}", 1),
			new TokenDefinition(TokenType.FieldStarter, "{@", 1),
			new TokenDefinition(TokenType.FieldFinalizer, "{\\\\@}", 1),
			new TokenDefinition(TokenType.SelectionSeperator, "{#}", 1),
			new TokenDefinition(TokenType.SelectionFinalizer, "{\\\\#}", 1),
			new TokenDefinition(TokenType.SelectionStarter, "{#", 1),
			new TokenDefinition(TokenType.Seperator, "{\\:}", 1),
			new TokenDefinition(TokenType.ConditionFollowUp, "{\\:\\?", 1),
			new TokenDefinition(TokenType.LanguageMarker, "{\\.[a-zA-Z_^%][a-zA-Z\\d_]*}", 1),
			new TokenDefinition(TokenType.TextId, "{=[a-zA-Z\\d_\\!\\*][a-zA-Z\\d_\\.]*}", 1),
			new TokenDefinition(TokenType.CloseBraces, "}", 1),
			new TokenDefinition(TokenType.OpenBraces, "{", 1),
			new TokenDefinition(TokenType.Minus, "\\-", 1),
			new TokenDefinition(TokenType.Multiply, "\\*", 1),
			new TokenDefinition(TokenType.Plus, "\\+", 1),
			new TokenDefinition(TokenType.Divide, "\\/", 1),
			new TokenDefinition(TokenType.Comma, ",", 1),
			new TokenDefinition(TokenType.CloseParenthesis, "\\)", 1),
			new TokenDefinition(TokenType.OpenParenthesis, "\\(", 1),
			new TokenDefinition(TokenType.CloseBrackets, "\\]", 1),
			new TokenDefinition(TokenType.OpenBrackets, "\\[", 1),
			new TokenDefinition(TokenType.ParameterWithMarkerOccurance, "\\$\\d+\\!.[a-zA-Z_][a-zA-Z\\d_]*", 1),
			new TokenDefinition(TokenType.ParameterWithMultipleMarkerOccurances, "\\$\\d+\\!\\.\\[([a-zA-Z]*)\\,([a-zA-Z]*\\,)*([a-zA-Z]+)\\]", 1),
			new TokenDefinition(TokenType.ParameterWithAttribute, "\\$\\d+\\.[a-zA-Z_][a-zA-Z\\d_]*", 1),
			new TokenDefinition(TokenType.StartsWith, "\\$\\d+\\([a-zA-Z_][a-zA-Z\\d_]*\\)", 1),
			new TokenDefinition(TokenType.StartsWith, "\\$\\d+\\(([a-zA-Z\\d_])*(,([a-zA-Z\\d_])*)*\\)", 1),
			new TokenDefinition(TokenType.FunctionParam, "\\$\\d+", 2),
			new TokenDefinition(TokenType.Number, "\\d+", 2),
			new TokenDefinition(TokenType.Match, "match", 1),
			new TokenDefinition(TokenType.QualifiedIdentifier, "[a-zA-Z_][a-zA-Z\\d_]*\\.[a-zA-Z_][a-zA-Z\\d_]*", 1),
			new TokenDefinition(TokenType.FunctionIdentifier, "[a-zA-Z_][a-zA-Z\\d_]*\\(", 1),
			new TokenDefinition(TokenType.Identifier, "[a-zA-Z_][a-zA-Z\\d_]*", 1),
			new TokenDefinition(TokenType.MarkerOccuranceIdentifier, "\\!.[a-zA-Z_][a-zA-Z\\d_]*", 1),
			new TokenDefinition(TokenType.Equals, "==", 1),
			new TokenDefinition(TokenType.NotEquals, "!=", 1),
			new TokenDefinition(TokenType.GreaterOrEqual, ">=", 1),
			new TokenDefinition(TokenType.LessOrEqual, "<=", 1),
			new TokenDefinition(TokenType.GreaterThan, ">", 1),
			new TokenDefinition(TokenType.LessThan, "<", 1),
			new TokenDefinition(TokenType.And, "and", 1),
			new TokenDefinition(TokenType.Or, "or", 1),
			new TokenDefinition(TokenType.Not, "not", 1)
		};
	}

	public List<MBTextToken> Tokenize(string text)
	{
		List<MBTextToken> list = new List<MBTextToken>(2);
		FindTokenMatchesAndText(text, list);
		list.Add(new MBTextToken(TokenType.SequenceTerminator));
		return list;
	}

	private void FindTokenMatchesAndText(string text, List<MBTextToken> mbTokenMatches)
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "FindTokenMatchesAndText");
		int num = 0;
		while (num < text.Length)
		{
			if (text[num] == '{')
			{
				if (mBStringBuilder.Length > 0)
				{
					string value = mBStringBuilder.ToStringAndRelease();
					mBStringBuilder.Initialize(16, "FindTokenMatchesAndText");
					mbTokenMatches.Add(new MBTextToken(TokenType.Text, value));
				}
				int num2 = FindExpressionEnd(text, num + 1);
				if (!FindTokenMatches(text, num, num2, mbTokenMatches))
				{
					mbTokenMatches.Clear();
					string value2 = mBStringBuilder.ToStringAndRelease();
					mbTokenMatches.Add(new MBTextToken(TokenType.Text, value2));
					return;
				}
				num = num2;
			}
			else
			{
				mBStringBuilder.Append(text[num]);
				num++;
			}
		}
		string text2 = mBStringBuilder.ToStringAndRelease();
		if (text2.Length > 0)
		{
			mbTokenMatches.Add(new MBTextToken(TokenType.Text, text2));
		}
	}

	private int FindExpressionEnd(string text, int startIndex)
	{
		int i = startIndex;
		int num = 1;
		for (; i < text.Length; i++)
		{
			if (num <= 0)
			{
				break;
			}
			switch (text[i])
			{
			case '{':
				num++;
				break;
			case '}':
				num--;
				break;
			}
		}
		return i;
	}

	private bool FindTokenMatches(string text, int beginIndex, int endIndex, List<MBTextToken> mbTokenMatches)
	{
		int num = _tokenDefinitions.Length;
		int num2 = beginIndex;
		while (num2 < endIndex)
		{
			bool flag = false;
			for (int i = 0; i < num; i++)
			{
				TokenDefinition tokenDefinition = _tokenDefinitions[i];
				Match match = tokenDefinition.CheckMatch(text, num2);
				if (match != null)
				{
					int num3 = match.Index + match.Length;
					if (num3 != num2)
					{
						mbTokenMatches.Add(new MBTextToken(tokenDefinition.TokenType, match.Value));
						num2 = num3;
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				MBTextManager.ThrowLocalizationError("Unexpected token at position " + num2 + " in:" + text);
				return false;
			}
		}
		return true;
	}
}
