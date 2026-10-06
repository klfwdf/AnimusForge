using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class FieldExpression : TextExpression
{
	private TextExpression _innerExpression;

	private TextExpression part2;

	public string FieldName => base.RawValue;

	public TextExpression InnerExpression => part2;

	internal override TokenType TokenType => TokenType.FieldExpression;

	public FieldExpression(TextExpression innerExpression)
	{
		_innerExpression = innerExpression;
		base.RawValue = innerExpression.RawValue;
	}

	public FieldExpression(TextExpression innerExpression, TextExpression part2)
		: this(innerExpression)
	{
		this.part2 = part2;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return "";
	}
}
