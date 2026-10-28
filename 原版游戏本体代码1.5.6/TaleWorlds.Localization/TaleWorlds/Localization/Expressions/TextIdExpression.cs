using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class TextIdExpression : TextExpression
{
	internal override TokenType TokenType => TokenType.TextId;

	public TextIdExpression(string innerText)
	{
		base.RawValue = innerText;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return "";
	}
}
