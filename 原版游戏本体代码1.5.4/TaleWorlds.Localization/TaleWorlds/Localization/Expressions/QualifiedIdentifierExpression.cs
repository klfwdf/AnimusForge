using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class QualifiedIdentifierExpression : TextExpression
{
	private readonly string _identifierName;

	public string IdentifierName => _identifierName;

	internal override TokenType TokenType => TokenType.QualifiedIdentifier;

	public QualifiedIdentifierExpression(string identifierName)
	{
		_identifierName = identifierName;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return context.GetQualifiedVariableValue(_identifierName, parent).value.ToStringWithoutClear();
	}
}
