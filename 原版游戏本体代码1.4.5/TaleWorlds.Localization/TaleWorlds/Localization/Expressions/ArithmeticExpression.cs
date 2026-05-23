using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class ArithmeticExpression : NumeralExpression
{
	private readonly ArithmeticOperation _op;

	private readonly TextExpression _exp1;

	private readonly TextExpression _exp2;

	internal override TokenType TokenType
	{
		get
		{
			if (_op != ArithmeticOperation.Add && _op != ArithmeticOperation.Subtract)
			{
				return TokenType.ArithmeticProduct;
			}
			return TokenType.ArithmeticSum;
		}
	}

	public ArithmeticExpression(ArithmeticOperation op, TextExpression exp1, TextExpression exp2)
	{
		_op = op;
		_exp1 = exp1;
		_exp2 = exp2;
		base.RawValue = string.Concat(exp1.RawValue, op, exp2.RawValue);
	}

	internal override int EvaluateNumber(TextProcessingContext context, TextObject parent)
	{
		return _op switch
		{
			ArithmeticOperation.Add => EvaluateAsNumber(_exp1, context, parent) + EvaluateAsNumber(_exp2, context, parent), 
			ArithmeticOperation.Subtract => EvaluateAsNumber(_exp1, context, parent) - EvaluateAsNumber(_exp2, context, parent), 
			ArithmeticOperation.Multiply => EvaluateAsNumber(_exp1, context, parent) * EvaluateAsNumber(_exp2, context, parent), 
			ArithmeticOperation.Divide => EvaluateAsNumber(_exp1, context, parent) / EvaluateAsNumber(_exp2, context, parent), 
			_ => 0, 
		};
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return EvaluateNumber(context, parent).ToString();
	}
}
