using TaleWorlds.Library;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class VariableExpression : TextExpression
{
	private VariableExpression _innerVariable;

	private string _identifierName;

	public string IdentifierName => _identifierName;

	internal override TokenType TokenType => TokenType.Identifier;

	public VariableExpression(string identifierName, VariableExpression innerExpression)
	{
		base.RawValue = identifierName;
		_identifierName = identifierName;
		_innerVariable = innerExpression;
	}

	internal MultiStatement GetValue(TextProcessingContext context, TextObject parent)
	{
		if (_innerVariable == null)
		{
			return context.GetVariableValue(_identifierName, parent);
		}
		MultiStatement value = _innerVariable.GetValue(context, parent);
		if (value != null && value != null)
		{
			foreach (TextExpression subStatement in value.SubStatements)
			{
				if (subStatement is FieldExpression fieldExpression && fieldExpression.FieldName == _identifierName)
				{
					if (fieldExpression.InnerExpression is MultiStatement)
					{
						return fieldExpression.InnerExpression as MultiStatement;
					}
					return new MultiStatement(new TextExpression[1] { fieldExpression.InnerExpression });
				}
			}
		}
		return null;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		MultiStatement value = GetValue(context, parent);
		if (value != null)
		{
			MBStringBuilder mBStringBuilder = default(MBStringBuilder);
			mBStringBuilder.Initialize(16, "EvaluateString");
			foreach (TextExpression subStatement in value.SubStatements)
			{
				if (subStatement != null)
				{
					mBStringBuilder.Append(subStatement.EvaluateString(context, parent));
				}
			}
			return mBStringBuilder.ToStringAndRelease();
		}
		return "";
	}
}
