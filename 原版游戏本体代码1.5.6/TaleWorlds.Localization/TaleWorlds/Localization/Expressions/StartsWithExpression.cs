using System;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class StartsWithExpression : TextExpression
{
	private readonly string _parameter;

	private readonly string[] _functionParams;

	internal override TokenType TokenType => TokenType.StartsWith;

	public StartsWithExpression(string identifierName)
	{
		int num = identifierName.IndexOf('(');
		int num2 = identifierName.IndexOf(')');
		_parameter = identifierName.Remove(num);
		_functionParams = identifierName.Substring(num + 1, num2 - num - 1).Split(new char[1] { ',' });
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		TextObject functionParamWithoutEvaluate = context.GetFunctionParamWithoutEvaluate(_parameter);
		(TextObject value, bool doesValueExist) qualifiedVariableValue = context.GetQualifiedVariableValue(functionParamWithoutEvaluate.ToStringWithoutClear(), parent);
		var (textObject, _) = qualifiedVariableValue;
		if (qualifiedVariableValue.doesValueExist)
		{
			string[] functionParams = _functionParams;
			foreach (string text in functionParams)
			{
				if (textObject.ToStringWithoutClear().StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
				{
					return text;
				}
			}
		}
		return "";
	}
}
