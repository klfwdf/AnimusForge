using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal abstract class TextExpression
{
	internal abstract TokenType TokenType { get; }

	internal string RawValue { get; set; }

	internal abstract string EvaluateString(TextProcessingContext context, TextObject parent);

	internal int EvaluateAsNumber(TextExpression exp, TextProcessingContext context, TextObject parent)
	{
		if (exp is NumeralExpression numeralExpression)
		{
			return numeralExpression.EvaluateNumber(context, parent);
		}
		if (int.TryParse(exp.EvaluateString(context, parent), out var result))
		{
			return result;
		}
		if (exp.RawValue == null)
		{
			return 0;
		}
		if (exp.RawValue.Length != 0)
		{
			return 1;
		}
		return 0;
	}
}
