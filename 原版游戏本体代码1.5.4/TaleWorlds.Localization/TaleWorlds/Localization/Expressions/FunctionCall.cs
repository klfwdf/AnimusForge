using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class FunctionCall : TextExpression
{
	private string _functionName;

	private List<TextExpression> _functionParams;

	internal override TokenType TokenType => TokenType.FunctionCall;

	public FunctionCall(string functionName, IEnumerable<TextExpression> functionParams)
	{
		_functionName = functionName;
		_functionParams = functionParams.ToList();
		base.RawValue = _functionName;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		return context.CallFunction(_functionName, _functionParams, parent).ToStringWithoutClear();
	}
}
