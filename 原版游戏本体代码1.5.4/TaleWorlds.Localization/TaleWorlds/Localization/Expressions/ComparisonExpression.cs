using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class ComparisonExpression : NumeralExpression
{
	private readonly ComparisonOperation _op;

	private readonly TextExpression _exp1;

	private readonly TextExpression _exp2;

	internal override TokenType TokenType => TokenType.ComparisonExpression;

	public ComparisonExpression(ComparisonOperation op, TextExpression exp1, TextExpression exp2)
	{
		_op = op;
		_exp1 = exp1;
		_exp2 = exp2;
		base.RawValue = string.Concat(exp1.RawValue, op, exp2.RawValue);
	}

	internal bool EvaluateBoolean(TextProcessingContext context, TextObject parent)
	{
		return _op switch
		{
			ComparisonOperation.Equals => EvaluateAsNumber(_exp1, context, parent) == EvaluateAsNumber(_exp2, context, parent), 
			ComparisonOperation.NotEquals => EvaluateAsNumber(_exp1, context, parent) != EvaluateAsNumber(_exp2, context, parent), 
			ComparisonOperation.GreaterThan => EvaluateAsNumber(_exp1, context, parent) > EvaluateAsNumber(_exp2, context, parent), 
			ComparisonOperation.GreaterOrEqual => EvaluateAsNumber(_exp1, context, parent) >= EvaluateAsNumber(_exp2, context, parent), 
			ComparisonOperation.LessThan => EvaluateAsNumber(_exp1, context, parent) < EvaluateAsNumber(_exp2, context, parent), 
			ComparisonOperation.LessOrEqual => EvaluateAsNumber(_exp1, context, parent) <= EvaluateAsNumber(_exp2, context, parent), 
			_ => false, 
		};
	}

	internal override int EvaluateNumber(TextProcessingContext context, TextObject parent)
	{
		if (!EvaluateBoolean(context, parent))
		{
			return 0;
		}
		return 1;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return EvaluateNumber(context, parent).ToString();
	}
}
