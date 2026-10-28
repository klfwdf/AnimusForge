using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal abstract class NumeralExpression : TextExpression
{
	internal abstract int EvaluateNumber(TextProcessingContext context, TextObject parent);
}
