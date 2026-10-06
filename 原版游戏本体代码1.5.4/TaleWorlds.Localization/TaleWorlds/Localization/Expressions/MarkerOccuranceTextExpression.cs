using System;
using TaleWorlds.Library;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class MarkerOccuranceTextExpression : TextExpression
{
	private VariableExpression _innerVariable;

	private string _identifierName;

	public string IdentifierName => _identifierName;

	internal override TokenType TokenType => TokenType.MarkerOccuranceExpression;

	public MarkerOccuranceTextExpression(string identifierName, VariableExpression innerExpression)
	{
		base.RawValue = identifierName;
		_identifierName = identifierName;
		_innerVariable = innerExpression;
	}

	private string MarkerOccuranceExpression(string identifierName, string text)
	{
		int i = 0;
		int num = 0;
		int num2 = 0;
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "MarkerOccuranceExpression");
		for (; i < text.Length; i++)
		{
			if (text[i] != '{')
			{
				if (num == 1 && num2 == 0)
				{
					mBStringBuilder.Append(text[i]);
				}
				continue;
			}
			string text2 = TextProcessingContext.ReadFirstToken(text, ref i);
			if (TextProcessingContext.IsDeclarationFinalizer(text2))
			{
				num--;
				if (num2 > num)
				{
					num2 = num;
				}
			}
			else if (TextProcessingContext.IsDeclaration(text2))
			{
				string strB = text2.Substring(1);
				bool num3 = num2 == num && string.Compare(identifierName, strB, StringComparison.InvariantCultureIgnoreCase) == 0;
				num++;
				if (num3)
				{
					num2 = num;
				}
			}
		}
		return mBStringBuilder.ToStringAndRelease();
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		MultiStatement value = _innerVariable.GetValue(context, parent);
		if (value != null)
		{
			foreach (TextExpression subStatement in value.SubStatements)
			{
				if (subStatement.TokenType == TokenType.LanguageMarker && subStatement.RawValue.Substring(2, subStatement.RawValue.Length - 3) == IdentifierName)
				{
					return "1";
				}
			}
		}
		return "0";
	}
}
