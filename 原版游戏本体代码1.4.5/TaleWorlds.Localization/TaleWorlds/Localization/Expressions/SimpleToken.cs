using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class SimpleToken : TextExpression
{
	public static readonly SimpleToken SequenceTerminator = new SimpleToken(TokenType.SequenceTerminator, ".");

	private readonly TokenType _tokenType;

	internal override TokenType TokenType => _tokenType;

	public SimpleToken(TokenType tokenType, string value)
	{
		base.RawValue = value;
		_tokenType = tokenType;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return TokenType switch
		{
			TokenType.ParameterWithMultipleMarkerOccurances => context.GetParameterWithMarkerOccurances(base.RawValue, parent), 
			TokenType.ParameterWithMarkerOccurance => context.GetParameterWithMarkerOccurance(base.RawValue, parent), 
			TokenType.FunctionParam => context.GetFunctionParam(base.RawValue).ToStringWithoutClear(), 
			_ => base.RawValue, 
		};
	}
}
