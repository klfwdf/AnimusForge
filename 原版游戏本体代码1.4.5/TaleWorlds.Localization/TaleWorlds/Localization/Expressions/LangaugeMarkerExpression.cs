using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class LangaugeMarkerExpression : TextExpression
{
	internal override TokenType TokenType => TokenType.LanguageMarker;

	public LangaugeMarkerExpression(string innerText)
	{
		base.RawValue = innerText;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return base.RawValue;
	}
}
