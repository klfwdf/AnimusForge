using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class SimpleExpression : TextExpression
{
	private TextExpression _innerExpression;

	internal override TokenType TokenType => TokenType.SimpleExpression;

	public SimpleExpression(TextExpression innerExpression)
	{
		_innerExpression = innerExpression;
		base.RawValue = innerExpression.RawValue;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return _innerExpression.EvaluateString(context, parent);
	}
}
