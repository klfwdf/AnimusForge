using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class ParanthesisExpression : TextExpression
{
	private readonly TextExpression _innerExp;

	internal override TokenType TokenType => TokenType.ParenthesisExpression;

	public ParanthesisExpression(TextExpression innerExpression)
	{
		_innerExp = innerExpression;
		base.RawValue = "(" + innerExpression.RawValue + ")";
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return _innerExp.EvaluateString(context, parent);
	}
}
