using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class ParameterWithAttributeExpression : TextExpression
{
	private readonly string _parameter;

	private readonly string _attribute;

	internal override TokenType TokenType => TokenType.ParameterWithAttribute;

	public ParameterWithAttributeExpression(string identifierName)
	{
		_parameter = identifierName.Remove(identifierName.IndexOf('.'));
		_attribute = identifierName.Substring(identifierName.IndexOf('.'));
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		TextObject functionParamWithoutEvaluate = context.GetFunctionParamWithoutEvaluate(_parameter);
		(TextObject value, bool doesValueExist) qualifiedVariableValue = context.GetQualifiedVariableValue(functionParamWithoutEvaluate.ToStringWithoutClear() + _attribute, parent);
		var (textObject, _) = qualifiedVariableValue;
		if (qualifiedVariableValue.doesValueExist)
		{
			return textObject.ToStringWithoutClear();
		}
		return "";
	}
}
