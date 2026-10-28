using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization.Expressions;

namespace TaleWorlds.Localization.TextProcessor;

internal class MBTextParser
{
	[ThreadStatic]
	private static MBTextParser _instance;

	private Stack<TextExpression> _symbolSequence = new Stack<TextExpression>();

	private TextExpression _lookaheadFirst;

	private TextExpression _lookaheadSecond;

	private TextExpression _lookaheadThird;

	private MBTextModel _queryModel;

	internal TextExpression LookAheadFirst => _lookaheadFirst;

	internal TextExpression LookAheadSecond => _lookaheadSecond;

	internal TextExpression LookAheadThird => _lookaheadThird;

	private static void Clear()
	{
		_instance._symbolSequence.Clear();
		_instance._lookaheadFirst = null;
		_instance._lookaheadSecond = null;
		_instance._lookaheadThird = null;
	}

	private TextExpression GetSimpleToken(TokenType tokenType, string strValue)
	{
		return tokenType switch
		{
			TokenType.Text => new SimpleText(strValue), 
			TokenType.Number => new SimpleNumberExpression(strValue), 
			TokenType.Identifier => new VariableExpression(strValue, null), 
			TokenType.LanguageMarker => new LangaugeMarkerExpression(strValue), 
			TokenType.TextId => new TextIdExpression(strValue), 
			TokenType.QualifiedIdentifier => new QualifiedIdentifierExpression(strValue), 
			TokenType.ParameterWithAttribute => new ParameterWithAttributeExpression(strValue), 
			TokenType.StartsWith => new StartsWithExpression(strValue), 
			_ => new SimpleToken(tokenType, strValue), 
		};
	}

	private void LoadSequenceStack(List<MBTextToken> tokens)
	{
		for (int num = tokens.Count - 1; num >= 0; num--)
		{
			TextExpression simpleToken = GetSimpleToken(tokens[num].TokenType, tokens[num].Value);
			_symbolSequence.Push(simpleToken);
		}
	}

	private void PushToken(TextExpression token)
	{
		_symbolSequence.Push(token);
		UpdateLookAheads();
	}

	private void UpdateLookAheads()
	{
		if (_symbolSequence.Count == 0)
		{
			_lookaheadFirst = SimpleToken.SequenceTerminator;
		}
		else
		{
			_lookaheadFirst = _symbolSequence.Peek();
		}
		if (_symbolSequence.Count < 2)
		{
			_lookaheadSecond = SimpleToken.SequenceTerminator;
		}
		else
		{
			TextExpression item = _symbolSequence.Pop();
			_lookaheadSecond = _symbolSequence.Peek();
			_symbolSequence.Push(item);
		}
		if (_symbolSequence.Count < 3)
		{
			_lookaheadThird = SimpleToken.SequenceTerminator;
			return;
		}
		TextExpression item2 = _symbolSequence.Pop();
		TextExpression item3 = _symbolSequence.Pop();
		_lookaheadThird = _symbolSequence.Peek();
		_symbolSequence.Push(item3);
		_symbolSequence.Push(item2);
	}

	private void DiscardToken()
	{
		if (_symbolSequence.Count > 0)
		{
			_symbolSequence.Pop();
		}
		UpdateLookAheads();
	}

	private void DiscardToken(TokenType tokenType)
	{
		if (_lookaheadFirst.TokenType != tokenType)
		{
			MBTextManager.ThrowLocalizationError(string.Format("Unxpected token: {1} while expecting: {0}", tokenType.ToString().ToUpper(), _lookaheadFirst.RawValue));
		}
		DiscardToken();
	}

	private void Statements()
	{
		TextExpression rootExpressions = GetRootExpressions();
		_queryModel.AddRootExpression(rootExpressions);
	}

	private bool IsRootExpression(TokenType tokenType)
	{
		if (tokenType != TokenType.Text && tokenType != TokenType.SimpleExpression && tokenType != TokenType.ConditionalExpression && tokenType != TokenType.TextId && tokenType != TokenType.SelectionExpression && tokenType != TokenType.MultiStatement && tokenType != TokenType.FieldExpression)
		{
			return tokenType == TokenType.LanguageMarker;
		}
		return true;
	}

	private void GetRootExpressionsImp(List<TextExpression> expList)
	{
		while (true)
		{
			if (!RunRootGrammarRulesExceptCollapse())
			{
				if (!IsRootExpression(LookAheadFirst.TokenType))
				{
					break;
				}
				TextExpression lookAheadFirst = LookAheadFirst;
				DiscardToken();
				expList.Add(lookAheadFirst);
			}
		}
	}

	private TextExpression GetRootExpressions()
	{
		List<TextExpression> list = new List<TextExpression>();
		GetRootExpressionsImp(list);
		if (list.Count == 0)
		{
			return null;
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		return new MultiStatement(list);
	}

	private bool RunRootGrammarRulesExceptCollapse()
	{
		if (!CheckSimpleStatement() && !CheckConditionalStatement() && !CheckSelectionStatement())
		{
			return CheckFieldStatement();
		}
		return true;
	}

	private bool CollapseStatements()
	{
		if (!IsRootExpression(LookAheadFirst.TokenType) || LookAheadFirst.TokenType == TokenType.MultiStatement)
		{
			return false;
		}
		List<TextExpression> list = new List<TextExpression>();
		TextExpression lookAheadFirst = LookAheadFirst;
		DiscardToken();
		list.Add(lookAheadFirst);
		bool flag = false;
		while (!flag)
		{
			while (RunRootGrammarRulesExceptCollapse())
			{
			}
			if (IsRootExpression(LookAheadFirst.TokenType))
			{
				TextExpression lookAheadFirst2 = LookAheadFirst;
				DiscardToken();
				list.Add(lookAheadFirst2);
			}
			else
			{
				flag = true;
			}
		}
		PushToken(new MultiStatement(list));
		return true;
	}

	private bool CheckSimpleStatement()
	{
		if (LookAheadFirst.TokenType != TokenType.OpenBraces)
		{
			return false;
		}
		DiscardToken(TokenType.OpenBraces);
		bool flag = false;
		while (!flag)
		{
			flag = !DoExpressionRules();
		}
		TextExpression textExpression = null;
		TokenType tokenType = LookAheadFirst.TokenType;
		if (IsArithmeticExpression(tokenType))
		{
			textExpression = new SimpleExpression(LookAheadFirst);
			DiscardToken();
			DiscardToken(TokenType.CloseBraces);
			PushToken(textExpression);
		}
		else
		{
			DiscardToken(TokenType.CloseBraces);
		}
		return true;
	}

	private bool CheckFieldStatement()
	{
		if (LookAheadFirst.TokenType != TokenType.FieldStarter)
		{
			return false;
		}
		DiscardToken(TokenType.FieldStarter);
		bool flag = false;
		while (!flag)
		{
			flag = !DoExpressionRules();
		}
		if (LookAheadFirst.TokenType != TokenType.Identifier)
		{
			Debug.FailedAssert("Can not parse the text: " + LookAheadFirst, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\TextProcessor\\MbTextParser.cs", "CheckFieldStatement", 299);
			return false;
		}
		TextExpression lookAheadFirst = LookAheadFirst;
		DiscardToken(TokenType.Identifier);
		DiscardToken(TokenType.CloseBraces);
		TextExpression textExpression = GetRootExpressions();
		if (textExpression == null)
		{
			textExpression = new SimpleToken(TokenType.Text, "");
		}
		DiscardToken(TokenType.FieldFinalizer);
		FieldExpression token = new FieldExpression(lookAheadFirst, textExpression);
		PushToken(token);
		return true;
	}

	private bool CheckConditionalStatement()
	{
		if (LookAheadFirst.TokenType != TokenType.ConditionStarter)
		{
			return false;
		}
		bool flag = false;
		List<TextExpression> list = new List<TextExpression>();
		List<TextExpression> list2 = new List<TextExpression>();
		while (!flag)
		{
			TokenType tokenType = LookAheadFirst.TokenType;
			if (LookAheadFirst.TokenType == TokenType.ConditionStarter || LookAheadFirst.TokenType == TokenType.ConditionFollowUp)
			{
				DiscardToken();
				while (DoExpressionRules())
				{
				}
				tokenType = LookAheadFirst.TokenType;
				if (!IsArithmeticExpression(tokenType))
				{
					Debug.FailedAssert("Can not parse the text: " + LookAheadFirst, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\TextProcessor\\MbTextParser.cs", "CheckConditionalStatement", 346);
					return false;
				}
				list.Add(LookAheadFirst);
				DiscardToken();
				DiscardToken(TokenType.CloseBraces);
			}
			else
			{
				if (tokenType != TokenType.ConditionSeperator && tokenType != TokenType.Seperator)
				{
					MBTextManager.ThrowLocalizationError("Can not parse the text: " + LookAheadFirst);
					return false;
				}
				DiscardToken();
				flag = true;
			}
			TextExpression textExpression = GetRootExpressions();
			if (textExpression == null)
			{
				textExpression = new SimpleToken(TokenType.Text, "");
			}
			list2.Add(textExpression);
		}
		while (!flag)
		{
		}
		DiscardToken(TokenType.ConditionFinalizer);
		ConditionExpression token = new ConditionExpression(list, list2);
		PushToken(token);
		return true;
	}

	private bool CheckSelectionStatement()
	{
		if (LookAheadFirst.TokenType != TokenType.SelectionStarter)
		{
			return false;
		}
		DiscardToken(TokenType.SelectionStarter);
		while (DoExpressionRules())
		{
		}
		TokenType tokenType = LookAheadFirst.TokenType;
		if (!IsArithmeticExpression(tokenType))
		{
			Debug.FailedAssert("Can not parse the text: " + LookAheadFirst, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\TextProcessor\\MbTextParser.cs", "CheckSelectionStatement", 392);
			return false;
		}
		TextExpression lookAheadFirst = LookAheadFirst;
		DiscardToken();
		DiscardToken(TokenType.CloseBraces);
		bool flag = false;
		List<TextExpression> list = new List<TextExpression>();
		do
		{
			TextExpression textExpression = GetRootExpressions();
			if (textExpression == null)
			{
				textExpression = new SimpleToken(TokenType.Text, "");
			}
			list.Add(textExpression);
			switch (LookAheadFirst.TokenType)
			{
			case TokenType.SelectionSeperator:
				DiscardToken();
				break;
			case TokenType.SelectionFinalizer:
				flag = true;
				DiscardToken();
				break;
			default:
				Debug.FailedAssert("Can not parse the text: " + LookAheadFirst, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\TextProcessor\\MbTextParser.cs", "CheckSelectionStatement", 424);
				return false;
			}
		}
		while (!flag);
		SelectionExpression token = new SelectionExpression(lookAheadFirst, list);
		PushToken(token);
		return true;
	}

	private bool DoExpressionRules()
	{
		if (ConsumeArrayAccessExpression() || ConsumeFunction() || ConsumeMarkerOccuranceExpression() || ConsumeNegativeAritmeticExpression() || ConsumeParenthesisExpression() || ConsumeInnerAritmeticExpression() || ConsumeOuterAritmeticExpression() || ConsumeComparisonExpression())
		{
			return true;
		}
		return false;
	}

	private bool ConsumeFunction()
	{
		if (LookAheadFirst.TokenType != TokenType.FunctionIdentifier)
		{
			return false;
		}
		string functionName = LookAheadFirst.RawValue.Substring(0, LookAheadFirst.RawValue.Length - 1);
		DiscardToken();
		bool flag = false;
		List<TextExpression> list = new List<TextExpression>();
		while (LookAheadFirst.TokenType != TokenType.CloseParenthesis && !flag)
		{
			if (list.Count > 0)
			{
				DiscardToken(TokenType.Comma);
			}
			while (DoExpressionRules())
			{
			}
			TokenType tokenType = LookAheadFirst.TokenType;
			if (IsArithmeticExpression(tokenType))
			{
				list.Add(LookAheadFirst);
				DiscardToken();
				continue;
			}
			Debug.FailedAssert("Can not parse the text: " + LookAheadFirst, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\TextProcessor\\MbTextParser.cs", "ConsumeFunction", 482);
			return false;
		}
		DiscardToken(TokenType.CloseParenthesis);
		FunctionCall token = new FunctionCall(functionName, list);
		PushToken(token);
		return true;
	}

	private bool ConsumeMarkerOccuranceExpression()
	{
		if (LookAheadFirst.TokenType == TokenType.Identifier && LookAheadSecond.TokenType == TokenType.MarkerOccuranceIdentifier)
		{
			VariableExpression innerExpression = LookAheadFirst as VariableExpression;
			TextExpression lookAheadSecond = LookAheadSecond;
			DiscardToken();
			DiscardToken();
			MarkerOccuranceTextExpression token = new MarkerOccuranceTextExpression(lookAheadSecond.RawValue.Substring(2), innerExpression);
			PushToken(token);
			return true;
		}
		return false;
	}

	private bool ConsumeArrayAccessExpression()
	{
		if (LookAheadFirst.TokenType == TokenType.Identifier && LookAheadSecond.TokenType == TokenType.OpenBrackets)
		{
			TextExpression lookAheadFirst = LookAheadFirst;
			DiscardToken();
			DiscardToken(TokenType.OpenBrackets);
			while (DoExpressionRules())
			{
			}
			TokenType tokenType = LookAheadFirst.TokenType;
			if (IsArithmeticExpression(tokenType))
			{
				TextExpression lookAheadFirst2 = LookAheadFirst;
				DiscardToken();
				DiscardToken(TokenType.CloseBrackets);
				ArrayReference token = new ArrayReference(lookAheadFirst.RawValue, lookAheadFirst2);
				PushToken(token);
				return true;
			}
		}
		return false;
	}

	private bool ConsumeNegativeAritmeticExpression()
	{
		if (LookAheadFirst.TokenType == TokenType.Minus)
		{
			ConsumeAritmeticOperation();
			TokenType tokenType = LookAheadFirst.TokenType;
			if (IsArithmeticExpression(tokenType))
			{
				ArithmeticExpression token = new ArithmeticExpression(ArithmeticOperation.Subtract, new SimpleToken(TokenType.Number, "0"), LookAheadFirst);
				PushToken(token);
				return true;
			}
		}
		return false;
	}

	private bool ConsumeParenthesisExpression()
	{
		if (LookAheadFirst.TokenType == TokenType.OpenParenthesis)
		{
			DiscardToken(TokenType.OpenParenthesis);
			while (DoExpressionRules())
			{
			}
			TokenType tokenType = LookAheadFirst.TokenType;
			if (IsArithmeticExpression(tokenType))
			{
				ParanthesisExpression token = new ParanthesisExpression(LookAheadFirst);
				DiscardToken();
				DiscardToken(TokenType.CloseParenthesis);
				PushToken(token);
				return true;
			}
			DiscardToken(TokenType.CloseParenthesis);
			return true;
		}
		return false;
	}

	private bool IsArithmeticExpression(TokenType t)
	{
		if (t != TokenType.ArithmeticProduct && t != TokenType.ArithmeticSum && t != TokenType.Identifier && t != TokenType.QualifiedIdentifier && t != TokenType.MarkerOccuranceExpression && t != TokenType.ParameterWithMarkerOccurance && t != TokenType.ParameterWithMultipleMarkerOccurances && t != TokenType.StartsWith && t != TokenType.Number && t != TokenType.ParenthesisExpression && t != TokenType.ComparisonExpression && t != TokenType.FunctionCall && t != TokenType.FunctionParam && t != TokenType.ArrayAccess)
		{
			return t == TokenType.ParameterWithAttribute;
		}
		return true;
	}

	private bool ConsumeInnerAritmeticExpression()
	{
		TokenType tokenType = LookAheadFirst.TokenType;
		TokenType tokenType2 = LookAheadSecond.TokenType;
		_ = LookAheadThird.TokenType;
		if (IsArithmeticExpression(tokenType) && (tokenType2 == TokenType.Multiply || tokenType2 == TokenType.Divide))
		{
			TextExpression lookAheadFirst = LookAheadFirst;
			DiscardToken();
			ArithmeticOperation op = ConsumeAritmeticOperation();
			if (!IsArithmeticExpression(LookAheadFirst.TokenType))
			{
				while (DoExpressionRules())
				{
				}
			}
			TextExpression lookAheadFirst2 = LookAheadFirst;
			DiscardToken();
			ArithmeticExpression token = new ArithmeticExpression(op, lookAheadFirst, lookAheadFirst2);
			PushToken(token);
			return true;
		}
		return false;
	}

	private bool ConsumeOuterAritmeticExpression()
	{
		TokenType tokenType = LookAheadFirst.TokenType;
		TokenType tokenType2 = LookAheadSecond.TokenType;
		if (IsArithmeticExpression(tokenType) && (tokenType2 == TokenType.Plus || tokenType2 == TokenType.Minus))
		{
			TextExpression lookAheadFirst = LookAheadFirst;
			DiscardToken();
			ArithmeticOperation op = ConsumeAritmeticOperation();
			while (DoExpressionRules())
			{
			}
			if (IsArithmeticExpression(LookAheadFirst.TokenType))
			{
				TextExpression lookAheadFirst2 = LookAheadFirst;
				DiscardToken();
				ArithmeticExpression token = new ArithmeticExpression(op, lookAheadFirst, lookAheadFirst2);
				PushToken(token);
				return true;
			}
			Debug.FailedAssert("Can not parse the text: " + LookAheadFirst, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\TextProcessor\\MbTextParser.cs", "ConsumeOuterAritmeticExpression", 656);
		}
		return false;
	}

	private ArithmeticOperation ConsumeAritmeticOperation()
	{
		int result = ((LookAheadFirst.TokenType != TokenType.Plus) ? ((LookAheadFirst.TokenType == TokenType.Minus) ? 1 : ((LookAheadFirst.TokenType == TokenType.Multiply) ? 2 : ((LookAheadFirst.TokenType == TokenType.Divide) ? 3 : 0))) : 0);
		DiscardToken();
		return (ArithmeticOperation)result;
	}

	private bool ConsumeComparisonExpression()
	{
		TokenType tokenType = LookAheadFirst.TokenType;
		TokenType tokenType2 = LookAheadSecond.TokenType;
		if (IsArithmeticExpression(tokenType) && IsComparisonOperator(tokenType2))
		{
			TextExpression lookAheadFirst = LookAheadFirst;
			DiscardToken();
			ComparisonOperation comparisonOp = GetComparisonOp(tokenType2);
			DiscardToken();
			while (DoExpressionRules())
			{
			}
			if (!IsArithmeticExpression(LookAheadFirst.TokenType))
			{
				Debug.FailedAssert("Can not parse the text: " + LookAheadFirst, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\TextProcessor\\MbTextParser.cs", "ConsumeComparisonExpression", 700);
				return false;
			}
			TextExpression lookAheadFirst2 = LookAheadFirst;
			DiscardToken();
			ComparisonExpression token = new ComparisonExpression(comparisonOp, lookAheadFirst, lookAheadFirst2);
			PushToken(token);
			return true;
		}
		return false;
	}

	private bool IsComparisonOperator(TokenType tokenType)
	{
		if (tokenType != TokenType.Equals && tokenType != TokenType.NotEquals && tokenType != TokenType.GreaterThan && tokenType != TokenType.GreaterOrEqual && tokenType != TokenType.GreaterThan && tokenType != TokenType.LessOrEqual)
		{
			return tokenType == TokenType.LessThan;
		}
		return true;
	}

	private BooleanOperation GetBooleanOp(TokenType tokenType)
	{
		return tokenType switch
		{
			TokenType.Not => BooleanOperation.Not, 
			TokenType.And => BooleanOperation.And, 
			TokenType.Or => BooleanOperation.Or, 
			_ => BooleanOperation.And, 
		};
	}

	private ComparisonOperation GetComparisonOp(TokenType tokenType)
	{
		return tokenType switch
		{
			TokenType.GreaterOrEqual => ComparisonOperation.GreaterOrEqual, 
			TokenType.GreaterThan => ComparisonOperation.GreaterThan, 
			TokenType.NotEquals => ComparisonOperation.NotEquals, 
			TokenType.Equals => ComparisonOperation.Equals, 
			_ => tokenType switch
			{
				TokenType.LessThan => ComparisonOperation.LessThan, 
				TokenType.LessOrEqual => ComparisonOperation.LessOrEqual, 
				TokenType.GreaterThan => ComparisonOperation.GreaterThan, 
				_ => ComparisonOperation.Equals, 
			}, 
		};
	}

	private MBTextModel ParseInternal(List<MBTextToken> tokens)
	{
		LoadSequenceStack(tokens);
		UpdateLookAheads();
		_queryModel = new MBTextModel();
		Statements();
		DiscardToken(TokenType.SequenceTerminator);
		return _queryModel;
	}

	internal static MBTextModel Parse(List<MBTextToken> tokens)
	{
		if (_instance == null)
		{
			_instance = new MBTextParser();
		}
		else
		{
			Clear();
		}
		return _instance.ParseInternal(tokens);
	}
}
