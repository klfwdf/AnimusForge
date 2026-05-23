using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class SimpleNumberExpression : TextExpression
{
	internal override TokenType TokenType => TokenType.Number;

	public SimpleNumberExpression(string value)
	{
		base.RawValue = value;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return base.RawValue;
	}
}
