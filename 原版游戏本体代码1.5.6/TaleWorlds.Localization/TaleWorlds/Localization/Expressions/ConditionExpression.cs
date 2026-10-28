using System.Collections.Generic;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class ConditionExpression : TextExpression
{
	private TextExpression[] _conditionExpressions;

	private TextExpression[] _resultExpressions;

	internal override TokenType TokenType => TokenType.ConditionalExpression;

	public ConditionExpression(TextExpression condition, TextExpression part1, TextExpression part2)
	{
		_conditionExpressions = new TextExpression[1] { condition };
		_resultExpressions = new TextExpression[2] { part1, part2 };
	}

	public ConditionExpression(List<TextExpression> conditionExpressions, List<TextExpression> resultExpressions2)
	{
		_conditionExpressions = conditionExpressions.ToArray();
		_resultExpressions = resultExpressions2.ToArray();
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		bool flag = false;
		int num = 0;
		TextExpression textExpression = null;
		while (!flag && num < _conditionExpressions.Length)
		{
			TextExpression textExpression2 = _conditionExpressions[num];
			string text = textExpression2.EvaluateString(context, parent);
			if (text.Length != 0)
			{
				flag = ((textExpression2.TokenType != TokenType.ParameterWithAttribute && textExpression2.TokenType != TokenType.StartsWith) ? (EvaluateAsNumber(textExpression2, context, parent) != 0) : (!string.IsNullOrEmpty(text)));
			}
			if (flag)
			{
				if (num < _resultExpressions.Length)
				{
					textExpression = _resultExpressions[num];
				}
			}
			else
			{
				num++;
			}
		}
		if (textExpression == null && num < _resultExpressions.Length)
		{
			textExpression = _resultExpressions[num];
		}
		return textExpression?.EvaluateString(context, parent) ?? "";
	}
}
