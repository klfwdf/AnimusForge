using System.Collections.Generic;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class SelectionExpression : TextExpression
{
	private TextExpression _selection;

	private List<TextExpression> _selectionExpressions;

	internal override TokenType TokenType => TokenType.SelectionExpression;

	public SelectionExpression(TextExpression selection, List<TextExpression> selectionExpressions)
	{
		_selection = selection;
		_selectionExpressions = selectionExpressions;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		int num = EvaluateAsNumber(_selection, context, parent);
		if (num >= 0 && num < _selectionExpressions.Count)
		{
			return _selectionExpressions[num].EvaluateString(context, parent);
		}
		return "";
	}
}
