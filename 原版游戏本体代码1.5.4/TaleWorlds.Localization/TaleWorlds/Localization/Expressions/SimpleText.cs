using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class SimpleText : TextExpression
{
	internal override TokenType TokenType => TokenType.Text;

	public SimpleText(string value)
	{
		base.RawValue = value;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return base.RawValue;
	}
}
